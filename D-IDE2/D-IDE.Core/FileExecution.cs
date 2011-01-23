using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace D_IDE.Core
{
	public class FileExecution
	{
		public delegate void DataReceivedEvent(string Data);
		public delegate void ProcessExitedEvent();

		/// <summary>
		/// Starts a process without showing a console.
		/// Error and standard outputs
		/// </summary>
		public static Process ExecuteSilentlyAsync(string Executable, string Arguments, string StartDirectory,
			DataReceivedEvent OnOutput, DataReceivedEvent OnError, ProcessExitedEvent OnExit)
		{
			var psi = new ProcessStartInfo(Executable, Arguments);
			psi.WorkingDirectory = StartDirectory;

			psi.CreateNoWindow = true;
			psi.RedirectStandardError = true;
			psi.RedirectStandardOutput = true;
			psi.UseShellExecute = false;

			var prc = Process.Start(psi);
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

			prc.BeginErrorReadLine();
			prc.BeginOutputReadLine();
			return prc;
		}
	}
}
