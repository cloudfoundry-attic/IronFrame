using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Test.TestSupport
{
    internal static class ExceptionAssert
    {
        public async static Task<Exception> RecordThrowsAsync(Func<Task> testCode)
        {
            try
            {
                await testCode();
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }
    }
}
