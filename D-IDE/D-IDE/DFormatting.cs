using System;
using System.Collections.Generic;
using System.Text;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Actions;
using System.Windows.Forms;

using D_Parser;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using ICSharpCode.SharpDevelop.Dom;
using System.Collections;

namespace D_IDE.CodeCompletion
{
	/// <summary>
	/// This class handles the auto and smart indenting in the textbuffer while
	/// you type.
	/// </summary>
	public class DFormattingStrategy : DefaultFormattingStrategy
	{
		public DFormattingStrategy()
		{
		}

		#region SmartIndentLine
		/// <summary>
		/// Define CSharp specific smart indenting for a line :)
		/// </summary>
		protected override int SmartIndentLine(TextArea textArea, int lineNr)
		{
			if(lineNr <= 0)
			{
				return AutoIndentLine(textArea, lineNr);
			}

			string oldText = textArea.Document.GetText(textArea.Document.GetLineSegment(lineNr));

			DocumentAccessor acc = new DocumentAccessor(textArea.Document, lineNr, lineNr);

			IndentationSettings set = new IndentationSettings();
			set.IndentString = Tab.GetIndentationString(textArea.Document);
			set.LeaveEmptyLines = false;
			IndentationReformatter r = new IndentationReformatter();

			r.Reformat(acc, set);

			//if(acc.ChangedLines > 0)	textArea.Document.UndoStack.CombineLast(2);

			string t = acc.Text;
			if(t.Length == 0)
			{
				// use AutoIndentation for new lines in comments / verbatim strings.
				return AutoIndentLine(textArea, lineNr);
			}
			else
			{
				int newIndentLength = t.Length - t.TrimStart().Length;
				int oldIndentLength = oldText.Length - oldText.TrimStart().Length;
				if(oldIndentLength != newIndentLength && lineNr == textArea.Caret.Position.Y)
				{
					// fix cursor position if indentation was changed
					int newX = textArea.Caret.Position.X - oldIndentLength + newIndentLength;
					textArea.Caret.Position = new TextLocation(Math.Max(newX, 0), lineNr);
				}
				return newIndentLength;
			}
		}

		/// <summary>
		/// This function sets the indentlevel in a range of lines.
		/// </summary>
		public override void IndentLines(TextArea textArea, int begin, int end)
		{
			if(textArea.Document.TextEditorProperties.IndentStyle != IndentStyle.Smart)
			{
				base.IndentLines(textArea, begin, end);
				return;
			}
			int cursorPos = textArea.Caret.Position.Y;
			int oldIndentLength = 0;

			if(cursorPos >= begin && cursorPos <= end)
				oldIndentLength = GetIndentation(textArea, cursorPos).Length;

			IndentationSettings set = new IndentationSettings();
			set.IndentString = Tab.GetIndentationString(textArea.Document);
			IndentationReformatter r = new IndentationReformatter();
			DocumentAccessor acc = new DocumentAccessor(textArea.Document, begin, end);
			r.Reformat(acc, set);

			if(cursorPos >= begin && cursorPos <= end)
			{
				int newIndentLength = GetIndentation(textArea, cursorPos).Length;
				if(oldIndentLength != newIndentLength)
				{
					// fix cursor position if indentation was changed
					int newX = textArea.Caret.Position.X - oldIndentLength + newIndentLength;
					textArea.Caret.Position = new TextLocation(Math.Max(newX, 0), cursorPos);
				}
			}

			//if(acc.ChangedLines > 0)				textArea.Document.UndoStack.CombineLast(acc.ChangedLines);
		}
		#endregion

		#region Private functions
		bool NeedCurlyBracket(string text)
		{
			int curlyCounter = 0;

			bool inString = false;
			bool inChar = false;
			bool verbatim = false;

			bool lineComment = false;
			bool blockComment = false;

			for(int i = 0; i < text.Length; ++i)
			{
				switch(text[i])
				{
					case '\r':
					case '\n':
						lineComment = false;
						inChar = false;
						if(!verbatim) inString = false;
						break;
					case '/':
						if(blockComment)
						{
							Debug.Assert(i > 0);
							if(text[i - 1] == '*')
							{
								blockComment = false;
							}
						}
						if(!inString && !inChar && i + 1 < text.Length)
						{
							if(!blockComment && text[i + 1] == '/')
							{
								lineComment = true;
							}
							if(!lineComment && text[i + 1] == '*')
							{
								blockComment = true;
							}
						}
						break;
					case '"':
						if(!(inChar || lineComment || blockComment))
						{
							if(inString && verbatim)
							{
								if(i + 1 < text.Length && text[i + 1] == '"')
								{
									++i; // skip escaped quote
									inString = false; // let the string go on
								}
								else
								{
									verbatim = false;
								}
							}
							else if(!inString && i > 0 && text[i - 1] == '@')
							{
								verbatim = true;
							}
							inString = !inString;
						}
						break;
					case '\'':
						if(!(inString || lineComment || blockComment))
						{
							inChar = !inChar;
						}
						break;
					case '{':
						if(!(inString || inChar || lineComment || blockComment))
						{
							++curlyCounter;
						}
						break;
					case '}':
						if(!(inString || inChar || lineComment || blockComment))
						{
							--curlyCounter;
						}
						break;
					case '\\':
						if((inString && !verbatim) || inChar)
							++i; // skip next character
						break;
				}
			}
			return curlyCounter > 0;
		}


