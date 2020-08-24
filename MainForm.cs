// Copyright (c) 2020 Nathan Williams. All rights reserved.
// Licensed under the MIT license. See LICENCE file in the project root
// for full license information.

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Insomnia
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            Hide();
            enableMenuItem.Checked = true;

            // Form.Icon getter returns a default icon from the WinForms assembly
            // when no icon is set. Use this as a placeholder for the tray for now.
            // TODO make a more unique icon.
            notifyIcon.Icon = Icon;

            // TODO read config and restore aggressive setting
        }

        protected override void SetVisibleCore(bool value)
        {
            // After the form is constructed and loaded, Application.Run()
            // will explicitly make the form visible, so it is not sufficient
            // to call Hide() in the ctor or even in the form Load handler.
            // By overriding this, we intercept any attempt to show the form.
            base.SetVisibleCore(false);
        }

        private void OnExitMenuItemClick(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        private void OnAggressiveMenuItemCheckedChanged(object? sender, EventArgs e)
        {
            // TODO save to config somewhere
        }

        private void OnEnableMenuItemCheckedChanged(object? sender, EventArgs e)
        {
            moveTimer.Enabled = enableMenuItem.Checked;
        }

        private bool direction = false;

        private unsafe void OnMoveTimerTick(object? sender, EventArgs e)
        {
            var input = new INPUT
            {
                type = 0, // INPUT_MOUSE
                mi = new MOUSEINPUT
                {
                    dwFlags = 1, // MOUSEEVENTF_MOVE
                }
            };
            if (aggressiveMenuItem.Checked)
            {
                input.mi.dx = direction ? 1 : -1;
                direction = !direction;
            }
            SendInput(1, &input, sizeof(INPUT));
        }

#pragma warning disable IDE1006 // Naming Styles

        [DllImport("user32.dll", ExactSpelling = true)]
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
