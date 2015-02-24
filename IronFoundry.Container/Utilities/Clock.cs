using System.Threading;

namespace IronFoundry.Container.Internal
{
    internal class Clock
    {
        public virtual void Sleep(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }
    }
}
