using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit;

namespace D_IDE.Core
{
	/*public interface ICodeCompletionSupport
	{
		void BuildToolTip(IEditorDocument EditorDocument,ToolTipRequestArgs ToolTipRequest);
		
		bool IsInsightWindowTrigger(char key);

		object BuildInsightWindowContent(IEditorDocument EditorDocument, string EnteredText);
		
		/// <summary>
		/// Used to check if recently entered text is still part of the completion process or if e.g. the current identifier ended
		/// </summary>
		bool IsIdentifierChar(char key);

		/// <summary>
		/// Used to check if completion is supported at the current caret location
		/// </summary>
		bool CanShowCompletionWindow(IEditorDocument EditorDocument);

		/// <summary>
		/// Create the initially shown completion data.
		/// </summary>
		/// <param name="EnteredText">Can be null. Then, all available members are requested to be shown.</param>
		void BuildCompletionData(IEditorDocument EditorDocument, IList<ICompletionData> CompletionDataList, string EnteredText);
	}*/

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
