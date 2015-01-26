using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;

namespace IronFoundry.Container.Utilities
{
    public class JobObjectLimitsTests : IDisposable
    {
        const ulong DefaultMemoryLimit = 1024 * 1024 * 25; // 25MB

        JobObject jobObject;
        JobObjectLimits jobObjectLimits;

        public JobObjectLimitsTests()
        {
            jobObject = Substitute.For<JobObject>();

            jobObjectLimits = new JobObjectLimits(jobObject, TimeSpan.FromMilliseconds(10));
        }

        public void Dispose()
        {
            jobObject.Dispose();
            jobObjectLimits.Dispose();
        }

        public class SetLimits : JobObjectLimitsTests
        {
            [Fact]
            public void CanLimitMemory()
            {
                jobObjectLimits.LimitMemory(DefaultMemoryLimit);

                jobObject.Received(1, x => x.SetJobMemoryLimit(DefaultMemoryLimit));
            }
        }

        public class Notifications : JobObjectLimitsTests
        {
            TaskCompletionSource<int>[] tcs;
            
            public Notifications() : base()
            {
                jobObject.GetJobMemoryLimit().Returns(DefaultMemoryLimit);
                jobObject.GetPeakJobMemoryUsed().Returns(DefaultMemoryLimit);

                tcs = new[]
                {
                    new TaskCompletionSource<int>(),
                    new TaskCompletionSource<int>(),
                };
                int tcsIndex = 0;
                jobObjectLimits.MemoryLimitReached += (sender, e) =>
                {
                    tcs[tcsIndex].SetResult(tcsIndex);
                    tcsIndex++;
                };

                jobObjectLimits.LimitMemory(DefaultMemoryLimit);
            }

            [Fact]
            public async void FiresOneNotificationPerLimit()
            {
                await AssertHelper.CompletesWithinTimeoutAsync(1000, tcs[0].Task);
                Assert.Equal(0, tcs[0].Task.Result);
                Assert.False(tcs[1].Task.IsCompleted);

                await AssertHelper.DoesNotCompleteWithinTimeoutAsync(250, tcs[1].Task);
                Assert.False(tcs[1].Task.IsCompleted);
            }

            [Fact]
            public async void FiresSecondNotificationWhenLimitIncreases()
            {
                await AssertHelper.CompletesWithinTimeoutAsync(1000, tcs[0].Task);
                Assert.Equal(0, tcs[0].Task.Result);
                Assert.False(tcs[1].Task.IsCompleted);

                jobObjectLimits.LimitMemory(DefaultMemoryLimit * 2);

                jobObject.GetJobMemoryLimit().Returns(DefaultMemoryLimit * 2);
                jobObject.GetPeakJobMemoryUsed().Returns(DefaultMemoryLimit * 2);

                await AssertHelper.CompletesWithinTimeoutAsync(1000, tcs[1].Task);
                Assert.Equal(1, tcs[1].Task.Result);
            }

            [Fact]
            public async void FiresSecondNotificationWhenLimitDecreases()
            {
                await AssertHelper.CompletesWithinTimeoutAsync(1000, tcs[0].Task);
                Assert.Equal(0, tcs[0].Task.Result);
                Assert.False(tcs[1].Task.IsCompleted);

                jobObjectLimits.LimitMemory(DefaultMemoryLimit / 2);

                jobObject.GetJobMemoryLimit().Returns(DefaultMemoryLimit / 2);
                jobObject.GetPeakJobMemoryUsed().Returns(DefaultMemoryLimit / 2);

                await AssertHelper.CompletesWithinTimeoutAsync(1000, tcs[1].Task);
                Assert.Equal(1, tcs[1].Task.Result);
            }
        }
    }
}
