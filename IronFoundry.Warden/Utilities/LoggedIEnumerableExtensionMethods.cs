namespace IronFoundry.Warden.Utilities
{
    using System;
    using System.Collections.Generic;
    using NLog;

    public static class LoggedIEnumerableExtensionMethods
    {
        public static void Foreach<T>(this IEnumerable<T> collection, Logger log, Action<T> action)
        {
            collection.Foreach(log, (obj) => true, action);
        }

        public static void Foreach<T>(this IEnumerable<T> collection, Logger log, Func<T, bool> predicate, Action<T> predicateMatchesAction)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }

            if (predicateMatchesAction == null)
            {
                throw new ArgumentNullException("predicateMatchesAction");
            }

            if (!collection.IsNullOrEmpty())
            {
                foreach (T item in collection)
                {
                    try
                    {
                        if (predicate(item))
                        {
                            predicateMatchesAction(item);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.WarnException(ex);
                    }
                }
            }
        }
    }
}
