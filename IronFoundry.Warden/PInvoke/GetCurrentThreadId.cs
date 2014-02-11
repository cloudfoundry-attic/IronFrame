using System.Runtime.InteropServices;

namespace IronFoundry.Warden.PInvoke
{
    internal partial class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int GetCurrentThreadId();
    }
}
