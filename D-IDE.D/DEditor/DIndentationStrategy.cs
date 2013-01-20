using System;
using D_IDE.Core;
using D_Parser.Dom;
using D_Parser.Formatting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Indentation;

namespace D_IDE.D
{
	public class DIndentationStrategy : IIndentationStrategy
	{
		readonly DEditorDocument dEditor;
		bool _doBeginUpdateManually = false;

		public DIndentationStrategy(DEditorDocument DEditorDocument)
		{
			dEditor = DEditorDocument;
		}

		public void RawlyIndentLine(int tabsToInsert, ICSharpCode.AvalonEdit.Document.TextDocument document, DocumentLine line)
		{
			if (!_doBeginUpdateManually)
				document.BeginUpdate();
			/*
			 * 1) Remove old indentation
			 * 2) Insert new one
			 */

			// 1)
			int prevInd = 0;
			int curOff = line.Offset;
			if (curOff < document.TextLength)
			{
				char curChar = '\0';
				while (curOff < document.TextLength && ((curChar = document.GetCharAt(curOff)) == ' ' || curChar == '\t'))
				{
					prevInd++;
					curOff++;
				}

				document.Remove(line.Offset, prevInd);
			}

			// 2)
			string indentString = "";
			for (int i = 0; i < tabsToInsert; i++)
				indentString += dEditor.Editor.Options.IndentationString;

			document.Insert(line.Offset, indentString);
			if (!_doBeginUpdateManually)
				document.EndUpdate();
		}
		
		public void RawlyIndentLine(string indentString, ICSharpCode.AvalonEdit.Document.TextDocument document, DocumentLine line)
		{
			if (!_doBeginUpdateManually)
				document.BeginUpdate();

			// 1)
			int prevInd = 0;
			int curOff = line.Offset;
			if (curOff < document.TextLength)
			{
				char curChar = '\0';
				while (curOff < document.TextLength && ((curChar = document.GetCharAt(curOff)) == ' ' || curChar == '\t'))
				{
					prevInd++;
					curOff++;
				}

				document.Remove(line.Offset, prevInd);
			}

			document.Insert(line.Offset, indentString);
			if (!_doBeginUpdateManually)
				document.EndUpdate();
		}

		public void UpdateIndentation(string typedText)
		{
			IndentLine(dEditor.Editor.Document, dEditor.Editor.Document.GetLineByNumber(dEditor.Editor.TextArea.Caret.Line),true);
		}

		public void IndentLine(ICSharpCode.AvalonEdit.Document.TextDocument document, DocumentLine line)
		{
			IndentLine(document, line, false);
		}

		public void IndentLine(ICSharpCode.AvalonEdit.Document.TextDocument document, DocumentLine line, bool TakeCaret)
		{
			if (line.PreviousLine == null)
				return;

			if (!DSettings.Instance.EnableSmartIndentation)
			{
				var t = document.GetText(line);
				int c=0;
				for(;c<t.Length && (t[c] == ' ' || t[c] == '\t');c++);

				RawlyIndentLine(t.Length==0 ? string.Empty : t.Substring(0, c+1), document, line);

				return;
			}

			var tr=document.CreateReader();
			var newIndent = D_Parser.Formatting.Indent.IndentEngineWrapper.CalculateIndent(tr, line.LineNumber, dEditor.Editor.Options.ConvertTabsToSpaces, dEditor.Editor.Options.IndentationSize);
			tr.Close();

			RawlyIndentLine(newIndent, document, line);
		}
		
		public void FormatDocument(DFormattingOptions policy = null)
		{
			policy = policy ?? DFormattingOptions.CreateDStandard();
			
			var formatter = new DFormattingVisitor(policy, new DocAdapter(dEditor.Editor.Document), dEditor.SyntaxTree);
			
			formatter.WalkThroughAst();
			
			dEditor.Editor.Document.UndoStack.StartUndoGroup();
			try
			{
				formatter.ApplyChanges(dEditor.Editor.Document.Replace);
			}
			catch(Exception ex)
			{
				ErrorLogger.Log(ex, ErrorType.Error, ErrorOrigin.Parser);
			}
			dEditor.Editor.Document.UndoStack.EndUndoGroup();
		}
		
		class DocAdapter : IDocumentAdapter
		{
			ICSharpCode.AvalonEdit.Document.TextDocument document;
			
			public DocAdapter(ICSharpCode.AvalonEdit.Document.TextDocument doc)
			{
				this.document = doc;
			}
			
			public char this[int o] {
				get {
					return document.GetCharAt(o);
				}
			}
			
			public int TextLength {
				get {
					return document.TextLength;
				}
			}
			
			public int LineCount {
				get {
					return document.LineCount;
				}
			}
			
			public string Text {
				get {
					return document.Text;
				}
			}
			
			public int ToOffset(CodeLocation loc)
			{
				return document.GetOffset(loc.Line, loc.Column);
			}
			
			public int ToOffset(int line, int column)
			{
				return document.GetOffset(line, column);
			}
			
			public D_Parser.Dom.CodeLocation ToLocation(int offset)
			{
				var dl = document.GetLocation(offset);
				return new CodeLocation(dl.Column, dl.Line);
			}
		}
		
		public void IndentLines(ICSharpCode.AvalonEdit.Document.TextDocument document, int beginLine, int endLine)
		{
			
		}
	}
}
