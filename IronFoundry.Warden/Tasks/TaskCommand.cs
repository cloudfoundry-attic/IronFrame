using System;
using IronFoundry.Warden.Containers;

namespace IronFoundry.Warden.Tasks
{
    public abstract class TaskCommand
    {
        protected readonly Container container;
        protected readonly string[] arguments;

        public TaskCommand(Container container, string[] arguments)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }
            this.container = container;
            this.arguments = arguments;
        }

        public abstract TaskCommandResult Execute();
    }
}
