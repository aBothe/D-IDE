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
			Editor.TextArea.MouseRightButtonDown += new System.Windows.Input.MouseButtonEventHandler(TextArea_MouseRightButtonDown);
			Editor.MouseHover += new System.Windows.Input.MouseEventHandler(Editor_MouseHover);
			Editor.MouseHoverStopped += new System.Windows.Input.MouseEventHandler(Editor_MouseHoverStopped);

			//TODO: Modify the layout - add two selection combo boxes to the editor view
			// One for selecting types that were declared in the module
			// The second for the type's members
		}

		#region Code Completion
		
		/// <summary>
		/// Parses the current document content
		/// </summary>
		public void Parse()
		{
			this.SyntaxTree=DParser.ParseString(Editor.Text);
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
}
