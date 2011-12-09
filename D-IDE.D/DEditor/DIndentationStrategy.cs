using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit.Indentation;
using ICSharpCode.AvalonEdit.Document;
using D_Parser.Dom;
using D_Parser;
using System.IO;
using D_IDE.Core;
using D_Parser.Dom.Statements;
using D_Parser.Parser;
using System.Collections;
using D_Parser.Formatting;

namespace D_IDE.D
{
	public class DIndentationStrategy : DFormatter, IIndentationStrategy
	{
		readonly DEditorDocument dEditor;
		bool _doBeginUpdateManually = false;

		public DIndentationStrategy(DEditorDocument DEditorDocument)
		{
			dEditor = DEditorDocument;
		}

		public void RawlyIndentLine(int tabsToInsert, TextDocument document, DocumentLine line)
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

		public void UpdateIndentation(string typedText)
		{
			IndentLine(dEditor.Editor.Document, dEditor.Editor.Document.GetLineByNumber(dEditor.Editor.TextArea.Caret.Line),true);
		}

		public void IndentLine(TextDocument document, DocumentLine line)
		{
			IndentLine(document, line, false);
		}

		public void IndentLine(TextDocument document, DocumentLine line, bool TakeCaret)
		{
			if (line.PreviousLine == null)
				return;

			if (!DSettings.Instance.EnableSmartIndentation)
			{
				var prevIndent = GetLineIndentation(document.GetText(line.PreviousLine));

				RawlyIndentLine(prevIndent, document, line);

				return;
			}

			//bool hasPostCaretCurlyCloser = false;

			var offset = TakeCaret?dEditor.CaretOffset: line.Offset;
			/*
			if (document.GetText(line).TrimStart().StartsWith("}") ||(TakeCaret && document.GetCharAt(offset-1)==':'))
				hasPostCaretCurlyCloser = true;
			*/
			var block = CalculateIndentation(document.Text, offset);

			//if (hasPostCaretCurlyCloser)	ind--;

			RawlyIndentLine(block != null ? block.InnerIndentation : 0, document, line);
		}

		public void IndentLines(TextDocument document, int beginLine, int endLine)
		{
			_doBeginUpdateManually = true;
			document.BeginUpdate();
			while (beginLine <= endLine)
			{
				IndentLine(document, document.GetLineByNumber(beginLine));
				beginLine++;
			}
			document.EndUpdate();
			_doBeginUpdateManually = false;
		}
	}
}
