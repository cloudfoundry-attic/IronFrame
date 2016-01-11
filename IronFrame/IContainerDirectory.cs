namespace IronFrame
{
    public interface IContainerDirectory
    {
        string RootPath { get; }
        string UserPath { get; }
        string Volume { get; }
        string MapBinPath(string containerPath);
        string MapPrivatePath(string containerPath);
        string MapUserPath(string containerPath);
        void Destroy();
        void CreateBindMounts(BindMount[] bindMounts, IContainerUser containerUser);
        void CreateSubdirectories(IContainerUser containerUser);
    }
}