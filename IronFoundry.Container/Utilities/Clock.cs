using System.Threading;

namespace IronFoundry.Container.Utilities
{
    internal class Clock
    {
        public virtual void Sleep(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }
    }
}
