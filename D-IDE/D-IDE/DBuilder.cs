using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Diagnostics;
using D_Parser;
using Microsoft.Win32;

namespace D_IDE
{
    /// <summary>
    /// Main class for building D projects
    /// </summary>
    public class DBuilder
    {
        /// <summary>
        /// Helper function to check if directory exists
        /// </summary>
        /// <param name="dir"></param>
        public static void CreateDirectoryRecursively(string dir)
        {
            if (Directory.Exists(dir)) return;

            string tdir = "";
            foreach (string d in dir.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries))
            {
                tdir += d + "\\";
                if (!Directory.Exists(tdir))
                {
                    try
                    {
                        Directory.CreateDirectory(tdir);
                    }
                    catch { return; }
                }
            }
        }

        const string ProcessTimeExceededMsg = "Process time exceeded 10 seconds - Exit process now";
        const int ProcessExecutionTimeLimit = 10 * 1000;

        /// <summary>
        /// Build a project. Also cares about the last versions and additional file and project dependencies
        /// </summary>
        /// <param name="prj"></param>
        /// <returns></returns>
        public static string BuildProject(DProject prj)
        {
            try { prj.Save(); }
            catch { }

            CompilerConfiguration cc = prj.Compiler;

            OnMessage(prj, prj.prjfn, "Build " + prj.name + " project");

            prj.RefreshBuildDate();

            List<string> builtfiles = new List<string>();
            bool IsDebug = !prj.isRelease;
            string args = "";
            bool OneFileChanged = String.IsNullOrEmpty(prj.LastBuiltTarget);

            Directory.SetCurrentDirectory(prj.basedir);

            CreateDirectoryRecursively(prj.AbsoluteOutputDirectory);

            #region Last version storing
            if (prj.LastVersionCount > 0)
            {
                List<string> tdirs = new List<string>(Directory.GetDirectories(prj.AbsoluteOutputDirectoryWithoutSVN));
                if (tdirs.Count > prj.LastVersionCount)
                {
                    // Very important: Sort array to ensure the descending order of dates/times
                    tdirs.Sort();
                    for (int i = 0; i < tdirs.Count - prj.LastVersionCount; i++)
                    {
                        if (!String.IsNullOrEmpty(prj.LastBuiltTarget) && tdirs[i].EndsWith(Path.GetDirectoryName(prj.LastBuiltTarget)))
                            continue;
                        try
                        {
                            Directory.Delete(tdirs[i], true);
                        }
                        catch { }
                    }
                }
            }
            if (prj.EnableSubversioning && prj.AlsoStoreSources)
                CreateDirectoryRecursively(prj.AbsoluteOutputDirectory + "\\src");
            #endregion

            #region File dependencies
            foreach (string depFile in prj.FileDependencies)
            {
                try
                {
                    if (File.Exists(depFile))
                        File.Copy(depFile, prj.AbsoluteOutputDirectory + "\\" + Path.GetFileName(depFile));
                }
                catch (Exception ex) { OnMessage(prj, depFile, "Couldn't copy " + depFile + ": " + ex.Message); }
            }
            #endregion

            #region Project dependencies
            foreach (string depFile in prj.ProjectDependencies)
            {
                if (!File.Exists(depFile) || depFile == prj.prjfn) continue;
                try
                {
                    DProject depProject = DProject.LoadFrom(depFile);
                    if (String.IsNullOrEmpty(BuildProject(depProject)))
                    {
                        OnMessage(depProject, depFile, "Couldn't build " + depProject.name + " ... break main build process!");
                        return null;
                    }
                    else
                    {
                        OnMessage(depProject, depProject.LastBuiltTarget, "Copy " + depProject.LastBuiltTarget + " to " + prj.AbsoluteOutputDirectory);
                        File.Copy(depProject.LastBuiltTarget, prj.AbsoluteOutputDirectory + "\\" + Path.GetFileName(depProject.LastBuiltTarget));
                    }
                }
                catch (Exception ex)
                {
                    OnMessage(prj, depFile, "Couldn't build " + depFile + ": " + ex.Message);
                    return null;
                }
            }
            #endregion

            #region Compile all sources
            List<string> FilesToCompile = new List<string>(prj.resourceFiles);

            if (!Directory.Exists("obj")) Directory.CreateDirectory("obj");

            if (prj.ManifestCreation == DProject.ManifestCreationType.IntegratedResource)
            {
                string manifestFile = "Manifest.manifest";
                string manifestRCFile = "Manifest.rc";
                DProject.CreateManifestFile(manifestFile);
                DProject.CreateManifestImportingResourceFile(manifestRCFile, manifestFile);
                FilesToCompile.Add(manifestRCFile);
            }

            foreach (string rc in FilesToCompile)
            {
                string phys_rc = prj.GetPhysFilePath(rc);
                string tdirname = Path.GetDirectoryName(rc).Replace('\\', '_').Replace(":", "") + "_";

                #region Compile Resources
                if (rc.EndsWith(".rc"))
                {
                    string res = "obj\\" + tdirname + Path.GetFileNameWithoutExtension(rc) + ".res";

                    if (prj.LastModifyingDates.ContainsKey(phys_rc) &&
                        prj.LastModifyingDates[phys_rc] == File.GetLastWriteTimeUtc(phys_rc).ToFileTimeUtc() &&
                        File.Exists(res))
                    {
                    }
                    else
                    {
                        OnMessage(prj, rc, "Compile resource " + Path.GetFileName(rc));
                        Form1.thisForm.ProgressStatusLabel.Text = "Compiling resource " + Path.GetFileName(rc);

                        OneFileChanged = true;
                        if (!BuildResFile(rc, cc, res, prj.basedir)) return null;

                        if (!prj.LastModifyingDates.ContainsKey(phys_rc))
                            prj.LastModifyingDates.Add(phys_rc, File.GetLastWriteTimeUtc(phys_rc).ToFileTimeUtc());
                        else
                            prj.LastModifyingDates[phys_rc] = File.GetLastWriteTimeUtc(phys_rc).ToFileTimeUtc();

                        if (prj.EnableSubversioning && prj.AlsoStoreSources)
                        {
                            try
                            {
                                File.Copy(phys_rc, prj.AbsoluteOutputDirectory + "\\src\\" + tdirname + Path.GetFileName(rc));
                            }
                            catch { }
                        }
                    }
                    builtfiles.Add(res);
                }
                #endregion
                #region Compile D Sources
                else if (DModule.Parsable(rc))
                {
                    string obj = "obj\\" + tdirname + Path.GetFileNameWithoutExtension(rc) + (IsDebug ? "_dbg" : String.Empty) + ".obj";

                    if (prj.LastModifyingDates.ContainsKey(phys_rc) &&
                        prj.LastModifyingDates[phys_rc] == File.GetLastWriteTimeUtc(phys_rc).ToFileTimeUtc() &&
                        File.Exists(obj))
                    {
                        // Do nothing because targeted obj file is already existing in its latest version
                    }
                    else
                    {
                        OnMessage(prj, rc, "Compile " + Path.GetFileName(rc));
                        Form1.thisForm.ProgressStatusLabel.Text = "Compiling " + Path.GetFileName(rc);

                        OneFileChanged = true;
                        if (!BuildObjFile(rc, cc, obj, prj.basedir, prj.compileargs, IsDebug)) return null;

                        if (!prj.LastModifyingDates.ContainsKey(phys_rc))
                            prj.LastModifyingDates.Add(phys_rc, File.GetLastWriteTimeUtc(phys_rc).ToFileTimeUtc());
                        else
                            prj.LastModifyingDates[phys_rc] = File.GetLastWriteTimeUtc(phys_rc).ToFileTimeUtc();

                        if (prj.EnableSubversioning && prj.AlsoStoreSources)
                        {
                            try
                            {
                                File.Copy(phys_rc, prj.AbsoluteOutputDirectory + "\\src\\" + tdirname + Path.GetFileName(rc));
                            }
                            catch { }
                        }
                    }

                    builtfiles.Add(obj);
                }
                #endregion
            }
            #endregion

            #region Linking
            string exe = "dmd.exe";
            string target = prj.AbsoluteOutputDirectory + "\\" + Path.ChangeExtension(prj.targetfilename, null);
            string objs = "";
            string libs = "";

            foreach (string f in builtfiles) objs += "\"" + f + "\" ";
            foreach (string f in prj.libs) libs += "\"" + f + "\" ";

            switch (prj.type)
            {
                case DProject.PrjType.WindowsApp:
                    exe = cc.Win32ExeLinker;
                    target += ".exe";
                    args = IsDebug ? cc.Win32ExeLinkerDebugArgs : cc.Win32ExeLinkerArgs;
                    break;
                case DProject.PrjType.ConsoleApp:
                    exe = cc.ExeLinker;
                    target += ".exe";
                    args = IsDebug ? cc.ExeLinkerDebugArgs : cc.ExeLinkerArgs;
                    break;
                case DProject.PrjType.Dll:
                    exe = cc.DllLinker;
                    target += ".dll";
                    args = IsDebug ? cc.DllLinkerDebugArgs : cc.DllLinkerArgs;
                    break;
                case DProject.PrjType.StaticLib:
                    exe = cc.LibLinker;
                    target += ".lib";
                    args = IsDebug ? cc.LibLinkerDebugArgs : cc.LibLinkerArgs;
                    break;
            }

            if (!OneFileChanged && (File.Exists(target) || prj.EnableSubversioning))
            {
                OnMessage(prj, target, "Anything changed...no linking necessary!");
                try
                {
                    if (prj.EnableSubversioning) Directory.Delete(prj.AbsoluteOutputDirectory, true);
                }
                catch { }
                return prj.LastBuiltTarget;
            }

            args = args.Replace("$target", target);
            args = args.Replace("$exe", Path.ChangeExtension(target, ".exe"));
            args = args.Replace("$dll", Path.ChangeExtension(target, ".dll"));
            args = args.Replace("$def", Path.ChangeExtension(target, ".def"));
            args = args.Replace("$libs", libs);
            args = args.Replace("$lib", Path.ChangeExtension(target, ".lib"));
            args = args.Replace("$objs", objs);

            OnMessage(prj, target, "Link file to " + target);
            Form1.thisForm.ProgressStatusLabel.Text = "Link file to " + target;

            if(!Path.IsPathRooted(exe))
                exe=cc.BinDirectory + "\\" + exe;

            Process prc = DBuilder.Exec(exe, args + " " + prj.linkargs, prj.basedir, true);
            if (prc == null) return null;
            if (!prc.WaitForExit(10 * 1000))
            {
                OnMessage(prj,exe,ProcessTimeExceededMsg);
                prc.Kill();
            }

            if (prc.ExitCode == 0)
            {
                // This line of code is very important for debugging!
                prj.LastBuiltTarget = target;

                // If enabled, create external manifest file now
                if (prj.ManifestCreation == DProject.ManifestCreationType.External) prj.CreateExternalManifestFile();

                #region cv2pdb
                // Create program database (pdb) file from CodeView data from the target exe
                if (IsDebug && D_IDE_Properties.Default.CreatePDBOnBuild)
                {
                    CreatePDBFromExe(prj,target);
                }
                #endregion

                Form1.thisForm.ProgressStatusLabel.Text = "Linking done!";
                OnMessage(prj, target, "Linking done!");
                return target;
            }
            #endregion
            return null;
        }

