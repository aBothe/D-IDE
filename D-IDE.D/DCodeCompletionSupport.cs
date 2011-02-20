using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using ICSharpCode.AvalonEdit.CodeCompletion;
using Parser.Core;

namespace D_IDE.D
{
	public class DCodeCompletionSupport:ICodeCompletionSupport
	{
		public bool IsIdentifierChar(char key)
		{
			return char.IsLetterOrDigit(key) || key == '_';
		}

		public bool CanShowCompletionWindow(IEditorDocument EditorDocument)
		{
			return true;
		}

		public void BuildCompletionData(IEditorDocument EditorDocument, IList<ICSharpCode.AvalonEdit.CodeCompletion.ICompletionData> l, string EnteredText)
		{
			if (EnteredText == ".")
			{
				l.Add(new DCompletionData("aaa"));
				l.Add(new DCompletionData("bab"));
				l.Add(new DCompletionData("cca"));
				l.Add(new DCompletionData("ddd"));
			}
		}

		public void BuildToolTip(IEditorDocument EditorDocument, ToolTipRequestArgs ToolTipRequest)
		{
			if (!ToolTipRequest.InDocument) return;

			ToolTipRequest.ToolTipContent = "A tool tip";
		}

		public bool IsInsightWindowTrigger(char key)
		{
			return key == '(' || key==',';
		}
	}

	public class DCompletionData : ICompletionData
	{
		public DCompletionData(string text)
		{
			Text = text;
		}

		public INode Node { get; protected set; }

		public void Complete(ICSharpCode.AvalonEdit.Editing.TextArea textArea, ICSharpCode.AvalonEdit.Document.ISegment completionSegment, EventArgs insertionRequestEventArgs)
		{
			textArea.Document.Replace(completionSegment, Text);
		}

		public object Content
		{
			get { return Text; }
		}

		public object Description
		{
			get { return "Description for "+Text; }
		}

		public System.Windows.Media.ImageSource Image
		{
			get { return null; }
		}

		public double Priority
		{
			get { return 1; }
		}

		public string Text
		{
			get;
			protected set;
		}
	}
}
