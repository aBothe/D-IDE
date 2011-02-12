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
				ErrorLogger.Log(Executable + " " + Arguments,ErrorType.Error,ErrorOrigin.Build);

			var psi = new ProcessStartInfo(Executable, Arguments) { 
				WorkingDirectory=StartDirectory,
				CreateNoWindow=true,
				UseShellExecute=false,
				RedirectStandardError=true,
				RedirectStandardOutput=true
			};

			var prc = new Process() { StartInfo=psi, EnableRaisingEvents=true};
			prc.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e) {
				if (!string.IsNullOrEmpty(e.Data) && OnError != null)
					OnError(e.Data);
			};
			prc.OutputDataReceived += delegate(object s, DataReceivedEventArgs e)
			{
				if (!string.IsNullOrEmpty(e.Data) && OnOutput != null)
					OnOutput(e.Data);
			};
			prc.Exited +=new EventHandler( delegate(object s, EventArgs e) { if (OnExit != null) OnExit(); });

			try
			{
				prc.Start();
				prc.BeginErrorReadLine();
				prc.BeginOutputReadLine();
			}
			catch (Exception ex)
			{
				ErrorLogger.Log(ex,ErrorType.Error,ErrorOrigin.Build);
				return null;
			}
			return prc;
		}

		/// <summary>
		/// Starts a console-based process
		/// </summary>
		public static Process ExecuteAsync(string Executable, string Arguments, string StartDirectory, ProcessExitedEvent OnExit)
		{
			if (GlobalProperties.Instance.VerboseDebugOutput)
				ErrorLogger. Log(Executable + " " + Arguments);

			var psi = new ProcessStartInfo(Executable, Arguments) { WorkingDirectory=StartDirectory, UseShellExecute=false};
			var prc = new Process() { StartInfo=psi, EnableRaisingEvents=true};

			prc.Exited +=new EventHandler( delegate(object s, EventArgs e) { if (OnExit != null) OnExit(); });

			try
			{
				prc.Start();
			}
			catch (Exception ex)
			{
				ErrorLogger.Log(ex, ErrorType.Error, ErrorOrigin.Build);
				return null;
			}

			return prc;
		}
	}
}
