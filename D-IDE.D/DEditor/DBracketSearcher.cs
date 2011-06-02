using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit.AddIn;
using ICSharpCode.AvalonEdit.Document;
using D_Parser.Core;
using D_Parser;

namespace D_IDE.D.DEditor
{
	public class DBracketSearcher
	{
		public static BracketSearchResult SearchBrackets(TextDocument doc, int caretOffset)
		{
			try
			{
				if (caretOffset < 1 || caretOffset>=doc.TextLength-2)
					return null;

				// Search backward
				DToken lastToken=null;
				var tk_start = SearchBackward(doc, caretOffset,out lastToken);

				if (tk_start == null)
					return null;

				// Search forward
				var tk_end = SearchForward(doc, doc.GetOffset(lastToken.Location.Line, lastToken.Location.Column)+ (tk_start==lastToken?1:0), getOppositeBracketToken(tk_start.Kind));

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

		static DToken SearchBackward(TextDocument doc, int caretOffset,out DToken lastToken)
		{
			var lexer = new DLexer(new System.IO.StringReader(doc.GetText(0,caretOffset)));
			lexer.NextToken();

			var caret_=doc.GetLocation(caretOffset);
			var caret = new CodeLocation(caret_.Column, caret_.Line);

			var stk=new Stack<DToken>();

			while (lexer.LookAhead.Kind!=DTokens.EOF)
			{
				if (lexer.LookAhead.Kind == DTokens.OpenParenthesis || lexer.LookAhead.Kind==DTokens.OpenSquareBracket || lexer.LookAhead.Kind==DTokens.OpenCurlyBrace)
					stk.Push(lexer.LookAhead);

				if (lexer.LookAhead.Kind == DTokens.CloseParenthesis || lexer.LookAhead.Kind == DTokens.CloseSquareBracket || lexer.LookAhead.Kind == DTokens.CloseCurlyBrace)
				{
					if (stk.Peek().Kind == getOppositeBracketToken( lexer.LookAhead.Kind))
						stk.Pop();
				}
				
				lexer.NextToken();
			}

			lastToken = lexer.CurrentToken;

			lexer.Dispose();

			if (stk.Count < 1)
				return null;

			return stk.Pop();
		}

		static DToken SearchForward(TextDocument doc, int caretOffset, int searchedBracketToken)
		{
			var lexer = new DLexer(new System.IO.StringReader(doc.GetText(caretOffset,doc.TextLength-caretOffset)));

			var caret_ = doc.GetLocation(caretOffset);
			var caret = new CodeLocation(caret_.Column, caret_.Line);

			lexer.SetInitialLocation(caret);
			lexer.NextToken();

			var stk = new Stack<DToken>();

			while (lexer.LookAhead.Kind!=DTokens.EOF)
			{
				if (lexer.LookAhead.Kind == DTokens.OpenParenthesis || lexer.LookAhead.Kind == DTokens.OpenSquareBracket || lexer.LookAhead.Kind == DTokens.OpenCurlyBrace)
					stk.Push(lexer.LookAhead);

				if (lexer.LookAhead.Kind == DTokens.CloseParenthesis || lexer.LookAhead.Kind == DTokens.CloseSquareBracket || lexer.LookAhead.Kind == DTokens.CloseCurlyBrace)
				{
					if (lexer.LookAhead.Kind == searchedBracketToken && (stk.Count < 1 || stk.Peek().Kind != getOppositeBracketToken(lexer.LookAhead.Kind)))
						return lexer.LookAhead;

					if (stk.Peek().Kind == getOppositeBracketToken( lexer.LookAhead.Kind))
						stk.Pop();
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
