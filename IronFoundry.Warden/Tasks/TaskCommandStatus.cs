using System;
using IronFoundry.Warden.Jobs;

namespace IronFoundry.Warden.Tasks
{
    public class TaskCommandStatus : IJobStatus
    {
        private readonly int? exitStatus;
        private readonly JobDataSource dataSource;
        private readonly string data;

        public TaskCommandStatus(int? exitStatus, string stdout, string stderr)
        {
            this.exitStatus = exitStatus;

            if (!stdout.IsNullOrEmpty())
            {
                this.dataSource = JobDataSource.stdout;
                this.data = stdout;
            }
            else if (!stderr.IsNullOrEmpty())
            {
                this.dataSource = JobDataSource.stderr;
                this.data = stderr;
            }
            else
            {
                this.dataSource = JobDataSource.stdout;
                this.data = String.Empty; // TODO empty or null?
            }
        }

        public int? ExitStatus
        {
            get { return exitStatus; }
        }

        public JobDataSource DataSource
        {
            get { return dataSource; }
        }

        public string Data
        {
            get { return data; }
        }
    }
}
