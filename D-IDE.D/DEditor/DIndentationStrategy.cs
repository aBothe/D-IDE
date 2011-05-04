using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit.Indentation;
using ICSharpCode.AvalonEdit.Document;
using D_Parser.Core;
using D_Parser;
using System.IO;

namespace D_IDE.D
{
	public class DIndentationStrategy:IIndentationStrategy
	{
		readonly DEditorDocument dEditor;

		public DIndentationStrategy(DEditorDocument DEditorDocument)
		{
			dEditor = DEditorDocument;
		}

		public void IndentLine(TextDocument document, DocumentLine line)
		{
			// Get block level
			var lexer = new DLexer(new StringReader(dEditor.Editor.Text));
			lexer.NextToken();

			string indentString="";
			var loc=dEditor.CaretLocation;

			int level=0;
			while (lexer.LookAhead != null && lexer.LookAhead.Location < loc)
			{
				if (lexer.LookAhead.Kind == DTokens.OpenCurlyBrace)
					level++;
				else if (lexer.LookAhead.Kind == DTokens.CloseCurlyBrace)
					level--;

				lexer.NextToken();
			}

			for (int i = 0; i < level; i++)
				indentString += "\t";

			document.Insert(line.Offset, indentString);
		}

		public void IndentLines(TextDocument document, int beginLine, int endLine)
		{
			
		}
	}
}
