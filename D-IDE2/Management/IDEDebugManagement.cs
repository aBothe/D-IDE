using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using System.IO;
using System.Diagnostics;
using DebugEngineWrapper;
using D_IDE.Core.Controls.Editor;
using System.Windows.Media;
using System.Threading;

namespace D_IDE
{
	partial class IDEManager
	{
		public class IDEDebugManagement:DebugManagement
		{
			#region Trivial methods
			/// <summary>
			/// Execute the current solution or single module by attaching a debugger to it
			/// </summary>
			/// <param name="ShowConsole">Show an external program console?</param>
			public static void LaunchWithDebugger()
			{
				if (ErrorManagement.LastSingleBuildResult != null)
				{
					if (ErrorManagement.LastSingleBuildResult.Successful)
						LaunchWithDebugger(ErrorManagement.LastSingleBuildResult.TargetFile, "",Path.GetDirectoryName(ErrorManagement.LastSingleBuildResult.SourceFile),true);
				}
				else
					LaunchWithDebugger(CurrentSolution.StartProject);
			}

			public static void LaunchWithDebugger(Solution sln)
			{
				LaunchWithDebugger(sln.StartProject);
			}
			public static void LaunchWithDebugger(Project prj)
			{
				if (prj != null)
					LaunchWithDebugger(prj.OutputFile,prj.ExecutingArguments,prj.BaseDirectory,prj.OutputType!=OutputTypes.CommandWindowLessExecutable);
			}

			/// <summary>
			/// Execute the current solution or single module without attaching a debugger to it
			/// </summary>
			/// <param name="ShowConsole">Show an external program console?</param>
			public static Process LaunchWithoutDebugger(bool ShowConsole)
			{
				if (ErrorManagement.LastSingleBuildResult != null)
				{
					if (ErrorManagement.LastSingleBuildResult.Successful)
						return LaunchWithoutDebugger(ErrorManagement.LastSingleBuildResult.TargetFile, "", ShowConsole);
				}
				else
				{
					if (CurrentSolution != null &&
						CurrentSolution.StartProject != null &&
						CurrentSolution.StartProject.LastBuildResult != null &&
						CurrentSolution.StartProject.LastBuildResult.Successful)
						return LaunchWithoutDebugger(CurrentSolution.StartProject, ShowConsole);
				}
				return null;
			}

			public static Process LaunchWithoutDebugger(Solution sln, bool ShowConsole)
			{
				return LaunchWithoutDebugger(sln.StartProject, ShowConsole);
			}
			public static Process LaunchWithoutDebugger(Project prj, bool ShowConsole)
			{
				if (prj == null)
					return null;

				return LaunchWithoutDebugger(prj.OutputFile, prj.ExecutingArguments, ShowConsole);
			}
			#endregion

			#region Debugging
			private static bool dbgEngineInited = false;
			static bool EngineStarting = false;
			static bool StopWaitingForEvents = false;
			static bool _PauseDebuggee = false;

			public static void ContinueDebugging()
			{
				if (!IsDebugging)	return;
				Engine.Execute("gh");
				WaitForDebugEvent();
			}

			/// <summary>
			/// Tries to halt the wait thread
			/// </summary>
			/*public static void PauseDebugging()
			{
				_PauseDebuggee = true;
			}*/

			public static void StepIn()
			{
				if (!IsDebugging) return;
				Engine.Execute("t"); // Trace
				WaitForDebugEvent();
				StopWaitingForEvents = false;
				GotoCurrentLocation();
			}

			public static void StepOver()
			{
				if (!IsDebugging) return;
				Engine.Execute("p"); // Step
				WaitForDebugEvent();
				StopWaitingForEvents = false;
				GotoCurrentLocation();
			}

			public static void StepOut()
			{
				if (!IsDebugging) return;
				Engine.Execute("pt"); // Halt on next return
				WaitForDebugEvent();
				StopWaitingForEvents = false;
				GotoCurrentLocation();
			}

			public static void GotoCurrentLocation()
			{
				string fn;
				uint ln;
			cont:
				if (!IsDebugging || StopWaitingForEvents) return;

				ulong off = Engine.CurrentFrame.InstructionOffset;
				if (Engine.Symbols.GetLineByOffset(off, out fn, out ln))
				{
					var ed = EditingManagement.OpenFile(fn) as EditorDocument;
					if (ed != null && ln<ed.Editor.Document.LineCount)
					{
						var text_off = ed.Editor.Document.GetOffset((int)ln, 0);
						ed.Editor.TextArea.Caret.Offset = text_off;
					}
				}
				else
				{
					Engine.WaitForEvent(10);
					if (!StopWaitingForEvents)
					{
						System.Windows.Forms.Application.DoEvents();
						goto cont;
					}
				}

				UpdateDebuggingPanels();
			}

