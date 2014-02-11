using System;

namespace IronFoundry.Warden.Tasks
{
    public class TaskCommandStatusEventArgs : EventArgs
    {
        private readonly TaskCommandStatus status;

        public TaskCommandStatusEventArgs(TaskCommandStatus status)
        {
            if (status == null)
            {
                throw new ArgumentNullException("status");
            }
            this.status = status;
        }

        public TaskCommandStatus Status
        {
            get { return status; }
        }
    }
}
