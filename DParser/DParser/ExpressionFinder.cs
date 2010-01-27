using System;
using System.Collections.Generic;
using System.Text;
using ICSharpCode.NRefactory.Parser;
using System.IO;
using ICSharpCode.NRefactory;

namespace D_Parser
{
	public class ExpressionFinder
	{
		protected List<Token> tokens;
		protected Comment[] comments;

		public List<Token> Tokens
		{
			get { return tokens; }
		}

		public static int SkipWhiteSpaceOffsets(string text, int CurOff)
		{
			if(text == null) return 0;
			char tch;
			for(int i = CurOff; i > 0 && CurOff < text.Length; i--)
			{
				tch = text[i];

				if(char.IsWhiteSpace(tch)) continue;

				return i;
			}
			return 0;
		}

		public static ExpressionFinder Create(string TextToParse)
		{
			if(TextToParse == null || TextToParse == "") return null;

			return new ExpressionFinder(TextToParse);
		}

		public static Token GetTokenAt(string TextToParse, CodeLocation where)
		{
			DLexer lexer = new DLexer(new StringReader(TextToParse));

			Token la = null;
			while((la =(Token) lexer.NextToken()).Kind != DTokens.EOF)
			{
				if(DParser.GetCodeLocation(la) >= where)
				{
					lexer.Dispose();
					return la;
				}
			}
			lexer.Dispose();
			return null;
		}

		public static Token GetPreviousTokenAt(string TextToParse, CodeLocation where)
		{
			DLexer lexer = new DLexer(new StringReader(TextToParse));

			Token la = null, prev=null;
			while((la =(Token) lexer.NextToken()).Kind != DTokens.EOF)
			{
				if(DParser.GetCodeLocation(la) >= where)
				{
					lexer.Dispose();
					return prev;
				}
				prev = la;
			}
			lexer.Dispose();
			return null;
		}

		ExpressionFinder(string TextToParse)
		{
			tokens = new List<Token>();
			DLexer lexer = new DLexer(new StringReader(TextToParse));

			Token la=null;
			while ((la = lexer.NextToken()).Kind != DTokens.EOF)
			{
				tokens.Add(la);
			}
			comments=lexer.Comments.ToArray();
			lexer.Dispose();
		}

		public bool IsInComment(CodeLocation where)
		{
			foreach(Comment c in comments)
			{
				if(c.StartPosition.X >= where.X && c.StartPosition.Y>=where.Y && c.EndPosition.X <= where.X && c.EndPosition.Y<=where.Y)
					return true;
			}
			return false;
		}

		public Token GetTokenAt(CodeLocation where,out int index)
		{
			Token t=null;
			for(int i=0; i<tokens.Count; i++)
			{
				t=tokens[i];
				if (DParser.GetCodeLocation(t) >= where)
				{
					index = i;
					return t;
				}
			}
			index = -1;
			return null;
		}

		public Token GetTokenAt(CodeLocation where)
		{
			foreach(Token t in tokens)
			{
				if (DParser.GetCodeLocation(t) >= where)
					return t;
			}
			return null;
		}

		public Token GetPreviousTokenAt(CodeLocation where,out int index)
		{
			Token prev = null, cur = null;
			for(int i = 0; i < tokens.Count; i++)
			{
				cur = tokens[i];
				if (DParser.GetCodeLocation(cur) >= where)
				{
					index = i-1;
					return prev;
				}
				prev = cur;
			}
			index = -1;
			return null;
		}

		public Token GetPreviousTokenAt(CodeLocation where)
		{
			Token prev=null, cur=null;
			for(int i=0; i < tokens.Count; i++ )
			{
				cur = tokens[i];
				if(DParser.GetCodeLocation(cur) >= where)
					return prev;
				prev = cur;
			}
			return null;
		}
	}
}