			public static void UpdateDebuggingPanels()
			{
				if(IDEManager.Instance.CurrentEditor!=null)
					IDEManager.Instance.MainWindow.Panel_Locals.RefreshTable();
				//TODO: Call Stack window - with switching between different stack levels?
			}

			public static void WaitForDebugEvent()
			{
				if (!IsDebugging) return;

				Log("Waiting for the program to interrupt...",ErrorType.Information);
				var wr = WaitResult.OK;
				while (IsDebugging && (wr = Engine.WaitForEvent(10)) == WaitResult.TimeOut)
				{
					if (wr == WaitResult.Unexpected)
						break;
					System.Windows.Forms.Application.DoEvents();
				}
				if (wr != WaitResult.Unexpected)
					Log("Program execution halted...",ErrorType.Information);
				/*
				 * After a program paused its execution, we'll be able to access its breakpoints and symbol data.
				 * When resuming the program, WaitForDebugEvent() will be called again.
				 * Note that it's not possible to 'wait' on a different thread.
				 */
				UpdateDebuggingPanels();
				RefreshAllDebugMarkers();
			}

			public static void InitDebugger()
			{
				#region Attach event handlers
				Engine.Output += delegate(OutputFlags type, string msg)
				{
					if (!GlobalProperties.Instance.VerboseDebugOutput && (type == OutputFlags.Verbose || type == OutputFlags.Normal)) return;

					var ErrType=ErrorType.Message;
					if (type == OutputFlags.Warning)
						return;
					if (type == OutputFlags.Error)
						ErrType = ErrorType.Error;
					Log(msg.Replace("\n",string.Empty),ErrType);
				};

				Engine.OnLoadModule += delegate(ulong BaseOffset, uint ModuleSize, string ModuleName, uint Checksum, uint Timestamp)
				{
					if (EngineStarting)
						return DebugStatus.Break;
					return DebugStatus.NoChange;
				};

				/*dbg.OnUnloadModule += delegate(ulong BaseOffset, string ModuleName)
				{
					LoadedModules.Remove(ModuleName);
					return DebugStatus.NoChange;
				};*/

				Engine.OnBreakPoint += delegate(uint Id, string cmd, ulong off, string exp)
				{
					StopWaitingForEvents = true;
					var bp = Engine.GetBreakPointById(Id);
					Log("Breakpoint #" + Id.ToString() + " at " + off.ToString() + ": " + exp,ErrorType.Information);
					string fn;
					uint ln;

					if (!Engine.Symbols.GetLineByOffset(off, out fn, out ln))
					{
						Log("No source associated with " + off.ToString(),ErrorType.Warning);
						return DebugStatus.Break;
					}

					if(GlobalProperties.Instance.VerboseDebugOutput)
						Log(fn + ":" + ln.ToString(),ErrorType.Information);

					var ed=EditingManagement.OpenFile(fn) as EditorDocument;
					if (ed != null)
					{
						Log("Unable to move to "+fn+":"+ln,ErrorType.Warning);
						return DebugStatus.Break;
					}

					var text_off=ed.Editor.Document.GetOffset((int)ln-1,0);
					ed.Editor.TextArea.Selection.StartSelectionOrSetEndpoint(text_off, text_off);

					UpdateDebuggingPanels();			
		
					return DebugStatus.Break;
				};

				Engine.OnException += delegate(CodeException ex)
				{
					StopWaitingForEvents = true;

					string extype = "";
					try
					{
						extype = ((ExceptionType)ex.Type).ToString();
					}
					catch
					{
						extype = "Unknown type (" + ex.Type.ToString() + ")";
					}
					
					string msg = "";
					if ((ExceptionType)ex.Type == ExceptionType.DException)
					{
						msg = ex.Message;
						if (ex.TypeInfo != null)
							msg = ex.TypeInfo + ": " + msg;

						Log(msg,ErrorType.Error);
						
					}
					else
					{
						Log(msg=extype + "-Exception",ErrorType.Error);
					}

					var ed = EditingManagement.OpenFile(ex.SourceFile) as EditorDocument;
					if (ed != null)
					{
						var m = new DebugErrorMarker(ed.MarkerStrategy, ex);
						m.Redraw();

						var off = ed.Editor.Document.GetOffset((int)ex.SourceLine - 1, 0);
						ed.Editor.TextArea.Selection.StartSelectionOrSetEndpoint(off,off);
					}

					UpdateDebuggingPanels();

					return DebugStatus.Break;
				};

				Engine.OnExitProcess += delegate(uint code)
				{
					Log("Debugger Process exited with code " + code.ToString(),
						code<1?ErrorType.Information:ErrorType.Error);
					StopExecution();
					return DebugStatus.NoChange;
				};

				/*Engine.OnSessionStatusChanged += delegate(SessionStatus ss)
				{
					Log("Session status changed to " + ss.ToString());
					//if (ss == SessionStatus.Active)	return DebugStatus.Break;
					return DebugStatus.NoChange;
				};*/
				#endregion

				Engine.Execute(".lines -e"); // Enable source code locating

				dbgEngineInited = true;
			}

