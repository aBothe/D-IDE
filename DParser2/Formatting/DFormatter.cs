using System;
using System.Collections.Generic;
using System.Text;
using D_Parser.Dom;
using D_Parser.Parser;
using System.IO;
using D_Parser.Resolver;

namespace D_Parser.Formatting
{
	public class DFormatter
	{
		public static bool IsPreStatementToken(int t)
		{
			return
				t==DTokens.Switch ||
				//t == DTokens.Version ||	t == DTokens.Debug || // Don't regard them because they also can occur as an attribute
				t == DTokens.If ||
				t == DTokens.While ||
				t == DTokens.For ||
				t == DTokens.Foreach ||
				t == DTokens.Foreach_Reverse ||
				t == DTokens.With ||
				t == DTokens.Synchronized;
		}

		public static int ReadRawLineIndentation(string lineText)
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

		DToken t { get { return Lexer.CurrentToken; } }
		DToken la { get { return Lexer.LookAhead; } }
		CodeBlock block;

		CodeBlock lastLineIndent;

		bool IsSemicolonContainingStatement
		{
			get {
				return block.LastPreBlockIdentifier.Kind == DTokens.For ||
						block.LastPreBlockIdentifier.Kind == DTokens.Foreach ||
						block.LastPreBlockIdentifier.Kind == DTokens.Foreach_Reverse;
			}
		}

		Lexer Lexer;

		public CodeBlock CalculateIndentation(string code, int offset)
		{
			if (offset >= code.Length)
				offset = code.Length - 1;

			return CalculateIndentation(code, DocumentHelper.OffsetToLocation(code, offset));
		}

		CodeBlock PushBlock()
		{
			return block = new CodeBlock { 
				LastPreBlockIdentifier=Lexer.LastToken ?? null,
				Parent=block,
				StartLocation = t.Location
			};
		}

		CodeBlock PopBlock()
		{
			if (block != null)
				return block = block.Parent;
			return null;
		}

		void SetLastLineIndent()
		{
			if (lastLineIndent == null)
				lastLineIndent = block;
		}

		public CodeBlock CalculateIndentation(string code, CodeLocation caret)
		{
			block = null;
			
			Lexer = new Lexer(new StringReader(code));

			Lexer.NextToken();

			while (!Lexer.IsEOF && (t==null || t.Location.Line <= caret.Line))
			{
				if (t != null && la.line > t.line)
					lastLineIndent = null;

				Lexer.NextToken();

				if (Lexer.IsEOF && la.line > t.line)
					lastLineIndent = null;

				/*
				 * if(..)
				 *		for(...)
				 *			while(...)
				 *				foo();
				 *	// No indentation anymore!
				 */
				if (t.Kind == DTokens.Comma || t.Kind == DTokens.Semicolon && la.line > t.line)
				{
					if (block == null)
						continue;

					if (block.Reason == CodeBlock.IndentReason.UnfinishedStatement)
						PopBlock();

					while (
						block != null &&
						block.Reason == CodeBlock.IndentReason.SingleLineStatement &&
						!IsSemicolonContainingStatement)
						PopBlock();
				}

				// (,[,{
				else if (t.Kind == DTokens.OpenParenthesis ||
					t.Kind == DTokens.OpenSquareBracket ||
					t.Kind == DTokens.OpenCurlyBrace)
				{
					while (block != null && (block.Reason == CodeBlock.IndentReason.SingleLineStatement || block.Reason == CodeBlock.IndentReason.UnfinishedStatement))
						PopBlock();

					PushBlock().BlockStartToken = t.Kind;
				}

				// ),],}
				else if (t.Kind == DTokens.Do ||
					t.Kind == DTokens.CloseParenthesis ||
					t.Kind == DTokens.CloseSquareBracket ||
					t.Kind == DTokens.CloseCurlyBrace)
				{
					if (t.Kind == DTokens.CloseCurlyBrace)
					{
						bool isBraceInLineOnly = true;
						while (block != null && !block.IsClampBlock)
						{
							isBraceInLineOnly = block.StartLocation.Line == t.line && la.line > t.line;

							if (!isBraceInLineOnly && block.Reason == CodeBlock.IndentReason.StatementLabel)
								PopBlock();

							PopBlock();
						}

						if (isBraceInLineOnly)
							PopBlock();
					}
					else
					{
						SetLastLineIndent();
						PopBlock();

						if (t.Kind == DTokens.Do ||
							t.Kind == DTokens.CloseParenthesis &&
							block != null &&
							block.BlockStartToken == DTokens.OpenParenthesis && IsPreStatementToken(block.LastPreBlockIdentifier.Kind))
						{
							PopBlock();
							if (la.Kind == DTokens.OpenCurlyBrace && la.line > t.line)
							{ }
							else
								PushBlock().Reason = CodeBlock.IndentReason.SingleLineStatement;
						}
					}
				}

				else if (t.Kind == DTokens.Colon)
				{
					if (block != null && block.Reason!= CodeBlock.IndentReason.Other)
						PopBlock();

					PushBlock().Reason = CodeBlock.IndentReason.StatementLabel;
				}

				else if (block == null ||
					block.Reason != CodeBlock.IndentReason.UnfinishedStatement &&
					block.Reason != CodeBlock.IndentReason.SingleLineStatement)
					PushBlock().Reason = CodeBlock.IndentReason.UnfinishedStatement;
			}

			return lastLineIndent ?? block;
		}
	}

	public class CodeBlock
	{
		public enum IndentReason
		{
			Other,
			SingleLineStatement,
			UnfinishedStatement,
			StatementLabel, // x1: ; case 3: ; default: 
		}

		public IndentReason Reason= IndentReason.Other;

		public CodeLocation StartLocation;
		//public CodeLocation EndLocation;

		public int BlockStartToken;

		public bool IsClampBlock { get {
			return BlockStartToken == DTokens.OpenCurlyBrace || BlockStartToken == DTokens.OpenParenthesis || BlockStartToken == DTokens.OpenSquareBracket;
		} }

		/// <summary>
		/// The last token before this CodeBlock started.
		/// </summary>
		public DToken LastPreBlockIdentifier;

		/// <summary>
		/// Last token found within current block.
		/// </summary>
		public DToken LastToken;

		public CodeBlock Parent;

		public int GetLineIndentation(int line)
		{
			if (StartLocation.Line > line)
				return 0;

			int indentation = 0;

			if (Parent != null)
				indentation = Parent.GetLineIndentation(line);

			if (line > StartLocation.Line)
				indentation++;

			return indentation;
		}
	}

}
