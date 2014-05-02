using System;
using System.Threading.Tasks;

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
}
