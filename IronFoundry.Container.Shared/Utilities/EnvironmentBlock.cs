﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace IronFoundry.Container.Utilities
{
    internal class EnvironmentBlock
    {
        private readonly Dictionary<string,string> _environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static EnvironmentBlock CreateSystemDefault()
        {
            return CreateForUser(userToken: IntPtr.Zero);
        }

        public static EnvironmentBlock Create(IDictionary dictionary)
        {
            Dictionary<string, string> typedDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var key in dictionary.Keys)
            {
                typedDictionary[key.ToString()] = dictionary[key].ToString();
            }

            var envBlock = new EnvironmentBlock().Merge(typedDictionary);

            return envBlock;
        }

        public static EnvironmentBlock CreateForUser(IntPtr userToken)
        {
            var defaultEnvBlock = CreateEnvBlock(userToken);
            var environment = new EnvironmentBlock();
            environment.Merge(defaultEnvBlock);

            return environment;
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


        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>(_environment, _environment.Comparer);
        }


        private static IDictionary<string, string> CreateEnvBlock(IntPtr userToken)
        {
            IntPtr unmanagedEnv;
            IntPtr hToken = userToken;
            if (!CreateEnvironmentBlock(out unmanagedEnv, hToken, false))
            {
                int lastError = Marshal.GetLastWin32Error();
                throw new Win32Exception(lastError, "Error calling CreateEnvironmentBlock: " + lastError);
            }

            Dictionary<string, string> envBlock = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
