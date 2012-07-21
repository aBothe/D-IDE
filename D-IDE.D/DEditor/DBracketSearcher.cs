using System.Collections.Generic;
using System.IO;
using D_Parser.Dom;
using D_Parser.Parser;
using ICSharpCode.AvalonEdit.AddIn;
using ICSharpCode.AvalonEdit.Document;

namespace D_IDE.D.DEditor
{
	public class DBracketSearcher
	{
		public static BracketSearchResult SearchBrackets(TextDocument doc, int caretOffset, TextLocation caret)
		{
			var caretLocation = new CodeLocation(caret.Column, caret.Line);
			try
			{
				if (caretOffset < 1 || caretOffset>=doc.TextLength-2)
					return null;

				// Search backward
				DToken lastToken=null;
				var tk_start = SearchBackward(doc, caretOffset, caretLocation,out lastToken);

				if (tk_start == null)
					return null;



				// Search forward
				var tk_end = SearchForward(doc, 
					doc.GetOffset(lastToken.EndLocation.Line,lastToken.EndLocation.Column),
					lastToken.EndLocation,
					getOppositeBracketToken(tk_start.Kind));

				if (tk_end == null)
					return null;

				int start = doc.GetOffset(tk_start.Location.Line, tk_start.Location.Column),
					end = doc.GetOffset(tk_end.Location.Line, tk_end.Location.Column);

				return new BracketSearchResult(start, 1, end, 1);
			}
			catch { return null; }
		}

		static int getOppositeBracketToken(int tk)
		{
			if(tk==DTokens.OpenParenthesis)
				return DTokens.CloseParenthesis;
			if (tk == DTokens.OpenSquareBracket)
				return DTokens.CloseSquareBracket;
			if (tk == DTokens.OpenCurlyBrace)
				return DTokens.CloseCurlyBrace;

			if (tk == DTokens.CloseParenthesis)
				return DTokens.OpenParenthesis;
			if (tk == DTokens.CloseSquareBracket)
				return DTokens.OpenSquareBracket;
			if(tk==DTokens.CloseCurlyBrace)
				return DTokens.OpenCurlyBrace;

			return -1;
		}

		static DToken SearchBackward(TextDocument doc, int caretOffset, CodeLocation caret,out DToken lastToken)
		{
			var ttp = doc.GetText(0, caretOffset);
			var sr = new StringReader(ttp);
			var lexer = new Lexer(sr);
			lexer.NextToken();

			var stk=new Stack<DToken>();

			while (lexer.LookAhead.Kind!=DTokens.EOF)
			{
				if (lexer.LookAhead.Kind == DTokens.OpenParenthesis || lexer.LookAhead.Kind==DTokens.OpenSquareBracket || lexer.LookAhead.Kind==DTokens.OpenCurlyBrace)
					stk.Push(lexer.LookAhead);

				else if (lexer.LookAhead.Kind == DTokens.CloseParenthesis || lexer.LookAhead.Kind == DTokens.CloseSquareBracket || lexer.LookAhead.Kind == DTokens.CloseCurlyBrace)
				{
					if (stk.Peek().Kind == getOppositeBracketToken( lexer.LookAhead.Kind))
						stk.Pop();
				}
				
				lexer.NextToken();
			}

			lastToken = lexer.CurrentToken;

			sr.Close();
			lexer.Dispose();

			if (stk.Count < 1)
				return null;

			return stk.Pop();
		}

		static DToken SearchForward(TextDocument doc, int caretOffset, CodeLocation caret, int searchedBracketToken)
		{
			var code = doc.GetText(caretOffset, doc.TextLength - caretOffset);
			var lexer = new Lexer(new System.IO.StringReader(code));

			lexer.SetInitialLocation(caret);
			lexer.NextToken();

			var stk = new Stack<DToken>();

			while (lexer.LookAhead.Kind!=DTokens.EOF)
			{
				if (lexer.LookAhead.Kind == DTokens.OpenParenthesis || 
					lexer.LookAhead.Kind == DTokens.OpenSquareBracket || 
					lexer.LookAhead.Kind == DTokens.OpenCurlyBrace)
					stk.Push(lexer.LookAhead);

				else if (lexer.LookAhead.Kind == DTokens.CloseParenthesis || 
					lexer.LookAhead.Kind == DTokens.CloseSquareBracket || 
					lexer.LookAhead.Kind == DTokens.CloseCurlyBrace)
				{
					if(stk.Count != 0)
						stk.Pop();
					else if (lexer.LookAhead.Kind == searchedBracketToken)
						return lexer.LookAhead;
				}

				lexer.NextToken();
			}

			lexer.Dispose();

			if (stk.Count < 1)
				return null;

			return stk.Pop();
		}
	}
}
