using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace DIDE.Installer
{
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