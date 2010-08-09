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

        protected DToken prevToken = null;
        protected DToken curToken = null;
        protected DToken lookaheadToken = null;
        protected DToken peekToken = null;

        protected StringBuilder sb = new StringBuilder();

        /// <summary>
        /// used for the original value of strings (with escape sequences).
        /// </summary>
        protected StringBuilder originalValue = new StringBuilder();

        public bool SkipAllComments { get; set; }
        public delegate void CommentHandler(Comment comment);
        public abstract event CommentHandler OnComment;

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

        protected int ReaderRead()
        {
            int val = reader.Read();
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
            if (curToken != null || lookaheadToken != null || peekToken != null)
                throw new InvalidOperationException();
            this.line = location.Line;
            this.col = location.Column;
        }

        public DToken LastToken
        {
            get { return prevToken; }
        }

        /// <summary>
        /// The current DToken. <seealso cref="ICSharpCode.NRefactory.Parser.DToken"/>
        /// </summary>
        public DToken CurrentToken
        {
            get
            {
                //				Console.WriteLine("Call to DToken");
                return curToken;
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
                return lookaheadToken;
            }
        }

        public DToken CurrentPeekToken
        {
            get { return peekToken; }
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
            curToken = lookaheadToken = peekToken = null;
            sb = originalValue = null;
        }
        #endregion

        /// <summary>
        /// Must be called before a peek operation.
        /// </summary>
        public void StartPeek()
        {
            peekToken = lookaheadToken;
        }

        /// <summary>
        /// Gives back the next token. A second call to Peek() gives the next token after the last call for Peek() and so on.
        /// </summary>
        /// <returns>An <see cref="CurrentToken"/> object.</returns>
        public DToken Peek()
        {
            if (peekToken == null) StartPeek();
            if (peekToken.next == null)
                peekToken.next = Next();
            peekToken = peekToken.next;
            return peekToken;
        }

        /// <summary>
        /// Reads the next token and gives it back.
        /// </summary>
        /// <returns>An <see cref="CurrentToken"/> object.</returns>
        public virtual DToken NextToken()
        {
            if (lookaheadToken == null)
            {
                lookaheadToken = Next();
                //specialTracker.InformToken(curToken.Kind);
                //Console.WriteLine(ICSharpCode.NRefactory.Parser.CSharp.Tokens.GetTokenString(curToken.kind) + " -- " + curToken.val + "(" + curToken.kind + ")");
                return lookaheadToken;
            }

            prevToken = curToken;

            curToken = lookaheadToken;

            if (lookaheadToken.next == null)
            {
                lookaheadToken.next = Next();
                if (lookaheadToken.next != null)
                {
                    //specialTracker.InformToken(curToken.next.Kind);
                }
            }

            lookaheadToken = lookaheadToken.next;
            StartPeek();
            //Console.WriteLine(ICSharpCode.NRefactory.Parser.CSharp.Tokens.GetTokenString(curToken.kind) + " -- " + curToken.val + "(" + curToken.kind + ")");
            return lookaheadToken;
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

        protected static bool IsOct(char digit)
        {
            return Char.IsDigit(digit) && digit != '9' && digit != '8';
        }

        protected static bool IsBin(char digit)
        {
            return digit == '0' || digit == '1';
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
        public abstract void SkipCurrentBlock();
    }

    public class DLexer : AbstractLexer
    {
        public DLexer(TextReader reader)
            : base(reader)
        {
            Comments = new List<Comment>();
        }

        #region Abstract Lexer Props & Methods
        public List<Comment> Comments;
        public override event AbstractLexer.CommentHandler OnComment;
        void OnError(int line, int col, string message)
        {
            //errors.Error(line, col, message);
        }
        #endregion

        protected override DToken Next()
        {
            int nextChar;
            char ch;
            bool hadLineEnd = false;
            if (Line == 1 && Col == 1) hadLineEnd = true; // beginning of document

            while ((nextChar = ReaderRead()) != -1)
            {
                DToken token;

                switch (nextChar)
                {
                    case ' ':
                    case '\t':
                        continue;
                    case '\r':
                    case '\n':
                        if (hadLineEnd)
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
                        if (peek == '/' || peek == '*' || peek == '+')
                        {
                            ReadComment();
                            continue;
                        }
                        else
                        {
                            token = ReadOperator('/');
                        }
                        break;
                    case 'r':
                        peek = ReaderPeek();
                        if (peek == '"')
                            continue;
                        else
                            goto default;
                    case '`':
                        token = ReadVerbatimString(nextChar);
                        break;
                    case '"':
                        token = ReadString(nextChar);
                        break;
                    case '\'':
                        token = ReadChar();
                        break;
                    case '@':
                        int next = ReaderRead();
                        if (next == -1)
                        {
                            OnError(Line, Col, String.Format("EOF after @"));
                            continue;
                        }
                        else
                        {
                            int x = Col - 1;
                            int y = Line;
                            ch = (char)next;
                            if (ch == '"')
                            {
                                token = ReadVerbatimString(next);
                            }
                            else if (Char.IsLetterOrDigit(ch) || ch == '_')
                            {
                                bool canBeKeyword;
                                string ident = ReadIdent(ch, out canBeKeyword);
                                int tkind = DTokens.GetTokenID("@" + ident);
                                token = new DToken(tkind < 0 ? DTokens.Identifier : tkind, x - 1, y, (tkind < 0 ? "" : "@") + ident);
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
                        if (Char.IsLetter(ch) || ch == '_' || ch == '\\')
                        {
                            int x = Col - 1; // Col was incremented above, but we want the start of the identifier
                            int y = Line;
                            bool canBeKeyword;
                            string s = ReadIdent(ch, out canBeKeyword);
                            if (canBeKeyword)
                            {
                                int keyWordToken = DKeywords.GetToken(s);
                                if (keyWordToken >= 0)
                                {
                                    return new DToken(keyWordToken, x, y);
                                }
                            }
                            return new DToken(DTokens.Identifier, x, y, s);
                        }
                        else if (Char.IsDigit(ch) || ((CurrentToken==null || CurrentToken.Kind!=DTokens.Dot) && ch == '.' && Char.IsDigit((char)ReaderPeek())))
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
                if (token != null)
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
            while (true)
            {
                if (ch == '\\')
                {
                    peek = ReaderPeek();
                    if (peek != 'u' && peek != 'U')
                    {
                        OnError(Line, Col, "Identifiers can only contain unicode escape sequences");
                    }
                    canBeKeyword = false;
                    string surrogatePair;
                    ReadEscapeSequence(out ch, out surrogatePair);
                    if (surrogatePair != null)
                    {
                        if (!char.IsLetterOrDigit(surrogatePair, 0))
                        {
                            OnError(Line, Col, "Unicode escape sequences in identifiers cannot be used to represent characters that are invalid in identifiers");
                        }
                        for (int i = 0; i < surrogatePair.Length - 1; i++)
                        {
                            if (curPos < MAX_IDENTIFIER_LENGTH)
                            {
                                identBuffer[curPos++] = surrogatePair[i];
                            }
                        }
                        ch = surrogatePair[surrogatePair.Length - 1];
                    }
                    else
                    {
                        if (!IsIdentifierPart(ch))
                        {
                            OnError(Line, Col, "Unicode escape sequences in identifiers cannot be used to represent characters that are invalid in identifiers");
                        }
                    }
                }

                if (curPos < MAX_IDENTIFIER_LENGTH)
                {
                    identBuffer[curPos++] = ch;
                }
                else
                {
                    OnError(Line, Col, String.Format("Identifier too long"));
                    while (IsIdentifierPart(ReaderPeek()))
                    {
                        ReaderRead();
                    }
                    break;
                }
                peek = ReaderPeek();
                if (IsIdentifierPart(peek) || peek == '\\')
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
            if (!Char.IsDigit(ch) && ch != '.')
            {
                OnError(Line, x, "Digit literals can only start with a digit (0-9) or a dot ('.')!");
                return null;
            }

            unchecked
            { // prevent exception when ReaderPeek() = -1 is cast to char
                int y = Line;
                sb.Length = 0;
                sb.Append(ch);
                string prefix = null;
                string expSuffix = "";
                string suffix = null;
                int exponent = 1;

                bool HasDot = false;
                bool isunsigned = false;
                bool islong = false;
                bool isfloat = false;
                bool isreal = false;
                bool isimaginary = false;
                int NumBase = 0; // Set it to 0 initially - it'll be set to another value later for sure

                char peek = (char)ReaderPeek();

                // At first, check pre-comma values
                if (ch == '0')
                {
                    if (peek == 'x' || peek == 'X') // Hex values
                    {
                        prefix = "0x";
                        ReaderRead(); // skip 'x'
                        sb.Length = 0; // Remove '0' from 0x prefix from the stringvalue
                        NumBase = 16;

                        peek = (char)ReaderPeek();
                        while (IsHex(peek) || peek == '_')
                        {
                            if (peek != '_')
                                sb.Append((char)ReaderRead());
                            else ReaderRead();
                            peek = (char)ReaderPeek();
                        }
                    }
                    else if (peek == 'b' || peek == 'B') // Bin values
                    {
                        prefix = "0b";
                        ReaderRead(); // skip 'b'
                        sb.Length = 0;
                        NumBase = 2;

                        peek = (char)ReaderPeek();
                        while (IsBin(peek) || peek == '_')
                        {
                            if (peek != '_')
                                sb.Append((char)ReaderRead());
                            else ReaderRead();
                            peek = (char)ReaderPeek();
                        }
                    }
                    else if (IsOct(peek) || peek == '_') // Oct values
                    {
                        NumBase = 8;
                        prefix = "0";
                        sb.Length = 0;

                        while (IsOct(peek) || peek == '_')
                        {
                            if (peek != '_')
                                sb.Append((char)ReaderRead());
                            else ReaderRead();
                            peek = (char)ReaderPeek();
                        }
                    }
                    else NumBase = 10;

                    if (sb.Length == 0)
                    {
                        sb.Append('0'); // dummy value to prevent exception
                        OnError(y, x, "Invalid decimal literal");
                    }
                }
                else if (ch != '.')
                {
                    NumBase = 10;
                    while (Char.IsDigit(peek) || peek == '_')
                    {
                        if (peek != '_')
                            sb.Append((char)ReaderRead());
                        else ReaderRead();
                        peek = (char)ReaderPeek();
                    }
                }

                // Read digits that occur after a comma
                DToken nextToken = null; // if we accidently read a 'dot'
                if ((NumBase == 0 && ch == '.') || peek == '.')
                {
                    if (ch != '.') ReaderRead();
                    else
                    {
                        NumBase = 10;
                        sb.Length = 0;
                        sb.Append('0');
                    }
                    peek = (char)ReaderPeek();
                    if (!Char.IsDigit(peek))
                    {
                        if (peek == '.')
                        {
                            nextToken = new DToken(DTokens.Dot, Col - 1, Line);
                        }
                    }
                    else
                    {
                        HasDot = true;
                        sb.Append('.');

                        while ((NumBase == 10 && Char.IsDigit(peek)) || (NumBase == 2 && IsBin(peek)) || (NumBase == 8 && IsOct(peek)) || (NumBase == 16 && IsHex(peek)) || peek == '_')
                        {
                            if (peek == '_')
                                ReaderRead();
                            else
                                sb.Append((char)ReaderRead());
                            peek = (char)ReaderPeek();
                        }
                    }
                }

                // Exponents
                if ((NumBase==16) ? (peek == 'p' || peek == 'P') : (peek == 'e' || peek == 'E'))
                { // read exponent
                    expSuffix = "e";
                    peek = (char)ReaderPeek();

                    if (peek == '-' || peek == '+')
                        expSuffix += (char)ReaderRead();
                    peek = (char)ReaderPeek();
                    while (Char.IsDigit(peek) || peek == '_')
                    { // read exponent value
                        if (peek == '_')
                            ReaderRead();
                        else
                            expSuffix += (char)ReaderRead();
                        peek = (char)ReaderPeek();
                    }

                    exponent = int.Parse(expSuffix);
                    peek = (char)ReaderPeek();
                }

                // Suffixes
                if (!HasDot)
                {
                unsigned:
                    if (peek == 'u' || peek == 'U')
                    {
                        ReaderRead();
                        suffix += "u";
                        isunsigned = true;
                        peek = (char)ReaderPeek();
                    }

                    if (peek == 'L')
                    {
                        ReaderRead();
                        suffix += "L";
                        islong = true;
                        peek = (char)ReaderPeek();
                        if (!isunsigned && (peek == 'u' || peek == 'U'))
                            goto unsigned;
                    }
                }


                if (peek == 'f' || peek == 'F')
                { // float value
                    ReaderRead();
                    suffix += "f";
                    isfloat = true;
                }
                else if (peek == 'L')
                { // real value
                    ReaderRead();
                    suffix += 'L';
                    isreal = true;
                }

                if (peek == 'i')
                { // imaginary value
                    ReaderRead();
                    suffix += "i";
                    isimaginary = true;
                }

                string digit = sb.ToString();
                string stringValue = prefix + digit + expSuffix + suffix;

                DToken token = null;

                // Parse the digit string
                double num = 0;

                // This here cares about floating points - it does work!
                int commaPos = digit.IndexOf('.');
                int k = digit.Length - 1;
                if (commaPos >= 0)
                    k = commaPos-1;

                for (int i = 0; i < digit.Length; i++)
                {
                    if (i == commaPos) { i++; k++; }

                    // Check if digit string contains some digits after the comma
                    if (i >= digit.Length) break;

                    int n = GetHexNumber(digit[i]);
                    num += n * Math.Pow(NumBase, (double)(k - i));
                }

                if (exponent != 1)
                    num = Math.Pow(num, exponent);

                token = new DToken(DTokens.Literal, new Location(x, y), new Location(x + stringValue.Length, y), stringValue, num, LiteralFormat.Scalar);

                if (token != null) token.next = nextToken;
                return token;
            }
        }

        DToken ReadString(int initialChar)
        {
            int x = Col - 1;
            int y = Line;

            sb.Length = 0;
            originalValue.Length = 0;
            originalValue.Append((char)initialChar);
            bool doneNormally = false;
            int nextChar;
            while ((nextChar = ReaderRead()) != -1)
            {
                char ch = (char)nextChar;

                if (nextChar == initialChar)
                {
                    doneNormally = true;
                    originalValue.Append((char)nextChar);
                    // Skip string literals
                    ch = (char)this.ReaderPeek();
                    if (ch == 'c' || ch == 'w' || ch == 'd') ReaderRead();
                    break;
                }
                HandleLineEnd(ch);
                if (ch == '\\')
                {
                    originalValue.Append('\\');
                    string surrogatePair;

                    originalValue.Append(ReadEscapeSequence(out ch, out surrogatePair));
                    if (surrogatePair != null)
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

            if (!doneNormally)
            {
                OnError(y, x, String.Format("End of file reached inside string literal"));
            }

            return new DToken(DTokens.Literal, new Location(x, y), new Location(x + originalValue.Length, y), originalValue.ToString(), sb.ToString(), LiteralFormat.StringLiteral);
        }

        DToken ReadVerbatimString(int EndingChar)
        {
            sb.Length = 0;
            originalValue.Length = 0;
            int x = Col - 2; // @ and " already read
            int y = Line;
            int nextChar;

            if (EndingChar == (int)'\"')
            {
                originalValue.Append("@\"");
            }
            else
            {
                originalValue.Append((char)EndingChar);
                x = Col - 1;
            }
            while ((nextChar = ReaderRead()) != -1)
            {
                char ch = (char)nextChar;

                if (nextChar == EndingChar)
                {
                    if (ReaderPeek() != (char)EndingChar)
                    {
                        originalValue.Append((char)EndingChar);
                        break;
                    }
                    originalValue.Append((char)EndingChar);
                    originalValue.Append((char)EndingChar);
                    sb.Append((char)EndingChar);
                    ReaderRead();
                }
                else if (HandleLineEnd(ch))
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

            if (nextChar == -1)
            {
                OnError(y, x, String.Format("End of file reached inside verbatim string literal"));
            }

            return new DToken(DTokens.Literal, new Location(x, y), new Location(x + originalValue.Length, y), originalValue.ToString(), sb.ToString(), LiteralFormat.VerbatimStringLiteral);
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
            if (nextChar == -1)
            {
                OnError(Line, Col, String.Format("End of file reached inside escape sequence"));
                ch = '\0';
                return String.Empty;
            }
            int number;
            char c = (char)nextChar;
            int curPos = 1;
            escapeSequenceBuffer[0] = c;
            switch (c)
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

                    if (number < 0)
                    {
                        OnError(Line, Col - 1, String.Format("Invalid char in literal : {0}", c));
                    }
                    for (int i = 0; i < 3; ++i)
                    {
                        if (IsHex((char)ReaderPeek()))
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
                    for (int i = 0; i < 8; ++i)
                    {
                        if (IsHex((char)ReaderPeek()))
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
                    if (number > 0xffff)
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
            if (nextChar == -1)
            {
                OnError(y, x, String.Format("End of file reached inside character literal"));
                return null;
            }
            char ch = (char)nextChar;
            char chValue = ch;
            string escapeSequence = String.Empty;
            if (ch == '\\')
            {
                string surrogatePair;
                escapeSequence = ReadEscapeSequence(out chValue, out surrogatePair);
                if (surrogatePair != null)
                {
                    OnError(y, x, String.Format("The unicode character must be represented by a surrogate pair and does not fit into a System.Char"));
                }
            }

            unchecked
            {
                if ((char)ReaderRead() != '\'')
                {
                    OnError(y, x, String.Format("Char not terminated"));
                }
            }
            return new DToken(DTokens.Literal, new Location(x, y), new Location(x + 1, y), "'" + ch + escapeSequence + "'", chValue, LiteralFormat.CharLiteral);
        }

        DToken ReadOperator(char ch)
        {
            int x = Col - 1;
            int y = Line;
            switch (ch)
            {
                case '+':
                    switch (ReaderPeek())
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
                    switch (ReaderPeek())
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
                    switch (ReaderPeek())
                    {
                        case '=':
                            ReaderRead();
                            return new DToken(DTokens.TimesAssign, x, y);
                        default:
                            break;
                    }
                    return new DToken(DTokens.Times, x, y);
                case '/':
                    switch (ReaderPeek())
                    {
                        case '=':
                            ReaderRead();
                            return new DToken(DTokens.DivAssign, x, y);
                    }
                    return new DToken(DTokens.Div, x, y);
                case '%':
                    switch (ReaderPeek())
                    {
                        case '=':
                            ReaderRead();
                            return new DToken(DTokens.ModAssign, x, y);
                    }
                    return new DToken(DTokens.Mod, x, y);
                case '&':
                    switch (ReaderPeek())
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
                    switch (ReaderPeek())
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
                    switch (ReaderPeek())
                    {
                        case '=':
                            ReaderRead();
                            return new DToken(DTokens.XorAssign, x, y);
                        case '^':
                            if (ReaderPeek() == '=')
                            {
                                ReaderRead();
                                ReaderRead();
                                return new DToken(DTokens.PowAssign, x, y);
                            }
                            return new DToken(DTokens.Pow, x, y);
                    }
                    return new DToken(DTokens.Xor, x, y);
                case '!':
                    switch (ReaderPeek())
                    {
                        case '=':
                            ReaderRead();
                            return new DToken(DTokens.NotEqual, x, y);

                        case '<':
                            ReaderRead();
                            switch (ReaderPeek())
                            {
                                case '=':
                                    ReaderRead();
                                    return new DToken(DTokens.NotLessThanAssign, x, y);
                                case '>':
                                    ReaderRead();
                                    switch (ReaderPeek())
                                    {
                                        case '=':
                                            ReaderRead();
                                            return new DToken(DTokens.NotUnequalAssign, x, y); // !<>=
                                    }
                                    return new DToken(DTokens.NotUnequal, x, y); // !<>
                            }
                            return new DToken(DTokens.NotLessThan, x, y);

                        case '>':
                            ReaderRead();
                            switch (ReaderPeek())
                            {
                                case '=':
                                    ReaderRead();
                                    return new DToken(DTokens.NotGreaterThanAssign, x, y); // !>=
                                default:
                                    break;
                            }
                            return new DToken(DTokens.NotGreaterThan, x, y); // !>

                    }
                    return new DToken(DTokens.Not, x, y);
                case '~':
                    switch (ReaderPeek())
                    {
                        case '=':
                            ReaderRead();
                            return new DToken(DTokens.TildeAssign, x, y);
                    }
                    return new DToken(DTokens.Tilde, x, y);
                case '=':
                    switch (ReaderPeek())
                    {
                        case '=':
                            ReaderRead();
                            return new DToken(DTokens.Equal, x, y);
                    }
                    return new DToken(DTokens.Assign, x, y);
                case '<':
                    switch (ReaderPeek())
                    {
                        case '<':
                            ReaderRead();
                            switch (ReaderPeek())
                            {
                                case '=':
                                    ReaderRead();
                                    return new DToken(DTokens.ShiftLeftAssign, x, y);
                                default:
                                    break;
                            }
                            return new DToken(DTokens.ShiftLeft, x, y);
                        case '>':
                            ReaderRead();
                            switch (ReaderPeek())
                            {
                                case '=':
                                    ReaderRead();
                                    return new DToken(DTokens.UnequalAssign, x, y);
                                default:
                                    break;
                            }
                            return new DToken(DTokens.Unequal, x, y);
                        case '=':
                            ReaderRead();
                            return new DToken(DTokens.LessEqual, x, y);
                    }
                    return new DToken(DTokens.LessThan, x, y);
                case '>':
                    switch (ReaderPeek())
                    {
                        case '>':
                            ReaderRead();
                            if (ReaderPeek() != -1)
                            {
                                switch ((char)ReaderPeek())
                                {
                                    case '=':
                                        ReaderRead();
                                        return new DToken(DTokens.ShiftRightAssign, x, y);
                                    case '>':
                                        ReaderRead();
                                        if (ReaderPeek() != -1)
                                        {
                                            switch ((char)ReaderPeek())
                                            {
                                                case '=':
                                                    ReaderRead();
                                                    return new DToken(DTokens.TripleRightAssign, x, y);
                                            }
                                            return new DToken(DTokens.ShiftRightUnsigned, x, y); // >>>
                                        }
                                        break;
                                }
                            }
                            return new DToken(DTokens.ShiftRight, x, y);
                        case '=':
                            ReaderRead();
                            return new DToken(DTokens.GreaterEqual, x, y);
                    }
                    return new DToken(DTokens.GreaterThan, x, y);
                case '?':
                    return new DToken(DTokens.Question, x, y);
                case '$':
                    return new DToken(DTokens.Dollar, x, y);
                case ';':
                    return new DToken(DTokens.Semicolon, x, y);
                case ':':
                    return new DToken(DTokens.Colon, x, y);
                case ',':
                    return new DToken(DTokens.Comma, x, y);
                case '.':
                    // Prevent OverflowException when ReaderPeek returns -1
                    int tmp = ReaderPeek();
                    if (tmp > 0 && (CurrentToken==null || CurrentToken.Kind!=DTokens.Dot) && Char.IsDigit((char)tmp))
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
            switch (ReaderRead())
            {
                case '+':
                    if (ReaderPeek() == '+')// DDoc
                    {
                        ReadMultiLineComment(Comment.Type.Documentation, true);
                    }
                    else
                        ReadMultiLineComment(Comment.Type.Block, true);
                    break;
                case '*':
                    if (ReaderPeek() == '*')// DDoc
                    {
                        ReadMultiLineComment(Comment.Type.Documentation, false);
                    }
                    else
                        ReadMultiLineComment(Comment.Type.Block, false);
                    break;
                case '/':
                    if (ReaderPeek() == '/')// DDoc
                        ReadSingleLineComment(Comment.Type.Documentation);
                    else
                        ReadSingleLineComment(Comment.Type.SingleLine);
                    break;
                default:
                    OnError(Line, Col, String.Format("Error while reading comment"));
                    break;
            }
        }

        void ReadSingleLineComment(Comment.Type commentType)
        {
            Location st = new Location(Col, Line);
            string comm = ReadToEndOfLine().TrimStart('/');
            Location end = new Location(Col, st.Line);
            Comment nComm = new Comment(commentType, comm.Trim(), st.Column < 2, st, end);
            Comments.Add(nComm);
            OnComment(nComm);
        }

        void ReadMultiLineComment(Comment.Type commentType, bool isNestingComment)
        {
            int nextChar;
            Comment nComm = null;
            Location st = new Location(Col, Line);
            StringBuilder scCurWord = new StringBuilder(); // current word, (scTag == null) or comment (when scTag != null)

            while ((nextChar = ReaderRead()) != -1)
            {
                char ch = (char)nextChar;

                // End of multiline comment reached ?
                if ((ch == '+' || (ch == '*' && !isNestingComment)) && ReaderPeek() == '/')
                {
                    ReaderRead(); // Skip "*" or "+"
                    nComm = new Comment(commentType, scCurWord.ToString().Trim(ch, ' ', '\t', '\r', '\n', '*', '+'), st.Column < 2, st, new Location(Col, Line));
                    Comments.Add(nComm);
                    OnComment(nComm);
                    return;
                }

                if (HandleLineEnd(ch))
                    scCurWord.AppendLine();
                else
                    scCurWord.Append(ch);
            }
            nComm = new Comment(commentType, scCurWord.ToString().Trim(), st.Column < 2, st, new Location(Col, Line));
            Comments.Add(nComm);
            OnComment(nComm);
            // Reached EOF before end of multiline comment.
            OnError(Line, Col, String.Format("Reached EOF before the end of a multiline comment"));
        }

        /// <summary>
        /// Bypass entire code blocks
        /// </summary>
        public override void SkipCurrentBlock()
        {
            int braceCount = 0;
            if (LookAhead.Kind == DTokens.OpenCurlyBrace) braceCount++;
            if (CurrentPeekToken.Kind == DTokens.OpenCurlyBrace) braceCount++;
            int nextChar;
            while ((nextChar = ReaderRead()) != -1)
            {
                switch (nextChar)
                {
                    // Handle line ends
                    case '\r':
                    case '\n':
                        HandleLineEnd((char)nextChar);
                        continue;

                    // Handle comments
                    case '/':
                        int peek = ReaderPeek();
                        if (peek == '/' || peek == '*' || peek == '+')
                        {
                            ReadComment();
                            continue;
                        }
                        break;

                    // handle string literals
                    case '`':
                        ReadVerbatimString(nextChar);
                        break;
                    case '"':
                        ReadString(nextChar);
                        break;
                    case '\'':
                        ReadChar();
                        break;

                    case '{':
                        braceCount++;
                        continue;
                    case '}':
                        braceCount--;
                        if (braceCount < 0)
                        {
                            lookaheadToken = new DToken(DTokens.CloseCurlyBrace, Col, Line);
                            StartPeek();
                            return;
                        }
                        break;
                }
            }
        }
    }
}
