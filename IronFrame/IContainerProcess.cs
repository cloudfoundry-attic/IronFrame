using System.Collections.Generic;

namespace IronFrame
{
    public interface IContainerProcess
    {
        int Id { get; }
        IReadOnlyDictionary<string, string> Environment { get; }
        void Kill();
        int WaitForExit();
        bool TryWaitForExit(int milliseconds, out int exitCode);
    }
}
