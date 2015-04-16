namespace IronFoundry.Warden.Containers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    // BR: Move to IronFoundry.Container.Shared

    /// <summary>
    /// warden/lib/warden/container/base.rb
    /// </summary>
    public class ContainerHandle : IEquatable<ContainerHandle>
    {
        private static readonly string Alphabet = "abcdefghijklmnopqrstuvwyxz0123456789";
        private static Random random = new Random();
        private readonly string value;

        public ContainerHandle(string handle)
        {
            if (String.IsNullOrWhiteSpace(handle))
            {
                throw new ArgumentNullException("handle");
            }

            if (handle.Length < 8)
            {
                throw new ArgumentException("handle must be 8-15 characters long");
            }

            if (handle.Length > 15)
            {
                value = handle.Substring(0, 15);
            }
            else
            {
                value = handle;
            }
        }

        public ContainerHandle()
        {
            value = GenerateId();
        }

        public ContainerHandle(Random random)
        {
            ContainerHandle.random = random;
            value = GenerateId();
        }

        public static bool operator ==(ContainerHandle x, ContainerHandle y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(ContainerHandle x, ContainerHandle y)
        {
            return !(x == y);
        }

        public override string ToString()
        {
            return value;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ContainerHandle);
        }

        public bool Equals(ContainerHandle other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return GetHashCode() == other.GetHashCode();
        }

        public static implicit operator string(ContainerHandle handle)
        {
            return handle.value;
        }

        private static string GenerateId()
        {
            lock (random)
            {
                var builder = new StringBuilder();
                for (int i = 0; i < 11; i++)
                {
                    var character = Alphabet[random.Next(0, Alphabet.Length)];
                    builder.Append(character);
                }
                return builder.ToString();
            }
        }
    }
}