		bool IsInsideStringOrComment(TextArea textArea, LineSegment curLine, int cursorOffset)
		{
			// scan cur line if it is inside a string or single line comment (//)
			bool insideString = false;
			char stringstart = ' ';
			bool verbatim = false; // true if the current string is verbatim (@-string)
			char c = ' ';
			char lastchar;

			for(int i = curLine.Offset; i < cursorOffset; ++i)
			{
				lastchar = c;
				c = textArea.Document.GetCharAt(i);
				if(insideString)
				{
					if(c == stringstart)
					{
						if(verbatim && i + 1 < cursorOffset && textArea.Document.GetCharAt(i + 1) == '"')
						{
							++i; // skip escaped character
						}
						else
						{
							insideString = false;
						}
					}
					else if(c == '\\' && !verbatim)
					{
						++i; // skip escaped character
					}
				}
				else if(c == '/' && i + 1 < cursorOffset && textArea.Document.GetCharAt(i + 1) == '/')
				{
					return true;
				}
				else if(c == '"' || c == '\'')
				{
					stringstart = c;
					insideString = true;
					verbatim = (c == '"') && (lastchar == '@');
				}
			}

			return insideString;
		}

		bool IsInsideDocumentationComment(TextArea textArea, LineSegment curLine, int cursorOffset)
		{
			for(int i = curLine.Offset; i < cursorOffset; ++i)
			{
				char ch = textArea.Document.GetCharAt(i);
				if(ch == '"')
				{
					// parsing strings correctly is too complicated (see above),
					// but I don't now any case where a doc comment is after a string...
					return false;
				}
				if(ch == '/' && i + 2 < cursorOffset && textArea.Document.GetCharAt(i + 1) == '/' && textArea.Document.GetCharAt(i + 2) == '/')
				{
					return true;
				}
			}
			return false;
		}
		#endregion

		#region FormatLine

