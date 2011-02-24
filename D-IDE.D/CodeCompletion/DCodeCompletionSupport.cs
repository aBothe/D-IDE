using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using ICSharpCode.AvalonEdit.CodeCompletion;
using Parser.Core;
using D_Parser;

namespace D_IDE.D
{
	public class DCodeCompletionSupport
	{
		public static DCodeCompletionSupport Instance = new DCodeCompletionSupport();

		public bool IsIdentifierChar(char key)
		{
			return char.IsLetterOrDigit(key) || key == '_';
		}

		public bool CanShowCompletionWindow(DEditorDocument EditorDocument)
		{
			return false; // While cc isn't available, disable cc functionality
		}

		public void BuildCompletionData(DEditorDocument EditorDocument, IList<ICSharpCode.AvalonEdit.CodeCompletion.ICompletionData> l, string EnteredText)
		{
			if (EnteredText == ".")
			{
				l.Add(new DCompletionData( new DVariable() { Name="myVar", Description="A description for myVar"}));
			}
		}

		public void BuildToolTip(DEditorDocument EditorDocument, ToolTipRequestArgs ToolTipRequest)
		{
			if (!ToolTipRequest.InDocument) 
				return;

			ToolTipRequest.ToolTipContent = "A tool tip";
		}

		public bool IsInsightWindowTrigger(char key)
		{
			return key == '(' || key==',';
		}
	}

	public class DCompletionData : ICompletionData
	{
		public DCompletionData(INode n)
		{
			Node = n;
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
			get { return Node.Description; }
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
			get { return Node.Name; }
			protected set { }
		}
	}
}
