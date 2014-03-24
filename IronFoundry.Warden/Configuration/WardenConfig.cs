namespace IronFoundry.Warden.Configuration
{
    using System.Configuration;

    public interface IWardenConfig
    {
        string ContainerBasePath { get; }
        ushort TcpPort { get; }
    }

    public class WardenConfig : IWardenConfig
    {
        private readonly WardenSection configSection;

        public WardenConfig()
        {
            this.configSection = (WardenSection)ConfigurationManager.GetSection(WardenSection.SectionName);
        }

        public string ContainerBasePath
        {
            get { return configSection.ContainerBasePath; }
        }

        public ushort TcpPort
        {
            get { return configSection.TcpPort; }
        }
    }
}
