using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using IronFoundry.Warden.Containers;
using NLog;

namespace IronFoundry.Warden.Utilities
{
    public class ProcessManager
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly ConcurrentDictionary<int, Process> processes = new ConcurrentDictionary<int, Process>();
        private readonly ContainerUser containerUser;

        private readonly Func<Process, bool> processMatchesUser;

        public ProcessManager(ContainerUser containerUser)
        {
            if (containerUser == null)
            {
                throw new ArgumentNullException("containerUser");
            }
            this.containerUser = containerUser;

            this.processMatchesUser = (process) =>
                {
                    string processUser = process.GetUserName();
                    return processUser == containerUser && !process.HasExited;
                };
        }

        public bool HasProcesses
        {
            get { return processes.Count > 0; }
        }

        public void AddProcess(Process process)
        {
            if (processes.TryAdd(process.Id, process))
            {
                process.Exited += process_Exited;
            }
            else
            {
                throw new InvalidOperationException(
                    String.Format("Process '{0}' already added to process manager for user '{1}'", process.Id, containerUser));
            }
        }

        public void RestoreProcesses()
        {
            var allProcesses = Process.GetProcesses();
            allProcesses.Foreach(log, (p) => processMatchesUser(p),
                (p) =>
                {
                    if (processes.TryAdd(p.Id, p))
                    {
                        log.Trace("Added process with PID '{0}' to container with user '{1}'", p.Id, containerUser);
                    }
                    else
                    {
                        log.Trace("Could NOT add process with PID '{0}' to container with user '{1}'", p.Id, containerUser);
                    }
                });
        }

        public void StopProcesses()
        {
            // TODO once job objects are working, we shouldn't need this.
            var processList = processes.Values.ToListOrNull();
            processList.Foreach(log, (p) => !p.HasExited, (p) => p.Kill());
            processList.Foreach(log, (p) => RemoveProcess(p.Id));

            var allProcesses = Process.GetProcesses();
            allProcesses.Foreach(log, (p) => processMatchesUser(p), (p) => p.Kill());
        }

        private void process_Exited(object sender, EventArgs e)
        {
            var process = (Process)sender;
            int pid = process.Id;
            process.Exited -= process_Exited;

            log.Trace("Process exited PID '{0}' exit code '{1}'", process.Id, process.ExitCode);

            RemoveProcess(pid);
        }

        private void RemoveProcess(int pid)
        {
            Process removed;
            if (processes.ContainsKey(pid) && !processes.TryRemove(pid, out removed))
            {
                log.Warn("Could not remove process '{0}' from collection!", pid);
            }
        }
    }
}
