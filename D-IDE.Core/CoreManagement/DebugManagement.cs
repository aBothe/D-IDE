using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using DebugEngineWrapper;
using D_IDE.Core.Controls.Editor;

namespace D_IDE.Core
{
	public abstract partial class CoreManager
	{
		public class DebugManagement
		{
			public static DBGEngine Engine=new DBGEngine();

			public static bool IsDebugging
			{
				get;
				protected set;
			}

			public class DebugErrorMarker : TextMarker
			{
				public readonly CodeException Exception;
				public DebugErrorMarker(TextMarkerService svc, CodeException ex)
					: base(svc, svc.Editor.Document.GetOffset((int)ex.SourceLine, 0), true)
				{
					this.Exception = ex;
					ToolTip = ex.Message;

					BackgroundColor = Colors.Yellow;
				}
			}

			public class DebugStackFrameMarker : TextMarker
			{
				public readonly DebugEngineWrapper.StackFrame Frame;

				public static DebugStackFrameMarker Create(TextMarkerService svc, DebugEngineWrapper.StackFrame frm)
				{
					string fn;
					uint ln;
					ulong off = frm.InstructionOffset;
					if (Engine.Symbols.GetLineByOffset(off, out fn, out ln))
					{
						var text_off = svc.Editor.Document.GetOffset((int)ln, 0);

						var m = new DebugStackFrameMarker(svc, frm, text_off);
						m.Redraw();

						return m;
					}
					return null;
				}

				public DebugStackFrameMarker(TextMarkerService svc, DebugEngineWrapper.StackFrame frm, int text_offset)
					: base(svc, text_offset, true)
				{
					Frame = frm;

					if (frm.FrameNumber < 1)
						BackgroundColor = Colors.Yellow;
					else
						BackgroundColor = Colors.Green;
					MarkerType = TextMarkerType.None;
				}
			}

			public static void RefreshAllDebugMarkers()
			{
				foreach (var aed in CoreManager.Instance.Editors)
				{
					var ed = aed as EditorDocument;
					if (ed==null)
						continue;

					ed.RefreshDebugHighlightings();
				}
			}

			static GenericDebugSupport genDbgSupport;
			static GenericDebugSupport curDbgSupport;
			/// <summary>
			/// Debug suppport for the recently built project/module.
			/// Note: This value will be set when building a project/module.
			/// </summary>
			/// <returns></returns>
			public static GenericDebugSupport CurrentDebugSupport
			{
				set {
					curDbgSupport = value;
				}
				get
				{
					if (curDbgSupport==null)
					{
						if (genDbgSupport == null)
							genDbgSupport = new GenericDebugSupport();
						return genDbgSupport;
					}

					return curDbgSupport;
				}
			}
		}

		/// <summary>
		/// Logs a debug message
		/// </summary>
		protected static void Log(string Message,ErrorType ErrType)
		{
			ErrorLogger.Log(Message, ErrType, ErrorOrigin.Debug);
		}

		public class BreakpointManagement
		{
			public static readonly List<BreakpointWrapper> Breakpoints = new List<BreakpointWrapper>();
			
			/// <summary>
			/// Tries to insert all breakpoints into the recently started debugger
			/// </summary>
			public static void SetupBreakpoints()
			{
				if (!DebugManagement.IsDebugging)
					return;

				foreach (var bp in Breakpoints)
				{
					ulong off = 0;
					if (!DebugManagement.Engine.Symbols.GetOffsetByLine(bp.File, (uint)bp.Line, out off))
						continue;

					bp.Breakpoint = DebugManagement.Engine.AddBreakPoint(BreakPointOptions.Enabled);
					bp.Breakpoint.Offset = off;
				}
			}

			/// <summary>
			/// Clears local breakpoint array and fills it using the 'Online' Breakpoint array of the Engine
			/// </summary>
			public static void RetrieveBreakpointsFromEngine()
			{
				if (!DebugManagement.IsDebugging)
					return;

				Breakpoints.Clear();

				var bps = DebugManagement.Engine.Breakpoints;

				foreach (var bp in bps)
				{
					var bpw = new BreakpointWrapper() { Breakpoint = bp };

					uint line = 0;
					string file = null;
					DebugManagement.Engine.Symbols.GetLineByOffset(bp.Offset, out file, out line);

					bpw.File = file;
					bpw.Line = (int)line+1;

					Breakpoints.Add(bpw);
				}
			}

			public static void RemoveAll()
			{
				Breakpoints.Clear();

				if (DebugManagement.IsDebugging)
					foreach (var bp in DebugManagement.Engine.Breakpoints)
						DebugManagement.Engine.RemoveBreakPoint(bp);
			}

			/// <summary>
			/// Tries to add a break point. If insertion fails, null will be returned.
			/// </summary>
			public static BreakpointWrapper AddBreakpoint(string file, int line)
			{
				var tbp = GetBreakpointAt(file, line);
				if (tbp != null)
					return tbp;

				tbp = new BreakpointWrapper();
				tbp.File = file;
				tbp.Line = line;

				if (DebugManagement.IsDebugging)
				{
					ulong off = 0;
					if (!DebugManagement.Engine.Symbols.GetOffsetByLine(file, (uint)line, out off))
						return null;

					var bp = DebugManagement.Engine.AddBreakPoint(BreakPointOptions.Enabled);
					bp.Offset = off;

					tbp.Breakpoint = bp;
				}

				Breakpoints.Add(tbp);
				return tbp;
			}

			public static void RemoveBreakpoint(string file, int line)
			{
				RemoveBreakpoint(GetBreakpointAt(file, line));
			}

			public static void RemoveBreakpoint(BreakpointWrapper bpw)
			{
				if (bpw == null)
					return;

				if (Breakpoints.Contains(bpw))
					Breakpoints.Remove(bpw);

				if (DebugManagement.IsDebugging && bpw.IsExisting)
					DebugManagement.Engine.RemoveBreakPoint(bpw.Breakpoint);

				bpw.Breakpoint = null;
			}

			public static void ToggleBreakpoint(string file, int line)
			{
				var bpw = GetBreakpointAt(file, line);
				if (bpw != null)
					RemoveBreakpoint(bpw);
				else AddBreakpoint(file, line);
			}

			public static BreakpointWrapper GetBreakpointAt(string file, int line)
			{
				try
				{
					return Breakpoints.First(bp => bp.File == file && bp.Line == line);
				}
				catch { return null; }
			}

			public static IEnumerable<BreakpointWrapper> GetBreakpointsAt(string file)
			{
				if (Breakpoints.Count < 1)
					return null;
				return Breakpoints.Where(bpw=>bpw.File==file);
			}

			public static bool IsBreakpointAt(string file, int line)
			{
				return GetBreakpointAt(file, line) != null;
			}
		}
	}

	public class BreakpointWrapper
	{
		/// <summary>
		/// True if breakpoint is bound to the host debugger
		/// </summary>
		public bool IsExisting
		{
			get{
				return Breakpoint!=null && Breakpoint.Offset!=0;
			}
		}
		public BreakPoint Breakpoint { get; set; }
		public string File { get; set; }
		/// <summary>
		/// 0-based line number
		/// </summary>
		public int Line { get; set; }
	}
}
