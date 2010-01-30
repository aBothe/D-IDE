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
	public class DBuilder
	{
		public static bool BuildProject(DProject prj)
		{
			List<string> builtfiles = new List<string>();
			string args = "";

			Directory.SetCurrentDirectory(prj.basedir);

			if (!Directory.Exists("obj")) Directory.CreateDirectory("obj");

			Form1.thisForm.BuildProgressBar.Value = 0;
			Form1.thisForm.BuildProgressBar.Maximum = prj.resourceFiles.Count + 1;

			bool OneFileChanged = false;

			foreach (string rc in prj.resourceFiles)
			{
				string phys_rc = prj.GetPhysFilePath(rc);
				#region Compile Resources
				if (rc.EndsWith(".rc"))
				{
					string res = "obj\\" + Path.GetFileNameWithoutExtension(rc) + ".res";

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
						if (!BuildResFile(rc, res, prj.basedir)) return false;

						if (!prj.LastModifyingDates.ContainsKey(phys_rc))
							prj.LastModifyingDates.Add(phys_rc, File.GetLastWriteTimeUtc(phys_rc).ToFileTimeUtc());
						else
							prj.LastModifyingDates[phys_rc] = File.GetLastWriteTimeUtc(phys_rc).ToFileTimeUtc();
					}
					Form1.thisForm.BuildProgressBar.Value++;

					builtfiles.Add(res);
				}
				#endregion
				#region Compile D Sources
				else if (DModule.Parsable(rc))
				{
					string obj = "obj\\" + Path.GetFileNameWithoutExtension(rc) + ".obj";

					if (prj.LastModifyingDates.ContainsKey(phys_rc) &&
						prj.LastModifyingDates[phys_rc] == File.GetLastWriteTimeUtc(phys_rc).ToFileTimeUtc() &&
						File.Exists(obj))
					{
					}
					else
					{
						OnMessage(prj, rc, "Compile " + Path.GetFileName(rc));
						Form1.thisForm.ProgressStatusLabel.Text = "Compiling " + Path.GetFileName(rc);

						OneFileChanged = true;
						if (!BuildObjFile(rc, obj, prj.basedir)) return false;

						if (!prj.LastModifyingDates.ContainsKey(phys_rc))
							prj.LastModifyingDates.Add(phys_rc, File.GetLastWriteTimeUtc(phys_rc).ToFileTimeUtc());
						else
							prj.LastModifyingDates[phys_rc] = File.GetLastWriteTimeUtc(phys_rc).ToFileTimeUtc();
					}

					Form1.thisForm.BuildProgressBar.Value++;

					builtfiles.Add(obj);
				}
				#endregion
			}

			string exe = "dmd.exe";
			string target = Path.ChangeExtension(prj.targetfilename, null);
			string objs = "";
			string libs = "";

			foreach (string f in builtfiles) objs += "\"" + f + "\" ";
			foreach (string f in prj.libs) libs += "\"" + f + "\" ";

			switch (prj.type)
			{
				case DProject.PrjType.WindowsApp:
					exe = D_IDE_Properties.Default.exe_win;
					target += ".exe";
					args = D_IDE_Properties.Default.link_win_exe;
					break;
				case DProject.PrjType.ConsoleApp:
					exe = D_IDE_Properties.Default.exe_console;
					target += ".exe";
					args = D_IDE_Properties.Default.link_to_exe;
					break;
				case DProject.PrjType.Dll:
					exe = D_IDE_Properties.Default.exe_dll;
					target += ".dll";
					args = D_IDE_Properties.Default.link_to_dll;
					break;
				case DProject.PrjType.StaticLib:
					exe = D_IDE_Properties.Default.exe_lib;
					target += ".lib";
					args = D_IDE_Properties.Default.link_to_lib;
					break;
			}

			if (!OneFileChanged && File.Exists(target))
			{
				OnMessage(prj,target,"Anything changed...no linking necessary!");
				Form1.thisForm.BuildProgressBar.Value++;
				return true;
			}

			args = args.Replace("$target", target);
			args = args.Replace("$exe", Path.ChangeExtension(prj.targetfilename, ".exe"));
			args = args.Replace("$dll", Path.ChangeExtension(prj.targetfilename, ".dll"));
			args = args.Replace("$def", Path.ChangeExtension(prj.targetfilename, ".def"));
			args = args.Replace("$libs", libs);
			args = args.Replace("$lib", Path.ChangeExtension(prj.targetfilename, ".lib"));
			args = args.Replace("$objs", objs);

			OnMessage(prj, target, "Link file to " + target);
			Form1.thisForm.ProgressStatusLabel.Text = "Link file to " + target;

			Process prc = DBuilder.Exec(exe, args, prj.basedir, true);
			prc.WaitForExit(10000);
			Form1.thisForm.BuildProgressBar.Value++;

			if (prc.ExitCode == 0)
			{
				Form1.thisForm.ProgressStatusLabel.Text = "Linking done!";
				OnMessage(prj, target, "Linking done!");
				return true;
			}
			return false;
		}

		public static bool BuildObjFile(string file, string target)
		{
			if (!DModule.Parsable(file)) { throw new Exception("Cannot build file type of " + file); return false; }

			string args = D_IDE_Properties.Default.cmp_obj;
			args = args.Replace("$src", file);
			args = args.Replace("$obj", target);

			Process prc = DBuilder.Exec(D_IDE_Properties.Default.exe_cmp, args, Path.GetDirectoryName(file), true);
			prc.WaitForExit(10000);

			return prc.ExitCode == 0;
		}
		public static bool BuildObjFile(string file, string target, string exeDir)
		{
			if (!DModule.Parsable(file)) { throw new Exception("Cannot build file type of " + file); return false; }

			string args = D_IDE_Properties.Default.cmp_obj;
			args = args.Replace("$src", file);
			args = args.Replace("$obj", target);

			Process prc = DBuilder.Exec(D_IDE_Properties.Default.exe_cmp, args, exeDir, true);
			prc.WaitForExit(10000);

			return prc.ExitCode == 0;
		}

		public static bool BuildResFile(string file, string target)
		{
			if (!file.EndsWith(".rc")) { throw new Exception("Cannot build resource file of " + file); return false; }

			string args = D_IDE_Properties.Default.cmp_res;
			args = args.Replace("$rc", file);
			args = args.Replace("$res", target);

			Process prc = DBuilder.Exec(D_IDE_Properties.Default.exe_res, args, Path.GetDirectoryName(file), true);
			prc.WaitForExit(10000);

			return prc.ExitCode == 0;
		}
		public static bool BuildResFile(string file, string target, string exeDir)
		{
			if (!file.EndsWith(".rc")) { throw new Exception("Cannot build resource file of " + file); return false; }

			string args = D_IDE_Properties.Default.cmp_res;
			args = args.Replace("$rc", file);
			args = args.Replace("$res", target);

			Process prc = DBuilder.Exec(D_IDE_Properties.Default.exe_res, args, exeDir, true);
			prc.WaitForExit(10000);

			return prc.ExitCode == 0;
		}

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
	}
}
