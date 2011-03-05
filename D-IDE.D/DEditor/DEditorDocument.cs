﻿using System;
using System.IO;
using System.Linq;
using D_IDE.Core;
using D_IDE.Core.Controls.Editor;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using Parser.Core;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using System.Windows.Controls;
using D_Parser;
using System.Threading;

namespace D_IDE.D
{
	public class DEditorDocument:EditorDocument
	{
		#region Properties
		IAbstractSyntaxTree _unboundTree;
		public IAbstractSyntaxTree SyntaxTree { 
			get {
				if (HasProject)
				{
					var prj = Project as DProject;
					if(prj!=null)
						return prj.ParsedModules[AbsoluteFilePath];
				}
				return _unboundTree;
			}
			set {
				if(value!=null)
				value.FileName = AbsoluteFilePath;
				if (HasProject)
				{
					var prj = Project as DProject;
					if (prj != null)
						prj.ParsedModules[AbsoluteFilePath]=value;
				}
				_unboundTree=value;
			}
		}

		ToolTip editorToolTip = new ToolTip();
		#endregion

		public DEditorDocument()
		{
			Init();
		}

		public DEditorDocument(string file)
			: base(file)
		{
			Init();
		}

		void Init()
		{
			// Register CodeCompletion events
			Editor.TextArea.TextEntered += new System.Windows.Input.TextCompositionEventHandler(TextArea_TextEntered);
			Editor.TextArea.TextEntering += new System.Windows.Input.TextCompositionEventHandler(TextArea_TextEntering);
			Editor.Document.TextChanged += new EventHandler(Document_TextChanged);
			Editor.TextArea.MouseRightButtonDown += new System.Windows.Input.MouseButtonEventHandler(TextArea_MouseRightButtonDown);
			Editor.MouseHover += new System.Windows.Input.MouseEventHandler(Editor_MouseHover);
			Editor.MouseHoverStopped += new System.Windows.Input.MouseEventHandler(Editor_MouseHoverStopped);

			//TODO: Modify the layout - add two selection combo boxes to the editor view
			// One for selecting types that were declared in the module
			// The second for the type's members

			Parse();
		}

		bool KeysTyped = false;
		Thread parseThread = null;
		void Document_TextChanged(object sender, EventArgs e)
		{
			if (parseThread == null || !parseThread.IsAlive)
			{
				// This thread will continously check if the file was modified.
				// If so, it'll reparse
				parseThread = new Thread(() =>
				{
					Thread.CurrentThread.IsBackground = true;
					while (true)
					{
						// While no keys were typed, do nothing
						while (!KeysTyped)
							Thread.Sleep(50);

						// Reset keystyped state for waiting again
						KeysTyped = false;

						// If a key was typed, wait.
						Thread.Sleep(1500);

						// If no other key was typed after waiting, parse the file
						if (KeysTyped)
							continue;

						// Prevent parsing it again; Assign 'false' to it before parsing the document, so if something was typed while parsing, it'll simply parse again
						KeysTyped = false;

						Parse();
					}
				});
				parseThread.Start();
			}

			KeysTyped = true;
		}

		#region Code Completion

		public string ProposedModuleName
		{
			get {
				return Path.ChangeExtension(RelativeFilePath,null).Replace('\\','.');
			}
		}

		/// <summary>
		/// Parses the current document content
		/// </summary>
		public void Parse()
		{
			Dispatcher.BeginInvoke(new Util.EmptyDelegate(()=>{
				try{
					if (SyntaxTree != null)
						lock (SyntaxTree)
							DParser.UpdateModuleFromText(SyntaxTree, Editor.Text);
					else
						SyntaxTree =DParser.ParseString(Editor.Text);
					SyntaxTree.FileName = AbsoluteFilePath;
					SyntaxTree.ModuleName = ProposedModuleName;
				}catch(Exception ex){ErrorLogger.Log(ex,ErrorType.Warning,ErrorOrigin.System);}
				CoreManager.ErrorManagement.RefreshErrorList();
			}));
		}

		public override System.Collections.Generic.IEnumerable<GenericError> ParserErrors
		{
			get
			{
				if (SyntaxTree != null)
					foreach (var pe in SyntaxTree.ParseErrors)
						yield return new DParseError(pe) { Project=HasProject?Project:null, FileName=AbsoluteFilePath};
			}
		}

		CompletionWindow completionWindow;
		InsightWindow insightWindow;

