using System.Configuration;

namespace IronFoundry.Warden.Configuration
{
    public class WardenConfig
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
