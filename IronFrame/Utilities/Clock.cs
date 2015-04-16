using System.Threading;

namespace IronFrame.Utilities
{
    internal class Clock
    {
        public virtual void Sleep(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }
    }
}
