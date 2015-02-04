using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace IronFoundry.Container.Utilities
{
    public class ExtensionMethodsTests
    {
        public class FuncExtensionMethods : ExtensionMethodsTests
        {
            public class RetryUpToNTimes : FuncExtensionMethods
            {
                [Fact]
                public void WhenActionReturnsFalseContinuously_RetriesUpToN()
                {
                    int count = 0;

                    Func<bool> failingAction = () =>
                    {
                        count++;
                        return false;
                    };

                    failingAction.RetryUpToNTimes(10, 0);

                    Assert.Equal(10, count);
                }

                [Fact]
                public void WhenActionReturnsTrue_StopsRetrying()
                {
                    int count = 0;

                    Func<bool> trueAfterThree = () =>
                    {
                        count++;
                        return count == 3;
                    };

                    trueAfterThree.RetryUpToNTimes(10, 0);

                    Assert.Equal(3, count);
                }
            }
        }
    }
}
