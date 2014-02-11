using System.Runtime.InteropServices;

namespace IronFoundry.Warden.PInvoke
{
    internal partial class NativeMethods
    {
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool RevertToSelf();
    }
}
