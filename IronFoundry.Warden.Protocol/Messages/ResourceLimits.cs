namespace IronFoundry.Warden.Protocol
{
    using System;

    public partial class ResourceLimits
    {
        public long JobMemoryLimit
        {
            get
            {
                ulong tmp = Math.Max(this.Memlock, this.Data);
                tmp = Math.Max(tmp, this.Rss);
                return (long)tmp; // TODO overflow? unchecked?
            }
        }
    }
}
