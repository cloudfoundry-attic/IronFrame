using System.Runtime.InteropServices;
using System.Text;

namespace IronFoundry.Warden.PInvoke
{
    internal partial class NativeMethods
    {
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool LookupAccountSid(
            string lpSystemName,
            [MarshalAs(UnmanagedType.LPArray)] byte[] Sid,
            StringBuilder lpName,
            ref uint cchName,
            StringBuilder referencedDomainName,
            ref uint cchReferencedDomainName,
            out SidNameUse peUse);
    }
}
