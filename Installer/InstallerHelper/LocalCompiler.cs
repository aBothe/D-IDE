using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Win32;      

namespace DIDE.Installer
{
    public class LocalCompiler
    {
        private const string BASE_REG_KEY = @"SOFTWARE\D-IDE";
        private const string DMD1_PATH_REG_KEY = @"Dmd1xBinPath";
        private const string DMD2_PATH_REG_KEY = @"Dmd2xBinPath";
        private const string ERROR_PREFIX = "[ERROR] ";
        private const string RELATIVE_COMPILER_PATH = @"\windows\bin";
        private const string COMPILER_EXE = @"\dmd.exe";
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
            if (localDMDInstallations != null) localDMDInstallations.Clear();
            if (DataFile.Exists) DataFile.Delete();
        }
        private static FileInfo fi = null;
        private static FileInfo DataFile
        {
            get
            {
                if (fi == null)
                {
                    DateTime now = DateTime.Now;
                    
                    DirectoryInfo d = new DirectoryInfo(Path.GetTempPath());
                    fi = new FileInfo(d.FullName + "\\LocalDmdInstall." + now.Year + "." + now.Month + "." + now.Day + ".txt");
                }
                else fi.Refresh();

                return fi;
            }
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

        public static CompilerInstallInfo AddPath(string path)
        {
            CompilerInstallInfo cinf = null;
            if (!string.IsNullOrEmpty(path))
            {
                FileInfo fi = new FileInfo(path);
                if (fi.Exists) path = fi.Directory.FullName;
                if (path.IndexOf(':') == 1) path = path.Substring(2);
                if (path.EndsWith(".exe")) path = path.Substring(0, path.LastIndexOf('\\'));
                path = path.TrimEnd('\\');
                if (!paths.Contains(path)) paths.Add(path.TrimEnd('\\'));

                cinf = GetLocalDMDInstallation(path);
                if (cinf != null)
                {
                    localDMDInstallations = null;
                    GetLocalDMDInstallations();
                }
            }
            return cinf;
        }

        public static List<CompilerInstallInfo> LocalDMDInstallations
        {
            get
            {
                GetLocalDMDInstallations();
                return localDMDInstallations;
            }
        }

        public static void Preload()
        {
            GetLocalDMDInstallations();
        }

        private static void GetLocalDMDInstallations()
        {
            if (localDMDInstallations == null)
            {
                paths = new List<string>(POSSIBLE_LOCATIONS);
                localDMDInstallations = new List<CompilerInstallInfo>();

                if (DataFile.Exists)
                {
                    string[] lines = File.ReadAllLines(DataFile.FullName);
                    CompilerInstallInfo cii;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        cii = new CompilerInstallInfo();
                        if (cii.FromString(lines[i]))
                        {
                            if ((cii.VersionInfo.Major == 1) && (latest1x == null || latest1x.VersionInfo < cii.VersionInfo)) latest1x = cii; 
                            else if ((cii.VersionInfo.Major == 2) && (latest2x == null || latest2x.VersionInfo < cii.VersionInfo)) latest2x = cii; 

                            localDMDInstallations.Add(cii);
                        }
                    }
                }
                else
                {
                    Version v1 = null, v2 = null;

                    string reg1 = ReadRegKey(DMD1_PATH_REG_KEY),
                        reg2 = ReadRegKey(DMD2_PATH_REG_KEY);

                    if (reg1 != null) paths.Add(reg1);
                    if (reg2 != null) paths.Add(reg2);

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
                                string path = possibleLocation + RELATIVE_COMPILER_PATH + COMPILER_EXE;
                                if (path[1] != ':') path = drive.Name.TrimEnd('\\') + path;

                                if (possibleLocation.IndexOf("%ENV%") >= 0)
                                {
                                    foreach (string dir in dirs)
                                    {
                                        string envPath = path.Replace("%ENV%", dir);
                                        CompilerInstallInfo inf = GetLocalDMDInstallation(envPath, ref v1, ref v2);
                                        if (inf != null) localDMDInstallations.Add(inf);
                                    }
                                }
                                else
                                {
                                    GetLocalDMDInstallation(path, ref v1, ref v2);
                                    CompilerInstallInfo inf = GetLocalDMDInstallation(path, ref v1, ref v2);
                                    if (inf != null) localDMDInstallations.Add(inf);
                                }
                            }
                        }
                    }


                    StringBuilder sb = new StringBuilder();
                    foreach (CompilerInstallInfo cii in localDMDInstallations) sb.Append(cii.ToString()).Append("\r\n");
                    File.WriteAllText(DataFile.FullName, sb.ToString());
                    //File.WriteAllText(@"C:\Users\Justin\Desktop\LocalInit_" + DateTime.Now.Ticks + ".txt", DateTime.Now.ToLongDateString());
                }
            }
        }

        private static CompilerInstallInfo GetLocalDMDInstallation(string possibleLocation)
        {
            Version v1 = new Version(), v2 = new Version();
            string path = possibleLocation.TrimEnd('\\');
            if (!path.EndsWith(RELATIVE_COMPILER_PATH, StringComparison.CurrentCultureIgnoreCase))
                path += RELATIVE_COMPILER_PATH;
            path += COMPILER_EXE;

            return GetLocalDMDInstallation(path, ref v1, ref v2);
        }

        private static CompilerInstallInfo GetLocalDMDInstallation(string path, ref Version v1, ref Version v2)
        {
            CompilerInstallInfo cii = null;
            if (File.Exists(path))
            {
                cii = new CompilerInstallInfo(new FileInfo(path));

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
            return cii;
        }

        private static string ReadRegKey(string key)
        {
            RegistryKey baseKey = Registry.LocalMachine.CreateSubKey(BASE_REG_KEY);
            if (baseKey == null)  return null;
            else
            {
                try
                {
                    return (string)baseKey.GetValue(key);
                }
                catch (Exception e)
                {
                    return null;
                }
            }
        }
    }
}
