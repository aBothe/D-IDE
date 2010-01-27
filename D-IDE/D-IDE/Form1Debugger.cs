﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using DebugEngineWrapper;
using D_Parser;
using WeifenLuo.WinFormsUI.Docking;
using System.Drawing;
using System.Diagnostics;
using System.Threading;

namespace D_IDE
{
	public class DIDEBreakpoint
	{
		public DIDEBreakpoint(string fn, int ln)
		{
			file = fn;
			line = ln;
		}
		public string file;
		public int line;
		public BreakPoint bp;
	}

	partial class Form1
	{
		private void RunDebugClick(object sender, EventArgs e)
		{
			ForceExitDebugging();
			if (Build())
			{
				Log(ProgressStatusLabel.Text = "Start debugging...");
				UseOutput = true;
				if (prj == null)
				{
					MessageBox.Show("Create project first!");
					return;
				}
				if (prj.type != DProject.PrjType.ConsoleApp && prj.type != DProject.PrjType.WindowsApp)
				{
					MessageBox.Show("Unable to execute a library!");
					return;
				}

				string bin = prj.basedir + "\\" + Path.ChangeExtension(prj.targetfilename, null) + ".exe";

				if (!File.Exists(bin))
				{
					Log("File " + bin + " not exists!");
					return;
				}

				if (!D_IDE_Properties.Default.UseExternalDebugger)
				{
					Debug(bin);
				}
				else
				{
					string dbgbin = D_IDE_Properties.Default.exe_dbg;
					string dbgargs = D_IDE_Properties.Default.dbg_args.Replace("$exe",bin);

					exeProc = Process.Start(dbgbin, dbgargs);
					exeProc.Exited += delegate(object se,EventArgs ev)
					{
						dbgStopButtonTS.Enabled = false;
						Log(ProgressStatusLabel.Text = ("Debug process exited with code " + exeProc.ExitCode.ToString()));
					};
					exeProc.EnableRaisingEvents = true;
					dbgStopButtonTS.Enabled = true;
				}
			}
		}

		private void dbgContinueClick(object sender, EventArgs e)
		{
			if (!IsDebugging)
			{
				RunDebugClick(sender, e);
			}
			else if(dbg!=null)
			{
				dbg.Execute("gh");
				WaitForEvent();
			}
		}

		private void dbgPauseButtonTS_Click(object sender, EventArgs e)
		{
			if (dbg == null) return;
			StopWaitingForEvents = true;
			dbg.EndPendingWaits();
			dbg.Execute("th");
			//WaitForEvent();
			/*dbg.ExecutionStatus = DebugStatus.Break;
			dbg.InterruptTimeOut = 0;
			dbg.Interrupt();
			while (dbg.IsInterruptRequested)
			{
				Log("Interrupt waiting...");
				Application.DoEvents();
			}*/
			//dbg.Execute("~n"); // Suspend program's main thread
			//dbg.WaitForEvent();
			/*dbg.ExecutionStatus = DebugStatus.Break;
			dbg.Interrupt();
			dbg.WaitForEvent(100);*/

			string fn;
			uint ln;

			Log(dbg.CurrentInstructionOffset.ToString());
			if (dbg.Symbols.GetLineByOffset(dbg.CurrentInstructionOffset, out fn, out ln))
				BreakpointWin.NavigateToPosition(fn, (int)ln - 1);
		}

		private void dbgStopButtonTS_Click(object sender, EventArgs e)
		{
			ForceExitDebugging();
		}

		private void toggleBreakpointToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (IsDebugging) return;

			DocumentInstanceWindow diw = SelectedTabPage;
			if (diw != null)
			{
				int line = diw.txt.ActiveTextAreaControl.Caret.Position.Line + 1;

				if (!dbgwin.Remove(diw.fileData.mod_file, line))
					dbgwin.AddBreakpoint(diw.fileData.mod_file, line);
			}
		}

		private void InitDebugger()
		{
			try
			{
				dbg = new DBGEngine();
				AttachDebugHandlers();
				dbg.Execute(".lines -e"); // Enable source code locating
			}
			catch (Exception ex)
			{
				Log(ex.Message);
			}
		}

		private void stepIn_Click(object sender, EventArgs e)
		{
			if (!IsDebugging) return;
			dbg.Execute("t");
			WaitForEvent();

			GoToCurrentLocation();
		}

		private void stepOver_Click(object sender, EventArgs e)
		{
			if (!IsDebugging) return;
			dbg.Execute("p");
			WaitForEvent();

			GoToCurrentLocation();
		}

		private void stepOutTS_Click(object sender, EventArgs e)
		{
			if (!IsDebugging) return;
			dbg.Execute("gu");
			WaitForEvent();

			GoToCurrentLocation();
		}

		void GoToCurrentLocation()
		{
			if (!IsDebugging) return;
			string fn;
			uint ln;
			if (dbg.Symbols.GetLineByOffset(dbg.CurrentInstructionOffset, out fn, out ln))
				BreakpointWin.NavigateToPosition(fn, (int)ln - 1);
		}

		#region Debug properties
		internal bool IsInitDebugger=false;
		public DBGEngine dbg;
		public Dictionary<string, ulong> LoadedModules = new Dictionary<string, ulong>();
		internal bool StopWaitingForEvents;
		#endregion

		public bool IsDebugging;/*{
			get
			{
				try{
				return dbg != null && !dbg.MainProcess.HasExited;
				}catch{return false;}
			}
		}*/

