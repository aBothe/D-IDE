using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;

namespace D_IDE.Core
{
	public class FileExecution
	{
		public delegate void DataReceivedEvent(string Data);
		public delegate void ProcessExitedEvent();

		/// <summary>
		/// Starts a process without showing a console.
		/// </summary>
		public static Process ExecuteSilentlyAsync(string Executable, string Arguments, string StartDirectory,
			DataReceivedEvent OnOutput, DataReceivedEvent OnError, ProcessExitedEvent OnExit)
		{
			if (GlobalProperties.Instance.VerboseDebugOutput)
				IDEInterface.Log(Executable + " " + Arguments);

			var psi = new ProcessStartInfo(Executable, Arguments);
			psi.WorkingDirectory = StartDirectory;

			psi.CreateNoWindow = true;
			psi.RedirectStandardError = true;
			psi.RedirectStandardOutput = true;
			psi.UseShellExecute = false;

			var prc = new Process();
			prc.StartInfo = psi;

			prc.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e) {
				if (!string.IsNullOrEmpty(e.Data) && OnError != null)
					OnError(e.Data);
			};
			prc.OutputDataReceived += delegate(object s, DataReceivedEventArgs e)
			{
				if (!string.IsNullOrEmpty(e.Data) && OnOutput != null)
					OnOutput(e.Data);
			};
			prc.Exited += delegate(object s, EventArgs e) { if (OnExit != null) OnExit(); };

			try
			{
				prc.Start();
			}
			catch (Exception ex)
			{
				ErrorLogger.Log(ex);
				return null;
			}

			prc.BeginErrorReadLine();
			prc.BeginOutputReadLine();
			return prc;
		}

		/// <summary>
		/// Starts a console-based process
		/// </summary>
		public static Process ExecuteAsync(string Executable, string Arguments, string StartDirectory, ProcessExitedEvent OnExit)
		{
			if (GlobalProperties.Instance.VerboseDebugOutput)
				IDEInterface.Log(Executable + " " + Arguments);

			var psi = new ProcessStartInfo(Executable, Arguments);
			psi.WorkingDirectory = StartDirectory;

			var prc = new Process();
			prc.StartInfo = psi;

			prc.Exited += delegate(object s, EventArgs e) { if (OnExit != null) OnExit(); };

			try
			{
				prc.Start();
			}
			catch (Exception ex)
			{
				ErrorLogger.Log(ex);
				return null;
			}

			return prc;
		}
	}
}
