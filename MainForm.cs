// Copyright (c) 2020 Nathan Williams. All rights reserved.
// Licensed under the MIT license. See LICENCE file in the project root
// for full license information.

using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Insomnia
{
    public partial class MainForm : Form
    {
        private static readonly string configPath
            = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Insomnia.cfg");

        private readonly Icon offIcon;
        private readonly Icon onIcon;

        public MainForm()
        {
            InitializeComponent();
            Hide();
            enableMenuItem.Checked = true;

            // Form.Icon getter returns a default icon from the WinForms assembly
            // when no icon is set. Use this as a placeholder for the tray for now.
            // TODO make a more unique icon.
            notifyIcon.Icon = onIcon = InvertIcon(offIcon = Icon);

            string[]? cfg = null;
            try
            {
                cfg = File.ReadAllLines(configPath);
            }
            catch (Exception) { }

            if (cfg is not null)
            {
                foreach (string line in cfg)
                {
                    int eqPos = line.IndexOf('=');
                    if (eqPos < 0 || eqPos == line.Length - 1) continue;
                    var value = line.AsSpan(eqPos + 1);
                    if (value.Contains('=')) continue;

                    var key = line.AsSpan(0, eqPos);
                    if (key.SequenceEqual("aggressive"))
                    {
                        if (bool.TryParse(value, out bool aggressive))
                        {
                            aggressiveMenuItem.Checked = aggressive;
                        }
                    }
                }
            }
        }

        private static Icon InvertIcon(Icon icon)
        {
            using var src = icon.ToBitmap();
            int width = src.Width;
            int height = src.Height;
            using var dst = new Bitmap(width, height, src.PixelFormat);
            double midX = (width - 1) / 2.0;
            double midY = (height - 1) / 2.0;
            for (int x = 0; x < width; ++x)
            {
                double xDistSquared = (x - midX) / midX;
                xDistSquared *= xDistSquared;
                for (int y = 0; y < height; ++y)
                {
                    double yDist = (y - midY) / midY;
                    var pxl = src.GetPixel(x, y);
                    var hsv = RGB2HSV(pxl);
                    // Invert V so only the brightness changes and not the colour.
                    double invertedV = 1 - hsv.V;
                    double origA = pxl.A / 255.0;
                    // Add a white glow behind the icon.
                    double glowTrans = Math.Pow(Math.Min(1, Math.Sqrt(xDistSquared + (yDist * yDist))), 4);
                    int resultA = (int)Math.Round(255 * (1 - ((1 - origA) * glowTrans)));
                    double resultV = (glowTrans == 1 && pxl.A == 0) ? 1
                        : (1 - ((1 - invertedV) * origA / (1 + ((origA - 1) * glowTrans))));
                    var rgb = HSV2RGB(hsv.H, hsv.S, resultV);
                    dst.SetPixel(x, y, Color.FromArgb(resultA, rgb.R, rgb.G, rgb.B));
                }
            }
            return Icon.FromHandle(dst.GetHicon());

            static (double H, double S, double V) RGB2HSV(Color pxl)
            {
                if (pxl.R == pxl.G && pxl.G == pxl.B)
                {
                    // Special-cased to prevent division by zero.
                    return (0.0, 0.0, pxl.R / 255.0);
                }
                byte max, mid, min;
                int hueSextant;
                if (pxl.R >= pxl.G)
                {
                    if (pxl.G >= pxl.B)
                    {
                        max = pxl.R;
                        mid = pxl.G;
                        min = pxl.B;
                        hueSextant = 0;
                    }
                    else if (pxl.B >= pxl.R)
                    {
                        max = pxl.B;
                        mid = pxl.R;
                        min = pxl.G;
                        hueSextant = 4;
                    }
                    else
                    {
                        max = pxl.R;
                        mid = pxl.B;
                        min = pxl.G;
                        hueSextant = 5;
                    }
                }
                else
                {
                    if (pxl.R >= pxl.B)
                    {
                        max = pxl.G;
                        mid = pxl.R;
                        min = pxl.B;
                        hueSextant = 1;
                    }
                    else if (pxl.B >= pxl.G)
                    {
                        max = pxl.B;
                        mid = pxl.G;
                        min = pxl.R;
                        hueSextant = 3;
                    }
                    else
                    {
                        max = pxl.G;
                        mid = pxl.B;
                        min = pxl.R;
                        hueSextant = 2;
                    }
                }
                double value = max / 255.0;
                double saturation = 1 - (min / (double)max);
                double hueDistanceFromPrimary = 60 * (mid - min) / (double)(max - min);
                double hue;
                if ((uint)hueSextant % 2 == 0)
                {
                    hue = (hueSextant * 60) + hueDistanceFromPrimary;
                }
                else
                {
                    hue = ((hueSextant + 1) * 60) - hueDistanceFromPrimary;
                }
                return (hue, saturation, value);
            }

            static (byte R, byte G, byte B) HSV2RGB(double h, double s, double v)
            {
                double max = 255 * v;
                double min = max * (1 - s);
                return h switch
                {
                    < 60    => (Round(max), Round(MidRgbValue(max, min, h)), Round(min)),
                    < 120   => (Round(MidRgbValue(max, min, 120 - h)), Round(max), Round(min)),
                    < 180   => (Round(min), Round(max), Round(MidRgbValue(max, min, h - 120))),
                    < 240   => (Round(min), Round(MidRgbValue(max, min, 240 - h)), Round(max)),
                    < 300   => (Round(MidRgbValue(max, min, h - 240)), Round(min), Round(max)),
                    _       => (Round(max), Round(min), Round(MidRgbValue(max, min, 360 - h)))
                };
            }

            static double MidRgbValue(double max, double min, double hueDistanceFromPrimary)
                => min + ((max - min) * hueDistanceFromPrimary / 60);

            static byte Round(double value)
                => (byte)Math.Clamp((int)Math.Round(value), byte.MinValue, byte.MaxValue);
        }

        protected override void SetVisibleCore(bool value)
        {
            // After the form is constructed and loaded, Application.Run()
            // will explicitly make the form visible, so it is not sufficient
            // to call Hide() in the ctor or even in the form Load handler.
            // By overriding this, we intercept any attempt to show the form.
            base.SetVisibleCore(false);
        }

        private async void OnExitMenuItemClick(object? sender, EventArgs e)
        {
            string[] cfg = new[]
            {
                $"aggressive={aggressiveMenuItem.Checked}",
            };
            try
            {
                await File.WriteAllLinesAsync(configPath, cfg);
            }
            catch (Exception) { }
            Application.Exit();
        }

        private void OnNotifyIconDoubleClick(object? sender, MouseEventArgs e)
        {
            enableMenuItem.Checked = !enableMenuItem.Checked;
        }

        private void OnEnableMenuItemCheckedChanged(object? sender, EventArgs e)
        {
            notifyIcon.Icon = (moveTimer.Enabled = enableMenuItem.Checked) ? onIcon : offIcon;
        }

        private bool direction;

        private unsafe void OnMoveTimerTick(object? sender, EventArgs e)
        {
            INPUT input;
            input.type = 0; // INPUT_MOUSE
            input.mi = new()
            {
                dwFlags = 1, // MOUSEEVENTF_MOVE
            };
            if (aggressiveMenuItem.Checked)
            {
                input.mi.dx = direction ? 1 : -1;
                direction = !direction;
            }
            _ = SendInput(1, &input, sizeof(INPUT));
        }

        [DllImport("user32.dll", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static unsafe extern uint SendInput(uint nInputs, INPUT* pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }
    }
}
