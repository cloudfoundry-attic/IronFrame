

namespace IronFoundry.Warden.Utilities
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;
    using EnvSpecialFolder = System.Environment.SpecialFolder;

    public static class EnvName
    {
        public const string AllUsersProfile = "ALLUSERSPROFILE";
        public const string AppData = "APPDATA";
        public const string CommonProgramFiles = "CommonProgramFiles";
        public const string CommonProgramW6432 = "CommonProgramW6432";
        public const string CommonProgramFilesX86 = "CommonProgramFiles(x86)";
        public const string ComputerName = "COMPUTERNAME";
        public const string HomeDrive = "HOMEDRIVE";
        public const string HomePath = "HOMEPATH";
        public const string LocalAppData = "LOCALAPPDATA";
        public const string LogOnServer = "LOGONSERVER";
        public const string ProgramData = "ProgramData";
        public const string ProgramFiles = "ProgramFiles";
        public const string ProgramW6432 = "ProframW6432";
        public const string ProgramFilesX86 = "ProgramFiles(x86)";
        public const string Public = "PUBLIC";
        public const string SystemDrive = "SystemDrive";
        public const string SystemRoot = "SystemRoot";
        public const string Temp = "TEMP";
        public const string Tmp = "TMP";
        public const string UserDomain = "USERDOMAIN";
        public const string UserDomainRoamingProfile = "USERDOMAIN_ROAMINGPROFILE";
        public const string UserProfile = "USERPROFILE";
    }

    public class EnvironmentBlock
    {
        private readonly Dictionary<string,string> _environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static EnvironmentBlock GenerateDefault(string userHomePath)
        {
            if (userHomePath.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("userHomePath");
            }

            string homePath, homeDrive;
            SplitPathFromDrive(userHomePath, out homeDrive, out homePath);

            // Get the system drive
            string systemDrive, systemRoot;
            SplitPathFromDrive(Environment.GetFolderPath(EnvSpecialFolder.Windows), out systemDrive, out systemRoot);
            string windir = Environment.GetFolderPath(EnvSpecialFolder.Windows);

            var defaults = new Dictionary<string, string>();

            defaults[EnvName.ComputerName] = Environment.MachineName;
            defaults[EnvName.CommonProgramFiles] = Environment.GetFolderPath(EnvSpecialFolder.CommonProgramFiles);
            defaults[EnvName.CommonProgramFilesX86] = Environment.GetFolderPath(EnvSpecialFolder.CommonProgramFilesX86);
            defaults[EnvName.CommonProgramW6432] = Environment.GetFolderPath(EnvSpecialFolder.CommonProgramFiles);
            defaults[EnvName.HomePath] = homePath;
            defaults[EnvName.HomeDrive] = homeDrive;
            defaults[EnvName.ProgramData] = Environment.GetFolderPath(EnvSpecialFolder.CommonApplicationData);
            defaults[EnvName.ProgramFiles] = Environment.GetFolderPath(EnvSpecialFolder.ProgramFiles);
            defaults[EnvName.ProgramW6432] = Environment.GetFolderPath(EnvSpecialFolder.ProgramFiles);
            defaults[EnvName.ProgramFilesX86] = Environment.GetFolderPath(EnvSpecialFolder.ProgramFilesX86);
            defaults[EnvName.SystemDrive] = systemDrive;
            defaults[EnvName.SystemRoot] = windir;
            defaults[EnvName.Temp] = Path.Combine(windir, "TEMP");
            defaults[EnvName.Tmp] = Path.Combine(windir, "TEMP");
            defaults[EnvName.UserProfile] = homeDrive + @"\Users\Default";

            // Inherit values from this process
            defaults.SetIfNotNull(EnvName.AllUsersProfile, Environment.GetEnvironmentVariable(EnvName.AllUsersProfile));
            defaults.SetIfNotNull(EnvName.UserDomainRoamingProfile, Environment.GetEnvironmentVariable(EnvName.UserDomainRoamingProfile));
            defaults.SetIfNotNull(EnvName.LogOnServer, Environment.GetEnvironmentVariable(EnvName.LogOnServer));
            defaults.SetIfNotNull(EnvName.Public, Environment.GetEnvironmentVariable(EnvName.Public));
            defaults.SetIfNotNull(EnvName.SystemRoot, Environment.GetEnvironmentVariable(EnvName.SystemRoot));
            defaults.SetIfNotNull(EnvName.SystemDrive, Environment.GetEnvironmentVariable(EnvName.SystemDrive));
            defaults.SetIfNotNull(EnvName.UserDomain, Environment.GetEnvironmentVariable(EnvName.UserDomain));

            var environment = new EnvironmentBlock();
            environment.Merge(defaults);

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

        private static void SplitPathFromDrive(string fullPath, out string drive, out string path)
        {
            drive = Path.GetPathRoot(fullPath);
            if (!drive.IsNullOrWhiteSpace() && drive.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                drive = drive.Substring(0, drive.Length - 1);
            }

            path = fullPath;
            if (Path.IsPathRooted(path))
            {
                path = path.Substring(drive.Length);
            }
        }

        public IDictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>(_environment);
        }
    }
}
