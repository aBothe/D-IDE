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

		public class RawDScanner
		{
			public class CodeBlock
			{
				public int InitialToken;
				public CodeLocation StartLocation;
				//public CodeLocation EndLocation;

				public bool IsSingleLineIndentation = false;
				//public bool IsWaitingForSemiColon = false;

				public bool IsNonClampBlock
				{
					get { return InitialToken != DTokens.OpenParenthesis && InitialToken != DTokens.OpenSquareBracket && InitialToken != DTokens.OpenCurlyBrace; }
				}

				/// <summary>
				/// The last token before this CodeBlock started.
				/// </summary>
				public DToken LastPreBlockIdentifier;

				/// <summary>
				/// Last token found within current block.
				/// </summary>
				public DToken LastToken;

				public CodeBlock Parent;

				public int OuterIndentation
				{
					get {
						if (Parent != null)
							return Parent.OuterIndentation + 1;

						return 0;
					}
				}

				public int InnerIndentation
				{
					get {
						return OuterIndentation + 1;			
					}
				}
			}

			static bool IsPreStatementToken(int t)
			{
				return
					t==DTokens.Version ||
					t==DTokens.Debug ||
					t == DTokens.If ||
					t == DTokens.While ||
					t == DTokens.For ||
					t == DTokens.Foreach ||
					t == DTokens.Foreach_Reverse ||
					t == DTokens.Synchronized;
			}

			public static CodeBlock CalculateIndentation(string code, int offset)
			{
				if (offset >= code.Length)
					offset = code.Length-1;

				CodeBlock block = null;

				var lex = new Lexer(new StringReader(code.Substring(0, offset)));

				lex.NextToken();

				while(!lex.IsEOF)
				{
					lex.NextToken();

					var t=lex.CurrentToken;
					var la=lex.LookAhead;

					if (t != null && (t.Kind == DTokens.Case || t.Kind == DTokens.Default))
					{
						if (block != null && block.IsNonClampBlock)
							block = block.Parent;

						if (la.Kind != DTokens.Colon)
						{
							var psr = new DParser(lex);
							psr.AssignExpression();
						}
						
						lex.NextToken();

						block = new CodeBlock
						{ 
							InitialToken=DTokens.Case,
							StartLocation=t.EndLocation,

							Parent=block
						};
					}

					else if (block!=null && (block.IsSingleLineIndentation) && la.Kind == DTokens.Semicolon)
						block=block.Parent;

					else if(la.Kind==DTokens.OpenParenthesis || la.Kind==DTokens.OpenSquareBracket || la.Kind==DTokens.OpenCurlyBrace)
					{
						block = new CodeBlock
						{ 
							LastPreBlockIdentifier=t, 
							InitialToken=la.Kind , 
							StartLocation=la.Location,

							Parent=block
						};
					}

					else if (
						block != null && (
						la.Kind == DTokens.CloseParenthesis ||
						la.Kind == DTokens.CloseSquareBracket ||
						la.Kind == DTokens.CloseCurlyBrace))
					{
						if (block.InitialToken == DTokens.OpenParenthesis && la.Kind == DTokens.CloseParenthesis
							&& IsPreStatementToken(block.LastPreBlockIdentifier.Kind)
							&& lex.Peek().Kind != DTokens.OpenCurlyBrace)
						{
							block = new CodeBlock
							{
								LastPreBlockIdentifier = t,
								IsSingleLineIndentation = true,
								StartLocation = t.Location,

								Parent = block
							};
						}

					rep:
						block = block.Parent;

						if (block != null && la.Kind == DTokens.CloseCurlyBrace && block.IsNonClampBlock)
							goto rep;
					}
				}

				return block;
			}
		}

		public int GetLineTabIndentation(string lineText)
		{
			int ret = 0;

			foreach (var c in lineText)
			{
				if (c == ' ' || c == '\t')
					ret++;
				else
					break;
			}

			return ret;
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
			if (!DSettings.Instance.EnableSmartIndentation ||line.PreviousLine == null)
				return;

			bool hasPostCaretCurlyCloser = false;

			var offset = TakeCaret?dEditor.CaretOffset: (line.PreviousLine.Offset + line.PreviousLine.Length);

			if (document.GetText(line).TrimStart().StartsWith("}") ||(TakeCaret && document.GetCharAt(offset-1)==':'))
				hasPostCaretCurlyCloser = true;

			var block = RawDScanner.CalculateIndentation(document.Text, offset);

			if (block != null)
			{
				int ind = block.InnerIndentation;

				if (hasPostCaretCurlyCloser)
					ind--;

				RawlyIndentLine(ind, document, line);
			}
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