		void TextArea_TextEntering(object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			if (e.Text.Length > 0 && completionWindow != null)
			{
				// If entered key isn't part of the identifier anymore, close the completion window and insert the item text.
				if (!DCodeCompletionSupport.Instance. IsIdentifierChar(e.Text[0]))
					completionWindow.CompletionList.RequestInsertion(e);
			}
		}

		void TextArea_TextEntered(object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			/*
			 * Note: Once we opened the completion list, it's not needed to care about a later refill of that list.
			 * The completionWindow will search the items that are partly typed into the editor automatically and on its own.
			 * - So there's just an initial filling required.
			 */

			if (!DCodeCompletionSupport.Instance.CanShowCompletionWindow(this) || 
				string.IsNullOrEmpty(e.Text))
				return;
			var ccs = DCodeCompletionSupport.Instance;

			if (completionWindow!=null)
				return;

			completionWindow = new CompletionWindow(Editor.TextArea);
			ccs.BuildCompletionData(this, completionWindow.CompletionList.CompletionData,e.Text);

			// If no data present, return
			if (completionWindow.CompletionList.CompletionData.Count < 1)
			{
				completionWindow = null;
				return;
			}

			completionWindow.Closed += (object o, EventArgs _e) => completionWindow = null; // After the window closed, reset it to null
			completionWindow.Show();
		}
		#endregion

		#region Editor events
		void Editor_TextChanged(object sender, EventArgs e)
		{
			Modified = true;

			// Relocate/Update build errors
			foreach (var m in MarkerStrategy.TextMarkers)
			{
				var bem = m as ErrorMarker;
				if(bem==null)
					continue;

				var nloc=bem.EditorDocument.Editor.Document.GetLocation(bem.StartOffset);
				bem.Error.Line = nloc.Line;
				bem.Error.Column = nloc.Column;
			}
			//CoreManager.Instance.MainWindow.RefreshErrorList();			
		}

		void Document_LineCountChanged(object sender, EventArgs e)
		{
			// Relocate breakpoint positions - when not being in debug mode!
			if (!CoreManager.DebugManagement.IsDebugging)
				foreach (var mk in MarkerStrategy.TextMarkers)
				{
					var bpm = mk as BreakpointMarker;
					if (bpm != null)
					{
						bpm.Breakpoint.Line = Editor.Document.GetLineByOffset(bpm.StartOffset).LineNumber;
					}
				}
		}

		#region Document ToolTips
		void Editor_MouseHoverStopped(object sender, System.Windows.Input.MouseEventArgs e)
		{
			editorToolTip.IsOpen = false;
		}

		void Editor_MouseHover(object sender, System.Windows.Input.MouseEventArgs e)
		{
			var edpos = e.GetPosition(Editor);
			var pos = Editor.GetPositionFromPoint(edpos);
			if (pos.HasValue)
			{
				// Avoid showing a tooltip if the cursor is located after a line-end
				var vpos = Editor.TextArea.TextView.GetVisualPosition(new TextViewPosition(pos.Value.Line, Editor.Document.GetLineByNumber(pos.Value.Line).TotalLength), ICSharpCode.AvalonEdit.Rendering.VisualYPosition.LineMiddle);
				// Add TextView position to Editor-related point
				vpos = Editor.TextArea.TextView.TranslatePoint(vpos, Editor);

				var ttArgs = new ToolTipRequestArgs(edpos.X <= vpos.X, pos.Value);
				try
				{
					DCodeCompletionSupport.Instance.BuildToolTip(this, ttArgs);
				}
				catch (Exception ex)
				{
					ErrorLogger.Log(ex);
					return;
				}

				// If no content present, close and return
				if (ttArgs.ToolTipContent == null)
				{
					editorToolTip.IsOpen = false;
					return;
				}

				editorToolTip.PlacementTarget = this; // required for property inheritance
				editorToolTip.Content = ttArgs.ToolTipContent;
				editorToolTip.IsOpen = true;
				e.Handled = true;
			}
		}
		#endregion

		void TextArea_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			// Pop up context menu
		}
		#endregion
	}

	public class ToolTipRequestArgs
	{
		public ToolTipRequestArgs(bool isDoc, TextViewPosition pos)
		{
			InDocument = isDoc;
			Position = pos;
		}

		public bool InDocument { get; protected set; }
		public TextViewPosition Position { get; protected set; }
		public int Line { get { return Position.Line; } }
		public int Column { get { return Position.Column; } }
		public object ToolTipContent { get; set; }
	}
}