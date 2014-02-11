using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronFoundry.Warden.Containers
{
    /// <summary>
    /// warden/lib/warden/container/base.rb
    /// </summary>
    public class ContainerHandle : IEquatable<ContainerHandle>
    {
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly char[] chars = new[] {
            '0','1','2','3','4','5','6','7','8','9',
            'a','b','c','d','e','f','g','h','i','j',
            'k','l','m','n','o','p','q','r','s','t',
            'u','v'
        };
        private static readonly int targetBase = chars.Length;

        private string value;

        public ContainerHandle(string handle)
        {
            if (handle.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("handle");
            }

            if (handle.Length < 8)
            {
                throw new ArgumentException("handle must be 8-15 characters long");
            }

            if (handle.Length > 15)
            {
                this.value = handle.Substring(0, 15);
            }
            else
            {
                this.value = handle;
            }
        }

        public ContainerHandle(long input)
            : this(GenerateID(input))
        {
        }

        public ContainerHandle()
            : this(SecondsSinceEpoch())
        {
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
            if (Object.ReferenceEquals(null, other))
            {
                return false;
            }

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            return this.GetHashCode() == other.GetHashCode();
        }

        public static implicit operator ContainerHandle(long input)
        {
            return new ContainerHandle(input);
        }

        public static implicit operator string(ContainerHandle handle)
        {
            return handle.value;
        }

        private static string GenerateID(long input)
        {
            var sb = new StringBuilder();
            for (ushort i = 0; i < 11; ++i)
            {
                var tmp = (input >> (55 - (i + 1) * 5)) & 31;
                sb.Append(ToBase32(tmp));
            }
            return sb.ToString();
        }

        private static char[] ToBase32(long value)
        {
            var result = new LinkedList<char>();
            do
            {
                result.AddFirst(chars[value % targetBase]);
                value /= targetBase;
            } 
            while (value > 0);
            return result.ToArray();
        }

        private static long SecondsSinceEpoch()
        {
            var sinceEpoch = DateTime.UtcNow.Subtract(epoch);
            return (long)Math.Ceiling(sinceEpoch.TotalMilliseconds * 1000);
        }
    }
}
