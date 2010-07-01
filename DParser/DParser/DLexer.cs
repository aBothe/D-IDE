using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;
using System.Collections;

namespace D_Parser
{
    /// <summary>
    /// Taken from SharpDevelop.NRefactory
    /// </summary>
    public abstract class AbstractLexer
    {
        TextReader reader;
        int col = 1;
        int line = 1;

        protected DToken lastToken = null;
        protected DToken curToken = null;
        protected DToken peekToken = null;

        string[] specialCommentTags = null;
        protected Hashtable specialCommentHash = null;
        protected StringBuilder sb = new StringBuilder();

        // used for the original value of strings (with escape sequences).
        protected StringBuilder originalValue = new StringBuilder();

        public bool SkipAllComments { get; set; }
        public bool EvaluateConditionalCompilation { get; set; }
        public virtual IDictionary<string, object> ConditionalCompilationSymbols
        {
            get { throw new NotSupportedException(); }
        }

        protected static IEnumerable<string> GetSymbols(string symbols)
        {
            if (!string.IsNullOrEmpty(symbols))
            {
                foreach (string symbol in symbols.Split(';', ' ', '\t'))
                {
                    string s = symbol.Trim();
                    if (s.Length == 0)
                        continue;
                    yield return s;
                }
            }
        }

        public virtual void SetConditionalCompilationSymbols(string symbols)
        {
            throw new NotSupportedException();
        }

        protected int Line
        {
            get
            {
                return line;
            }
        }
        protected int Col
        {
            get
            {
                return col;
            }
        }

        protected bool recordRead = false;
        protected StringBuilder recordedText = new StringBuilder();

        protected int ReaderRead()
        {
            int val = reader.Read();
            if (recordRead)
                recordedText.Append((char)val);
            if ((val == '\r' && reader.Peek() != '\n') || val == '\n')
            {
                ++line;
                col = 1;
                LineBreak();
            }
            else if (val >= 0)
            {
                col++;
            }
            return val;
        }
        protected int ReaderPeek()
        {
            return reader.Peek();
        }

        public void SetInitialLocation(Location location)
        {
            if (lastToken != null || curToken != null || peekToken != null)
                throw new InvalidOperationException();
            this.line = location.Line;
            this.col = location.Column;
        }

        /// <summary>
        /// Special comment tags are tags like TODO, HACK or UNDONE which are read by the lexer and stored in <see cref="TagComments"/>.
        /// </summary>
        public string[] SpecialCommentTags
        {
            get
            {
                return specialCommentTags;
            }
            set
            {
                specialCommentTags = value;
                specialCommentHash = null;
                if (specialCommentTags != null && specialCommentTags.Length > 0)
                {
                    specialCommentHash = new Hashtable();
                    foreach (string str in specialCommentTags)
                    {
                        specialCommentHash.Add(str, null);
                    }
                }
            }
        }

        /// <summary>
        /// The current DToken. <seealso cref="ICSharpCode.NRefactory.Parser.DToken"/>
        /// </summary>
        public DToken CurrentToken
        {
            get
            {
                //				Console.WriteLine("Call to DToken");
                return lastToken;
            }
        }

        /// <summary>
        /// The next DToken (The <see cref="CurrentToken"/> after <see cref="NextToken"/> call) . <seealso cref="ICSharpCode.NRefactory.Parser.DToken"/>
        /// </summary>
        public DToken LookAhead
        {
            get
            {
                //				Console.WriteLine("Call to LookAhead");
                return curToken;
            }
        }

        /// <summary>
        /// Constructor for the abstract lexer class.
        /// </summary>
        protected AbstractLexer(TextReader reader)
        {
            this.reader = reader;
        }

        #region System.IDisposable interface implementation
        public virtual void Dispose()
        {
            reader.Close();
            reader = null;
            lastToken = curToken = peekToken = null;
            specialCommentHash = null;
            sb = originalValue = null;
        }
        #endregion

        /// <summary>
        /// Must be called before a peek operation.
        /// </summary>
        public void StartPeek()
        {
            peekToken = curToken;
        }

        /// <summary>
        /// Gives back the next token. A second call to Peek() gives the next token after the last call for Peek() and so on.
        /// </summary>
        /// <returns>An <see cref="CurrentToken"/> object.</returns>
        public DToken Peek()
        {
            //			Console.WriteLine("Call to Peek");
            if (peekToken.next == null)
            {
                peekToken.next = Next();
                //specialTracker.InformToken(peekToken.next.kind);
            }
            peekToken = peekToken.next;
            return peekToken;
        }

        /// <summary>
        /// Reads the next token and gives it back.
        /// </summary>
        /// <returns>An <see cref="CurrentToken"/> object.</returns>
        public virtual DToken NextToken()
        {
            if (curToken == null)
            {
                curToken = Next();
                //specialTracker.InformToken(curToken.Kind);
                //Console.WriteLine(ICSharpCode.NRefactory.Parser.CSharp.Tokens.GetTokenString(curToken.kind) + " -- " + curToken.val + "(" + curToken.kind + ")");
                return curToken;
            }

            lastToken = curToken;

            if (curToken.next == null)
            {
                curToken.next = Next();
                if (curToken.next != null)
                {
                    //specialTracker.InformToken(curToken.next.Kind);
                }
            }

            curToken = curToken.next;
            //Console.WriteLine(ICSharpCode.NRefactory.Parser.CSharp.Tokens.GetTokenString(curToken.kind) + " -- " + curToken.val + "(" + curToken.kind + ")");
            return curToken;
        }

