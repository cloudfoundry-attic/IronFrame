namespace IronFrame
{
    public interface IContainerDirectory
    {
        string RootPath { get; }
        string UserPath { get; }
        string MapBinPath(string containerPath);
        string MapPrivatePath(string containerPath);
        string MapUserPath(string containerPath);
        void Destroy();
    }
}