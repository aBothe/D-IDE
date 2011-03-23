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

            public CompilerInstallInfo() {}

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
                            druntime = new DirectoryInfo(dir.FullName + @"\src\druntime\import"),
                            phobos = new DirectoryInfo(dir.FullName + @"\src\phobos");

                        if (druntime.Exists) dirs.Add(druntime.FullName);
                        if (phobos.Exists) dirs.Add(phobos.FullName);
                    }
                    return dirs.ToArray();
                }
            }

            /*public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(this.ExecutableFile.ToString());
                sb.AppendLine(this.CompilerString);
                sb.AppendLine(this.VersionInfo.ToString());
                return sb.ToString();
            }*/

            private void GetVersion()
            {
                try
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
                catch (Exception ex)
                {
                    DirectoryInfo
                        dir = executableFile.Directory.Parent.Parent,
                        druntime = new DirectoryInfo(dir.FullName + @"\src\druntime\import");
                    versionString = druntime.Exists ? "2.0" : "1.0";
                    versionInfo = new Version(versionString);
                }
            }

            public bool FromString(string s)
            {
                string[] items = s.Split('\t');
                if (items.Length == 3)
                {
                    ExecutableFile = new FileInfo(items[0]);
                    CompilerString = items[1];
                    VersionInfo = new Version(items[2]);
                    VersionString = items[2];
                    return true;
                }
                return false;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.ExecutableFile.ToString()).Append("\t")
                    .Append(this.CompilerString).Append("\t")
                    .Append(this.VersionString);
                return sb.ToString();
            }
        }
	}