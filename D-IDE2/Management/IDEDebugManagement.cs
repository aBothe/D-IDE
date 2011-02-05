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
			/// Executes the last built project or single module.
			/// </summary>
			/// <param name="ShowConsole"></param>
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
			public static void PauseDebugging()
			{
				_PauseDebuggee = true;
			}

			public static void StepIn()
			{
				if (!IsDebugging) return;
				Engine.Execute("t"); // Trace
				WaitForDebugEvent(false);
				StopWaitingForEvents = false;
				GotoCurrentLocation();
			}

			public static void StepOver()
			{
				if (!IsDebugging) return;
				Engine.Execute("p"); // Step
				WaitForDebugEvent(false);
				StopWaitingForEvents = false;
				GotoCurrentLocation();
			}

			public static void StepOut()
			{
				if (!IsDebugging) return;
				Engine.Execute("pt"); // Halt on next return
				WaitForDebugEvent(false);
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
					if (ed != null)
					{
						var text_off = ed.Editor.Document.GetOffset((int)ln - 1, 0);
						ed.Editor.TextArea.Selection.StartSelectionOrSetEndpoint(text_off, text_off);
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
				//TODO: Local variables window
				//TODO: Call Stack window - with switching between different stack levels?
			}

			static void _waitDelegate(object o)
			{
				bool IsAsync = (bool)o;
				Log("Waiting for the program to interrupt...");
				var wr = WaitResult.OK;
				while (IsDebugging && (wr = Engine.WaitForEvent(10)) == WaitResult.TimeOut)
				{
					if (wr == WaitResult.Unexpected)
						break;
					if(!IsAsync)
						System.Windows.Forms.Application.DoEvents();
				}
				if (wr != WaitResult.Unexpected)
					Log("Program execution paused...");
			}

			public static void WaitForDebugEvent() { WaitForDebugEvent(true); }

			public static void WaitForDebugEvent(bool Async)
			{
				if (!IsDebugging) return;

				if (Async)
					new Thread(_waitDelegate).Start(true);
				else _waitDelegate(false);
				/*
				 * After a program paused its execution, we'll be able to access its breakpoints and symbol data.
				 * When resuming the program, WaitForDebugEvent() will be called again.
				 * Note that it's not possible to 'wait' on a different thread.
				 */
			}

			public static void InitDebugger()
			{
				#region Attach event handlers
				Engine.Output += delegate(OutputFlags type, string msg)
				{
					if (!GlobalProperties.Instance.VerboseDebugOutput && (type == OutputFlags.Verbose || type == OutputFlags.Normal)) return;

					if (type != OutputFlags.Warning)
						Log( msg.Replace("\n", "\r\n"));
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
					Log("Breakpoint #" + Id.ToString() + " at " + off.ToString() + ": " + exp);
					string fn;
					uint ln;

					if (!Engine.Symbols.GetLineByOffset(off, out fn, out ln))
					{
						Log("No source associated with "+off.ToString());
						return DebugStatus.Break;
					}

					if(GlobalProperties.Instance.VerboseDebugOutput)
						Log(fn + ":" + ln.ToString());

					var ed=EditingManagement.OpenFile(fn) as EditorDocument;
					if (ed != null)
					{
						Log("Unable to move to "+fn+": "+ln);
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
					if (ExceptionType.DException == (ExceptionType)ex.Type)
					{
						msg = ex.Message;
						if (ex.TypeInfo != null)
							msg = ex.TypeInfo + ": " + msg;

						Log(msg);
						
					}
					else
					{
						Log(msg=extype + "-Exception");
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
					Log("Process exited with code " + code.ToString());
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

			public class DebugErrorMarker : TextMarker
			{
				public readonly CodeException Exception;
				public DebugErrorMarker(TextMarkerService svc, CodeException ex)
					:base(svc,svc.Editor.Document.GetOffset((int)ex.SourceLine,0),true)
				{
					this.Exception = ex;
					ToolTip = ex.Message;

					BackgroundColor = Colors.Yellow;
				}
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

				EngineStarting = true;
				StopWaitingForEvents = false;

				DebugCreateProcessOptions opt = new DebugCreateProcessOptions();
				opt.CreateFlags = CreateFlags.DebugOnlyThisProcess | (showConsole ? CreateFlags.CreateNewConsole : 0);
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
					IDEInterface.Log(exe +" not found");
					return null;
				}

				if (ShowConsole)
					CurrentProcess = FileExecution.ExecuteAsync(exe, args, Path.GetDirectoryName(exe), CurrentProcess_Exited);
				else
					CurrentProcess = FileExecution.ExecuteSilentlyAsync(exe, args, Path.GetDirectoryName(exe),
						CurrentProcess_OutputDataReceived, CurrentProcess_ErrorDataReceived, CurrentProcess_Exited);
				return CurrentProcess;
			}

			static void CurrentProcess_Exited()
			{
				IDEInterface.Log("Process exited with code "+CurrentProcess.ExitCode.ToString()+" ("+(CurrentProcess.ExitTime-CurrentProcess.StartTime).ToString()+")");
			}

			static void CurrentProcess_ErrorDataReceived(string Data)
			{
				IDEInterface.Log("Error occured: "+Data);
			}

			static void CurrentProcess_OutputDataReceived(string Data)
			{
				IDEInterface.Log(Data);
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
					Engine.Terminate();
					IsDebugging = false;
				}
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