			/// <summary>
			/// Launch the debugger asynchronously
			/// </summary>
			/// <param name="exe"></param>
			/// <param name="args"></param>
			/// <param name="sourcePath"></param>
			/// <param name="showConsole"></param>
			public static void LaunchWithDebugger(string exe, string args,string sourcePath,bool showConsole)
			{
				StopExecution();

				if (!dbgEngineInited)
					InitDebugger();

				IsDebugging = true;
				Instance.UpdateGUI();
				EngineStarting = true;
				StopWaitingForEvents = false;

				DebugCreateProcessOptions opt = new DebugCreateProcessOptions();
				opt.CreateFlags = CreateFlags.DebugOnlyThisProcess | (true ? CreateFlags.CreateNewConsole : 0);
				opt.EngCreateFlags = EngCreateFlags.Default;

				Engine.CreateProcessAndAttach(0, exe + (string.IsNullOrWhiteSpace(args) ? "" : (" " + args)), opt, Path.GetDirectoryName(exe), "", 0, 0);
				
				Engine.Symbols.SourcePath = string.IsNullOrWhiteSpace(sourcePath) ? sourcePath : Path.GetDirectoryName(exe);
				Engine.IsSourceCodeOrientedStepping = true;

				Engine.WaitForEvent();
				Engine.Execute("bc"); // Clear breakpoint list
				Engine.WaitForEvent();

				BreakpointManagement.SetupBreakpoints();

				EngineStarting = false;

				WaitForDebugEvent(); // Wait for the first breakpoint/exception/program exit to occur
			}
			#endregion

			#region Non-Debug executing
			public static Process CurrentProcess { get; protected set; }

			public static Process LaunchWithoutDebugger(string exe,string args,bool ShowConsole)
			{
				StopExecution();

				if (!File.Exists(exe))
				{
					ErrorLogger.Log(exe +" not found",ErrorType.Error,ErrorOrigin.Program);
					return null;
				}

				if (ShowConsole)
					CurrentProcess = FileExecution.ExecuteAsync(exe, args, Path.GetDirectoryName(exe), CurrentProcess_Exited);
				else
					CurrentProcess = FileExecution.ExecuteSilentlyAsync(exe, args, Path.GetDirectoryName(exe),
						CurrentProcess_OutputDataReceived, CurrentProcess_ErrorDataReceived, CurrentProcess_Exited);

				Instance.UpdateGUI();

				return CurrentProcess;
			}

			static void CurrentProcess_Exited()
			{
				ErrorLogger.Log("Process exited with code "+CurrentProcess.ExitCode.ToString()+" ("+(CurrentProcess.ExitTime-CurrentProcess.StartTime).ToString()+")",
					CurrentProcess.ExitCode<1?ErrorType.Information:ErrorType.Error,
					ErrorOrigin.Program);
				Instance.MainWindow.RefreshMenu();
			}

			static void CurrentProcess_ErrorDataReceived(string Data)
			{
				ErrorLogger. Log("Error occured: "+Data,ErrorType.Error,ErrorOrigin.Program);
			}

			static void CurrentProcess_OutputDataReceived(string Data)
			{
				ErrorLogger. Log(Data,ErrorType.Message,ErrorOrigin.Program);
			}
			#endregion

			public static void PauseExecution()
			{
				if (IsDebugging)
				{
					Engine.InterruptTimeOut = 50;
					Engine.Interrupt();
				}
			}

			public static void StopExecution()
			{
				if (IsDebugging)
				{
					IsDebugging = false;
					Engine.EndPendingWaits();
					Engine.Terminate();
					//Engine.MainProcess.Kill();
					Instance.MainWindow.RefreshMenu();
					UpdateDebuggingPanels();
					RefreshAllDebugMarkers();
				}
				if (CurrentProcess != null && !CurrentProcess.HasExited)
				{
					CurrentProcess.Kill();
					CurrentProcess = null;
				}
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
