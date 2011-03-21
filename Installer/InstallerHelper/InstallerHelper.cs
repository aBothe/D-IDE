using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using System.IO;

namespace DIDE.Installer
{
    public class InstallerHelper
    {
        private static Thread t;
        private static string fileListLocation;

        public static void Initialize(String fileListLocation)
        {
            InstallerHelper.fileListLocation = fileListLocation;
            ThreadStart ts = new ThreadStart(Preload);
            t = new Thread(ts);
            t.Start();
        }

        public static string FixDmdInstallPath(string s)
        {
            try
            {
                DirectoryInfo d = new DirectoryInfo(s);
                if (d.Name.Equals("dmd", StringComparison.CurrentCultureIgnoreCase) ||
                    d.Name.Equals("dmd1", StringComparison.CurrentCultureIgnoreCase) ||
                    d.Name.Equals("dmd2", StringComparison.CurrentCultureIgnoreCase))
                    return d.Parent.FullName;
                else
                    return d.FullName;
            }
            catch
            {
                s = s.TrimEnd('\\');
                if (s.IndexOf("dmd") >= s.Length - 5)
                {
                    int i = s.LastIndexOf("\\");
                    if (i > 0) s = s.Substring(i);
                }
                return s;
            }
        }

        private static void Preload()
        {
            try
            {
                LocalCompiler.Refresh();
                DigitalMars.PreloadFromHtmlList(fileListLocation);
                DigitalMars.Preload();
                LocalCompiler.Preload();
            }
            catch (ThreadAbortException)
            {
                //do nothing now - just return
            }
            catch (Exception)
            {
                //do nothing now - just return
            }
        }

        public static bool IsThreadActive
        {
            get
            {
                return (t != null) && (t.ThreadState == ThreadState.Running);
            }
        }

        private static void CheckThread()
        {
            if (t != null)
            {
                if (t.ThreadState == ThreadState.Running) t.Join(30);
                t = null;
            }
        }

        public static string GetLatestDMD1Url()
        {
            CheckThread();
            int ver;
            return DigitalMars.GetLatestDMDInfo(1, out ver);
        }

        public static string GetLatestDMD2Url()
        {
            CheckThread();
            int ver;
            return DigitalMars.GetLatestDMDInfo(2, out ver);
        }

        public static int GetLatestDMD1Version()
        {
            CheckThread();
            int ver;
            DigitalMars.GetLatestDMDInfo(1, out ver);
            return ver;
        }

        public static int GetLatestDMD2Version()
        {
            CheckThread();
            int ver;
            DigitalMars.GetLatestDMDInfo(2, out ver);
            return ver;
        }

        public static int GetLocalDMD1Version()
        {
            CheckThread();
            return (LocalCompiler.DMD1Info != null) ? LocalCompiler.DMD1Info.VersionInfo.Minor : -1;
        }

        public static int GetLocalDMD2Version()
        {
            CheckThread();
            return (LocalCompiler.DMD2Info != null) ? LocalCompiler.DMD2Info.VersionInfo.Minor : -1;
        }

        public static string GetLocalDMD1Path()
        {
            CheckThread();
            return (LocalCompiler.DMD1Info != null) ? 
                LocalCompiler.DMD1Info.ExecutableFile.Directory.Parent.Parent.FullName : @"C:\d\dmd1";
        }

        public static string GetLocalDMD2Path()
        {
            CheckThread();
            return (LocalCompiler.DMD2Info != null && LocalCompiler.DMD2Info.ExecutableFile.Exists) ? 
                LocalCompiler.DMD2Info.ExecutableFile.Directory.Parent.Parent.FullName : @"C:\d\dmd2";
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

        public static string IsConfigurationValid(string filePath)
        {
            string isValid = Boolean.FalseString;
            try
            {
                if (File.Exists(filePath))
                {
                    var list = new List<string>();

                    list.Add("//dsettings/dmd[@version='1']/basedirectory");
                    list.Add("//dsettings/dmd[@version='2']/basedirectory");

                    isValid = Configuration.IsValid(filePath, list).ToString();
                }
            }
            catch {}

            return isValid;
        }

        public static string CreateConfigurationFile(string filePath)
        {
            string error = "";
            try
            {
                var dict = new Dictionary<string, string[]>();
                if (LocalCompiler.DMD1Info != null)
                {
                    dict.Add("//dsettings/dmd[@version='1']/basedirectory", new string[] { LocalCompiler.DMD1Info.ExecutableFile.Directory.FullName });
                    //dict.Add("//dsettings/dmd[@version='1']/imports/dir", LocalCompiler.DMD1Info.LibraryPaths);
                }
                if (LocalCompiler.DMD2Info != null)
                {
                    dict.Add("//dsettings/dmd[@version='2']/basedirectory", new string[] { LocalCompiler.DMD2Info.ExecutableFile.Directory.FullName });
                    //dict.Add("//dsettings/dmd[@version='2']/imports/dir", LocalCompiler.DMD2Info.LibraryPaths);
                }

                Configuration.CreateConfigurationFile(filePath, dict);
            }
            catch (Exception ex)
            {
                error = ex.Message + Environment.NewLine + ex.StackTrace;
            }
            return error;
        }
    }
}