		bool NeedEndregion(IDocument document)
		{
			int regions = 0;
			int endregions = 0;
			foreach(LineSegment line in document.LineSegmentCollection)
			{
				string text = document.GetText(line).Trim();
				if(text.StartsWith("#region"))
				{
					++regions;
				}
				else if(text.StartsWith("#endregion"))
				{
					++endregions;
				}
			}
			return regions > endregions;
		}
		public override void FormatLine(TextArea textArea, int lineNr, int cursorOffset, char ch) // used for comment tag formater/inserter
		{
			LineSegment curLine = textArea.Document.GetLineSegment(lineNr);
			LineSegment lineAbove = lineNr > 0 ? textArea.Document.GetLineSegment(lineNr - 1) : null;
			string terminator = textArea.TextEditorProperties.LineTerminator;

			//// local string for curLine segment
			string curLineText = "";

			if(ch != '\n' && ch != '>')
			{
				if(IsInsideStringOrComment(textArea, curLine, cursorOffset))
				{
					return;
				}
			}
			switch(ch)
			{
				case '>':
					if(IsInsideDocumentationComment(textArea, curLine, cursorOffset))
					{
						curLineText = textArea.Document.GetText(curLine);
						int column = textArea.Caret.Offset - curLine.Offset;
						int index = Math.Min(column - 1, curLineText.Length - 1);

						while(index >= 0 && curLineText[index] != '<')
						{
							--index;
							if(curLineText[index] == '/')
								return; // the tag was an end tag or already
						}

						if(index > 0)
						{
							StringBuilder commentBuilder = new StringBuilder("");
							for(int i = index; i < curLineText.Length && i < column && !Char.IsWhiteSpace(curLineText[i]); ++i)
							{
								commentBuilder.Append(curLineText[i]);
							}
							string tag = commentBuilder.ToString().Trim();
							if(!tag.EndsWith(">"))
							{
								tag += ">";
							}
							if(!tag.StartsWith("/"))
							{
								textArea.Document.Insert(textArea.Caret.Offset, "</" + tag.Substring(1));
							}
						}
					}
					break;
				case ':':
				case ')':
				case ']':
				case '}':
				case '{':
					if(textArea.Document.TextEditorProperties.IndentStyle == IndentStyle.Smart)
					{
						textArea.Document.FormattingStrategy.IndentLine(textArea, lineNr);
					}
					break;
				case '\n':
					string lineAboveText = lineAbove == null ? "" : textArea.Document.GetText(lineAbove);
					//// curLine might have some text which should be added to indentation
					curLineText = "";
					if(curLine.Length > 0)
					{
						curLineText = textArea.Document.GetText(curLine);
					}

					LineSegment nextLine = lineNr + 1 < textArea.Document.TotalNumberOfLines ? textArea.Document.GetLineSegment(lineNr + 1) : null;
					string nextLineText = lineNr + 1 < textArea.Document.TotalNumberOfLines ? textArea.Document.GetText(nextLine) : "";

					int addCursorOffset = 0;

					if(lineAbove.HighlightSpanStack != null && !lineAbove.HighlightSpanStack.IsEmpty)
					{
						if(!lineAbove.HighlightSpanStack.Peek().StopEOL)
						{	// case for /* style comments
							int index = lineAboveText.IndexOf("/*");
							if(index > 0)
							{
								StringBuilder indentation = new StringBuilder(GetIndentation(textArea, lineNr - 1));
								for(int i = indentation.Length; i < index; ++i)
								{
									indentation.Append(' ');
								}
								//// adding curline text
								textArea.Document.Replace(curLine.Offset, curLine.Length, indentation.ToString() + " * " + curLineText);
								indentation.Length += 3 + curLineText.Length;
							}

							index = lineAboveText.IndexOf("*");
							if(index > 0)
							{
								StringBuilder indentation = new StringBuilder(GetIndentation(textArea, lineNr - 1));
								for(int i = indentation.Length; i < index; ++i)
								{
									indentation.Append(' ');
								}
								//// adding curline if present
								textArea.Document.Replace(curLine.Offset, curLine.Length, indentation.ToString() + "* " + curLineText);
								indentation.Length += 2 + curLineText.Length;
							}
						}
						else
						{ // don't handle // lines, because they're only one lined comments
							int indexAbove = lineAboveText.IndexOf("///");
							int indexNext = nextLineText.IndexOf("///");
							if(indexAbove > 0 && (indexNext != -1 || indexAbove + 4 < lineAbove.Length))
							{
								StringBuilder indentation = new StringBuilder(GetIndentation(textArea, lineNr - 1));
								for(int i = indentation.Length; i < indexAbove; ++i)
								{
									indentation.Append(' ');
								}
								//// adding curline text if present
								textArea.Document.Replace(curLine.Offset, curLine.Length, indentation.ToString() + "/// " + curLineText);
								//textArea.Document.UndoStack.CombineLast(2);
								//return indentation.Length + 4 /*+ curLineText.Length*/;
							}

							if(IsInNonVerbatimString(lineAboveText, curLineText))
							{
								textArea.Document.Insert(lineAbove.Offset + lineAbove.Length,
														 "\" +");
								curLine = textArea.Document.GetLineSegment(lineNr);
								textArea.Document.Insert(curLine.Offset, "\"");
								//textArea.Document.UndoStack.CombineLast(3);
								addCursorOffset = 1;
							}
						}
					}
					int result = IndentLine(textArea, lineNr) + addCursorOffset;
					if(textArea.TextEditorProperties.AutoInsertCurlyBracket)
					{
						string oldLineText = TextUtilities.GetLineAsString(textArea.Document, lineNr - 1);
						if(oldLineText.EndsWith("{"))
						{
							if(NeedCurlyBracket(textArea.Document.TextContent))
							{
								textArea.Document.Insert(curLine.Offset + curLine.Length, terminator + "}");
								IndentLine(textArea, lineNr + 1);
							}
						}
					}
					return;
			}
			return;
		}

