using System.IO;

// TODO: Keep one copy of this file - perhaps in a shared library for tests?

namespace IronFoundry.Container
{
    public class TestProcessIO : IProcessIO
    {
        public StringWriter Output = new StringWriter();
        public StringWriter Error = new StringWriter();

        public TextWriter StandardOutput
        {
            get { return Output; }
        }

        public TextWriter StandardError
        {
            get { return Error; }
        }

        public TextReader StandardInput { get; set; }
    }
}
