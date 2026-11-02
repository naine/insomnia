using System;
using Avalonia;
using Avalonia.Controls;

namespace Insomnia
{
    static class Extensions
    {
        extension(TrayIcon icon)
        {
            public EventHandler ClickedHandler
            {
                set => icon.Clicked += value;
            }
        }

        extension(NativeMenuItem item)
        {
            public EventHandler ClickHandler
            {
                set => item.Click += value;
            }
        }

        extension(AvaloniaObject obj)
        {
            public EventHandler<AvaloniaPropertyChangedEventArgs> PropertyChangedHandler
            {
                set => obj.PropertyChanged += value;
            }
        }
    }
}