        public static void CreatePDBFromExe(DProject prj,string exe)
        {
            string pdb = Path.ChangeExtension(exe, ".pdb");
            OnMessage(prj, pdb, "Create debug information database " + pdb);
            CodeViewToPDB.CodeViewToPDBConverter.DoConvert(/*prj.CompilerVersion==CompilerConfiguration.DVersion.D2,*/exe, pdb);
        }

        public static bool BuildObjFile(string file, CompilerConfiguration cc, string target, string exeDir, string additionalArgs, bool IsDebug)
        {
            if (!DModule.Parsable(file)) { throw new Exception("Cannot build file type of " + file); }

            string args = IsDebug ? cc.SoureCompilerDebugArgs : cc.SoureCompilerArgs;
            args = args.Replace("$src", file);
            args = args.Replace("$obj", target);

            Process prc = DBuilder.Exec(Path.IsPathRooted(cc.SoureCompiler) ? cc.SoureCompiler : (cc.BinDirectory + "\\" + cc.SoureCompiler), args + " " + additionalArgs, exeDir, true);
            if (prc == null) return false;
            if (!prc.WaitForExit(ProcessExecutionTimeLimit))
            {
                OnMessage(null, file, ProcessTimeExceededMsg);
                prc.Kill();
            }

            return prc.ExitCode == 0;
        }

