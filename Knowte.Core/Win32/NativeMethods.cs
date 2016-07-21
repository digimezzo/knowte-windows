using System;
using System.Runtime.InteropServices;

namespace Knowte.Core.Win32
{
    public static class NativeMethods
    {
        [DllImport("user32.dll")]
        static internal extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
    }
}