		/// <summary>
		/// Checks if the cursor is inside a non-verbatim string.
		/// This method is used to check if a line break was inserted in a string.
		/// The text editor has already broken the line for us, so we just need to check
		/// the two lines.
		/// </summary>
		/// <param name="start">The part before the line break</param>
		/// <param name="end">The part after the line break</param>
		/// <returns>
		/// True, when the line break was inside a non-verbatim-string, so when
		/// start does not contain a comment, but a non-even number of ", and
		/// end contains a non-even number of " before the first comment.
		/// </returns>
		bool IsInNonVerbatimString(string start, string end)
		{
			bool inString = false;
			bool inChar = false;
			for(int i = 0; i < start.Length; ++i)
			{
				char c = start[i];
				if(c == '"' && !inChar)
				{
					if(!inString && i > 0 && start[i - 1] == '@')
						return false; // no string line break for verbatim strings
					inString = !inString;
				}
				else if(c == '\'' && !inString)
				{
					inChar = !inChar;
				}
				if(!inString && i > 0 && start[i - 1] == '/' && (c == '/' || c == '*'))
					return false;
				if(inString && start[i] == '\\')
					++i;
			}
			if(!inString) return false;
			// we are possibly in a string, or a multiline string has just ended here
			// check if the closing double quote is in end
			for(int i = 0; i < end.Length; ++i)
			{
				char c = end[i];
				if(c == '"' && !inChar)
				{
					if(!inString && i > 0 && end[i - 1] == '@')
						break; // no string line break for verbatim strings
					inString = !inString;
				}
				else if(c == '\'' && !inString)
				{
					inChar = !inChar;
				}
				if(!inString && i > 0 && end[i - 1] == '/' && (c == '/' || c == '*'))
					break;
				if(inString && end[i] == '\\')
					++i;
			}
			// return true if the string was closed properly
			return !inString;
		}
		#endregion

		#region SearchBracket helper functions
		static int ScanLineStart(IDocument document, int offset)
		{
			for(int i = offset - 1; i > 0; --i)
			{
				if(document.GetCharAt(i) == '\n')
					return i + 1;
			}
			return 0;
		}

		/// <summary>
		/// Gets the type of code at offset.<br/>
		/// 0 = Code,<br/>
		/// 1 = Comment,<br/>
		/// 2 = String<br/>
		/// Block comments and multiline strings are not supported.
		/// </summary>
		static int GetStartType(IDocument document, int linestart, int offset)
		{
			bool inString = false;
			bool inChar = false;
			bool verbatim = false;
			for(int i = linestart; i < offset; i++)
			{
				switch(document.GetCharAt(i))
				{
					case '/':
						if(!inString && !inChar && i + 1 < document.TextLength)
						{
							if(document.GetCharAt(i + 1) == '/')
							{
								return 1;
							}
						}
						break;
					case '"':
						if(!inChar)
						{
							if(inString && verbatim)
							{
								if(i + 1 < document.TextLength && document.GetCharAt(i + 1) == '"')
								{
									++i; // skip escaped quote
									inString = false; // let the string go on
								}
								else
								{
									verbatim = false;
								}
							}
							else if(!inString && i > 0 && document.GetCharAt(i - 1) == '@')
							{
								verbatim = true;
							}
							inString = !inString;
						}
						break;
					case '\'':
						if(!inString) inChar = !inChar;
						break;
					case '\\':
						if((inString && !verbatim) || inChar)
							++i; // skip next character
						break;
				}
			}
			return (inString || inChar) ? 2 : 0;
		}
		#endregion

		#region SearchBracketBackward
		public override int SearchBracketBackward(IDocument document, int offset, char openBracket, char closingBracket)
		{
			if(offset + 1 >= document.TextLength) return -1;
			// this method parses a c# document backwards to find the matching bracket

			// first try "quick find" - find the matching bracket if there is no string/comment in the way
			int quickResult = base.SearchBracketBackward(document, offset, openBracket, closingBracket);
			if(quickResult >= 0) return quickResult;

			// we need to parse the line from the beginning, so get the line start position
			int linestart = ScanLineStart(document, offset + 1);

			// we need to know where offset is - in a string/comment or in normal code?
			// ignore cases where offset is in a block comment
			int starttype = GetStartType(document, linestart, offset + 1);
			if(starttype != 0)
			{
				return -1; // start position is in a comment/string
			}

			// I don't see any possibility to parse a C# document backwards...
			// We have to do it forwards and push all bracket positions on a stack.
			Stack bracketStack = new Stack();
			bool blockComment = false;
			bool lineComment = false;
			bool inChar = false;
			bool inString = false;
			bool verbatim = false;

			for(int i = 0; i <= offset; ++i)
			{
				char ch = document.GetCharAt(i);
				switch(ch)
				{
					case '\r':
					case '\n':
						lineComment = false;
						inChar = false;
						if(!verbatim) inString = false;
						break;
					case '/':
						if(blockComment)
						{
							Debug.Assert(i > 0);
							if(document.GetCharAt(i - 1) == '*')
							{
								blockComment = false;
							}
						}
						if(!inString && !inChar && i + 1 < document.TextLength)
						{
							if(!blockComment && document.GetCharAt(i + 1) == '/')
							{
								lineComment = true;
							}
							if(!lineComment && document.GetCharAt(i + 1) == '*')
							{
								blockComment = true;
							}
						}
						break;
					case '"':
						if(!(inChar || lineComment || blockComment))
						{
							if(inString && verbatim)
							{
								if(i + 1 < document.TextLength && document.GetCharAt(i + 1) == '"')
								{
									++i; // skip escaped quote
									inString = false; // let the string go
								}
								else
								{
									verbatim = false;
								}
							}
							else if(!inString && offset > 0 && document.GetCharAt(i - 1) == '@')
							{
								verbatim = true;
							}
							inString = !inString;
						}
						break;
					case '\'':
						if(!(inString || lineComment || blockComment))
						{
							inChar = !inChar;
						}
						break;
					case '\\':
						if((inString && !verbatim) || inChar)
							++i; // skip next character
						break;
					default:
						if(ch == openBracket)
						{
							if(!(inString || inChar || lineComment || blockComment))
							{
								bracketStack.Push(i);
							}
						}
						else if(ch == closingBracket)
						{
							if(!(inString || inChar || lineComment || blockComment))
							{
								if(bracketStack.Count > 0)
									bracketStack.Pop();
							}
						}
						break;
				}
			}
			if(bracketStack.Count > 0) return (int)bracketStack.Pop();
			return -1;
		}
		#endregion

