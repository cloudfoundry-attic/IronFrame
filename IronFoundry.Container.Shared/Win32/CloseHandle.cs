﻿using System;
using System.Runtime.InteropServices;

namespace IronFoundry.Container.Win32
{
    internal partial class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError=true)]
        public static extern bool CloseHandle(IntPtr handle);
    }
}
