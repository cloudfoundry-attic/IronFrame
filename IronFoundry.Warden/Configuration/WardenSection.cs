using System.Configuration;

namespace IronFoundry.Warden.Configuration
{
    public class WardenSection : ConfigurationSection
    {
        public const string SectionName = "warden-server";

        private const string ContainerBasePathPropName = "container-basepath";
        private const string TcpPortPropName = "tcp-port";

        [ConfigurationProperty(ContainerBasePathPropName, DefaultValue = "C:\\IronFoundry\\warden\\containers", IsRequired = false)]
        public string ContainerBasePath
        {
            get
            {
                return (string)this[ContainerBasePathPropName];
            }
            set
            {
                this[ContainerBasePathPropName] = value;
            }
        }

        [ConfigurationProperty(TcpPortPropName, DefaultValue = ((ushort)4444), IsRequired = false)]
        public ushort TcpPort
        {
            get
            {
                return (ushort)this[TcpPortPropName];
            }
            set
            {
                this[TcpPortPropName] = value;
            }
        }
    }
}
