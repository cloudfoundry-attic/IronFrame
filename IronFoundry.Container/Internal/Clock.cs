using System.Threading;

namespace IronFoundry.Container.Internal
{
    public class Clock
    {
        public virtual void Sleep(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }
    }
}
