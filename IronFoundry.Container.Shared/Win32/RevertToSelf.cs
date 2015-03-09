using System.Runtime.InteropServices;

namespace IronFoundry.Container.Win32
{
    public partial class NativeMethods
    {
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool RevertToSelf();
    }
}
