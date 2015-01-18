namespace IronFoundry.Container.Win32
{
    using System.Runtime.InteropServices;

    public partial class NativeMethods
    {
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool RevertToSelf();
    }
}
