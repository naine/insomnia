// Copyright (c) 2020 Nathan Williams. All rights reserved.
// Licensed under the MIT license. See LICENCE file in the project root
// for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

[module: SkipLocalsInit]

namespace Insomnia
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using var form = new MainForm();
            Application.Run(form);
        }
    }
}
