using System;
using System.Diagnostics;
using System.IO;
using System.Text;

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
			if (GlobalProperties.Instance.VerboseBuildOutput)
				ErrorLogger.Log(Executable + " " + Arguments,ErrorType.Message,ErrorOrigin.Build);

			if (!File.Exists(Executable))
			{
				ErrorLogger.Log(Executable + " not found!",ErrorType.Error,ErrorOrigin.Build);
				return null;
			}

			var psi = new ProcessStartInfo(Executable, Arguments) { 
				WorkingDirectory=StartDirectory,
				CreateNoWindow=true,
				UseShellExecute=false,
				//RedirectStandardInput=true,
				RedirectStandardError=true,
				RedirectStandardOutput=true,
                StandardErrorEncoding= Encoding.UTF8,
                StandardOutputEncoding=Encoding.UTF8
			};

			var prc = new Process() { StartInfo=psi, EnableRaisingEvents=true};

			prc.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e) {
				if (!string.IsNullOrEmpty(e.Data))
				{ 
					if(GlobalProperties.Instance.VerboseBuildOutput)
						ErrorLogger.Log(e.Data,ErrorType.Warning,ErrorOrigin.Build);

					if(OnError != null)
						OnError(e.Data);
				}
			};
			prc.OutputDataReceived += delegate(object s, DataReceivedEventArgs e)
			{
				if (!string.IsNullOrEmpty(e.Data))
				{
					if(GlobalProperties.Instance.VerboseBuildOutput)
						ErrorLogger.Log(e.Data,ErrorType.Message,ErrorOrigin.Build);

					if(OnOutput != null)
						OnOutput(e.Data);
				}
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
				ErrorLogger.Log(ex, ErrorType.Error, ErrorOrigin.Build);
				return null;
			}
			return prc;
		}

		/// <summary>
		/// Starts a console-based process
		/// </summary>
		public static Process ExecuteAsync(string Executable, string Arguments, string StartDirectory, ProcessExitedEvent OnExit)
		{
			if (GlobalProperties.Instance.VerboseBuildOutput)
				ErrorLogger.Log(Executable + " " + Arguments, ErrorType.Error, ErrorOrigin.Build);

			var psi = new ProcessStartInfo(Executable, Arguments) { WorkingDirectory=StartDirectory, UseShellExecute=OnExit==null};
			var prc = new Process() { StartInfo=psi, EnableRaisingEvents=OnExit!=null};

			if(OnExit!=null)
				prc.Exited +=new EventHandler( delegate(object s, EventArgs e) { OnExit(); });

			try
			{
				prc.Start();
			}
			catch (Exception ex)
			{
				if (ex is FileNotFoundException)
					ErrorLogger.Log(Executable+" not found!\r\n\r\nArguments:\t"+Arguments+"\r\nStart Directory:\t"+StartDirectory, ErrorType.Error, ErrorOrigin.Build);
				else
					ErrorLogger.Log(ex, ErrorType.Error, ErrorOrigin.Build);
				return null;
			}

			return prc;
		}
	}
}
