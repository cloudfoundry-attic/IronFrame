using System;
using System.IO;

namespace IronFoundry.Container
{
    public interface IContainer : IDisposable
    {
        string Id { get; }
        string Handle { get; }
        IContainerDirectory Directory { get; }
        ContainerInfo GetInfo();
        void Stop(bool kill);
        int ReservePort(int requestedPort);
        IContainerProcess Run(ProcessSpec spec, IProcessIO io);
        void LimitMemory(ulong limitInBytes);
        ulong CurrentMemoryLimit();
        void SetProperty(string name, string value);
        string GetProperty(string name);
        void RemoveProperty(string name);
        void Destroy();

        //ContainerState State { get; }
        //void BindMounts(IEnumerable<BindMount> mounts);
        //void CreateTarFile(string sourcePath, string tarFilePath, bool compress);
        //void CopyFileIn(string sourceFilePath, string destinationFilePath);
        //void CopyFileOut(string sourceFilePath, string destinationFilePath);
        //void ExtractTarFile(string tarFilePath, string destinationPath, bool decompress);
    }

    public interface IProcessIO
    {
        TextWriter StandardOutput { get; }
        TextWriter StandardError { get; }
        TextReader StandardInput { get; }
    }
}