        protected abstract DToken Next();

        protected static bool IsIdentifierPart(int ch)
        {
            if (ch == 95) return true;  // 95 = '_'
            if (ch == -1) return false;
            return char.IsLetterOrDigit((char)ch); // accept unicode letters
        }

        protected static bool IsHex(char digit)
        {
            return Char.IsDigit(digit) || ('A' <= digit && digit <= 'F') || ('a' <= digit && digit <= 'f');
        }

        protected int GetHexNumber(char digit)
        {
            if (Char.IsDigit(digit))
            {
                return digit - '0';
            }
            if ('A' <= digit && digit <= 'F')
            {
                return digit - 'A' + 0xA;
            }
            if ('a' <= digit && digit <= 'f')
            {
                return digit - 'a' + 0xA;
            }
            //errors.Error(line, col, String.Format("Invalid hex number '" + digit + "'"));
            return 0;
        }
        protected Location lastLineEnd = new Location(1, 1);
        protected Location curLineEnd = new Location(1, 1);
        protected void LineBreak()
        {
            lastLineEnd = curLineEnd;
            curLineEnd = new Location(col - 1, line);
        }
        protected bool HandleLineEnd(char ch)
        {
            // Handle MS-DOS or MacOS line ends.
            if (ch == '\r')
            {
                if (reader.Peek() == '\n')
                { // MS-DOS line end '\r\n'
                    ReaderRead(); // LineBreak (); called by ReaderRead ();
                    return true;
                }
                else
                { // assume MacOS line end which is '\r'
                    LineBreak();
                    return true;
                }
            }
            if (ch == '\n')
            {
                LineBreak();
                return true;
            }
            return false;
        }

        protected void SkipToEndOfLine()
        {
            int nextChar;
            while ((nextChar = reader.Read()) != -1)
            {
                if (nextChar == '\r')
                {
                    if (reader.Peek() == '\n')
                        reader.Read();
                    nextChar = '\n';
                }
                if (nextChar == '\n')
                {
                    ++line;
                    col = 1;
                    break;
                }
            }
        }

        protected string ReadToEndOfLine()
        {
            sb.Length = 0;
            int nextChar;
            while ((nextChar = reader.Read()) != -1)
            {
                char ch = (char)nextChar;

                if (nextChar == '\r')
                {
                    if (reader.Peek() == '\n')
                        reader.Read();
                    nextChar = '\n';
                }
                // Return read string, if EOL is reached
                if (nextChar == '\n')
                {
                    ++line;
                    col = 1;
                    return sb.ToString();
                }

                sb.Append(ch);
            }

            // Got EOF before EOL
            string retStr = sb.ToString();
            col += retStr.Length;
            return retStr;
        }

        /// <summary>
        /// Skips to the end of the current code block.
        /// For this, the lexer must have read the next token AFTER the token opening the
        /// block (so that Lexer.DToken is the block-opening token, not Lexer.LookAhead).
        /// After the call, Lexer.LookAhead will be the block-closing token.
        /// </summary>
        public abstract void SkipCurrentBlock(int targetToken);
    }

	public class DLexer : AbstractLexer
	{
		public DLexer(TextReader reader) :base(reader)
		{
			Comments = new List<Comment>();
		}

		#region Abstract Lexer Props & Methods
		#region Properties
		public List<Comment> Comments;
		/*protected DToken lastToken = null;
		protected DToken curToken = null;
		protected DToken curTokennext = null;
		protected DToken peekToken = null;
		protected DToken peekTokennext = null;
		string[] specialCommentTags = null;
		protected StringBuilder sb = new StringBuilder();

		// used for the original value of strings (with escape sequences).
		protected StringBuilder originalValue = new StringBuilder();
		protected Hashtable specialCommentHash = null;*/

		//public delegate void LexerErrorHandler(int line, int col, string message);
		//static public event LexerErrorHandler OnError;
		void OnError(int line, int col, string message)
		{
			//errors.Error(line, col, message);
		}
		#endregion
		#endregion

