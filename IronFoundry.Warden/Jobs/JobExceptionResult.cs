namespace IronFoundry.Warden.Jobs
{
    using System;

    public class JobExceptionResult : IJobResult
    {
        private readonly string stderr;

        public JobExceptionResult(Exception ex)
        {
            this.stderr = ex.Message;
        }

        public int ExitCode
        {
            get { return 1; }
        }

        public string Stdout
        {
            get { return null; }
        }

        public string Stderr
        {
            get { return stderr; }
        }
    }
}
