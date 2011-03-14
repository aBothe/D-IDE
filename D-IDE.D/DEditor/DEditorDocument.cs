using System;
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
using System.Collections.Generic;

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
			Editor.TextArea.TextEntering += new System.Windows.Input.TextCompositionEventHandler(TextArea_TextEntering);
			Editor.TextArea.TextEntered += new System.Windows.Input.TextCompositionEventHandler(TextArea_TextEntered);
			Editor.Document.TextChanged += new EventHandler(Document_TextChanged);
			Editor.TextArea.Caret.PositionChanged += new EventHandler(TextArea_SelectionChanged);
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
				if (HasProject)
					return Path.ChangeExtension(RelativeFilePath, null).Replace('\\', '.');
				else 
					return Path.GetFileNameWithoutExtension(FileName);
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

					UpdateBlockCompletionData();
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

		public CodeLocation CaretLocation
		{
			get { return new CodeLocation(Editor.TextArea.Caret.Column,Editor.TextArea.Caret.Line); }
		}

		IBlockNode lastSelectedBlock = null;
		IEnumerable<ICompletionData> currentEnvCompletionData = null;

		public void UpdateBlockCompletionData()
		{
			var curBlock = DCodeResolver.SearchBlockAt(SyntaxTree, CaretLocation);
			if (curBlock != lastSelectedBlock)
			{
				currentEnvCompletionData = null;

				// If different code blocks was selected, 
				// update the list of items that are available in the current scope
				var l = new List<ICompletionData>();
				DCodeCompletionSupport.Instance.BuildCompletionData(this, l, null);
				currentEnvCompletionData = l;
			}
			curBlock = lastSelectedBlock;
		}

		void TextArea_SelectionChanged(object sender, EventArgs e)
		{
			UpdateBlockCompletionData();
		}

		CompletionWindow completionWindow;
		InsightWindow insightWindow;

		void ShowCodeCompletionWindow(string EnteredText)
		{
			try
			{
				if (!DCodeCompletionSupport.Instance.CanShowCompletionWindow(this))
					return;

				/*
				 * Note: Once we opened the completion list, it's not needed to care about a later refill of that list.
				 * The completionWindow will search the items that are partly typed into the editor automatically and on its own.
				 * - So there's just an initial filling required.
				 */

				var ccs = DCodeCompletionSupport.Instance;

				if (completionWindow != null)
					return;

				completionWindow = new CompletionWindow(Editor.TextArea);

				if (string.IsNullOrEmpty(EnteredText))
					foreach (var i in currentEnvCompletionData)
						completionWindow.CompletionList.CompletionData.Add(i);
				else
					ccs.BuildCompletionData(this, completionWindow.CompletionList.CompletionData, EnteredText);

				// If no data present, return
				if (completionWindow.CompletionList.CompletionData.Count < 1)
				{
					completionWindow = null;
					return;
				}

				completionWindow.Closed += (object o, EventArgs _e) => completionWindow = null; // After the window closed, reset it to null
				completionWindow.Show();
			}
			catch (Exception ex) { ErrorLogger.Log(ex); }
		}

		void TextArea_TextEntering(object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			// Return also if there are parser errors - just to prevent crashes
			if (string.IsNullOrWhiteSpace(e.Text) || (SyntaxTree!=null && SyntaxTree.ParseErrors!=null && SyntaxTree.ParseErrors.Count()>0)) return;

			if (completionWindow != null)
			{
				// If entered key isn't part of the identifier anymore, close the completion window and insert the item text.
				if (!DCodeCompletionSupport.Instance. IsIdentifierChar(e.Text[0]))
					completionWindow.CompletionList.RequestInsertion(e);
			}

			// Note: Show completion window even before the first key has been processed by the editor!
			else if(e.Text!=".")
				ShowCodeCompletionWindow(e.Text);
		}

		void TextArea_TextEntered(object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			if (e.Text == ".") // Show the cc window after the dot has been inserted in the text because the cc win would overwrite it anyway
				ShowCodeCompletionWindow(e.Text);
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