		protected override DToken Next()
		{
			int nextChar;
			char ch;
			bool hadLineEnd = false;
			if(Line == 1 && Col == 1) hadLineEnd = true; // beginning of document

			while((nextChar = ReaderRead()) != -1)
			{
				DToken token;

				switch(nextChar)
				{
					case ' ':
					case '\t':
						continue;
					case '\r':
					case '\n':
						if(hadLineEnd)
						{
							// second line end before getting to a token
							// -> here was a blank line
							//specialTracker.AddEndOfLine(new Location(Col, Line));
						}
						HandleLineEnd((char)nextChar);
						hadLineEnd = true;
						continue;
					case '/':
						int peek = ReaderPeek();
						if(peek == '/' || peek == '*' || peek == '+')
						{
							ReadComment();
							continue;
						}
						else
						{
							token = ReadOperator('/');
						}
						break;
					case '"':
						token = ReadString();
						break;
					case '\'':
						token = ReadChar();
						break;
					case '@':
						int next = ReaderRead();
						if(next == -1)
						{
							OnError(Line, Col, String.Format("EOF after @"));
							continue;
						}
						else
						{
							int x = Col - 1;
							int y = Line;
							ch = (char)next;
							if(ch == '"')
							{
								token = ReadVerbatimString();
							}
							else if(Char.IsLetterOrDigit(ch) || ch == '_')
							{
								bool canBeKeyword;
								string ident = ReadIdent(ch, out canBeKeyword);
								int tkind = DTokens.GetTokenID("@" + ident);
								token = new DToken(tkind<0? DTokens.Identifier:tkind, x - 1, y, (tkind<0?"":"@")+ident);
							}
							else
							{
								OnError(y, x, String.Format("Unexpected char in Lexer.Next() : {0}", ch));
								continue;
							}
						}
						break;
					default:
						ch = (char)nextChar;
						if(Char.IsLetter(ch) || ch == '_' || ch == '\\')
						{
							int x = Col - 1; // Col was incremented above, but we want the start of the identifier
							int y = Line;
							bool canBeKeyword;
							string s = ReadIdent(ch, out canBeKeyword);
							if(canBeKeyword)
							{
								int keyWordToken = DKeywords.GetToken(s);
								if(keyWordToken >= 0)
								{
									return new DToken(keyWordToken, x, y);
								}
							}
							return new DToken(DTokens.Identifier, x, y, s);
						}
						else if(Char.IsDigit(ch))
						{
							token = ReadDigit(ch, Col - 1);
						}
						else
						{
							token = ReadOperator(ch);
						}
						break;
				}

				// try error recovery (token = null -> continue with next char)
				if(token != null)
				{
					//token.prev = base.curToken;
					return token;
				}
			}

			return new DToken(DTokens.EOF, Col, Line, String.Empty);
		}

		// The C# compiler has a fixed size length therefore we'll use a fixed size char array for identifiers
		// it's also faster than using a string builder.
		const int MAX_IDENTIFIER_LENGTH = 512;
		char[] identBuffer = new char[MAX_IDENTIFIER_LENGTH];

		string ReadIdent(char ch, out bool canBeKeyword)
		{
			int peek;
			int curPos = 0;
			canBeKeyword = true;
			while(true)
			{
				if(ch == '\\')
				{
					peek = ReaderPeek();
					if(peek != 'u' && peek != 'U')
					{
						OnError(Line, Col, "Identifiers can only contain unicode escape sequences");
					}
					canBeKeyword = false;
					string surrogatePair;
					ReadEscapeSequence(out ch, out surrogatePair);
					if(surrogatePair != null)
					{
						if(!char.IsLetterOrDigit(surrogatePair, 0))
						{
							OnError(Line, Col, "Unicode escape sequences in identifiers cannot be used to represent characters that are invalid in identifiers");
						}
						for(int i = 0; i < surrogatePair.Length - 1; i++)
						{
							if(curPos < MAX_IDENTIFIER_LENGTH)
							{
								identBuffer[curPos++] = surrogatePair[i];
							}
						}
						ch = surrogatePair[surrogatePair.Length - 1];
					}
					else
					{
						if(!IsIdentifierPart(ch))
						{
							OnError(Line, Col, "Unicode escape sequences in identifiers cannot be used to represent characters that are invalid in identifiers");
						}
					}
				}

				if(curPos < MAX_IDENTIFIER_LENGTH)
				{
					identBuffer[curPos++] = ch;
				}
				else
				{
					OnError(Line, Col, String.Format("Identifier too long"));
					while(IsIdentifierPart(ReaderPeek()))
					{
						ReaderRead();
					}
					break;
				}
				peek = ReaderPeek();
				if(IsIdentifierPart(peek) || peek == '\\')
				{
					ch = (char)ReaderRead();
				}
				else
				{
					break;
				}
			}
			return new String(identBuffer, 0, curPos);
		}

