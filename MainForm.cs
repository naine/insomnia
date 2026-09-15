// Copyright (c) 2020 Nathan Williams. All rights reserved.
// Licensed under the MIT license. See LICENCE file in the project root
// for full license information.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

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
            offIcon = Icon ?? throw new Exception("Error: Missing WinForms default icon");
            notifyIcon.Icon = onIcon = InvertIcon(offIcon);

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
                    if (eqPos < 1 || eqPos == line.Length - 1) continue;
                    var value = line.AsSpan(eqPos + 1);
                    if (value.Contains('=')) continue;

                    switch (line.AsSpan(0, eqPos))
                    {
                    case "aggressive":
                        if (bool.TryParse(value, out bool aggressive))
                        {
                            aggressiveMenuItem.Checked = aggressive;
                        }
                        break;
                    }
                }
            }

            SystemEvents.SessionSwitch += OnSessionSwitch;
        }

        private static unsafe Icon InvertIcon(Icon icon)
        {
            const PixelFormat fmt = PixelFormat.Format32bppArgb;
            using var bmp = icon.ToBitmap();
            if (bmp.PixelFormat != fmt)
            {
                bmp.ConvertFormat(fmt);
            }
            int width = bmp.Width;
            int height = bmp.Height;
            var bmpd = bmp.LockBits(new(0, 0, width, height), ImageLockMode.ReadWrite, fmt);
            byte* ptr = (byte*)bmpd.Scan0;
            double midX = (width - 1) / 2.0;
            double midY = (height - 1) / 2.0;
            for (int y = 0; y < height; ++y)
            {
                double yDistSquared = (y - midY) / midY;
                yDistSquared *= yDistSquared;
                for (int x = 0; x < width; ++x)
                {
                    var pxl = new ArgbPixel(Unsafe.ReadUnaligned<uint>(ptr + (x * sizeof(uint))));
                    // Convert to HSV so we can invert only the brightness and not the colour.
                    var hsv = RGB2HSV(pxl.R, pxl.G, pxl.B);
                    double origA = pxl.A / 255.0;
                    // Add a white glow behind the icon.
                    double xDist = (x - midX) / midX;
                    double glowTrans = Math.Pow(Math.Min(1, Math.Sqrt((xDist * xDist) + yDistSquared)), 4);
                    byte resultA = (byte)Math.Round(255 * (1 - ((1 - origA) * glowTrans)));
                    double resultV = pxl.A == 0 ? 1
                        : (1 - (hsv.V * origA / (1 + ((origA - 1) * glowTrans))));
                    var rgb = HSV2RGB(hsv.H, hsv.S, resultV);
                    Unsafe.WriteUnaligned<uint>(ptr + (x * sizeof(uint)), new ArgbPixel(resultA, rgb.R, rgb.G, rgb.B));
                }
                ptr += bmpd.Stride;
            }
            bmp.UnlockBits(bmpd);
            return Icon.FromHandle(bmp.GetHicon());

            static (double H, double S, double V) RGB2HSV(uint r, uint g, uint b)
            {
                if (r == g && g == b)
                {
                    // Special-cased to prevent division by zero.
                    return (0.0, 0.0, r / 255.0);
                }
                uint max, mid, min, hueSextant;
                if (r >= g)
                {
                    if (g >= b)
                    {
                        (max, mid, min, hueSextant) = (r, g, b, 0);
                    }
                    else if (b >= r)
                    {
                        (max, mid, min, hueSextant) = (b, r, g, 4);
                    }
                    else
                    {
                        (max, mid, min, hueSextant) = (r, b, g, 5);
                    }
                }
                else
                {
                    if (r >= b)
                    {
                        (max, mid, min, hueSextant) = (g, r, b, 1);
                    }
                    else if (b >= g)
                    {
                        (max, mid, min, hueSextant) = (b, g, r, 3);
                    }
                    else
                    {
                        (max, mid, min, hueSextant) = (g, b, r, 2);
                    }
                }
                double value = max / 255.0;
                double saturation = 1 - (min / (double)max);
                double hueDistanceFromPrimary = 60 * (mid - min) / (double)(max - min);
                double hue = hueSextant % 2 == 0
                    ? (hueSextant * 60) + hueDistanceFromPrimary
                    : ((hueSextant + 1) * 60) - hueDistanceFromPrimary;
                return (hue, saturation, value);
            }

            static (byte R, byte G, byte B) HSV2RGB(double h, double s, double v)
            {
                double max = 255 * v;
                double min = max * (1 - s);
                return h switch
                {
                    < 60    => (Round(max), MidRgbValue(max, min, h), Round(min)),
                    < 120   => (MidRgbValue(max, min, 120 - h), Round(max), Round(min)),
                    < 180   => (Round(min), Round(max), MidRgbValue(max, min, h - 120)),
                    < 240   => (Round(min), MidRgbValue(max, min, 240 - h), Round(max)),
                    < 300   => (MidRgbValue(max, min, h - 240), Round(min), Round(max)),
                    _       => (Round(max), Round(min), MidRgbValue(max, min, 360 - h)),
                };
            }

            static byte MidRgbValue(double max, double min, double hueDistanceFromPrimary)
                => Round(min + ((max - min) * hueDistanceFromPrimary / 60));

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
            string[] cfg = [
                $"aggressive={aggressiveMenuItem.Checked}",
            ];
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
        private bool sessionActive = true;

        private unsafe void OnMoveTimerTick(object? sender, EventArgs e)
        {
            if (!sessionActive)
            {
                return;
            }
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

        private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
            case SessionSwitchReason.SessionLock:
                sessionActive = false;
                break;
            case SessionSwitchReason.SessionUnlock:
                sessionActive = true;
                break;
            }
        }

        [LibraryImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static unsafe partial uint SendInput(uint nInputs, INPUT* pInputs, int cbSize);

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
