using System.Runtime.InteropServices;
using DiskQuotaTypeLibrary;

namespace IronFrame.Utilities
{
    internal interface IDiskQuotaManager
    {
        IContainerDiskQuota CreateDiskQuotaControl(IContainerDirectory dir, string sid);
    }

    internal class DiskQuotaManager : IDiskQuotaManager
    {
        public IContainerDiskQuota CreateDiskQuotaControl(IContainerDirectory dir, string sid)
        {
            var diskQuotaControl = new DiskQuotaControl();
            diskQuotaControl.UserNameResolution = UserNameResolutionConstants.dqResolveNone;
            diskQuotaControl.Initialize(dir.Volume, true);
            return new ContainerDiskQuota(diskQuotaControl, sid);
        }
    }

    public interface IContainerDiskQuota
    {
        ulong CurrentLimit();
        void SetQuotaLimit(ulong limit);
        ulong Usage();
        void DeleteQuota();
    }

    public class ContainerDiskQuota : IContainerDiskQuota
    {
        private readonly DiskQuotaControl _diskQuotaControl;
        private readonly string _sid;
        private readonly ulong _offset;

        public ContainerDiskQuota(DiskQuotaControl diskQuotaControl, string sid)
        {
            this._diskQuotaControl = diskQuotaControl;
            this._sid = sid;
            this._offset = Usage();
        }

        public ulong CurrentLimit()
        {
            var quotaLimit = _diskQuotaControl.FindUser(_sid).QuotaLimit;

            if (quotaLimit > 0)
            {
                return (ulong) quotaLimit - _offset;
            }

            return 0;
        }

        public void SetQuotaLimit(ulong limit)
        {
            _diskQuotaControl.FindUser(_sid).QuotaLimit = limit + _offset;
        }

        public ulong Usage()
        {
            try
            {
                return (ulong) (_diskQuotaControl.FindUser(_sid).QuotaUsed - _offset);
            }
            catch (COMException)
            {
                return 0;
            }
        }

        public void DeleteQuota()
        {
            var dskuser = _diskQuotaControl.FindUser(_sid);
            _diskQuotaControl.DeleteUser(dskuser);
        }
    }
}