		DToken ReadDigit(char ch, int x)
		{
			unchecked
			{ // prevent exception when ReaderPeek() = -1 is cast to char
				int y = Line;
				sb.Length = 0;
				sb.Append(ch);
				string prefix = null;
				string suffix = null;

				bool ishex = false;
				bool isunsigned = false;
				bool islong = false;
				bool isfloat = false;
				bool isdouble = false;
				bool isdecimal = false;

				char peek = (char)ReaderPeek();

				if(ch == '.')
				{
					isdouble = true;

					while(Char.IsDigit((char)ReaderPeek()))
					{ // read decimal digits beyond the dot
						sb.Append((char)ReaderRead());
					}
					peek = (char)ReaderPeek();
				}
				else if(ch == '0' && (peek == 'x' || peek == 'X'))
				{
					ReaderRead(); // skip 'x'
					sb.Length = 0; // Remove '0' from 0x prefix from the stringvalue
					while(IsHex((char)ReaderPeek()))
					{
						sb.Append((char)ReaderRead());
					}
					if(sb.Length == 0)
					{
						sb.Append('0'); // dummy value to prevent exception
						OnError(y, x, "Invalid hexadecimal integer literal");
					}
					ishex = true;
					prefix = "0x";
					peek = (char)ReaderPeek();
				}
				else
				{
					while(Char.IsDigit((char)ReaderPeek()))
					{
						sb.Append((char)ReaderRead());
					}
					peek = (char)ReaderPeek();
				}

				DToken nextToken = null; // if we accidently read a 'dot'
				if(peek == '.')
				{ // read floating point number
					ReaderRead();
					peek = (char)ReaderPeek();
					if(!Char.IsDigit(peek))
					{
						nextToken = new DToken(DTokens.Dot, Col - 1, Line);
						peek = '.';
					}
					else
					{
						isdouble = true; // double is default
						if(ishex)
						{
							OnError(y, x, String.Format("No hexadecimal floating point values allowed"));
						}
						sb.Append('.');

						while(Char.IsDigit((char)ReaderPeek()))
						{ // read decimal digits beyond the dot
							sb.Append((char)ReaderRead());
						}
						peek = (char)ReaderPeek();
					}
				}

				if(peek == 'e' || peek == 'E')
				{ // read exponent
					isdouble = true;
					sb.Append((char)ReaderRead());
					peek = (char)ReaderPeek();
					if(peek == '-' || peek == '+')
					{
						sb.Append((char)ReaderRead());
					}
					while(Char.IsDigit((char)ReaderPeek()))
					{ // read exponent value
						sb.Append((char)ReaderRead());
					}
					isunsigned = true;
					peek = (char)ReaderPeek();
				}

				if(peek == 'f' || peek == 'F')
				{ // float value
					ReaderRead();
					suffix = "f";
					isfloat = true;
				}
				else if(peek == 'd' || peek == 'D')
				{ // double type suffix (obsolete, double is default)
					ReaderRead();
					suffix = "d";
					isdouble = true;
				}
				else if(peek == 'm' || peek == 'M')
				{ // decimal value
					ReaderRead();
					suffix = "m";
					isdecimal = true;
				}
				else if(!isdouble)
				{
					if(peek == 'u' || peek == 'U')
					{
						ReaderRead();
						suffix = "u";
						isunsigned = true;
						peek = (char)ReaderPeek();
					}

					if(peek == 'l' || peek == 'L')
					{
						ReaderRead();
						peek = (char)ReaderPeek();
						islong = true;
						if(!isunsigned && (peek == 'u' || peek == 'U'))
						{
							ReaderRead();
							suffix = "lu";
							isunsigned = true;
						}
						else
						{
							suffix = isunsigned ? "ul" : "l";
						}
					}
				}

				string digit = sb.ToString();
				string stringValue = prefix + digit + suffix;

				if(isfloat)
				{
					float num;
					if(float.TryParse(digit, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
					{
						return new DToken(DTokens.Literal, new Location(x, y), new Location(x+stringValue.Length, y), stringValue, num, LiteralFormat.DecimalNumber);
					}
					else
					{
						OnError(y, x, String.Format("Can't parse float {0}", digit));
						return new DToken(DTokens.Literal, new Location(x, y), new Location(x + stringValue.Length, y), stringValue, 0f, LiteralFormat.DecimalNumber);
					}
				}
				if(isdecimal)
				{
					decimal num;
					if(decimal.TryParse(digit, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
					{
						return new DToken(DTokens.Literal, new Location(x, y), new Location(x + stringValue.Length, y), stringValue, num, LiteralFormat.DecimalNumber);
					}
					else
					{
						OnError(y, x, String.Format("Can't parse decimal {0}", digit));
						return new DToken(DTokens.Literal, new Location(x, y), new Location(x + stringValue.Length, y), stringValue, 0m, LiteralFormat.DecimalNumber);
					}
				}
				if(isdouble)
				{
					double num;
					if(double.TryParse(digit, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
					{
						return new DToken(DTokens.Literal, new Location(x, y), new Location(x + stringValue.Length, y), stringValue, num, LiteralFormat.DecimalNumber);
					}
					else
					{
						OnError(y, x, String.Format("Can't parse double {0}", digit));
						return new DToken(DTokens.Literal, new Location(x, y), new Location(x + stringValue.Length, y), stringValue, 0d, LiteralFormat.DecimalNumber);
					}
				}

				// Try to determine a parsable value using ranges.
				ulong result;
				if(ishex)
				{
					if(!ulong.TryParse(digit, NumberStyles.HexNumber, null, out result))
					{
						OnError(y, x, String.Format("Can't parse hexadecimal constant {0}", digit));
						return new DToken(DTokens.Literal, new Location(x, y), new Location(x + stringValue.Length, y), stringValue.ToString(), 0, LiteralFormat.DecimalNumber);
					}
				}
				else
				{
					if(!ulong.TryParse(digit, NumberStyles.Integer, null, out result))
					{
						OnError(y, x, String.Format("Can't parse integral constant {0}", digit));
						return new DToken(DTokens.Literal, new Location(x, y), new Location(x + stringValue.Length, y), stringValue.ToString(), 0, LiteralFormat.DecimalNumber);
					}
				}

				if(result > long.MaxValue)
				{
					islong = true;
					isunsigned = true;
				}
				else if(result > uint.MaxValue)
				{
					islong = true;
				}
				else if(result > int.MaxValue)
				{
					isunsigned = true;
				}

				DToken token;

				if(islong)
				{
					if(isunsigned)
					{
						ulong num;
						if(ulong.TryParse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number, CultureInfo.InvariantCulture, out num))
						{
							token = new DToken(DTokens.Literal, new Location(x, y), new Location(x + stringValue.Length, y), stringValue, num, LiteralFormat.DecimalNumber);
						}
						else
						{
							OnError(y, x, String.Format("Can't parse unsigned long {0}", digit));
							token = new DToken(DTokens.Literal, new Location(x, y), new Location(x + stringValue.Length, y), stringValue, 0UL, LiteralFormat.DecimalNumber);
						}
					}
					else
					{
						long num;
						if(long.TryParse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number, CultureInfo.InvariantCulture, out num))
						{
							token = new DToken(DTokens.Literal, new Location(x, y), new Location(x + stringValue.Length, y), stringValue, num, LiteralFormat.DecimalNumber);
						}
						else
						{
							OnError(y, x, String.Format("Can't parse long {0}", digit));
							token = new DToken(DTokens.Literal, new Location(x, y), new Location(x+stringValue.Length, y), stringValue, 0L, LiteralFormat.DecimalNumber);
						}
					}
				}
				else
				{
					if(isunsigned)
					{
						uint num;
						if(uint.TryParse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number, CultureInfo.InvariantCulture, out num))
						{
							token = new DToken(DTokens.Literal, new Location(x, y), new Location(x + stringValue.Length, y), stringValue, num, LiteralFormat.DecimalNumber);
						}
						else
						{
							OnError(y, x, String.Format("Can't parse unsigned int {0}", digit));
							token = new DToken(DTokens.Literal, new Location(x, y), new Location(x + stringValue.Length, y), stringValue, (uint)0, LiteralFormat.DecimalNumber);
						}
					}
					else
					{
						int num;
						if(int.TryParse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number, CultureInfo.InvariantCulture, out num))
						{
							token = new DToken(DTokens.Literal, new Location(x, y), new Location(x + stringValue.Length, y), stringValue, num, LiteralFormat.DecimalNumber);
						}
						else
						{
							OnError(y, x, String.Format("Can't parse int {0}", digit));
							token = new DToken(DTokens.Literal, new Location(x, y), new Location(x + stringValue.Length, y), stringValue, 0, LiteralFormat.DecimalNumber);
						}
					}
				}
				//token.next = nextToken;
				return token;
			}
		}

		DToken ReadString()
		{
			int x = Col - 1;
			int y = Line;

			sb.Length = 0;
			originalValue.Length = 0;
			originalValue.Append('"');
			bool doneNormally = false;
			int nextChar;
			while((nextChar = ReaderRead()) != -1)
			{
				char ch = (char)nextChar;

				if(ch == '"')
				{
					doneNormally = true;
					originalValue.Append('"');
					// Skip string literals
					ch = (char)this.ReaderPeek();
					if(ch == 'c' || ch == 'w' || ch == 'd') ReaderRead();
					break;
				}
				HandleLineEnd(ch);
				if(ch == '\\')
				{
					originalValue.Append('\\');
					string surrogatePair;
					
					originalValue.Append(ReadEscapeSequence(out ch, out surrogatePair));
					if(surrogatePair != null)
					{
						sb.Append(surrogatePair);
					}
					else
					{
						sb.Append(ch);
					}
				}
				else
				{
					originalValue.Append(ch);
					sb.Append(ch);
				}
			}

			if(!doneNormally)
			{
				OnError(y, x, String.Format("End of file reached inside string literal"));
			}

			return new DToken(DTokens.Literal, new Location(x, y), new Location(x + originalValue.Length, y), originalValue.ToString(), sb.ToString(),LiteralFormat.StringLiteral);
		}

		DToken ReadVerbatimString()
		{
			sb.Length = 0;
			originalValue.Length = 0;
			originalValue.Append("@\"");
			int x = Col - 2; // @ and " already read
			int y = Line;
			int nextChar;
			while((nextChar = ReaderRead()) != -1)
			{
				char ch = (char)nextChar;

				if(ch == '"')
				{
					if(ReaderPeek() != '"')
					{
						originalValue.Append('"');
						break;
					}
					originalValue.Append("\"\"");
					sb.Append('"');
					ReaderRead();
				}
				else if(HandleLineEnd(ch))
				{
					sb.Append("\r\n");
					originalValue.Append("\r\n");
				}
				else
				{
					sb.Append(ch);
					originalValue.Append(ch);
				}
			}

			if(nextChar == -1)
			{
				OnError(y, x, String.Format("End of file reached inside verbatim string literal"));
			}

			return new DToken(DTokens.Literal, new Location(x, y), new Location(x + originalValue.Length, y), originalValue.ToString(), sb.ToString(),LiteralFormat.VerbatimStringLiteral);
		}

		char[] escapeSequenceBuffer = new char[12];

		/// <summary>
		/// reads an escape sequence
		/// </summary>
		/// <param name="ch">The character represented by the escape sequence,
		/// or '\0' if there was an error or the escape sequence represents a character that
		/// can be represented only be a suggorate pair</param>
		/// <param name="surrogatePair">Null, except when the character represented
		/// by the escape sequence can only be represented by a surrogate pair (then the string
		/// contains the surrogate pair)</param>
		/// <returns>The escape sequence</returns>
		string ReadEscapeSequence(out char ch, out string surrogatePair)
		{
			surrogatePair = null;

			int nextChar = ReaderRead();
			if(nextChar == -1)
			{
				OnError(Line, Col, String.Format("End of file reached inside escape sequence"));
				ch = '\0';
				return String.Empty;
			}
			int number;
			char c = (char)nextChar;
			int curPos = 1;
			escapeSequenceBuffer[0] = c;
			switch(c)
			{
				case '\'':
					ch = '\'';
					break;
				case '\"':
					ch = '\"';
					break;
				case '\\':
					ch = '\\';
					break;
				case '0':
					ch = '\0';
					break;
				case 'a':
					ch = '\a';
					break;
				case 'b':
					ch = '\b';
					break;
				case 'f':
					ch = '\f';
					break;
				case 'n':
					ch = '\n';
					break;
				case 'r':
					ch = '\r';
					break;
				case 't':
					ch = '\t';
					break;
				case 'v':
					ch = '\v';
					break;
				case 'u':
				case 'x':
					// 16 bit unicode character
					c = (char)ReaderRead();
					number = GetHexNumber(c);
					escapeSequenceBuffer[curPos++] = c;

					if(number < 0)
					{
						OnError(Line, Col - 1, String.Format("Invalid char in literal : {0}", c));
					}
					for(int i = 0; i < 3; ++i)
					{
						if(IsHex((char)ReaderPeek()))
						{
							c = (char)ReaderRead();
							int idx = GetHexNumber(c);
							escapeSequenceBuffer[curPos++] = c;
							number = 16 * number + idx;
						}
						else
						{
							break;
						}
					}
					ch = (char)number;
					break;
				case 'U':
					// 32 bit unicode character
					number = 0;
					for(int i = 0; i < 8; ++i)
					{
						if(IsHex((char)ReaderPeek()))
						{
							c = (char)ReaderRead();
							int idx = GetHexNumber(c);
							escapeSequenceBuffer[curPos++] = c;
							number = 16 * number + idx;
						}
						else
						{
							OnError(Line, Col - 1, String.Format("Invalid char in literal : {0}", (char)ReaderPeek()));
							break;
						}
					}
					if(number > 0xffff)
					{
						ch = '\0';
						surrogatePair = char.ConvertFromUtf32(number);
					}
					else
					{
						ch = (char)number;
					}
					break;
				default:
					OnError(Line, Col, String.Format("Unexpected escape sequence : {0}", c));
					ch = '\0';
					break;
			}
			return new String(escapeSequenceBuffer, 0, curPos);
		}

		DToken ReadChar()
		{
			int x = Col - 1;
			int y = Line;
			int nextChar = ReaderRead();
			if(nextChar == -1)
			{
				OnError(y, x, String.Format("End of file reached inside character literal"));
				return null;
			}
			char ch = (char)nextChar;
			char chValue = ch;
			string escapeSequence = String.Empty;
			if(ch == '\\')
			{
				string surrogatePair;
				escapeSequence = ReadEscapeSequence(out chValue, out surrogatePair);
				if(surrogatePair != null)
				{
					OnError(y, x, String.Format("The unicode character must be represented by a surrogate pair and does not fit into a System.Char"));
				}
			}

			unchecked
			{
				if((char)ReaderRead() != '\'')
				{
					OnError(y, x, String.Format("Char not terminated"));
				}
			}
			return new DToken(DTokens.Literal, new Location(x, y), new Location(x + 1, y), "'" + ch + escapeSequence + "'", chValue,LiteralFormat.CharLiteral);
		}

		DToken ReadOperator(char ch)
		{
			int x = Col - 1;
			int y = Line;
			switch(ch)
			{
				case '+':
					switch(ReaderPeek())
					{
						case '+':
							ReaderRead();
							return new DToken(DTokens.Increment, x, y);
						case '=':
							ReaderRead();
							return new DToken(DTokens.PlusAssign, x, y);
					}
					return new DToken(DTokens.Plus, x, y);
				case '-':
					switch(ReaderPeek())
					{
						case '-':
							ReaderRead();
							return new DToken(DTokens.Decrement, x, y);
						case '=':
							ReaderRead();
							return new DToken(DTokens.MinusAssign, x, y);
						case '>':
							ReaderRead();
							return new DToken(DTokens.TildeAssign, x, y);
					}
					return new DToken(DTokens.Minus, x, y);
				case '*':
					switch(ReaderPeek())
					{
						case '=':
							ReaderRead();
							return new DToken(DTokens.TimesAssign, x, y);
						default:
							break;
					}
					return new DToken(DTokens.Times, x, y);
				case '/':
					switch(ReaderPeek())
					{
						case '=':
							ReaderRead();
							return new DToken(DTokens.DivAssign, x, y);
					}
					return new DToken(DTokens.Div, x, y);
				case '%':
					switch(ReaderPeek())
					{
						case '=':
							ReaderRead();
							return new DToken(DTokens.ModAssign, x, y);
					}
					return new DToken(DTokens.Mod, x, y);
				case '&':
					switch(ReaderPeek())
					{
						case '&':
							ReaderRead();
							return new DToken(DTokens.LogicalAnd, x, y);
						case '=':
							ReaderRead();
							return new DToken(DTokens.BitwiseAndAssign, x, y);
					}
					return new DToken(DTokens.BitwiseAnd, x, y);
				case '|':
					switch(ReaderPeek())
					{
						case '|':
							ReaderRead();
							return new DToken(DTokens.LogicalOr, x, y);
						case '=':
							ReaderRead();
							return new DToken(DTokens.BitwiseOrAssign, x, y);
					}
					return new DToken(DTokens.BitwiseOr, x, y);
				case '^':
					switch(ReaderPeek())
					{
						case '=':
							ReaderRead();
							return new DToken(DTokens.XorAssign, x, y);
						default:
							break;
					}
					return new DToken(DTokens.Xor, x, y);
				case '!':
					switch(ReaderPeek())
					{
						case '=':
							ReaderRead();
							return new DToken(DTokens.NotEqual, x, y);
					}
					return new DToken(DTokens.Not, x, y);
				case '~':
					switch(ReaderPeek())
					{
						case '=':
							ReaderRead();
							return new DToken(DTokens.TildeAssign, x, y);
					}
					return new DToken(DTokens.Tilde, x, y);
				case '=':
					switch(ReaderPeek())
					{
						case '=':
							ReaderRead();
							return new DToken(DTokens.Equal, x, y);
					}
					return new DToken(DTokens.Assign, x, y);
				case '<':
					switch(ReaderPeek())
					{
						case '<':
							ReaderRead();
							switch(ReaderPeek())
							{
								case '=':
									ReaderRead();
									return new DToken(DTokens.ShiftLeftAssign, x, y);
								default:
									break;
							}
							return new DToken(DTokens.ShiftLeft, x, y);
						case '=':
							ReaderRead();
							return new DToken(DTokens.LessEqual, x, y);
					}
					return new DToken(DTokens.LessThan, x, y);
				case '>':
					switch(ReaderPeek())
					{
						case '>':
							ReaderRead();
							if(ReaderPeek() != -1)
							{
								switch((char)ReaderPeek())
								{
									case '=':
										ReaderRead();
										return new DToken(DTokens.ShiftRightAssign, x, y);
									case '>':
										ReaderRead();
										if(ReaderPeek() != -1)
										{
											switch((char)ReaderPeek())
											{
												case '=':
													ReaderRead();
													return new DToken(DTokens.TripleRightAssign, x, y);
												default:
													break;
											}
										}
										break;
									default:
										break;
								}
							}
							return new DToken(DTokens.ShiftLeft, x, y);
						case '=':
							ReaderRead();
							return new DToken(DTokens.GreaterEqual, x, y);
					}
					return new DToken(DTokens.GreaterThan, x, y);
				case '?':
					if(ReaderPeek() == '?')
					{
						ReaderRead();
						return new DToken(DTokens.DoubleQuestion, x, y);
					}
					return new DToken(DTokens.Question, x, y);
				case ';':
					return new DToken(DTokens.Semicolon, x, y);
				case ':':
					if(ReaderPeek() == ':')
					{
						ReaderRead();
						return new DToken(DTokens.DoubleColon, x, y);
					}
					return new DToken(DTokens.Colon, x, y);
				case ',':
					return new DToken(DTokens.Comma, x, y);
				case '.':
					// Prevent OverflowException when ReaderPeek returns -1
					int tmp = ReaderPeek();
					if(tmp > 0 && Char.IsDigit((char)tmp))
					{
						return ReadDigit('.', Col - 1);
					}
					return new DToken(DTokens.Dot, x, y);
				case ')':
					return new DToken(DTokens.CloseParenthesis, x, y);
				case '(':
					return new DToken(DTokens.OpenParenthesis, x, y);
				case ']':
					return new DToken(DTokens.CloseSquareBracket, x, y);
				case '[':
					return new DToken(DTokens.OpenSquareBracket, x, y);
				case '}':
					return new DToken(DTokens.CloseCurlyBrace, x, y);
				case '{':
					return new DToken(DTokens.OpenCurlyBrace, x, y);
				default:
					return null;
			}
		}

		void ReadComment()
		{
			switch(ReaderRead())
			{
				case '+':
					if(ReaderPeek() == '+')// DDoc
					{
						while(ReaderPeek() == '+') ReaderRead(); // Skip initial "++++"
						if(ReaderRead() != '/')
							ReadMultiLineComment(Comment.Type.Documentation, true);
					}
					else
					{
						ReadMultiLineComment(Comment.Type.Block, true);
					}
					break;
				case '*':
					if(ReaderPeek() == '*')// DDoc
					{
						while(ReaderPeek() == '*') ReaderRead(); // Skip initial "****"
						if(ReaderRead() != '/')
							ReadMultiLineComment(Comment.Type.Documentation, false);
					}
					else
					{
						ReadMultiLineComment(Comment.Type.Block, false);
					}
					break;
				case '/':
					if(ReaderPeek() == '/')// DDoc
					{
						ReaderRead();
						ReadSingleLineComment(Comment.Type.Documentation);
					}
					else
					{
						ReadSingleLineComment(Comment.Type.SingleLine);
					}
					break;
				default:
					OnError(Line, Col, String.Format("Error while reading comment"));
					break;
			}
		}

		void ReadSingleLineComment(Comment.Type commentType)
		{
			Location st = new Location(Col, Line);
			string comm = ReadToEndOfLine();
			Location end = new Location(Col, st.Line);
			Comments.Add(new Comment(commentType, comm, st.Column<2, st, end));
		}

		void ReadMultiLineComment(Comment.Type commentType, bool isNestingComment)
		{
			int nextChar;

			Location st = new Location(Col, Line);
			StringBuilder scCurWord = new StringBuilder(); // current word, (scTag == null) or comment (when scTag != null)

			while((nextChar = ReaderRead()) != -1)
			{
				char ch = (char)nextChar;

				// End of multiline comment reached ?
				if((ch == '+' || (ch == '*' && !isNestingComment)) && ReaderPeek() == '/')
				{
					ReaderRead(); // Skip "*" or "+"
					Comments.Add(new Comment(commentType, scCurWord.ToString().TrimEnd(ch), st.Column<2, st, new Location(Col, Line)));
					return;
				}

				if(HandleLineEnd(ch)) scCurWord.AppendLine();

				scCurWord.Append(ch);
			}
			Comments.Add(new Comment(commentType, scCurWord.ToString(), st.Column < 2, st, new Location(Col, Line)));
			// Reached EOF before end of multiline comment.
			OnError(Line, Col, String.Format("Reached EOF before the end of a multiline comment"));
		}

		public override void SkipCurrentBlock(int targetToken)
		{
			int braceCount = 0;
			while(curToken != null)
			{
				if(curToken.Kind == DTokens.OpenCurlyBrace)
				{
					++braceCount;
				}
				else if(curToken.Kind == DTokens.CloseCurlyBrace)
				{
					if(--braceCount < 0)
						return;
				}
				NextToken();
			}
		}

		/*
		 * TODO: Fix it
		 * public string SkipTo(char targetToken)
		{
			int tt = DTokens.GetTokenID(targetToken.ToString());
			if (LookAhead.Kind == tt) return "";
			StringBuilder sb = new StringBuilder(100);
			int braceCount = 0, parenthesisCount=0;

			int nextChar;
			while((nextChar = ReaderRead()) != -1)
			{
				if (parenthesisCount < 1 && braceCount < 1 && (char)nextChar == targetToken)
				{}
				else if(sb.Length<10000)
					sb.Append((char)nextChar);

				switch(nextChar)
				{
					case '(':
						parenthesisCount++;
						break;
					case ')':
						parenthesisCount--;
						break;
					case '{':
						braceCount++;
						break;
					case '}':
						if(--braceCount < 0)
						{
							curToken = new DToken(DTokens.CloseCurlyBrace, Col - 1, Line);
							return sb.ToString();
						}
						break;
					case '/':
						int peek = ReaderPeek();
						if(peek == '/' || peek == '*' || peek=='+')
						{
							ReadComment();
						}
						break;
					case '"':
						if (sb.Length < 10000) sb.Append((string)ReadString().LiteralValue + "\"");
						break;
					case '\'':
						if (sb.Length < 10000) sb.Append(ReadChar().Value);
						break;
					case '\r':
					case '\n':
						HandleLineEnd((char)nextChar);
						break;
					case '@':
						int next = ReaderRead();
						if(next == -1)
						{
							OnError(Line, Col, String.Format("EOF after @"));
						}
						else if(next == '"')
						{
							if (sb.Length < 10000) sb.Append("\"" + (string)ReadVerbatimString().LiteralValue + "\"");
						}
						break;
				}

				if (parenthesisCount<1 && braceCount<1 && (char)nextChar == targetToken)
				{
					curToken = new DToken(tt, Col - 1, Line);
					return sb.ToString();
				}
			}
			curToken = new DToken(DTokens.EOF, Col, Line);
			return sb.ToString();
		}*/
	}
}
