using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Tasks
{
    public interface ICommandRunner
    {
        Task<TaskCommandResult> RunCommandAsync(bool shouldImpersonate, string command, string[] arguments);
    }
}
