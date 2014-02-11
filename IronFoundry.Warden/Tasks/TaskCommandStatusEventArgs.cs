namespace IronFoundry.Warden.Tasks
{
    using System;

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
