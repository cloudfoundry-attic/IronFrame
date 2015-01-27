using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Container.Win32
{
    class SafeHGlobal : SafeBuffer
    {
        public SafeHGlobal(int cb)
            : base(true)
        {
            this.SetHandle(Marshal.AllocHGlobal(cb));
            this.Initialize((ulong)cb);
        }

        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(this.handle);
            return true;
        }
    }
}