		#region SearchBracketForward
		public override int SearchBracketForward(IDocument document, int offset, char openBracket, char closingBracket)
		{
			bool inString = false;
			bool inChar = false;
			bool verbatim = false;

			bool lineComment = false;
			bool blockComment = false;

			if(offset < 0) return -1;

			// first try "quick find" - find the matching bracket if there is no string/comment in the way
			int quickResult = base.SearchBracketForward(document, offset, openBracket, closingBracket);
			if(quickResult >= 0) return quickResult;

			// we need to parse the line from the beginning, so get the line start position
			int linestart = ScanLineStart(document, offset);

			// we need to know where offset is - in a string/comment or in normal code?
			// ignore cases where offset is in a block comment
			int starttype = GetStartType(document, linestart, offset);
			if(starttype != 0) return -1; // start position is in a comment/string

			int brackets = 1;

			while(offset < document.TextLength)
			{
				char ch = document.GetCharAt(offset);
				switch(ch)
				{
					case '\r':
					case '\n':
						lineComment = false;
						inChar = false;
						if(!verbatim) inString = false;
						break;
					case '/':
						if(blockComment)
						{
							Debug.Assert(offset > 0);
							if(document.GetCharAt(offset - 1) == '*')
							{
								blockComment = false;
							}
						}
						if(!inString && !inChar && offset + 1 < document.TextLength)
						{
							if(!blockComment && document.GetCharAt(offset + 1) == '/')
							{
								lineComment = true;
							}
							if(!lineComment && document.GetCharAt(offset + 1) == '*')
							{
								blockComment = true;
							}
						}
						break;
					case '"':
						if(!(inChar || lineComment || blockComment))
						{
							if(inString && verbatim)
							{
								if(offset + 1 < document.TextLength && document.GetCharAt(offset + 1) == '"')
								{
									++offset; // skip escaped quote
									inString = false; // let the string go
								}
								else
								{
									verbatim = false;
								}
							}
							else if(!inString && offset > 0 && document.GetCharAt(offset - 1) == '@')
							{
								verbatim = true;
							}
							inString = !inString;
						}
						break;
					case '\'':
						if(!(inString || lineComment || blockComment))
						{
							inChar = !inChar;
						}
						break;
					case '\\':
						if((inString && !verbatim) || inChar)
							++offset; // skip next character
						break;
					default:
						if(ch == openBracket)
						{
							if(!(inString || inChar || lineComment || blockComment))
							{
								++brackets;
							}
						}
						else if(ch == closingBracket)
						{
							if(!(inString || inChar || lineComment || blockComment))
							{
								--brackets;
								if(brackets == 0)
								{
									return offset;
								}
							}
						}
						break;
				}
				++offset;
			}
			return -1;
		}
		#endregion
	}

	/// <summary>
	/// Interface used for the indentation class to access the document.
	/// </summary>
	public interface IDocumentAccessor
	{
		/// <summary>Gets if something was changed in the document.</summary>
		bool Dirty { get; }
		/// <summary>Gets if the current line is read only (because it is not in the
		/// selected text region)</summary>
		bool ReadOnly { get; }
		/// <summary>Gets the number of the current line.</summary>
		int LineNumber { get; }
		/// <summary>Gets/Sets the text of the current line.</summary>
		string Text { get; set; }
		/// <summary>Advances to the next line.</summary>
		bool Next();
	}

