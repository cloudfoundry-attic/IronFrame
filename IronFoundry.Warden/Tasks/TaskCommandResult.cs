using IronFoundry.Warden.Jobs;

namespace IronFoundry.Warden.Tasks
{
    public class TaskCommandResult : IJobResult
    {
        private readonly int exitCode;
        private readonly string stdout;
        private readonly string stderr;

        public TaskCommandResult(int exitCode, string stdout, string stderr)
        {
            this.exitCode = exitCode;
            this.stdout = stdout;
            this.stderr = stderr;
        }

        public int ExitCode
        {
            get { return exitCode; }
        }

        public string Stdout
        {
            get { return stdout; }
        }

        public string Stderr
        {
            get { return stderr; }
        }
    }
}