		/// <summary>
		/// Main function for starting debugging
		/// </summary>
		/// <param name="exe"></param>
		/// <returns></returns>
		public bool Debug(string exe)
		{
			if (dbg == null) InitDebugger();
			
			ForceExitDebugging();

			IsDebugging = true;

			Log(ProgressStatusLabel.Text = "Launch internal debugger engine...");

			toggleBreakpointToolStripMenuItem.Enabled = false;

			dbgPauseButtonTS.Enabled =
			dbgStopButtonTS.Enabled =
			stepOverToolStripMenuItem.Enabled =
			singleSteptoolStripMenuItem3.Enabled =
			stepInTS.Enabled = stepOutTS.Enabled = stepOverTS.Enabled = true;

			StopWaitingForEvents = false;

			LoadedModules.Clear();
			output.Clear();

			DebugCreateProcessOptions opt = new DebugCreateProcessOptions();
			opt.CreateFlags = CreateFlags.DebugOnlyThisProcess;
			opt.EngCreateFlags = EngCreateFlags.Default;

			dbg.CreateProcessAndAttach(0, exe + " " + prj.execargs, opt, Path.GetDirectoryName(exe), "", 0, 0);
			dbg.Symbols.SourcePath = prj.basedir;

			IsInitDebugger = true;

			dbg.WaitForEvent();

			dbg.Execute("bc"); // Clear breakpoint list

			dbg.WaitForEvent();
			//Log("Basedir: " + prj.basedir);
			//dbg.Execute("l+s");
			

			foreach (KeyValuePair<string, List<DIDEBreakpoint>> kv in dbgwin.Breakpoints)
			{
				foreach (DIDEBreakpoint dbp in kv.Value)
				{
					ulong off = 0;
					if (!dbg.Symbols.GetOffsetByLine(dbp.file, (uint)dbp.line, out off))
					{
						continue;
					}
					dbp.bp = dbg.AddBreakPoint(BreakPointOptions.Enabled);
					dbp.bp.Offset = off;
				}
			}

			IsInitDebugger = false;

			WaitForEvent();

			return true;
		}

		public void WaitForEvent()
		{
			if (!IsDebugging) return;
			ProgressStatusLabel.Text = "Debuggee running...";
			WaitResult wr=WaitResult.OK;
			while (IsDebugging && (wr = dbg.WaitForEvent(10)) == WaitResult.TimeOut)
			{
				if (wr == WaitResult.Unexpected) break;
				Application.DoEvents();
			}
			ProgressStatusLabel.Text = "Debuggee broke into debugger...";
		}

		public bool AllDocumentsReadOnly
		{
			set
			{
				foreach (DockContent tp in dockPanel.Documents)
				{
					if (tp is DocumentInstanceWindow)
					{
						DocumentInstanceWindow mtp = (DocumentInstanceWindow)tp;
						mtp.txt.IsReadOnly = value;
					}
				}
			}
		}

		void AttachDebugHandlers()
		{
			if (dbg == null) return;

			dbg.Output += delegate(OutputFlags type, string msg)
			{
				if (type != OutputFlags.Warning)
					Log(msg.Replace("\n", "\r\n"));
			};

			dbg.OnLoadModule += delegate(ulong BaseOffset, uint ModuleSize, string ModuleName, uint Checksum, uint Timestamp)
			{
				LoadedModules.Add(ModuleName, BaseOffset);
				if (IsInitDebugger)
					return DebugStatus.Break;
				return DebugStatus.NoChange;
			};

			dbg.OnUnloadModule += delegate(ulong BaseOffset, string ModuleName)
			{
				LoadedModules.Remove(ModuleName);
				return DebugStatus.NoChange;
			};

			dbg.OnBreakPoint += delegate(uint Id, string cmd, ulong off, string exp)
			{
				StopWaitingForEvents = true;
				BreakPoint bp = dbg.GetBreakPointById(Id);
				Log("Breakpoint #" + Id.ToString() + " at " + off.ToString() + ": " + exp);
				callstackwin.Update();
				string fn;
				uint ln;

				if (!dbg.Symbols.GetLineByOffset(off, out fn, out ln))
				{
					Log("No source data found!");
					return DebugStatus.Break;
				}

				Log(fn + ":" + ln.ToString());
				BreakpointWin.NavigateToPosition(fn, (int)ln - 1);

				return DebugStatus.Break;
			};

			dbg.OnException += delegate(CodeException ex)
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
				Log("Exception: " + extype + " at " + ex.Address.ToString());
				callstackwin.Update();
				string fn;
				uint ln;
				if (dbg.Symbols.GetLineByOffset(ex.Address, out fn, out ln))
				{
					AddHighlightedBuildError(fn, (int)ln, "An exception occured: " + extype, Color.OrangeRed);
				}

				return DebugStatus.Break;
			};

			dbg.OnExitProcess += delegate(uint code)
			{
				Log("Process exited with code " + code.ToString());
				ForceExitDebugging();
				return DebugStatus.NoChange;
			};

			dbg.OnSessionStatusChanged += delegate(SessionStatus ss)
			{
				Log("Session status changed to " + ss.ToString());
				//if (ss == SessionStatus.Active)	return DebugStatus.Break;
				return DebugStatus.NoChange;
			};
		}

		public void ForceExitDebugging()
		{
			try
			{
				if (exeProc != null && !exeProc.HasExited)
				{
					exeProc.Kill();
				}

				if (dbg != null)
				{
					try
					{
						dbg.EndPendingWaits();
						dbg.Terminate();
						dbg.MainProcess.Kill();
					}
					catch { }
				}

				IsDebugging = false;

				toggleBreakpointToolStripMenuItem.Enabled = true;

				stepOverToolStripMenuItem.Enabled =
				singleSteptoolStripMenuItem3.Enabled =
				stepInTS.Enabled = stepOutTS.Enabled = stepOverTS.Enabled =
			dbgPauseButtonTS.Enabled = dbgStopButtonTS.Enabled = false;

			}
			catch { }

			//callstackwin.Clear();
		}
	}
}