	#region DocumentAccessor
	public sealed class DocumentAccessor : IDocumentAccessor
	{
		IDocument doc;

		int minLine;
		int maxLine;
		int changedLines = 0;

		public DocumentAccessor(IDocument document)
		{
			doc = document;
			this.minLine = 0;
			this.maxLine = doc.TotalNumberOfLines - 1;
		}

		public DocumentAccessor(IDocument document, int minLine, int maxLine)
		{
			doc = document;
			this.minLine = minLine;
			this.maxLine = maxLine;
		}

		int num = -1;
		bool dirty;
		string text;
		LineSegment line;

		public bool ReadOnly
		{
			get
			{
				return num < minLine;
			}
		}

		public bool Dirty
		{
			get
			{
				return dirty;
			}
		}

		public int LineNumber
		{
			get
			{
				return num;
			}
		}

		public int ChangedLines
		{
			get
			{
				return changedLines;
			}
		}

		bool lineDirty = false;

		public string Text
		{
			get { return text; }
			set
			{
				if(num < minLine) return;
				text = value;
				dirty = true;
				lineDirty = true;
			}
		}

		public bool Next()
		{
			if(lineDirty)
			{
				doc.Replace(line.Offset, line.Length, text);
				lineDirty = false;
				++changedLines;
			}
			++num;
			if(num > maxLine) return false;
			line = doc.GetLineSegment(num);
			text = doc.GetText(line);
			return true;
		}
	}
	#endregion

	#region StringAccessor
	public sealed class StringAccessor : IDocumentAccessor
	{
		public bool Dirty
		{
			get
			{
				return dirty;
			}
		}

		public bool ReadOnly
		{
			get
			{
				return false;
			}
		}

		StringReader r;
		StringWriter w;
		bool dirty = false;

		public string CodeOutput
		{
			get
			{
				return w.ToString();
			}
		}

		public StringAccessor(string code)
		{
			r = new StringReader(code);
			w = new StringWriter();
		}

		int num = 0;

		public int LineNumber
		{
			get { return num; }
		}

		string text = "";

		public string Text
		{
			get
			{
				return text;
			}
			set
			{
				dirty = true;
				text = value;
			}
		}

		public bool Next()
		{
			if(num > 0)
			{
				w.WriteLine(text);
			}
			text = r.ReadLine();
			++num;
			return text != null;
		}
	}
	#endregion

	public sealed class IndentationSettings
	{
		public string IndentString = "\t";
		/// <summary>Leave empty lines empty.</summary>
		public bool LeaveEmptyLines = true;
	}

	public sealed class IndentationReformatter
	{
		public struct Block
		{
			public string OuterIndent;
			public string InnerIndent;
			public string LastWord;
			public char Bracket;
			public bool Continuation;
			public bool OneLineBlock;
			public int StartLine;

			public void Indent(IndentationSettings set)
			{
				Indent(set, set.IndentString);
			}

			public void Indent(IndentationSettings set, string str)
			{
				OuterIndent = InnerIndent;
				InnerIndent += str;
				Continuation = false;
				OneLineBlock = false;
				LastWord = "";
			}
		}

		StringBuilder wordBuilder;
		Stack<Block> blocks; // blocks contains all blocks outside of the current
		Block block;  // block is the current block

		bool inString = false;
		bool inChar = false;
		bool verbatim = false;
		bool escape = false;

		bool lineComment = false;
		bool blockComment = false;

		char lastRealChar = ' '; // last non-comment char

		public void Reformat(IDocumentAccessor doc, IndentationSettings set)
		{
			Init();

			while(doc.Next())
			{
				Step(doc, set);
			}
		}

		public void Init()
		{
			wordBuilder = new StringBuilder();
			blocks = new Stack<Block>();
			block = new Block();
			block.InnerIndent = "";
			block.OuterIndent = "";
			block.Bracket = '{';
			block.Continuation = false;
			block.LastWord = "";
			block.OneLineBlock = false;
			block.StartLine = 0;

			inString = false;
			inChar = false;
			verbatim = false;
			escape = false;

			lineComment = false;
			blockComment = false;

			lastRealChar = ' '; // last non-comment char
		}

