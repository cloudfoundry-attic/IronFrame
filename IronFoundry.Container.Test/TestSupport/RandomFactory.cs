using System;

namespace IronFoundry.Container
{
    public static class RandomFactory
    {
        static Random global = new Random();

        /// <summary>
        /// Create a new random number generataor in a paralllel-safe way.
        /// </summary>
        /// <remarks>
        /// If you have multiple threads in the same process creating a Random number generator,
        /// they can end up with the same seed if they are run at the same time.  This implementation
        /// prevents that by keeping a process wide random number generator that is used to seed
        /// new Random instances.
        /// </remarks>
        public static Random Create()
        {
            lock (global)
            {
                return new Random(global.Next());
            }
        }
    }
}
