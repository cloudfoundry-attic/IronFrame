

namespace IronFoundry.Warden.Utilities
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Text;

    public class EnvironmentBlock
    {
        private readonly Dictionary<string,string> _environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static EnvironmentBlock GenerateDefault()
        {
            var defaultEnvBlock = CreateDefaultEnvBlock();
            var environment = new EnvironmentBlock();
            environment.Merge(defaultEnvBlock);

            return environment;
        }

        public static EnvironmentBlock Create(IDictionary dictionary)
        {
            Dictionary<string, string> typedDictionary = new Dictionary<string, string>();

            foreach (var key in dictionary.Keys)
            {
                typedDictionary[key.ToString()] = dictionary[key].ToString();
            }

            var envBlock = new EnvironmentBlock().Merge(typedDictionary);

            return envBlock;
        }


        /// <summary>
        /// Upserts the specified environment variables into the existing ones.
        /// </summary>
        public EnvironmentBlock Merge(IDictionary<string, string> environmentVariables)
        {
            foreach (var kv in environmentVariables)
            {
                _environment[kv.Key] = kv.Value;
            }

            return this;
        }

        public EnvironmentBlock Merge(EnvironmentBlock envBlock)
        {
            return this.Merge(envBlock._environment);
        }


        public IDictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>(_environment);
        }


        private static IDictionary<string, string> CreateDefaultEnvBlock()
        {
            IntPtr unmanagedEnv;
            IntPtr hToken = IntPtr.Zero;
            if (!CreateEnvironmentBlock(out unmanagedEnv, hToken, false))
            {
                int lastError = Marshal.GetLastWin32Error();
                throw new System.ComponentModel.Win32Exception(lastError, "Error calling CreateEnvironmentBlock: " + lastError);
            }

            Dictionary<string, string> envBlock = new Dictionary<string, string>();
            try
            {
                var stringList = SplitDoubleNullTerminatedUniStrings(unmanagedEnv);

                foreach (var str in stringList)
                {
                    int equalsIndex = str.IndexOf("=", StringComparison.Ordinal);
                    string key = str.Substring(0, equalsIndex);
                    string value = str.Substring(equalsIndex + 1);

                    envBlock[key] = value;
                }
            }
            finally
            {
                DestroyEnvironmentBlock(unmanagedEnv);
            }

            return envBlock;
        }

        /// <summary>
        /// Split a double null terminated list of unicode strings and return the list.
        /// </summary>
        /// <remarks>
        /// Expects an unmanaged unicode string similar to   abc\0def\0lastString\0\0.
        /// </remarks>
        private static IList<string> SplitDoubleNullTerminatedUniStrings(IntPtr ptrStrings)
        {
            List<string> strings = new List<string>();

            IntPtr current = ptrStrings;
            string buffer = null;

            do
            {
                buffer = Marshal.PtrToStringUni(current);
                
                current += Encoding.Unicode.GetByteCount(buffer) + Encoding.Unicode.GetByteCount("\0") /* Null uni char */;

                if (buffer.Length > 0)
                {
                    strings.Add(buffer);
                }
            } while (buffer.Length > 0);

            return strings;
        }

        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/bb762270(v=vs.85).aspx
        /// </summary>
        [DllImport("userenv.dll", SetLastError = true)]
        private static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/bb762274%28v=vs.85%29.aspx
        /// </summary>
        [DllImport("userenv.dll", SetLastError = true)]
        private static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);
    }
}
