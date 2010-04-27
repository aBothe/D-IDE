using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace DIDE.Installer
{
    public class LocalCompiler
    {
        private const string ERROR_PREFIX = "[ERROR] ";
        private const string RELATIVE_COMPILER_PATH = @"\windows\bin\dmd.exe";
        private static string[] POSSIBLE_LOCATIONS = new string[] { 
            @"\d\dmd", @"\d\dmd2", 
            @"\dmd2", @"\dmd",
            @"%ENV%\dmd", @"%ENV%\dmd2", 
            @"%ENV%\d\dmd", @"%ENV%\d\dmd2", 
            @"%ENV%\D-IDE\dmd", @"%ENV%\D-IDE\dmd2" };

        private static CompilerInstallInfo latest1x, latest2x;
        private static List<CompilerInstallInfo> localDMDInstallations;
        private static List<string> paths = new List<string>(POSSIBLE_LOCATIONS);

        public static void Refresh()
        {
            localDMDInstallations.Clear();
        }

        public static CompilerInstallInfo DMD1Info
        {
            get
            {
                GetLocalDMDInstallations();
                return latest1x;
            }
        }

        public static CompilerInstallInfo DMD2Info
        {
            get
            {
                GetLocalDMDInstallations();
                return latest2x;
            }
        }

        public static string InstallPathDMD1
        {
            get
            {
                GetLocalDMDInstallations();
                return (latest1x == null) ? null : latest1x.ExecutableFile.FullName;
            }
        }

        public static string InstallPathDMD2
        {
            get
            {
                GetLocalDMDInstallations();
                return (latest2x == null) ? null : latest2x.ExecutableFile.FullName;
            }
        }

        public static string InstallPathDMD1Version
        {
            get
            {
                GetLocalDMDInstallations();
                return (latest1x == null) ? null : latest1x.VersionString;
            }
        }

        public static string InstallPathDMD2Version
        {
            get
            {
                GetLocalDMDInstallations();
                return (latest1x == null) ? null : latest2x.VersionString;
            }
        }

        public static void AddPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            FileInfo fi = new FileInfo(path);
            if (fi.Exists) path = fi.Directory.FullName;
            if (path.IndexOf(':') == 1) path = path.Substring(2);
            if (path.EndsWith(".exe")) path = path.Substring(0, path.LastIndexOf('\\'));
            path = path.TrimEnd('\\');
            if (!paths.Contains(path)) paths.Add(path.TrimEnd('\\'));

            localDMDInstallations = null;
            GetLocalDMDInstallations();
        }

        public static List<CompilerInstallInfo> LocalDMDInstallations
        {
            get
            {
                GetLocalDMDInstallations();
                return localDMDInstallations;
            }
        }

        private static void GetLocalDMDInstallations()
        {
            if (localDMDInstallations == null) 
            {
                Version v1 = null, v2 = null;
                paths = new List<string>(POSSIBLE_LOCATIONS);
                localDMDInstallations = new List<CompilerInstallInfo>();

                string[] dirs = new string[] { 
                    Environment.GetEnvironmentVariable("PROGRAMFILES"),
                    Environment.GetEnvironmentVariable("PROGRAMFILES(X86)"),
                    Environment.GetEnvironmentVariable("APPDATA"),
                    Environment.GetEnvironmentVariable("USERPROFILE") };

                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach (DriveInfo drive in drives)
                {
                    if (drive.DriveType == DriveType.Fixed ||
                        drive.DriveType == DriveType.Removable)
                    {
                        foreach (string possibleLocation in paths)
                        {
                            string path = possibleLocation + RELATIVE_COMPILER_PATH;
                            if (path[1] != ':') path = drive.Name.TrimEnd('\\') + path;

                            if (possibleLocation.IndexOf("%ENV%") >= 0)
                            {
                                foreach (string dir in dirs)
                                {
                                    string envPath = path.Replace("%ENV%", dir);
                                    GetLocalDMDInstallation(envPath, ref v1, ref v2);
                                }
                            }
                            else
                            {
                                GetLocalDMDInstallation(path, ref v1, ref v2);
                            }
                        }
                    }
                }
            }
        }

        private static void GetLocalDMDInstallation(string path, ref Version v1, ref Version v2)
        {
            if (File.Exists(path))
            {
                CompilerInstallInfo cii = new CompilerInstallInfo(new FileInfo(path));
                localDMDInstallations.Add(cii);

                if ((cii.VersionInfo.Major == 1) &&
                    ((v1 == null) || (v1 > cii.VersionInfo)))
                {
                    v1 = cii.VersionInfo;
                    latest1x = cii;
                }

                if ((cii.VersionInfo.Major == 2) &&
                    ((v2 == null) || (v2 > cii.VersionInfo)))
                {
                    v2 = cii.VersionInfo;
                    latest2x = cii;
                }
            }
        }

        public class CompilerInstallInfo
        {
            private FileInfo executableFile;
            private Version versionInfo = new Version();
            private string compilerString = string.Empty;
            private string versionString = string.Empty;

            public CompilerInstallInfo(FileInfo executableFile)
            {
                this.executableFile = executableFile;
                GetVersion();
            }

            public string CompilerString
            {
                get { return compilerString; }
                set { compilerString = value; }
            }

            public Version VersionInfo
            {
                get { return versionInfo; }
                set { versionInfo = value; }
            }

            public string VersionString
            {
                get { return versionString; }
                set { versionString = value; }
            }

            public FileInfo ExecutableFile
            {
                get { return executableFile; }
                set { executableFile = value; }
            }

            public string[] LibraryPaths
            {
                get
                {
                    List<string> dirs = new List<string>();
                    if (executableFile.Exists)
                    {
                        DirectoryInfo
                            dir = executableFile.Directory.Parent.Parent,
                            druntime = new DirectoryInfo(dir.FullName + @"\src\druntime"),
                            phobos = new DirectoryInfo(dir.FullName + @"\src\phobos");

                        if (druntime.Exists) dirs.Add(druntime.FullName);
                        if (phobos.Exists) dirs.Add(phobos.FullName);
                    }
                    return dirs.ToArray();
                }
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(this.ExecutableFile.ToString());
                sb.AppendLine(this.CompilerString);
                sb.AppendLine(this.VersionInfo.ToString());
                return sb.ToString();
            }

            private void GetVersion()
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = executableFile.FullName;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardOutput = true;
                using (Process process = Process.Start(startInfo))
                {
                    this.compilerString = process.StandardOutput.ReadLine();
                    process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    process.Close();

                    int idx = this.compilerString.LastIndexOf('v');
                    if (idx > 0)
                    {
                        versionString = this.CompilerString.Substring(idx + 1).Trim();
                        versionInfo = new Version(versionString);
                    }
                }
            }
        }
    }
}
