using DiskQuotaTypeLibrary;

namespace IronFrame.Utilities
{
    internal interface IDiskQuotaManager
    {
        DiskQuotaControl CreateDiskQuotaControl(IContainerDirectory dir);
    }

    internal class DiskQuotaManager : IDiskQuotaManager
    {
        public DiskQuotaControl CreateDiskQuotaControl(IContainerDirectory dir)
        {
            var diskQuotaControl = new DiskQuotaControl();
            diskQuotaControl.Initialize(dir.Volume, true);
            return diskQuotaControl;
        }
    }
}