using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using System.IO;
using System.Diagnostics;

namespace D_IDE
{
	partial class IDEManager
	{
		public class ExeLaunchManagement:DebugManagement
		{
			public static Process CurrentProcess { get; protected set; }

			public static void LaunchWithoutDebugger(Solution sln)
			{
				StopExecution();
			}

			public static void LaunchWithoutDebugger(string exe,string args,bool ShowConsole)
			{
				StopExecution();

				if (!File.Exists(exe))
				{
					IDEInterface.Log(exe +" not found");
					return;
				}

				CurrentProcess = new Process();
				var psi = CurrentProcess.StartInfo;
				psi.FileName = exe;
				psi.Arguments = args;
				psi.WorkingDirectory = Path.GetDirectoryName(exe);
				CurrentProcess.Exited += new EventHandler(CurrentProcess_Exited);

				if (!ShowConsole)
				{
					psi.CreateNoWindow = true;
					psi.UseShellExecute = false;
					psi.RedirectStandardError = true;
					psi.RedirectStandardOutput = true;

					CurrentProcess.ErrorDataReceived += new DataReceivedEventHandler(CurrentProcess_ErrorDataReceived);
					CurrentProcess.OutputDataReceived += new DataReceivedEventHandler(CurrentProcess_OutputDataReceived);
				}

				if (GlobalProperties.Instance.VerboseDebugOutput)
					IDEInterface.Log("Execute "+exe+" "+args);

				try
				{
					CurrentProcess.Start();
				}
				catch (Exception ex)
				{
					ErrorLogger.Log(ex);
				}
			}

			static void CurrentProcess_Exited(object sender, EventArgs e)
			{
				IDEInterface.Log("Process exited with code "+CurrentProcess.ExitCode.ToString()+" ("+(CurrentProcess.ExitTime-CurrentProcess.StartTime).ToString()+")");
			}

			static void CurrentProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
			{
				IDEInterface.Log("Error occured: "+e.Data);
			}

			static void CurrentProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
			{
				IDEInterface.Log(e.Data);
			}

			public static void StopExecution()
			{
				if (IsDebugging)
					Engine.Terminate();
				if (CurrentProcess != null || !CurrentProcess.HasExited)
					CurrentProcess.Kill();
			}

			public static bool IsExecuting
			{
				get { 
					return IsDebugging || (CurrentProcess!=null && !CurrentProcess.HasExited);
				}
			}
		}
	}
}
