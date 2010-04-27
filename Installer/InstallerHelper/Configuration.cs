using System;
using System.Collections.Generic;

using System.Text;
using System.IO;
using System.Diagnostics;

namespace DIDE.Installer
{
    public class Configuration
    {
        private const string ERROR_PREFIX = "[ERROR] ";
        private const string RELATIVE_COMPILER_PATH = @"\windows\bin\dmd.exe";
        private static string[] POSSIBLE_LOCATIONS = new string[]{ @"\d\dmd", @"\d\dmd2", @"\dmd2", @"\dmd", @"\dmd", @"%PF%\dmd", @"%PF%\d\dmd", @"%PF%\d\dmd2" };

        /*public static List<string> FindLocalDMDPath(int version)
        {
            string programFiles = Environment.GetEnvironmentVariable("PROGRAMFILES");
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in (drives))
            {
                if (drive.DriveType == DriveType.Fixed ||
                    drive.DriveType == DriveType.Removable)
                {
                    foreach (string possibleLocation in POSSIBLE_LOCATIONS)
                    {
                        string path = possibleLocation.Replace("%PF%", programFiles) + RELATIVE_COMPILER_PATH;
                        if (path[1] != ':') path = drive.Name.TrimEnd('\\') + path;
                        if (File.Exists(path))
                        {
                            FileInfo fi = new FileInfo(path);
                            string s = GetVersion(fi);
                            System.Diagnostics.Debug.WriteLine(path);
                            System.Diagnostics.Debug.WriteLine(s);
                        }
                    }
                }
            }
            return "";
        }*/

        public static List<CompilerInstallInfo> FindLocalDMDPath(int version)
        {
            List<CompilerInstallInfo> compilers = new List<CompilerInstallInfo>();
            string programFiles = Environment.GetEnvironmentVariable("PROGRAMFILES");
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in (drives)) 
            {
                if (drive.DriveType == DriveType.Fixed || 
                    drive.DriveType == DriveType.Removable) 
                {
                    foreach (string possibleLocation in POSSIBLE_LOCATIONS) 
                    {
                        string path = possibleLocation.Replace("%PF%", programFiles) + RELATIVE_COMPILER_PATH;
                        if (path[1] != ':') path = drive.Name.TrimEnd('\\') + path;
                        if (File.Exists(path))
                        {
                            FileInfo fi = new FileInfo(path);
                            string s = GetVersion(fi);
                            System.Diagnostics.Debug.WriteLine(path);
                            System.Diagnostics.Debug.WriteLine(s);
                        }
                    }
                }
            }

            return compilers;
        }

        public static string GetVersion(FileInfo fi)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = fi.FullName;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            Process process = Process.Start(startInfo);
            string s = process.StandardOutput.ReadLine();
            process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return s;
        }

        public class CompilerInstallInfo
        {
            private FileInfo executablePath;
            private Version versionInfo;
            private string compilerString;

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

            public FileInfo ExecutablePath
            {
                get { return executablePath; }
                set { executablePath = value; }
            }
        }
    }
}
