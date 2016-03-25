using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using IronFrame.Win32;
using Microsoft.Win32.SafeHandles;

namespace IronFrame.Utilities
{
    internal class ImpersonationProcessRunner : IProcessRunner
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IProcess Run(ProcessRunSpec runSpec)
        {
            var process = new ImpersonationProcess(runSpec);

            if (runSpec.ExitHandler != null)
                process.Exited += runSpec.ExitHandler;

            process.Start();
            return process;
        }

        public void StopAll(bool kill)
        {
            throw new NotImplementedException();
        }

        public IProcess FindProcessById(int id)
        {
            throw new NotImplementedException();
        }
    }

    internal class ImpersonationProcess : IProcess
    {
        private readonly ProcessRunSpec runSpec;
        private NativeMethods.ProcessInformation processInfo;
        private readonly ManualResetEvent exited = new ManualResetEvent(false);

        public ImpersonationProcess(ProcessRunSpec runSpec)
        {
            this.runSpec = runSpec;
        }

        ~ImpersonationProcess()
        {
            if (!NativeMethods.CloseHandle(Handle))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public int ExitCode { get; private set; }
        public IntPtr Handle { get; private set; }
        public int Id { get; private set; }
        public IReadOnlyDictionary<string, string> Environment { get; private set; }
        public long PrivateMemoryBytes { get; private set; }
        public event EventHandler Exited;
        public event EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived { add{} remove{} }
        public event EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived { add{} remove{} }
        public TextReader StandardOutput { get; private set; }
        public TextReader StandardError { get; private set; }
        public TextWriter StandardInput { get; private set; }
        public void Kill()
        {
            throw new NotImplementedException();
        }

        public void WaitForExit()
        {
            WaitForExit(-1);
        }

        public bool WaitForExit(int milliseconds)
        {
            return exited.WaitOne(milliseconds);
        }

        public bool Start()
        {
            processInfo = new NativeMethods.ProcessInformation();
            var startInfo = new NativeMethods.StartupInfo();
            var success = false;

            SafeFileHandle hToken, hReadOut, hWriteOut, hReadErr, hWriteErr, hReadIn, hWriteIn;

            var securityAttributes = new NativeMethods.SecurityAttributes();
            securityAttributes.bInheritHandle = true;

            success = NativeMethods.CreatePipe(out hReadOut, out hWriteOut, securityAttributes, 0);
            if (!success)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            success = NativeMethods.CreatePipe(out hReadErr, out hWriteErr, securityAttributes, 0);
            if (!success)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            success = NativeMethods.CreatePipe(out hReadIn, out hWriteIn, securityAttributes, 0);
            if (!success)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            success = NativeMethods.SetHandleInformation(hReadOut, NativeMethods.Constants.HANDLE_FLAG_INHERIT, 0);
            if (!success)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            // Logon user
            success = NativeMethods.LogonUser(
                runSpec.Credentials.UserName,
                runSpec.Credentials.Domain,
                runSpec.Credentials.Password,
                NativeMethods.LogonType.LOGON32_LOGON_BATCH,
                NativeMethods.LogonProvider.LOGON32_PROVIDER_DEFAULT,
                out hToken
            );
            if (!success)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            IntPtr unmanagedEnv;
            if (!NativeMethods.CreateEnvironmentBlock(out unmanagedEnv, hToken.DangerousGetHandle(), false))
            {
                int lastError = Marshal.GetLastWin32Error();
                throw new Win32Exception(lastError, "Error calling CreateEnvironmentBlock: " + lastError);
            }

            // Create process
            startInfo.cb = Marshal.SizeOf(startInfo);
            startInfo.dwFlags = NativeMethods.Constants.STARTF_USESTDHANDLES;
            startInfo.hStdOutput = hWriteOut;
            startInfo.hStdError = hWriteErr;
            startInfo.hStdInput = hReadIn;

            success = NativeMethods.CreateProcessAsUser(
                hToken,
                null,
                CommandLine(),
                IntPtr.Zero,
                IntPtr.Zero,
                true,
                NativeMethods.CreateProcessFlags.CREATE_UNICODE_ENVIRONMENT,
                unmanagedEnv,
                null,
                ref startInfo,
                out processInfo
            );

            if (!success)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            NativeMethods.DestroyEnvironmentBlock(unmanagedEnv);

            Handle = processInfo.hProcess;

            startInfo.hStdOutput.Close();
            startInfo.hStdError.Close();
            startInfo.hStdInput.Close();
            StandardOutput = new StreamReader(new FileStream(hReadOut, FileAccess.Read), Console.OutputEncoding);
            StandardError = new StreamReader(new FileStream(hReadErr, FileAccess.Read), Console.OutputEncoding);
            StandardInput = new StreamWriter(new FileStream(hWriteIn, FileAccess.Write), Console.InputEncoding)
            {
                AutoFlush = true
            };

            WaitForExitAsync();

            return success;
        }

        private void WaitForExitAsync()
        {
            var thr = new Thread(() =>
            {
                NativeMethods.WaitForSingleObject(processInfo.hProcess, NativeMethods.Constants.INFINITE);
                if (Exited != null) Exited(this, EventArgs.Empty);
                exited.Set();
            });
            thr.Start();
        }

        private string CommandLine()
        {
            var commandLine = new StringBuilder();

            var exePath = runSpec.ExecutablePath.Trim();
            if (!exePath.StartsWith("\""))
                commandLine.Append("\"");
            commandLine.Append(exePath);
            if (!exePath.EndsWith("\""))
                commandLine.Append("\"");

            if (runSpec.Arguments.Length != 0)
            {
                commandLine.Append(" ");
                commandLine.Append(String.Join(" ", runSpec.Arguments));
            }

            return commandLine.ToString();
        }
    }
}
