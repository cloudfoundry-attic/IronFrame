using System;
using System.Threading.Tasks;
using Xunit;

public static class AssertHelper
{
    public static async Task CompletesWithinTimeoutAsync(int milliseconds, Task task)
    {
        var completedTask = await Task.WhenAny(task, Task.Delay(milliseconds));

        if (!Object.ReferenceEquals(task, completedTask))
            throw new TimeoutException(String.Format("The task did not complete within the timeout period of {0}ms", milliseconds));
    }

    public static Task CompletesWithinTimeoutAsync(int milliseconds, Func<Task> callback)
    {
        var task = callback();
        return CompletesWithinTimeoutAsync(milliseconds, task);
    }

    public static void DeepEqual<T>(T expected, T actual)
    {
        if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Xunit.Sdk.AssertActualExpectedException(expected, actual, "DeepEqual failed.");
    }

    public static async Task DoesNotCompleteWithinTimeoutAsync(int milliseconds, Task task)
    {
        var timeoutTask = Task.Delay(milliseconds);
        var completedTask = await Task.WhenAny(task, timeoutTask);

        if (!Object.ReferenceEquals(timeoutTask, completedTask))
            throw new Exception(String.Format("The task completed before the timeout period of {0}ms", milliseconds));

        Assert.Equal(false, task.IsCompleted);
    }

    public static Task DoesNotCompleteWithinTimeoutAsync(int milliseconds, Func<Task> callback)
    {
        var task = callback();
        return DoesNotCompleteWithinTimeoutAsync(milliseconds, task);
    }
}
