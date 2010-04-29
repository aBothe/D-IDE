using System;
using System.Collections.Generic;
using System.Text;

using System.IO;

namespace DIDE.Installer
{
    public class InstallerHelper
    {
        public static string GetLatestDMD1Url()
        {
            int ver;
            return DigitalMars.GetLatestDMDInfo(1, out ver);
        }

        public static string GetLatestDMD2Url()
        {
            int ver;
            return DigitalMars.GetLatestDMDInfo(2, out ver);
        }

        public static int GetLatestDMD1Version()
        {
            int ver;
            DigitalMars.GetLatestDMDInfo(1, out ver);
            return ver;
        }

        public static int GetLatestDMD2Version()
        {
            int ver;
            DigitalMars.GetLatestDMDInfo(2, out ver);
            return ver;
        }

        public static int GetLocalDMD1Version()
        {
            return (LocalCompiler.DMD1Info != null) ? LocalCompiler.DMD1Info.VersionInfo.Minor : -1;
        }

        public static int GetLocalDMD2Version()
        {
            return (LocalCompiler.DMD2Info != null) ? LocalCompiler.DMD2Info.VersionInfo.Minor : -1;
        }

        public static string GetLocalDMD1Path()
        {
            return (LocalCompiler.DMD1Info != null) ? LocalCompiler.DMD1Info.ExecutableFile.Directory.FullName : string.Empty;
        }

        public static string GetLocalDMD2Path()
        {
            return (LocalCompiler.DMD2Info != null) ? LocalCompiler.DMD2Info.ExecutableFile.Directory.FullName : string.Empty;
        }

        public static bool IsValidDMDInstallForVersion(int majorVersion, string path)
        {
            bool isValid = false;
            CompilerInstallInfo installInfo = LocalCompiler.AddPath(path);

            if (installInfo != null)
            {
                isValid = (installInfo.VersionInfo.Major == majorVersion);
            }

            return isValid;
        }

        public static void Refresh()
        {
            LocalCompiler.Refresh();
        }

        public static void CreateConfigurationFile(string filePath)
        {
            Dictionary<string, string[]> dict = new Dictionary<string, string[]>();
            if (LocalCompiler.DMD1Info != null)
            {
                dict.Add("//settings/dmd[@version='1']/binpath", new string[] { LocalCompiler.DMD1Info.ExecutableFile.Directory.FullName });
                dict.Add("//settings/dmd[@version='1']/imports/dir", LocalCompiler.DMD1Info.LibraryPaths);
            }
            if (LocalCompiler.DMD2Info != null)
            {
                dict.Add("//settings/dmd[@version='2']/binpath", new string[] { LocalCompiler.DMD2Info.ExecutableFile.Directory.FullName });
                dict.Add("//settings/dmd[@version='2']/imports/dir", LocalCompiler.DMD2Info.LibraryPaths);
            }

            Configuration.CreateConfigurationFile(filePath, dict);
        }
    }
}
