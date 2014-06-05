namespace IronFoundry.Warden.Configuration
{
    using System.Configuration;

    public class WardenSection : ConfigurationSection
    {
        public const string SectionName = "warden-server";

        private const string ContainerBasePathPropName = "container-basepath";
        private const string TcpPortPropName = "tcp-port";
        private const string DeleteContainerDirectoriesPropName = "delete-container-directories";
        private const string WardenUsersGroupPropName = "warden-users-group";

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

        [ConfigurationProperty(DeleteContainerDirectoriesPropName, DefaultValue = true, IsRequired = false)]
        public bool DeleteContainerDirectories
        {
            get
            {
                return (bool)this[DeleteContainerDirectoriesPropName];
            }
            set
            {
                this[DeleteContainerDirectoriesPropName] = value;
            }
        }

        [ConfigurationProperty(WardenUsersGroupPropName, DefaultValue = "WardenUsers", IsRequired = false)]
        public string WardenUsersGroup
        {
            get
            {
                return (string)this[WardenUsersGroupPropName];
            }
            set
            {
                this[WardenUsersGroupPropName] = value;
            }
        }
    }
}
