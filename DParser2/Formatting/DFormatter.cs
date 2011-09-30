using System;
using System.Collections.Generic;
using System.Text;
using D_Parser.Dom;
using D_Parser.Parser;
using System.IO;

namespace D_Parser.Formatting
{
	public class DFormatter
	{
		public static bool IsPreStatementToken(int t)
		{
			return
				t == DTokens.Version ||
				t == DTokens.Debug ||
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
				offset = code.Length - 1;

			CodeBlock block = null;

			var lex = new Lexer(new StringReader(code.Substring(0, offset)));

			lex.NextToken();

			while (!lex.IsEOF)
			{
				lex.NextToken();

				var t = lex.CurrentToken;
				var la = lex.LookAhead;

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
						InitialToken = DTokens.Case,
						StartLocation = t.EndLocation,

						Parent = block
					};
				}

				else if (block != null && (block.IsSingleLineIndentation) && la.Kind == DTokens.Semicolon)
					block = block.Parent;

				else if (la.Kind == DTokens.OpenParenthesis || la.Kind == DTokens.OpenSquareBracket || la.Kind == DTokens.OpenCurlyBrace)
				{
					block = new CodeBlock
					{
						LastPreBlockIdentifier = t,
						InitialToken = la.Kind,
						StartLocation = la.Location,

						Parent = block
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

		public static int GetLineTabIndentation(string lineText)
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
	}

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
			get
			{
				if (Parent != null)
					return Parent.OuterIndentation + 1;

				return 0;
			}
		}

		public int InnerIndentation
		{
			get
			{
				return OuterIndentation + 1;
			}
		}
	}

}
