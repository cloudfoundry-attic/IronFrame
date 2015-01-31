using System.Collections.Generic;

namespace IronFoundry.Container.Internal
{
    public interface IContainerPropertyService
    {
        void SetProperty(IContainer container, string name, string value);
        string GetProperty(IContainer container, string name);
        Dictionary<string, string> GetProperties(IContainer container);
        void RemoveProperty(IContainer container, string name);
        void SetProperties(IContainer container, Dictionary<string, string> properties);
    }
}
