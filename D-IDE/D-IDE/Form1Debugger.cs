using System;
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
using System.Runtime.InteropServices;
using ICSharpCode.TextEditor.Document;
using D_IDE.CodeCompletion;
using Parser.Core;

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

    partial class D_IDEForm
    {
        public class BreakpointHelper
        {
            protected D_IDEForm f;

            public BreakpointHelper(D_IDEForm mainForm)
            {
                f = mainForm;
            }

            /// <summary>
            /// This is the main array for storing Breakpoints
            /// </summary>
            public Dictionary<string, List<DIDEBreakpoint>> Breakpoints = new Dictionary<string, List<DIDEBreakpoint>>();

            public void RetrieveFromCurrentDebugger()
            {
                if (f.IsDebugging)
                {
                    BreakPoint[] bps = f.dbg.Breakpoints;

                    Breakpoints.Clear();

                    foreach (BreakPoint bp in bps)
                    {
                        string file = "";
                        uint line = 0;
                        if (!f.dbg.Symbols.GetLineByOffset(bp.Offset, out file, out line)) continue;

                        if (!Path.IsPathRooted(file) && f.prj!=null)
                            file = f.prj.basedir + "\\" + file;

                        //line++; // Set line to 1-based
                        DIDEBreakpoint dbp = new DIDEBreakpoint(file, (int)line);
                        dbp.bp = bp;

                        if (!Breakpoints.ContainsKey(file))
                            Breakpoints.Add(file, new List<DIDEBreakpoint>());
                        Breakpoints[file].Add(dbp);
                    }
                }
            }

            public void RemoveAll()
            {
                if (f.IsDebugging)
                {
                    foreach (BreakPoint bp in f.dbg.Breakpoints)
                    {
                        f.dbg.RemoveBreakPoint(bp);
                    }
                    Breakpoints.Clear();
                }
            }

            public void InsertAllBreakpoints()
            {
                if (f.IsDebugging)
                {
                    foreach (KeyValuePair<string, List<DIDEBreakpoint>> kv in Breakpoints)
                    {
                        foreach (DIDEBreakpoint dbp in kv.Value)
                        {
                            ulong off = 0;
                            if (!f.dbg.Symbols.GetOffsetByLine(f.prj == null ? dbp.file : f.prj.GetRelFilePath(dbp.file), (uint)dbp.line, out off))
                                continue;

                            dbp.bp = f.dbg.AddBreakPoint(BreakPointOptions.Enabled);
                            dbp.bp.Offset = off;
                        }
                    }
                }
            }

            public bool AddBreakpoint(string file, int line)
            {
                if (IsBreakpointAt(file, line)) return false;

                if (!Breakpoints.ContainsKey(file))
                    Breakpoints.Add(file, new List<DIDEBreakpoint>());

                DIDEBreakpoint dbp = new DIDEBreakpoint(file, line);

                if (f.IsDebugging)
                {
                    ulong off = 0;
                    if (!f.dbg.Symbols.GetOffsetByLine(f.prj == null ? dbp.file : f.prj.GetRelFilePath(dbp.file), (uint)dbp.line, out off))
                        return false;

                    dbp.bp=f.dbg.AddBreakPoint(BreakPointOptions.Enabled);
                    dbp.bp.Offset = off;
                }

                Breakpoints[file].Add(dbp);
                return true;
            }

            public bool RemoveBreakpoint(string file,int line)
            {
                DIDEBreakpoint bp = GetBreakpointAt(file, line);
                if (bp == null) return false;

                if (f.IsDebugging)
                {
                    f.dbg.RemoveBreakPoint(bp.bp);
                }

                Breakpoints[file].Remove(bp);
                return true;
            }

            public DIDEBreakpoint GetBreakpointAt(string file, int line)
            {
                if (!Breakpoints.ContainsKey(file)) return null;

                foreach (DIDEBreakpoint dbp in Breakpoints[file])
                {
                    if (dbp.line == line) return dbp;
                }
                return null;
            }

            public bool IsBreakpointAt(string file, int line)
            {
                return GetBreakpointAt(file,line)!=null;
            }

            public bool ToggleBreakpoint(string file, int line)
            {
                if (IsBreakpointAt(file, line))
                    return RemoveBreakpoint(file, line);
                else return AddBreakpoint(file, line);
            }
        }

        public BreakpointHelper Breakpoints;

        #region Debug menu

        public void RunDebugClick(object sender, EventArgs e)
        {
            ForceExitDebugging();
            bool before = false;
            if (prj != null)
            {
                before = prj.isRelease;
                prj.isRelease = false;
            }
            string targetbin = Build();
            if (!String.IsNullOrEmpty(targetbin))
            {
                Log(ProgressStatusLabel.Text = "Start debugging...");
                UseOutput = true;
                if (Path.GetExtension(targetbin) != ".exe")
                {
                    MessageBox.Show("Unable to execute a non-executable file!");
                    return;
                }

                if (!File.Exists(targetbin))
                {
                    Log("File " + targetbin + " not exists!");
                    return;
                }

                if (!D_IDE_Properties.Default.UseExternalDebugger)
                {
                    Debug(targetbin, sender is string && sender == (object)"untilmain");
                }
                else
                {
                    string dbgbin = D_IDE_Properties.Default.exe_dbg;
                    string dbgargs = D_IDE_Properties.Default.dbg_args.Replace("$exe", targetbin);

                    exeProc = Process.Start(dbgbin, dbgargs);
                    exeProc.Exited += delegate(object se, EventArgs ev)
                    {
                        dbgStopButtonTS.Enabled = false;
                        Log(ProgressStatusLabel.Text = ("Debug process exited with code " + exeProc.ExitCode.ToString()));
                    };
                    exeProc.EnableRaisingEvents = true;
                    dbgStopButtonTS.Enabled = true;
                }
            }
            if (prj != null) prj.isRelease = before;
        }

        public void BuildAndRunClick(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(Build()))
                Run();
        }

        private void RunUntilMainClick(object sender, EventArgs e)
        {
            RunDebugClick("untilmain", EventArgs.Empty);
        }

        public void StepInClick(object sender, EventArgs e)
        {
            if (!IsDebugging) return;
            dbg.Execute("t");
            WaitForEvent();
            StopWaitingForEvents = false;
            GoToCurrentLocation();
        }

        public void StepOverClick(object sender, EventArgs e)
        {
            if (!IsDebugging) return;
            dbg.Execute("p");
            WaitForEvent();
            StopWaitingForEvents = false;
            GoToCurrentLocation();
        }

        public void ToggleBreakPoint(object sender, EventArgs e)
        {
            DocumentInstanceWindow diw = SelectedTabPage;
            if (diw == null) return;

            int line = diw.txt.ActiveTextAreaControl.Caret.Position.Line + 1;
            Breakpoints.ToggleBreakpoint(diw.Module.ModuleFileName, line);

            dbgwin.Update();
            diw.DrawBreakPoints();
        }

        private void RefreshBreakpointsClick(object sender, EventArgs e)
        {
            UpdateBreakPointMarkers();
        }

