namespace IronFoundry.Warden.Handlers
{
    public class ResponseData
    {
        private readonly int exitStatus;
        private readonly string message;

        public ResponseData(int exitStatus, string message)
        {
            this.exitStatus = exitStatus;
            this.message = message;
        }

        public int ExitStatus
        {
            get { return exitStatus; }
        }

        public string Message
        {
            get { return message; }
        }
    }
}
