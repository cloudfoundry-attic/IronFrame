using System;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace IronFoundry.Warden.Utilities
{
    [System.ComponentModel.DesignerCategory("Code")]
    public class BackgroundProcess : Process
    {
        public BackgroundProcess(string workingDirectory, string executable, string arguments, NetworkCredential credential = null)
            : base()
        {
            if (workingDirectory.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("workingDirectory");
            }

            if (!Directory.Exists(workingDirectory))
            {
                throw new ArgumentException("workingDirectory must exist.");
            }

            if (executable.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("executable");
            }

            StartInfo.FileName               = executable;
            StartInfo.Arguments              = arguments;
            StartInfo.LoadUserProfile        = false;
            StartInfo.CreateNoWindow         = true;
            StartInfo.UseShellExecute        = false;
            StartInfo.RedirectStandardInput  = true;
            StartInfo.RedirectStandardOutput = true;
            StartInfo.RedirectStandardError  = true;
            StartInfo.WorkingDirectory       = workingDirectory;

            if (credential != null)
            {
                StartInfo.UserName = credential.UserName;
                StartInfo.Password = credential.SecurePassword;
            }

            EnableRaisingEvents = true;
        }

        public void StartAndWait(bool asyncOutput, Action<Process> postStartAction = null)
        {
            Start();

            if (asyncOutput)
            {
                BeginErrorReadLine();
                BeginOutputReadLine();
            }

            StandardInput.WriteLine(Environment.NewLine);

            if (postStartAction != null)
            {
                postStartAction(this);
            }

            WaitForExit(); // TODO timeout?
        }
    }
}