		public void Step(IDocumentAccessor doc, IndentationSettings set)
		{
			string line = doc.Text;
			if(set.LeaveEmptyLines && line.Length == 0) return; // leave empty lines empty
			line = line.TrimStart();

			StringBuilder indent = new StringBuilder();
			if(line.Length == 0)
			{
				// Special treatment for empty lines:
				if(blockComment || (inString && verbatim))
					return;
				indent.Append(block.InnerIndent);
				if(block.OneLineBlock)
					indent.Append(set.IndentString);
				if(block.Continuation)
					indent.Append(set.IndentString);
				if(doc.Text != indent.ToString())
					doc.Text = indent.ToString();
				return;
			}

			if(TrimEnd(doc))
				line = doc.Text.TrimStart();

			Block oldBlock = block;
			bool startInComment = blockComment;
			bool startInString = (inString && verbatim);

			#region Parse char by char
			lineComment = false;
			inChar = false;
			escape = false;
			if(!verbatim) inString = false;

			lastRealChar = '\n';

			char lastchar = ' ';
			char c = ' ';
			char nextchar = line[0];
			for(int i = 0; i < line.Length; i++)
			{
				if(lineComment) break; // cancel parsing current line

				lastchar = c;
				c = nextchar;
				if(i + 1 < line.Length)
					nextchar = line[i + 1];
				else
					nextchar = '\n';

				if(escape)
				{
					escape = false;
					continue;
				}

				#region Check for comment/string chars
				switch(c)
				{
					case '/':
						if(blockComment && lastchar == '*')
							blockComment = false;
						if(!inString && !inChar)
						{
							if(!blockComment && nextchar == '/')
								lineComment = true;
							if(!lineComment && nextchar == '*')
								blockComment = true;
						}
						break;
					case '#':
						if(!(inChar || blockComment || inString))
							lineComment = true;
						break;
					case '"':
						if(!(inChar || lineComment || blockComment))
						{
							inString = !inString;
							if(!inString && verbatim)
							{
								if(nextchar == '"')
								{
									escape = true; // skip escaped quote
									inString = true;
								}
								else
								{
									verbatim = false;
								}
							}
							else if(inString && lastchar == '@')
							{
								verbatim = true;
							}
						}
						break;
					case '\'':
						if(!(inString || lineComment || blockComment))
						{
							inChar = !inChar;
						}
						break;
					case '\\':
						if((inString && !verbatim) || inChar)
							escape = true; // skip next character
						break;
				}
				#endregion

				if(lineComment || blockComment || inString || inChar)
				{
					if(wordBuilder.Length > 0)
						block.LastWord = wordBuilder.ToString();
					wordBuilder.Length = 0;
					continue;
				}

				if(!Char.IsWhiteSpace(c) && c != '[' && c != '/')
				{
					if(block.Bracket == '{')
						block.Continuation = true;
				}

				if(Char.IsLetterOrDigit(c))
				{
					wordBuilder.Append(c);
				}
				else
				{
					if(wordBuilder.Length > 0)
						block.LastWord = wordBuilder.ToString();
					wordBuilder.Length = 0;
				}

				#region Push/Pop the blocks
				switch(c)
				{
					case '{':
						block.OneLineBlock = false;
						blocks.Push(block);
						block.StartLine = doc.LineNumber;
						if(block.LastWord == "switch")
						{
							block.Indent(set, set.IndentString + set.IndentString);
							/* oldBlock refers to the previous line, not the previous block
							 * The block we want is not available anymore because it was never pushed.
							 * } else if (oldBlock.OneLineBlock) {
							// Inside a one-line-block is another statement
							// with a full block: indent the inner full block
							// by one additional level
							block.Indent(set, set.IndentString + set.IndentString);
							block.OuterIndent += set.IndentString;
							// Indent current line if it starts with the '{' character
							if (i == 0) {
								oldBlock.InnerIndent += set.IndentString;
							}*/
						}
						else
						{
							block.Indent(set);
						}
						block.Bracket = '{';
						break;
					case '}':
						while(block.Bracket != '{')
						{
							if(blocks.Count == 0) break;
							block = blocks.Pop();
						}
						if(blocks.Count == 0) break;
						block = blocks.Pop();
						block.Continuation = false;
						block.OneLineBlock = false;
						break;
					case '(':
					case '[':
						blocks.Push(block);
						if(block.StartLine == doc.LineNumber)
							block.InnerIndent = block.OuterIndent;
						else
							block.StartLine = doc.LineNumber;
						block.Indent(set,
									 (oldBlock.OneLineBlock ? set.IndentString : "") +
									 (oldBlock.Continuation ? set.IndentString : "") +
									 (i == line.Length - 1 ? set.IndentString : new String(' ', i + 1)));
						block.Bracket = c;
						break;
					case ')':
						if(blocks.Count == 0) break;
						if(block.Bracket == '(')
						{
							block = blocks.Pop();
							if(IsSingleStatementKeyword(block.LastWord))
								block.Continuation = false;
						}
						break;
					case ']':
						if(blocks.Count == 0) break;
						if(block.Bracket == '[')
							block = blocks.Pop();
						break;
					case ';':
					case ',':
						block.Continuation = false;
						block.OneLineBlock = false;
						break;
					case ':':
						if(block.LastWord == "case" || line.StartsWith("case ") || line.StartsWith(block.LastWord + ":"))
						{
							block.Continuation = false;
							block.OneLineBlock = false;
						}
						break;
				}

				if(!Char.IsWhiteSpace(c))
				{
					// register this char as last char
					lastRealChar = c;
				}
				#endregion
			}
			#endregion

			if(wordBuilder.Length > 0)
				block.LastWord = wordBuilder.ToString();
			wordBuilder.Length = 0;

			if(startInString) return;
			if(startInComment && line[0] != '*') return;
			if(doc.Text.StartsWith("//\t") || doc.Text == "//")
				return;

			if(line[0] == '}')
			{
				indent.Append(oldBlock.OuterIndent);
				oldBlock.OneLineBlock = false;
				oldBlock.Continuation = false;
			}
			else
			{
				indent.Append(oldBlock.InnerIndent);
			}

			if(indent.Length > 0 && oldBlock.Bracket == '(' && line[0] == ')')
			{
				indent.Remove(indent.Length - 1, 1);
			}
			else if(indent.Length > 0 && oldBlock.Bracket == '[' && line[0] == ']')
			{
				indent.Remove(indent.Length - 1, 1);
			}

			if(line[0] == ':')
			{
				oldBlock.Continuation = true;
			}
			else if(lastRealChar == ':' && indent.Length >= set.IndentString.Length)
			{
				if(block.LastWord == "case" || line.StartsWith("case ") || line.StartsWith(block.LastWord + ":"))
					indent.Remove(indent.Length - set.IndentString.Length, set.IndentString.Length);
			}
			else if(lastRealChar == ')')
			{
				if(IsSingleStatementKeyword(block.LastWord))
				{
					block.OneLineBlock = true;
				}
			}
			else if(lastRealChar == 'e' && block.LastWord == "else")
			{
				block.OneLineBlock = true;
				block.Continuation = false;
			}

			if(doc.ReadOnly)
			{
				// We can't change the current line, but we should accept the existing
				// indentation if possible (=if the current statement is not a multiline
				// statement).
				if(!oldBlock.Continuation && !oldBlock.OneLineBlock &&
					oldBlock.StartLine == block.StartLine &&
					block.StartLine < doc.LineNumber && lastRealChar != ':')
				{
					// use indent StringBuilder to get the indentation of the current line
					indent.Length = 0;
					line = doc.Text; // get untrimmed line
					for(int i = 0; i < line.Length; ++i)
					{
						if(!Char.IsWhiteSpace(line[i]))
							break;
						indent.Append(line[i]);
					}
					// /* */ multiline comments have an extra space - do not count it
					// for the block's indentation.
					if(startInComment && indent.Length > 0 && indent[indent.Length - 1] == ' ')
					{
						indent.Length -= 1;
					}
					block.InnerIndent = indent.ToString();
				}
				return;
			}

			if(line[0] != '{')
			{
				if(line[0] != ')' && oldBlock.Continuation && oldBlock.Bracket == '{')
					indent.Append(set.IndentString);
				if(oldBlock.OneLineBlock)
					indent.Append(set.IndentString);
			}

			// this is only for blockcomment lines starting with *,
			// all others keep their old indentation
			if(startInComment)
				indent.Append(' ');

			if(indent.Length != (doc.Text.Length - line.Length) ||
				!doc.Text.StartsWith(indent.ToString()) ||
				Char.IsWhiteSpace(doc.Text[indent.Length]))
			{
				doc.Text = indent.ToString() + line;
			}
		}

		bool IsSingleStatementKeyword(string keyword)
		{
			switch(keyword)
			{
				case "if":
				case "for":
				case "while":
				case "do":
				case "foreach":
				case "using":
				case "lock":
					return true;
				default:
					return false;
			}
		}

		bool TrimEnd(IDocumentAccessor doc)
		{
			string line = doc.Text;
			if(!Char.IsWhiteSpace(line[line.Length - 1])) return false;

			// one space after an empty comment is allowed
			if(line.EndsWith("// ") || line.EndsWith("* "))
				return false;

			doc.Text = line.TrimEnd();
			return true;
		}
	}
}
