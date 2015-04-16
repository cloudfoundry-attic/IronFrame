using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using IronFoundry.Container.Utilities;

internal static class IFTestHelper
{
    const string ExeName = "IFTestHelper.exe";

    public const int SUCCEEDED = 0;
    public const int FAILED = 1;
    public const int FATAL = -1;

    public static Process Execute(string command, params object[] args)
    {
        return Execute(command, (IEnumerable<object>)args);
    }

    public static Process Execute(string command, IEnumerable<object> args)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(command);
        foreach (var arg in args)
        {
            sb.Append(" ").Append(arg);
        }

        var startInfo = new ProcessStartInfo
        {
            Arguments = sb.ToString(),
            FileName = ExeName,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            CreateNoWindow = false,
            WorkingDirectory = Directory.GetCurrentDirectory(),
            UseShellExecute = false,
        };

        return Process.Start(startInfo);
    }

    public static Process ExecuteInJob(JobObject jobObject, string command, params object[] args)
    {
        var process = ExecuteWithWait(command, args);
        jobObject.AssignProcessToJob(process);
        ContinueAndWait(process);
        return process;
    }

    public static Process ExecuteWithWait(string command, params object[] args)
    {
        return Execute(command, args.ToList().Concat(new[] { "--wait" }));
    }

    public static int ContinueAndWait(Process process, bool throwOnError = true, int? timeout = null)
    {
        process.StandardInput.WriteLine();
        process.WaitForExit(timeout ?? Int32.MaxValue);

        var error = process.StandardError.ReadToEnd();
        if (process.ExitCode == FATAL || !String.IsNullOrWhiteSpace(error))
            throw new Exception("The test helper failed:\n" + error);

        return process.ExitCode;
    }

    public static bool Succeeded(Process process)
    {
        return process.ExitCode == SUCCEEDED;
    }

    public static bool Failed(Process process)
    {
        return process.ExitCode == FAILED;
    }
}
