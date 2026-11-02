// Copyright (c) 2020 Nathan Williams. All rights reserved.
// Licensed under the MIT license. See LICENCE file in the project root
// for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Microsoft.Win32;

namespace Insomnia
{
    public sealed partial class InsomniaApp : Application
    {
        private static readonly string configPath
            = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Insomnia.cfg");

        // TODO: make icons
        //private WindowIcon? offIcon;
        //private WindowIcon? onIcon;
        private DispatcherTimer? moveTimer;
        private TrayIcon? trayIcon;
        private NativeMenuItem? enableMenuItem;
        private NativeMenuItem? aggressiveMenuItem;

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            }
            base.OnFrameworkInitializationCompleted();
        }

        public override void Initialize()
        {
            RequestedThemeVariant = ThemeVariant.Default;
            Styles.Add(new FluentTheme());
            moveTimer = new(TimeSpan.FromSeconds(60), DispatcherPriority.Background, OnMoveTimerTick);
            TrayIcon.SetIcons(this, [trayIcon = new()
            {
                ToolTipText = "Insomnia",
                Menu = [
                    enableMenuItem = new NativeMenuItem("Enabled")
                    {
                        ToggleType = MenuItemToggleType.CheckBox,
                        IsChecked = true,
                        PropertyChangedHandler = OnEnableMenuItemPropertyChanged,
                    },
                    aggressiveMenuItem = new NativeMenuItem("Aggressive Mode")
                    {
                        ToggleType = MenuItemToggleType.CheckBox,
                    },
                    new NativeMenuItemSeparator(),
                    new NativeMenuItem("Exit")
                    {
                        ClickHandler = OnExitMenuItemClick,
                    },
                ],
                ClickedHandler = OnTrayIconClick,
            }]);

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
                        if (bool.TryParse(value, out var aggressive))
                        {
                            aggressiveMenuItem.IsChecked = aggressive;
                        }
                        break;
                    }
                }
            }
            SystemEvents.SessionSwitch += OnSessionSwitch;
            moveTimer.IsEnabled = true;
        }

        private async void OnExitMenuItemClick(object? sender, EventArgs e)
        {
            if (aggressiveMenuItem is not null)
            {
                string[] cfg = [
                    $"aggressive={aggressiveMenuItem.IsChecked}",
                ];
                try
                {
                    await File.WriteAllLinesAsync(configPath, cfg);
                }
                catch (Exception) { }
            }
            (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
        }

        private void OnTrayIconClick(object? sender, EventArgs e)
        {
            var item = enableMenuItem;
            item?.IsChecked = !item.IsChecked;
        }

        private void OnEnableMenuItemPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (enableMenuItem is not null && trayIcon is not null && moveTimer is not null)
            {
                moveTimer.IsEnabled = enableMenuItem.IsChecked;
                //trayIcon.Icon = enableMenuItem.IsChecked ? onIcon : offIcon;
            }
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
            if (aggressiveMenuItem is var item and not null && item.IsChecked)
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
