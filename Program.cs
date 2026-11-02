// Copyright (c) 2020 Nathan Williams. All rights reserved.
// Licensed under the MIT license. See LICENCE file in the project root
// for full license information.

using System;
using System.Runtime.CompilerServices;
using Avalonia;

[module: SkipLocalsInit]
[assembly: DisableRuntimeMarshalling]

namespace Insomnia
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
            => AppBuilder.Configure<InsomniaApp>()
            .UsePlatformDetect()
            .LogToTrace()
            .StartWithClassicDesktopLifetime(args);
    }
}
