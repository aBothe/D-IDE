using D_Parser.Formatting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Indentation;

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
				var prevIndent = ReadRawLineIndentation(document.GetText(line));

				RawlyIndentLine(prevIndent, document, line);

				return;
			}

			var tr=document.CreateReader();
			var block = CalculateIndentation(tr, line.LineNumber);
			tr.Close();

			RawlyIndentLine(block != null ? block.GetLineIndentation(line.LineNumber) : 0, document, line);
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
