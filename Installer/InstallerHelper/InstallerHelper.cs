using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DIDE.Installer
{
    public class InstallerHelper
    {

        public static string GetCurrentDMD1Version()
        {
            int ver;
            return DigitalMars.GetLatestDMDInfo(1, out ver);
        }

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
