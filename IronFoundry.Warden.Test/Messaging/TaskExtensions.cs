namespace IronFoundry.Warden.Test.Messaging
{
    using System.Threading.Tasks;

    public static class TaskExtensions
    {
        /// <summary>
        /// Used to silence warning CS4014 caused when an async call is not awaited or assigned to a variable.
        /// For example see MessagingClientTest.ThrowsWhenReceivingDuplicateRequest
        /// 
        /// This seems cleaner than ignoring it with pragma.
        /// </summary>
        public static void Forget(this Task task)
        { }
    }
}