#endregion

        #region Additional ToolStrip button events

        public void ContinueClick(object sender, EventArgs e)
        {
            if (!IsDebugging)
            {
                RunDebugClick(sender, e);
            }
            else if (dbg != null)
            {
                dbg.Execute("gh");
                WaitForEvent();
            }
        }

        public void PauseClick(object sender, EventArgs e)
        {
            if (dbg == null) return;
            StopWaitingForEvents = true;
            dbg.EndPendingWaits();
            Thread.Sleep(20);
            //dbg.Execute("th");
            GoToCurrentLocation();
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
        }

        public void StopClick(object sender, EventArgs e)
        {
            ForceExitDebugging();
        }

        public void StepOutClick(object sender, EventArgs e)
        {
            if (!IsDebugging) return;
            dbg.Execute("pt");
            WaitForEvent();
            StopWaitingForEvents = false;
            GoToCurrentLocation();
        }

        #endregion

        #region Debugger related stuff
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

        #region Exression evaluation
        public static Type DetermineArrayElementType(string type, out uint size, out bool IsString)
        {
            IsString = false;
            Type t = typeof(int);
            size = 4;
            switch (type)
            {
                default:
                    break;
                case "string":
                case "char":
                    IsString = true;
                    t = typeof(byte);
                    size = 1;
                    break;
                case "wstring":
                case "wchar":
                    IsString = true;
                    t = typeof(ushort);
                    size = 2;
                    break;
                case "dstring":
                case "dchar":
                    IsString = true;
                    t = typeof(uint);
                    size = 4;
                    break;

                case "ubyte":
                    t = typeof(byte); size = 1;
                    break;
                case "ushort":
                    t = typeof(ushort); size = 2;
                    break;
                case "uint":
                    t = typeof(uint); size = 4;
                    break;
                case "int":
                    t = typeof(int); size = 4;
                    break;
                case "short":
                    t = typeof(short); size = 2;
                    break;
                case "byte":
                    t = typeof(sbyte); size = 1;
                    break;
                case "float":
                    t = typeof(float); size = 4;
                    break;
                case "double":
                    t = typeof(double); size = 8;
                    break;
                case "ulong":
                    t = typeof(ulong); size = 8;
                    break;
                case "long":
                    t = typeof(long); size = 8;
                    break;
            }
            return t;
        }

        public object[] ExtractArray(ulong Offset, ITypeDeclaration RawTypeExpression, out bool IsString)
        {
            IsString = false;

            var type=RawTypeExpression;

			int DimCount = 0;
			while(type!=null)
			{
				if(type is ClampDecl)
					DimCount++;

				if (type is NormalDeclaration)
				{
					var id = (type as NormalDeclaration).Name;

					/*
					 * TODO: [w,d]string are aliases! So later it'll be also required to resolve the basic types of them...
					 */

					if (id == "string" || id == "wstring" || id == "dstring")
						DimCount++;
					break;
				}

				type=type.Base;
			}
			if (type == null || DimCount<1)
				return null;
            
            uint elsz = 4;
            Type t = DetermineArrayElementType(type.ToString(), out elsz, out IsString);

            object[] ret = null;
            if(DimCount==1)
                ret = dbg.Symbols.ReadArray(Offset, t, elsz);
            else
            {
                ret = dbg.Symbols.ReadArrayArray(Offset, t, elsz);
            }
            return ret;
        }

        public string BuildArrayContentString(object[] marr, bool IsString)
        {
            string str = "";
            if (marr != null)
            {
                Type t = marr[0].GetType();
                if (IsString && !t.IsArray)
                {
                    try
                    {
                        str = "\"";
                        foreach (object o in marr)
                        {
                            if (o is uint)
                                str += Char.ConvertFromUtf32((int)(uint)o);
                            else if (o is UInt16)
                                str += (char)(ushort)o;
                            else if (o is byte)
                                str += (char)(byte)o;
                        }
                        str += "\"";
                    }
                    catch { str = "[Invalid / Not assigned]"; }

                }
                else
                {
                    str = "{";
                    foreach (object o in marr)
                    {
                        if (t.IsArray)
                            str += BuildArrayContentString((object[])o, IsString) + "; ";
                        else
                            str += o.ToString() + "; ";
                    }
                    str = str.Trim().TrimEnd(';') + "}";
                }
            }
            return str;
        }

        public string BuildArrayContentString(ulong Offset, ITypeDeclaration type)
        {
            bool IsString;
            object[] marr = ExtractArray(Offset, type, out IsString);
            return BuildArrayContentString(marr, IsString);
        }

        public Dictionary<object, object> ExtractAssocArray(ulong Offset, string type)
        {
            Dictionary<object, object> ret = new Dictionary<object, object>();
            //TODO
            return ret;
        }

        public string BuildSymbolValueString(uint ScopedSrcLine, DebugScopedSymbol sym)
        {
            #region Search fitting node
            DocumentInstanceWindow diw = D_IDEForm.SelectedTabPage;

			// Note: Build symbol path to retrieve a symbol node exactly
			var tsym = sym;
			var idList=new List<string>();

			while (tsym != null)
			{
				if (String.IsNullOrEmpty(tsym.Name) || tsym.Name=="*") 
					break;
				// Quite often names with a '.' somewhere inside represent a type within a module
				// - so ignore it since we only want the variable name and it's accessed properties
				if(!tsym.Name.Contains("."))
					idList.Add(tsym.Name);
				tsym = tsym.Parent;
			}

			ITypeDeclaration nodeType = null;
			if (idList.Count == 1)
				nodeType = new NormalDeclaration(idList[0]);
			else if(idList.Count>1)
			{
				nodeType = new IdentifierList();
				idList.Reverse();

				foreach (var s in idList)
					(nodeType as IdentifierList).Add(s);
			}

			if (nodeType != null)
			{
				// Search expression called sym.Name in all superior blocks
				var matches = D_IDECodeResolver.ResolveTypeDeclarations(
					new List<CodeModule> { diw.Module },
					D_IDECodeResolver.SearchBlockAt(diw.Module, new CodeLocation(0, (int)ScopedSrcLine)),
					nodeType);
				if (matches == null || matches.Length < 1)
					return String.Empty;
				var symNode = matches[0];

				if (symNode != null && symNode.Type != null)
				{
					string type = symNode.Type.ToString();
					if (sym.Size == 8 && sym.TextValue.IndexOfAny("`".ToCharArray()) > 0) // If it's an array
					{
						return BuildArrayContentString(sym.Offset, symNode.Type);
					}
					else if (type.IndexOf('[') > 0)
					{
						int i = type.IndexOf('[');
						//TODO
					}
				}
			}
			#endregion
            return sym.TextValue;
        }

        public void RefreshLocals()
        {
            if (!IsDebugging) return;

            DebugScopedSymbol[] locals = dbg.Symbols.ScopeLocalSymbols;

            dbgLocalswin.list.BeginUpdate();

            dbgLocalswin.Clear();

            string fn;
            uint ln;
            ulong off = dbg.CurrentFrame.InstructionOffset;
            if (!dbg.Symbols.GetLineByOffset(off, out fn, out ln))
            {
            }

            foreach (DebugScopedSymbol sym in locals)
            {
                ListViewItem lvi = new ListViewItem();
                string n = "";
                for (int i = 0; i < (int)sym.Depth; i++)
                    n += "   ";
                n += sym.Name;
                lvi.Text = n;
                lvi.Tag = sym;

                lvi.SubItems.Add(BuildSymbolValueString(ln, sym));
                dbgLocalswin.list.Items.Add(lvi);
            }

            dbgLocalswin.list.EndUpdate();
        }
        #endregion

        void GoToCurrentLocation()
        {
            string fn;
            uint ln;
        cont:
            if (!IsDebugging || StopWaitingForEvents) return;

            ulong off = dbg.CurrentFrame.InstructionOffset;
            if (dbg.Symbols.GetLineByOffset(off, out fn, out ln))
                BreakpointWin.NavigateToPosition(fn, (int)ln - 1);
            else
            {
                dbg.WaitForEvent(10);
                if (!StopWaitingForEvents)
                {
                    Application.DoEvents();
                    goto cont;
                }
            }

            RefreshLocals();

            callstackwin.Update();
        }

        /// <summary>
        /// Highlights all breakpoints in all open files
        /// </summary>
        public void UpdateBreakPointMarkers()
        {
            Breakpoints.RetrieveFromCurrentDebugger();

            foreach (DockContent dc in dockPanel.Documents)
            {
                if (!(dc is DocumentInstanceWindow)) continue;
                DocumentInstanceWindow diw = (DocumentInstanceWindow)dc;

                diw.DrawBreakPoints();
            }

            dbgwin.Update();
        }

        #region Debug properties
        internal bool IsInitDebugger = false;
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
        public bool Debug(string exe, bool runUntilMainOnly)
        {
            if (dbg == null) InitDebugger();

            ForceExitDebugging();
            AllDocumentsReadOnly = true;
            IsDebugging = true;

            #region GUI changes when debugging
            //toggleBreakpointToolStripMenuItem.Enabled = false;

            dbgPauseButtonTS.Enabled =
            dbgStopButtonTS.Enabled =
            stepOverToolStripMenuItem.Enabled =
            singleSteptoolStripMenuItem3.Enabled =
            stepInTS.Enabled = stepOutTS.Enabled = stepOverTS.Enabled = true;

            if (D_IDE_Properties.Default.ShowDbgPanelsOnDebugging)
            {
                dbgLocalswin.Show();
                callstackwin.Show();
                dbgwin.Show();
            }

            #endregion

            StopWaitingForEvents = false;

            LoadedModules.Clear();
            output.Clear();

            DebugCreateProcessOptions opt = new DebugCreateProcessOptions();
            opt.CreateFlags = CreateFlags.DebugOnlyThisProcess|(D_IDE_Properties.Default.ShowExternalConsoleWhenExecuting?CreateFlags.CreateNewConsole:0);
            opt.EngCreateFlags = EngCreateFlags.Default;

            dbg.CreateProcessAndAttach(0, exe + (prj != null ? (" " + prj.execargs) : ""), opt, Path.GetDirectoryName(exe), "", 0, 0);

            dbg.Symbols.SourcePath = prj != null ? prj.basedir : Path.GetDirectoryName(exe);
            dbg.IsSourceCodeOrientedStepping = true;

            IsInitDebugger = true;

            dbg.WaitForEvent();
            dbg.Execute("bc"); // Clear breakpoint list
            dbg.WaitForEvent();

            Breakpoints.InsertAllBreakpoints();
            
            IsInitDebugger = false;

            if (runUntilMainOnly) dbg.Execute("g _Dmain");

            WaitForEvent();

            return true;
        }

        public void WaitForEvent()
        {
            if (!IsDebugging) return;
            ProgressStatusLabel.Text = "Debuggee running...";
            WaitResult wr = WaitResult.OK;
            while (IsDebugging && (wr = dbg.WaitForEvent(10)) == WaitResult.TimeOut)
            {
                if (wr == WaitResult.Unexpected) break;
                Application.DoEvents();
            }
            if(wr!=WaitResult.Unexpected)ProgressStatusLabel.Text = "Debuggee broke into debugger...";
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

            dbg.InputRequest += delegate(uint RequestLength)
            {
                InputDlg dlg = new InputDlg();
                dlg.MaxInputLength = RequestLength;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    return dlg.InputString;
                }
                return "";
            };

            dbg.Output += delegate(OutputFlags type, string msg)
            {
                if (!D_IDE_Properties.Default.VerboseDebugOutput && (type == OutputFlags.Verbose || type == OutputFlags.Normal)) return;

                string m = msg.Replace("\n", "\r\n");

                if (type != OutputFlags.Warning)
                    Log(m);
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
                string fn;
                uint ln;

                if (!dbg.Symbols.GetLineByOffset(off, out fn, out ln))
                {
                    Log("No source data found!");
                    return DebugStatus.Break;
                }

                Log(fn + ":" + ln.ToString());
                BreakpointWin.NavigateToPosition(fn, (int)ln - 1);
                callstackwin.Update();

                RefreshLocals();

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
                callstackwin.Update();


                if (ExceptionType.DException == (ExceptionType)ex.Type)
                {
                    if (ex.TypeInfo == null)
                    {
                        Log(ex.Message);
                        AddHighlightedBuildError(ex.SourceFile, (int)ex.SourceLine, ex.Message, Color.OrangeRed);
                    }
                    else
                    {
                        Log(ex.TypeInfo.Name + ": " + ex.Message);
                        AddHighlightedBuildError(ex.SourceFile, (int)ex.SourceLine, ex.TypeInfo.Name + ": " + ex.Message, Color.OrangeRed);
                    }
                }
                else
                {
                    Log("Exception: " + extype + " at " + ex.Address.ToString());
                    AddHighlightedBuildError(ex.SourceFile, (int)ex.SourceLine, "An exception occured: " + extype, Color.OrangeRed);
                }

                RefreshLocals();

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
                AllDocumentsReadOnly = false;
                if (exeProc != null && !exeProc.HasExited)
                {
                    exeProc.Kill();
                }
                try
                {
                    if (dbg != null)
                    {
                        dbg.EndPendingWaits();
                        dbg.Terminate();
                        dbg.MainProcess.Kill();
                        ProgressStatusLabel.Text = "Debuggee terminated";

                    }
                }
                catch { }
                IsDebugging = false;

                toggleBreakpointToolStripMenuItem.Enabled = true;

                stepOverToolStripMenuItem.Enabled =
                singleSteptoolStripMenuItem3.Enabled =
                stepInTS.Enabled = stepOutTS.Enabled = stepOverTS.Enabled =
            dbgPauseButtonTS.Enabled = dbgStopButtonTS.Enabled = false;

                if (D_IDE_Properties.Default.ShowDbgPanelsOnDebugging)
                {
                    dbgLocalswin.Hide();
                    callstackwin.Hide();
                    dbgwin.Hide();
                }
            }
            catch { }

            //callstackwin.Clear();
        }

        #endregion
    }
}
