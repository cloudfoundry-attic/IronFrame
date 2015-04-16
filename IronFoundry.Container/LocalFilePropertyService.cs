using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using IronFoundry.Container.Utilities;
using Newtonsoft.Json;

namespace IronFoundry.Container
{
    internal interface IContainerPropertyService
    {
        void SetProperty(IContainer container, string name, string value);
        string GetProperty(IContainer container, string name);
        Dictionary<string, string> GetProperties(IContainer container);
        void RemoveProperty(IContainer container, string name);
        void SetProperties(IContainer container, Dictionary<string, string> properties);
    }

    internal class LocalFilePropertyService : IContainerPropertyService
    {
        readonly Clock clock;
        readonly string fileName;
        readonly FileSystemManager fileSystem;

        public LocalFilePropertyService(FileSystemManager fileSystem, string fileName, Clock clock)
        {
            this.fileSystem = fileSystem;
            this.fileName = fileName;
            this.clock = clock;
        }

        public LocalFilePropertyService(FileSystemManager fileSystem, string fileName)
            : this(fileSystem, fileName, new Clock())
        {
        }

        Dictionary<string, string> Deserialize(Stream stream)
        {
            var streamReader = new StreamReader(stream, Encoding.UTF8);
            var jsonReader = new JsonTextReader(streamReader);

            var serializer = new JsonSerializer();

            return serializer.Deserialize<Dictionary<string, string>>(jsonReader)
                ?? new Dictionary<string, string>();            
        }

        public Dictionary<string, string> GetProperties(IContainer container)
        {
            using (var fs = OpenOrCreateFile(container.Directory, FileAccess.Read, FileShare.Read))
            {
                fs.Position = 0;
                return Deserialize(fs);
            }
        }

        public string GetProperty(IContainer container, string name)
        {
            var properties = GetProperties(container);

            string value;
            if (properties.TryGetValue(name, out value))
                return value;

            return null;
        }

        Stream OpenOrCreateFile(IContainerDirectory directory, FileAccess fileAccess, FileShare fileShare)
        {
            const int MaxAttempt = 10;
            const int SleepPerAttempt = 250;

            var path = directory.MapPrivatePath(fileName);
            int attempt = 1;
            while (true)
            {
                try
                {
                    return fileSystem.OpenFile(path, FileMode.OpenOrCreate, fileAccess, (fileShare | FileShare.Delete));
                }
                catch (UnauthorizedAccessException)
                {
                    if (attempt >= MaxAttempt)
                        throw;
                }

                attempt++;
                clock.Sleep(SleepPerAttempt);
            }    
        }

        public void RemoveProperty(IContainer container, string name)
        {
            UpdatePropertiesAtomically(
                container.Directory,
                properties => properties.Remove(name)
            );
        }

        void Serialize(Dictionary<string, string> properties, Stream stream)
        {
            var streamWriter = new StreamWriter(stream, new UTF8Encoding(false));
            var jsonWriter = new JsonTextWriter(streamWriter);

            var serializer = new JsonSerializer();
            serializer.Serialize(jsonWriter, properties);

            jsonWriter.Flush();
            streamWriter.Flush();
        }

        public void SetProperties(IContainer container, Dictionary<string, string> properties)
        {
            if (properties == null)
                properties = new Dictionary<string, string>();

            using (var fs = OpenOrCreateFile(container.Directory, FileAccess.Write, FileShare.None))
            {
                fs.Position = 0;
                Serialize(properties, fs);
                fs.SetLength(fs.Position);
            }
        }

        public void SetProperty(IContainer container, string name, string value)
        {
            UpdatePropertiesAtomically(
                container.Directory,
                properties => properties[name] = value
            );
        }

        void UpdatePropertiesAtomically(IContainerDirectory directory, Action<Dictionary<string, string>> updateAction)
        {
            using (var fs = OpenOrCreateFile(directory, FileAccess.ReadWrite, FileShare.None))
            {
                fs.Position = 0;
                var properties = Deserialize(fs);

                updateAction(properties);

                fs.Position = 0;
                Serialize(properties, fs);
                fs.SetLength(fs.Position);
            }
        }
    }
}
