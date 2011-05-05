using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit.Indentation;
using ICSharpCode.AvalonEdit.Document;
using D_Parser.Core;
using D_Parser;
using System.IO;
using D_IDE.Core;

namespace D_IDE.D
{
	public class DIndentationStrategy:IIndentationStrategy
	{
		readonly DEditorDocument dEditor;
		bool _doBeginUpdateManually = false;

		public DIndentationStrategy(DEditorDocument DEditorDocument)
		{
			dEditor = DEditorDocument;
		}

		public void RawlyIndentLine(int tabsToInsert,TextDocument document,DocumentLine line)
		{
			if(!_doBeginUpdateManually)
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
				while (curOff<document.TextLength&& ((curChar = document.GetCharAt(curOff)) == ' ' || curChar == '\t'))
				{
					prevInd++;
					curOff++;
				}

				document.Remove(line.Offset, prevInd);
			}
			
			// 2)
			string indentString ="";
			for (int i = 0; i < tabsToInsert; i++)
				indentString += dEditor.Editor.Options.IndentationString;

			document.Insert(line.Offset, indentString);
			if (!_doBeginUpdateManually)
			document.EndUpdate();
		}

		public int GetLineTabIndentation(string lineText)
		{
			int ret = 0;
			
			foreach(var c in lineText)
			{
				if(c==' ' || c=='\t')
					ret++;
				else 
					break;
			}

			return ret;
		}

		public void UpdateIndentation(string typedText)
		{
			// if both typed { or }, remove one tab
			if (typedText == "{" || typedText == "}")
			{
				var document = dEditor.Editor.Document;
				var line = dEditor.Editor.Document.GetLineByOffset(dEditor.Editor.CaretOffset);
				var lineText = document.GetText(line);

				// Check if nothing else stands in front of the { or } on 'line'
				if (!lineText.TrimStart().StartsWith(typedText))
					return;

				var prevLineText = line.PreviousLine != null ? document.GetText(line.PreviousLine) : "";

				// New indentation is that of the previous line - 1
				int newInd = GetLineTabIndentation(prevLineText)-1;
				RawlyIndentLine(newInd, document, line);
			}
		}

		public void IndentLine(TextDocument document, DocumentLine line)
		{
			/*
			 * Way of solving this:
			 *	1) Get the indentation of the previous line
			 *	2) If the prev line NOT ends with a semicolon, add an additional tab EXCEPT when a { was typed
			 *	3) When a } was typed, remove one tab
			 *	4) Insert calculated indentation
			 *	
			 * Note: Primarily, this method is fired only when a new line is about to be created - so for better indentation, firing after (special) keys were typed is needed!
			 */
			if (line.PreviousLine == null)
				return;

			string lineText = null;

			var prevLineText = document.GetText(line.PreviousLine);

			var caretChar = 
				_doBeginUpdateManually? // When doing a multi-line reformat, take the first non-ws line character instead of the character beneath the caret
				(!string.IsNullOrWhiteSpace(lineText=document.GetText(line))?lineText.TrimStart()[0]:'\0'):

				(dEditor.Editor.CaretOffset>=document.TextLength?'\0':document.GetCharAt(dEditor.Editor.CaretOffset));

			// 1)
			int prevInd = GetLineTabIndentation(prevLineText);

			// 2)
			prevLineText=prevLineText.TrimEnd();
			if (!string.IsNullOrWhiteSpace(prevLineText) && !prevLineText.EndsWith("}") && prevLineText.EndsWith("{") && caretChar!='{')
			{
				prevInd++;
			}

			// 3)
			if (caretChar == '}')
				prevInd--;

			// 4)
			RawlyIndentLine(prevInd, document, line);
		}

		public void IndentLines(TextDocument document, int beginLine, int endLine)
		{
			_doBeginUpdateManually = true;
			document.BeginUpdate();
			while(beginLine<=endLine)
			{
				IndentLine(document,document.GetLineByNumber(beginLine));
				beginLine++;
			}
			document.EndUpdate();
			_doBeginUpdateManually = false;
		}
	}
}