        public static bool BuildResFile(string file, CompilerConfiguration cc, string target, string exeDir)
        {
            if (!file.EndsWith(".rc")) { throw new Exception("Cannot build resource file of " + file); }

            string args = cc.ResourceCompilerArgs;
            args = args.Replace("$rc", file);
            args = args.Replace("$res", target);

            Process prc = DBuilder.Exec(Path.IsPathRooted(cc.ResourceCompiler)? cc.ResourceCompiler:(cc.BinDirectory+"\\"+cc.ResourceCompiler), args, exeDir, true);
            if (prc == null) return false;
            if (!prc.WaitForExit(10 * 1000))
            {
                OnMessage(null, file, ProcessTimeExceededMsg);
                prc.Kill();
            }

            return prc.ExitCode == 0;
        }

        /// <summary>
        /// Wrapper for executing compilers, linkers and other processes
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        /// <param name="execdir"></param>
        /// <param name="showConsole"></param>
        /// <returns></returns>
        public static Process Exec(string cmd, string args, string execdir, bool showConsole)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.CreateNoWindow = showConsole;
            psi.RedirectStandardError = true;
            psi.RedirectStandardInput = showConsole;
            psi.RedirectStandardOutput = showConsole;
            psi.UseShellExecute = false;
            psi.WorkingDirectory = execdir;

            psi.FileName = cmd;
            psi.Arguments = args;

            Process proc = new Process();

            proc.StartInfo = psi;
            proc.ErrorDataReceived += new DataReceivedEventHandler(p_ErrorDataReceived);
            if (showConsole) proc.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
            if (D_IDE_Properties.Default.ShowBuildCommands) OnMessage(null, cmd, cmd + " " + args);
            proc.Exited += new EventHandler(running_Exited);

            try
            {
                proc.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error executing " + cmd + ":\n\n" + ex.Message);
                return null;
            }

            if (showConsole) proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            return proc;
        }

        #region Error Handlers
        public delegate void OutputHandler(DProject project, string file, string message);
        static public event OutputHandler OnMessage;
        public static event DataReceivedEventHandler OnError;
        public static event DataReceivedEventHandler OnOutput;
        public static event EventHandler OnExit;

        static void running_Exited(object sender, EventArgs e)
        {
            OnExit(sender, e);
        }

        static void p_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
                OnError(sender, e);
        }

        static void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
                OnOutput(sender, e);
        }
        #endregion
    }
}
