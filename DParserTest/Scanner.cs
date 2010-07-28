
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;
using System.Collections;

namespace D_Parser {

    /// <summary>
    /// Taken from SharpDevelop.NRefactory
    /// </summary>
    public abstract class AbstractLexer
    {
        protected TextReader reader;
        protected int col = 1;
        protected int line = 1;

        protected DToken t = null;
        protected DToken la = null;
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
            if (t != null || la != null || peekToken != null)
                throw new InvalidOperationException();
            this.line = location.Line;
            this.col = location.Column;
        }

        /// <summary>
        /// The current DToken. <seealso cref="ICSharpCode.NRefactory.Parser.DToken"/>
        /// </summary>
        public DToken CurrentToken
        {
            get
            {
                //				Console.WriteLine("Call to DToken");
                return t;
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
                return la;
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
            t = la = peekToken = null;
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
            if (peekToken == null) StartPeek();
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
            if (t == null)
            {
                t = Next();
                t.next = Next();
                la = t.next;
                return t;
            }

            t = la;

            if (la.next == null)
                la.next = Next();

            la = la.next;
            return t;
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
    }







public class DLexer : AbstractLexer
    {
        public DLexer(TextReader reader)
            : base(reader)
        {
            Comments = new List<Comment>();
        }

        public List<Comment> Comments;
        public override event AbstractLexer.CommentHandler OnComment;
        void OnError(int line, int col, string message)
        {
            //errors.Error(line, col, message);
        }

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
						{
							ReaderRead();
							token = ReadString(nextChar);
                            break;
                        }else
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
                        else if (Char.IsDigit(ch))
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

                if (ch == '.')
                {
                    isdouble = true;

                    while (Char.IsDigit((char)ReaderPeek()))
                    { // read decimal digits beyond the dot
                        sb.Append((char)ReaderRead());
                    }
                    peek = (char)ReaderPeek();
                }
                else if (ch == '0' && (peek == 'x' || peek == 'X'))
                {
                    ReaderRead(); // skip 'x'
                    sb.Length = 0; // Remove '0' from 0x prefix from the stringvalue
                    while (IsHex((char)ReaderPeek()))
                    {
                        sb.Append((char)ReaderRead());
                    }
                    if (sb.Length == 0)
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
                    while (Char.IsDigit((char)ReaderPeek()))
                    {
                        sb.Append((char)ReaderRead());
                    }
                    peek = (char)ReaderPeek();
                }

                DToken nextToken = null; // if we accidently read a 'dot'
                if (peek == '.')
                { // read floating point number
                    ReaderRead();
                    peek = (char)ReaderPeek();
                    if (!Char.IsDigit(peek))
                    {
                        nextToken = new DToken(DTokens.Dot, Col - 1, Line);
                        peek = '.';
                    }
                    else
                    {
                        isdouble = true; // double is default
                        if (ishex)
                        {
                            OnError(y, x, String.Format("No hexadecimal floating point values allowed"));
                        }
                        sb.Append('.');

                        while (Char.IsDigit((char)ReaderPeek()))
                        { // read decimal digits beyond the dot
                            sb.Append((char)ReaderRead());
                        }
                        peek = (char)ReaderPeek();
                    }
                }

                if (peek == 'e' || peek == 'E')
                { // read exponent
                    isdouble = true;
                    sb.Append((char)ReaderRead());
                    peek = (char)ReaderPeek();
                    if (peek == '-' || peek == '+')
                    {
                        sb.Append((char)ReaderRead());
                    }
                    while (Char.IsDigit((char)ReaderPeek()))
                    { // read exponent value
                        sb.Append((char)ReaderRead());
                    }
                    isunsigned = true;
                    peek = (char)ReaderPeek();
                }

                if (peek == 'f' || peek == 'F')
                { // float value
                    ReaderRead();
                    suffix = "f";
                    isfloat = true;
                }
                else if (peek == 'd' || peek == 'D')
                { // double type suffix (obsolete, double is default)
                    ReaderRead();
                    suffix = "d";
                    isdouble = true;
                }
                else if (peek == 'm' || peek == 'M')
                { // decimal value
                    ReaderRead();
                    suffix = "m";
                    isdecimal = true;
                }
                else if (!isdouble)
                {
                    if (peek == 'u' || peek == 'U')
                    {
                        ReaderRead();
                        suffix = "u";
                        isunsigned = true;
                        peek = (char)ReaderPeek();
                    }

                    if (peek == 'l' || peek == 'L')
                    {
                        ReaderRead();
                        peek = (char)ReaderPeek();
                        islong = true;
                        if (!isunsigned && (peek == 'u' || peek == 'U'))
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

                if (isfloat)
                {
                    float num;
                    if (float.TryParse(digit, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
                    {
                        return new DToken(DTokens.Literal, new Location(x, y), new Location(x + stringValue.Length, y), stringValue, num, LiteralFormat.DecimalNumber);
                    }
                    else
                    {
                        OnError(y, x, String.Format("Can't parse float {0}", digit));
                        return new DToken(DTokens.Literal, new Location(x, y), new Location(x + stringValue.Length, y), stringValue, 0f, LiteralFormat.DecimalNumber);
                    }
                }
                if (isdecimal)
                {
                    decimal num;
                    if (decimal.TryParse(digit, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
                    {
                        return new DToken(DTokens.Literal, new Location(x, y), new Location(x + stringValue.Length, y), stringValue, num, LiteralFormat.DecimalNumber);
                    }
                    else
                    {
                        OnError(y, x, String.Format("Can't parse decimal {0}", digit));
                        return new DToken(DTokens.Literal, new Location(x, y), new Location(x + stringValue.Length, y), stringValue, 0m, LiteralFormat.DecimalNumber);
                    }
                }
                if (isdouble)
                {
                    double num;
                    if (double.TryParse(digit, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
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
                if (ishex)
                {
                    if (!ulong.TryParse(digit, NumberStyles.HexNumber, null, out result))
                    {
                        OnError(y, x, String.Format("Can't parse hexadecimal constant {0}", digit));
                        return new DToken(DTokens.Literal, new Location(x, y), new Location(x + stringValue.Length, y), stringValue.ToString(), 0, LiteralFormat.DecimalNumber);
                    }
                }
                else
                {
                    if (!ulong.TryParse(digit, NumberStyles.Integer, null, out result))
                    {
                        OnError(y, x, String.Format("Can't parse integral constant {0}", digit));
                        return new DToken(DTokens.Literal, new Location(x, y), new Location(x + stringValue.Length, y), stringValue.ToString(), 0, LiteralFormat.DecimalNumber);
                    }
                }

                if (result > long.MaxValue)
                {
                    islong = true;
                    isunsigned = true;
                }
                else if (result > uint.MaxValue)
                {
                    islong = true;
                }
                else if (result > int.MaxValue)
                {
                    isunsigned = true;
                }

                DToken token;

                if (islong)
                {
                    if (isunsigned)
                    {
                        ulong num;
                        if (ulong.TryParse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number, CultureInfo.InvariantCulture, out num))
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
                        if (long.TryParse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number, CultureInfo.InvariantCulture, out num))
                        {
                            token = new DToken(DTokens.Literal, new Location(x, y), new Location(x + stringValue.Length, y), stringValue, num, LiteralFormat.DecimalNumber);
                        }
                        else
                        {
                            OnError(y, x, String.Format("Can't parse long {0}", digit));
                            token = new DToken(DTokens.Literal, new Location(x, y), new Location(x + stringValue.Length, y), stringValue, 0L, LiteralFormat.DecimalNumber);
                        }
                    }
                }
                else
                {
                    if (isunsigned)
                    {
                        uint num;
                        if (uint.TryParse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number, CultureInfo.InvariantCulture, out num))
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
                        if (int.TryParse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number, CultureInfo.InvariantCulture, out num))
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
                x=Col-1;
            }
            while ((nextChar = ReaderRead()) != -1)
            {
                char ch = (char)nextChar;

                if (nextChar==EndingChar)
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
                        default:
                            break;
                    }
                    return new DToken(DTokens.Xor, x, y);
                case '!':
                    switch (ReaderPeek())
                    {
                        case '=':
                            ReaderRead();
                            return new DToken(DTokens.NotEqual, x, y);
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
                    return new DToken(DTokens.Question, x, y);
                case '$':
                    return new DToken(DTokens.Dollar, x, y);
                case ';':
                    return new DToken(DTokens.Semicolon, x, y);
                case ':':
                    if (ReaderPeek() == ':')
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
                    if (tmp > 0 && Char.IsDigit((char)tmp))
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
            int pk = 0;
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
                    nComm = new Comment(commentType, scCurWord.ToString().Trim(ch, ' ', '\t', '\r', '\n','*','+'), st.Column < 2, st, new Location(Col, Line));
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
    }



























//-----------------------------------------------------------------------------------
// Scanner
//-----------------------------------------------------------------------------------
/*
public class Scanner :AbstractLexer {
	const char EOL = '\n';
	const int eofSym = 0; // pdt
	const int maxT = 223;
	const int noSym = 223;


	int ch;           // current input character
	int pos=-1;          // byte position of current character
	int oldEols=0;      // EOLs that appeared in a comment;
	static readonly Hashtable start; // maps first token character to start state

	DToken tokens;     // list of tokens already peeked (first token is a dummy)

	char[] tval = new char[128]; // text of current token
	int tlen;         // length of current token

	static DLexer() {
		start = new Hashtable(128);
		for (int i = 65; i <= 66; ++i) start[i] = 1;
		for (int i = 68; i <= 90; ++i) start[i] = 1;
		for (int i = 95; i <= 95; ++i) start[i] = 1;
		for (int i = 97; i <= 113; ++i) start[i] = 1;
		for (int i = 115; i <= 122; ++i) start[i] = 1;
		for (int i = 170; i <= 170; ++i) start[i] = 1;
		for (int i = 181; i <= 181; ++i) start[i] = 1;
		for (int i = 186; i <= 186; ++i) start[i] = 1;
		for (int i = 192; i <= 214; ++i) start[i] = 1;
		for (int i = 216; i <= 246; ++i) start[i] = 1;
		for (int i = 248; i <= 255; ++i) start[i] = 1;
		for (int i = 49; i <= 57; ++i) start[i] = 113;
		start[92] = 15; 
		start[64] = 147; 
		start[48] = 114; 
		start[46] = 115; 
		start[39] = 43; 
		start[34] = 60; 
		start[114] = 116; 
		start[47] = 148; 
		start[38] = 117; 
		start[124] = 118; 
		start[45] = 119; 
		start[43] = 120; 
		start[60] = 121; 
		start[62] = 122; 
		start[33] = 123; 
		start[40] = 97; 
		start[41] = 98; 
		start[91] = 99; 
		start[93] = 100; 
		start[123] = 101; 
		start[125] = 102; 
		start[63] = 103; 
		start[44] = 104; 
		start[59] = 105; 
		start[58] = 106; 
		start[36] = 107; 
		start[61] = 124; 
		start[42] = 125; 
		start[37] = 126; 
		start[94] = 149; 
		start[126] = 127; 
		start[67] = 150; 
		start[Buffer.EOF] = -1;

	}

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

	void NextCh() {
		if (oldEols > 0) { ch = EOL; oldEols--; }
		else {
			pos = buffer.Pos;
			ch = ReaderRead();
			// replace isolated '\r' by '\n' in order to make
			// eol handling uniform across Windows, Unix and Mac
			if (ch == '\r' && buffer.Peek() != '\n') ch = EOL;
			if (ch == EOL) { line++; col = 0; }
		}

	}

	void AddCh() {
		if (tlen >= tval.Length) {
			char[] newBuf = new char[2 * tval.Length];
			Array.Copy(tval, 0, newBuf, 0, tval.Length);
			tval = newBuf;
		}
		if (ch != Buffer.EOF) {
			tval[tlen++] = (char) ch;
			NextCh();
		}
	}



	bool Comment0() {
		int level = 1, pos0 = pos, line0 = line, col0 = col;
		NextCh();
		if (ch == '/') {
			NextCh();
			for(;;) {
				if (ch == 10) {
					level--;
					if (level == 0) { oldEols = line - line0; NextCh(); return true; }
					NextCh();
				} else if (ch == Buffer.EOF) return false;
				else NextCh();
			}
		} else {
			buffer.Pos = pos0; NextCh(); line = line0; col = col0;
		}
		return false;
	}

	bool Comment1() {
		int level = 1, pos0 = pos, line0 = line, col0 = col;
		NextCh();
		if (ch == '+') {
			NextCh();
			for(;;) {
				if (ch == '+') {
					NextCh();
					if (ch == '/') {
						level--;
						if (level == 0) { oldEols = line - line0; NextCh(); return true; }
						NextCh();
					}
				} else if (ch == '/') {
					NextCh();
					if (ch == '+') {
						level++; NextCh();
					}
				} else if (ch == Buffer.EOF) return false;
				else NextCh();
			}
		} else {
			buffer.Pos = pos0; NextCh(); line = line0; col = col0;
		}
		return false;
	}

	bool Comment2() {
		int level = 1, pos0 = pos, line0 = line, col0 = col;
		NextCh();
		if (ch == '*') {
			NextCh();
			for(;;) {
				if (ch == '*') {
					NextCh();
					if (ch == '/') {
						level--;
						if (level == 0) { oldEols = line - line0; NextCh(); return true; }
						NextCh();
					}
				} else if (ch == Buffer.EOF) return false;
				else NextCh();
			}
		} else {
			buffer.Pos = pos0; NextCh(); line = line0; col = col0;
		}
		return false;
	}


	void CheckLiteral() {
		switch (t.val) {
			case "abstract": t.kind = 63; break;
			case "alias": t.kind = 64; break;
			case "align": t.kind = 65; break;
			case "asm": t.kind = 66; break;
			case "assert": t.kind = 67; break;
			case "auto": t.kind = 68; break;
			case "body": t.kind = 69; break;
			case "bool": t.kind = 70; break;
			case "break": t.kind = 71; break;
			case "byte": t.kind = 72; break;
			case "case": t.kind = 73; break;
			case "cast": t.kind = 74; break;
			case "catch": t.kind = 75; break;
			case "cdouble": t.kind = 76; break;
			case "cent": t.kind = 77; break;
			case "cfloat": t.kind = 78; break;
			case "char": t.kind = 79; break;
			case "class": t.kind = 80; break;
			case "const": t.kind = 81; break;
			case "continue": t.kind = 82; break;
			case "creal": t.kind = 83; break;
			case "dchar": t.kind = 84; break;
			case "debug": t.kind = 85; break;
			case "default": t.kind = 86; break;
			case "delegate": t.kind = 87; break;
			case "delete": t.kind = 88; break;
			case "deprecated": t.kind = 89; break;
			case "do": t.kind = 90; break;
			case "double": t.kind = 91; break;
			case "else": t.kind = 92; break;
			case "enum": t.kind = 93; break;
			case "export": t.kind = 94; break;
			case "extern": t.kind = 95; break;
			case "false": t.kind = 96; break;
			case "final": t.kind = 97; break;
			case "finally": t.kind = 98; break;
			case "float": t.kind = 99; break;
			case "for": t.kind = 100; break;
			case "foreach": t.kind = 101; break;
			case "foreach_reverse": t.kind = 102; break;
			case "function": t.kind = 103; break;
			case "goto": t.kind = 104; break;
			case "idouble": t.kind = 105; break;
			case "if": t.kind = 106; break;
			case "ifloat": t.kind = 107; break;
			case "immutable": t.kind = 108; break;
			case "import": t.kind = 109; break;
			case "in": t.kind = 110; break;
			case "inout": t.kind = 111; break;
			case "int": t.kind = 112; break;
			case "interface": t.kind = 113; break;
			case "invariant": t.kind = 114; break;
			case "ireal": t.kind = 115; break;
			case "is": t.kind = 116; break;
			case "lazy": t.kind = 117; break;
			case "long": t.kind = 118; break;
			case "macro": t.kind = 119; break;
			case "mixin": t.kind = 120; break;
			case "module": t.kind = 121; break;
			case "new": t.kind = 122; break;
			case "nothrow": t.kind = 123; break;
			case "null": t.kind = 124; break;
			case "out": t.kind = 125; break;
			case "override": t.kind = 126; break;
			case "package": t.kind = 127; break;
			case "pragma": t.kind = 128; break;
			case "private": t.kind = 129; break;
			case "protected": t.kind = 130; break;
			case "public": t.kind = 131; break;
			case "pure": t.kind = 132; break;
			case "real": t.kind = 133; break;
			case "ref": t.kind = 134; break;
			case "return": t.kind = 135; break;
			case "scope": t.kind = 136; break;
			case "shared": t.kind = 137; break;
			case "short": t.kind = 138; break;
			case "static": t.kind = 139; break;
			case "struct": t.kind = 140; break;
			case "super": t.kind = 141; break;
			case "switch": t.kind = 142; break;
			case "synchronized": t.kind = 143; break;
			case "template": t.kind = 144; break;
			case "this": t.kind = 145; break;
			case "throw": t.kind = 146; break;
			case "true": t.kind = 147; break;
			case "try": t.kind = 148; break;
			case "typedef": t.kind = 149; break;
			case "typeid": t.kind = 150; break;
			case "typeof": t.kind = 151; break;
			case "ubyte": t.kind = 152; break;
			case "ucent": t.kind = 153; break;
			case "uint": t.kind = 154; break;
			case "ulong": t.kind = 155; break;
			case "union": t.kind = 156; break;
			case "unittest": t.kind = 157; break;
			case "ushort": t.kind = 158; break;
			case "version": t.kind = 159; break;
			case "void": t.kind = 160; break;
			case "volatile": t.kind = 161; break;
			case "wchar": t.kind = 162; break;
			case "while": t.kind = 163; break;
			case "with": t.kind = 164; break;
			case "U": t.kind = 165; break;
			case "u": t.kind = 166; break;
			case "L": t.kind = 167; break;
			case "l": t.kind = 168; break;
			case "UL": t.kind = 169; break;
			case "Ul": t.kind = 170; break;
			case "uL": t.kind = 171; break;
			case "ul": t.kind = 172; break;
			case "LU": t.kind = 173; break;
			case "Lu": t.kind = 174; break;
			case "lU": t.kind = 175; break;
			case "lu": t.kind = 176; break;
			case "__FILE__": t.kind = 177; break;
			case "__LINE__": t.kind = 178; break;
			case "__gshared": t.kind = 179; break;
			case "disable": t.kind = 181; break;
			case "property": t.kind = 182; break;
			case "safe": t.kind = 183; break;
			case "C": t.kind = 184; break;
			case "D": t.kind = 186; break;
			case "Windows": t.kind = 187; break;
			case "Pascal": t.kind = 188; break;
			case "System": t.kind = 189; break;
			case "exit": t.kind = 193; break;
			case "success": t.kind = 194; break;
			case "failure": t.kind = 195; break;
			case "__traits": t.kind = 196; break;
			case "isAbstractClass": t.kind = 197; break;
			case "isArithmetic": t.kind = 198; break;
			case "isAssociativeArray": t.kind = 199; break;
			case "isFinalClass": t.kind = 200; break;
			case "isFloating": t.kind = 201; break;
			case "isIntegral": t.kind = 202; break;
			case "isScalar": t.kind = 203; break;
			case "isStaticArray": t.kind = 204; break;
			case "isUnsigned": t.kind = 205; break;
			case "isVirtualFunction": t.kind = 206; break;
			case "isAbstractFunction": t.kind = 207; break;
			case "isFinalFunction": t.kind = 208; break;
			case "isStaticFunction": t.kind = 209; break;
			case "isRef": t.kind = 210; break;
			case "isOut": t.kind = 211; break;
			case "isLazy": t.kind = 212; break;
			case "hasMember": t.kind = 213; break;
			case "identifier": t.kind = 214; break;
			case "getMember": t.kind = 215; break;
			case "getOverloads": t.kind = 216; break;
			case "getVirtualFunctions": t.kind = 217; break;
			case "classInstanceSize": t.kind = 218; break;
			case "allMembers": t.kind = 219; break;
			case "derivedMembers": t.kind = 220; break;
			case "isSame": t.kind = 221; break;
			case "compiles": t.kind = 222; break;
			default: break;
		}
	}

	protected override DToken Next(){
		while (ch == ' ' ||
			ch >= 9 && ch <= 10 || ch == 13
		) NextCh();
		if (ch == '/' && Comment0() ||ch == '/' && Comment1() ||ch == '/' && Comment2()) return NextToken();
		int apx = 0;
		int recKind = noSym;
		int recEnd = pos;
		DToken t = new DToken();
		t.pos = pos; t.col = col; t.line = line;
		int state;
		if (start.ContainsKey(ch)) { state = (int) start[ch]; }
		else { state = 0; }
		tlen = 0; AddCh();

		switch (state) {
			case -1: { t.kind = eofSym; break; } // NextCh already done
			case 0: {
				if (recKind != noSym) {
					tlen = recEnd - t.pos;
					SetScannerBehindT();
				}
				t.kind = recKind; break;
			} // NextCh already done
			case 1:
				recEnd = pos; recKind = 1;
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'Z' || ch == '_' || ch >= 'a' && ch <= 'z' || ch == 128 || ch >= 160 && ch <= 179 || ch == 181 || ch == 186 || ch >= 192 && ch <= 214 || ch >= 216 && ch <= 246 || ch >= 248 && ch <= 255) {AddCh(); goto case 1;}
				else if (ch == 92) {AddCh(); goto case 2;}
				else {t.kind = 1; t.val = new String(tval, 0, tlen); CheckLiteral(); return t;}
			case 2:
				if (ch == 'u') {AddCh(); goto case 3;}
				else if (ch == 'U') {AddCh(); goto case 7;}
				else {goto case 0;}
			case 3:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 4;}
				else {goto case 0;}
			case 4:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 5;}
				else {goto case 0;}
			case 5:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 6;}
				else {goto case 0;}
			case 6:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 1;}
				else {goto case 0;}
			case 7:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 8;}
				else {goto case 0;}
			case 8:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 9;}
				else {goto case 0;}
			case 9:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 10;}
				else {goto case 0;}
			case 10:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 11;}
				else {goto case 0;}
			case 11:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 12;}
				else {goto case 0;}
			case 12:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 13;}
				else {goto case 0;}
			case 13:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 14;}
				else {goto case 0;}
			case 14:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 1;}
				else {goto case 0;}
			case 15:
				if (ch == 'u') {AddCh(); goto case 16;}
				else if (ch == 'U') {AddCh(); goto case 20;}
				else {goto case 0;}
			case 16:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 17;}
				else {goto case 0;}
			case 17:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 18;}
				else {goto case 0;}
			case 18:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 19;}
				else {goto case 0;}
			case 19:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 1;}
				else {goto case 0;}
			case 20:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 21;}
				else {goto case 0;}
			case 21:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 22;}
				else {goto case 0;}
			case 22:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 23;}
				else {goto case 0;}
			case 23:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 24;}
				else {goto case 0;}
			case 24:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 25;}
				else {goto case 0;}
			case 25:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 26;}
				else {goto case 0;}
			case 26:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 27;}
				else {goto case 0;}
			case 27:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 1;}
				else {goto case 0;}
			case 28:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 29;}
				else {goto case 0;}
			case 29:
				recEnd = pos; recKind = 2;
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 29;}
				else {t.kind = 2; break;}
			case 30:
				{
					tlen -= apx;
					SetScannerBehindT();
					t.kind = 2; break;}
			case 31:
				recEnd = pos; recKind = 3;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 31;}
				else if (ch == 'D' || ch == 'F' || ch == 'M' || ch == 'd' || ch == 'f' || ch == 'm') {AddCh(); goto case 42;}
				else if (ch == 'E' || ch == 'e') {AddCh(); goto case 32;}
				else {t.kind = 3; break;}
			case 32:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 34;}
				else if (ch == '+' || ch == '-') {AddCh(); goto case 33;}
				else {goto case 0;}
			case 33:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 34;}
				else {goto case 0;}
			case 34:
				recEnd = pos; recKind = 3;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 34;}
				else if (ch == 'D' || ch == 'F' || ch == 'M' || ch == 'd' || ch == 'f' || ch == 'm') {AddCh(); goto case 42;}
				else {t.kind = 3; break;}
			case 35:
				recEnd = pos; recKind = 3;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 35;}
				else if (ch == 'D' || ch == 'F' || ch == 'M' || ch == 'd' || ch == 'f' || ch == 'm') {AddCh(); goto case 42;}
				else if (ch == 'E' || ch == 'e') {AddCh(); goto case 36;}
				else {t.kind = 3; break;}
			case 36:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 38;}
				else if (ch == '+' || ch == '-') {AddCh(); goto case 37;}
				else {goto case 0;}
			case 37:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 38;}
				else {goto case 0;}
			case 38:
				recEnd = pos; recKind = 3;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 38;}
				else if (ch == 'D' || ch == 'F' || ch == 'M' || ch == 'd' || ch == 'f' || ch == 'm') {AddCh(); goto case 42;}
				else {t.kind = 3; break;}
			case 39:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 41;}
				else if (ch == '+' || ch == '-') {AddCh(); goto case 40;}
				else {goto case 0;}
			case 40:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 41;}
				else {goto case 0;}
			case 41:
				recEnd = pos; recKind = 3;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 41;}
				else if (ch == 'D' || ch == 'F' || ch == 'M' || ch == 'd' || ch == 'f' || ch == 'm') {AddCh(); goto case 42;}
				else {t.kind = 3; break;}
			case 42:
				{t.kind = 3; break;}
			case 43:
				if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= '&' || ch >= '(' && ch <= '[' || ch >= ']' && ch <= 65535) {AddCh(); goto case 44;}
				else if (ch == 92) {AddCh(); goto case 128;}
				else {goto case 0;}
			case 44:
				if (ch == 39) {AddCh(); goto case 59;}
				else {goto case 0;}
			case 45:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 46;}
				else {goto case 0;}
			case 46:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 129;}
				else if (ch == 39) {AddCh(); goto case 59;}
				else {goto case 0;}
			case 47:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 48;}
				else {goto case 0;}
			case 48:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 49;}
				else {goto case 0;}
			case 49:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 50;}
				else {goto case 0;}
			case 50:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 44;}
				else {goto case 0;}
			case 51:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 52;}
				else {goto case 0;}
			case 52:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 53;}
				else {goto case 0;}
			case 53:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 54;}
				else {goto case 0;}
			case 54:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 55;}
				else {goto case 0;}
			case 55:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 56;}
				else {goto case 0;}
			case 56:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 57;}
				else {goto case 0;}
			case 57:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 58;}
				else {goto case 0;}
			case 58:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 44;}
				else {goto case 0;}
			case 59:
				{t.kind = 4; break;}
			case 60:
				if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= '!' || ch >= '#' && ch <= '[' || ch >= ']' && ch <= 65535) {AddCh(); goto case 60;}
				else if (ch == '"') {AddCh(); goto case 76;}
				else if (ch == 92) {AddCh(); goto case 131;}
				else {goto case 0;}
			case 61:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 62;}
				else {goto case 0;}
			case 62:
				if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= '!' || ch >= '#' && ch <= '/' || ch >= ':' && ch <= '@' || ch >= 'G' && ch <= '[' || ch >= ']' && ch <= '`' || ch >= 'g' && ch <= 65535) {AddCh(); goto case 60;}
				else if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 132;}
				else if (ch == '"') {AddCh(); goto case 76;}
				else if (ch == 92) {AddCh(); goto case 131;}
				else {goto case 0;}
			case 63:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 64;}
				else {goto case 0;}
			case 64:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 65;}
				else {goto case 0;}
			case 65:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 66;}
				else {goto case 0;}
			case 66:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 60;}
				else {goto case 0;}
			case 67:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 68;}
				else {goto case 0;}
			case 68:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 69;}
				else {goto case 0;}
			case 69:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 70;}
				else {goto case 0;}
			case 70:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 71;}
				else {goto case 0;}
			case 71:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 72;}
				else {goto case 0;}
			case 72:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 73;}
				else {goto case 0;}
			case 73:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 74;}
				else {goto case 0;}
			case 74:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 60;}
				else {goto case 0;}
			case 75:
				if (ch <= '!' || ch >= '#' && ch <= 65535) {AddCh(); goto case 75;}
				else if (ch == '"') {AddCh(); goto case 134;}
				else {goto case 0;}
			case 76:
				{t.kind = 5; break;}
			case 77:
				{t.kind = 6; break;}
			case 78:
				{t.kind = 9; break;}
			case 79:
				{t.kind = 11; break;}
			case 80:
				{t.kind = 12; break;}
			case 81:
				{t.kind = 14; break;}
			case 82:
				{t.kind = 15; break;}
			case 83:
				{t.kind = 17; break;}
			case 84:
				{t.kind = 18; break;}
			case 85:
				{t.kind = 20; break;}
			case 86:
				{t.kind = 21; break;}
			case 87:
				{t.kind = 23; break;}
			case 88:
				{t.kind = 25; break;}
			case 89:
				{t.kind = 27; break;}
			case 90:
				{t.kind = 29; break;}
			case 91:
				{t.kind = 30; break;}
			case 92:
				{t.kind = 31; break;}
			case 93:
				{t.kind = 35; break;}
			case 94:
				{t.kind = 37; break;}
			case 95:
				{t.kind = 39; break;}
			case 96:
				{t.kind = 41; break;}
			case 97:
				{t.kind = 42; break;}
			case 98:
				{t.kind = 43; break;}
			case 99:
				{t.kind = 44; break;}
			case 100:
				{t.kind = 45; break;}
			case 101:
				{t.kind = 46; break;}
			case 102:
				{t.kind = 47; break;}
			case 103:
				{t.kind = 48; break;}
			case 104:
				{t.kind = 49; break;}
			case 105:
				{t.kind = 50; break;}
			case 106:
				{t.kind = 51; break;}
			case 107:
				{t.kind = 52; break;}
			case 108:
				{t.kind = 54; break;}
			case 109:
				{t.kind = 56; break;}
			case 110:
				{t.kind = 58; break;}
			case 111:
				{t.kind = 60; break;}
			case 112:
				{t.kind = 62; break;}
			case 113:
				recEnd = pos; recKind = 2;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 113;}
				else if (ch == '.') {apx++; AddCh(); goto case 135;}
				else if (ch == 'E' || ch == 'e') {AddCh(); goto case 39;}
				else if (ch == 'D' || ch == 'F' || ch == 'M' || ch == 'd' || ch == 'f' || ch == 'm') {AddCh(); goto case 42;}
				else {t.kind = 2; break;}
			case 114:
				recEnd = pos; recKind = 2;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 113;}
				else if (ch == '.') {apx++; AddCh(); goto case 135;}
				else if (ch == 'X' || ch == 'x') {AddCh(); goto case 28;}
				else if (ch == 'E' || ch == 'e') {AddCh(); goto case 39;}
				else if (ch == 'D' || ch == 'F' || ch == 'M' || ch == 'd' || ch == 'f' || ch == 'm') {AddCh(); goto case 42;}
				else {t.kind = 2; break;}
			case 115:
				recEnd = pos; recKind = 7;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 31;}
				else if (ch == '.') {AddCh(); goto case 136;}
				else {t.kind = 7; break;}
			case 116:
				recEnd = pos; recKind = 1;
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'Z' || ch == '_' || ch >= 'a' && ch <= 'z' || ch == 128 || ch >= 160 && ch <= 179 || ch == 181 || ch == 186 || ch >= 192 && ch <= 214 || ch >= 216 && ch <= 246 || ch >= 248 && ch <= 255) {AddCh(); goto case 1;}
				else if (ch == 92) {AddCh(); goto case 2;}
				else if (ch == '"') {AddCh(); goto case 75;}
				else {t.kind = 1; t.val = new String(tval, 0, tlen); CheckLiteral(); return t;}
			case 117:
				recEnd = pos; recKind = 10;
				if (ch == '=') {AddCh(); goto case 79;}
				else if (ch == '&') {AddCh(); goto case 80;}
				else {t.kind = 10; break;}
			case 118:
				recEnd = pos; recKind = 13;
				if (ch == '=') {AddCh(); goto case 81;}
				else if (ch == '|') {AddCh(); goto case 82;}
				else {t.kind = 13; break;}
			case 119:
				recEnd = pos; recKind = 16;
				if (ch == '=') {AddCh(); goto case 83;}
				else if (ch == '-') {AddCh(); goto case 84;}
				else {t.kind = 16; break;}
			case 120:
				recEnd = pos; recKind = 19;
				if (ch == '=') {AddCh(); goto case 85;}
				else if (ch == '+') {AddCh(); goto case 86;}
				else {t.kind = 19; break;}
			case 121:
				recEnd = pos; recKind = 22;
				if (ch == '=') {AddCh(); goto case 87;}
				else if (ch == '<') {AddCh(); goto case 137;}
				else if (ch == '>') {AddCh(); goto case 138;}
				else {t.kind = 22; break;}
			case 122:
				recEnd = pos; recKind = 28;
				if (ch == '=') {AddCh(); goto case 90;}
				else if (ch == '>') {AddCh(); goto case 139;}
				else {t.kind = 28; break;}
			case 123:
				recEnd = pos; recKind = 34;
				if (ch == '=') {AddCh(); goto case 93;}
				else if (ch == '<') {AddCh(); goto case 140;}
				else if (ch == '>') {AddCh(); goto case 141;}
				else {t.kind = 34; break;}
			case 124:
				recEnd = pos; recKind = 53;
				if (ch == '=') {AddCh(); goto case 108;}
				else {t.kind = 53; break;}
			case 125:
				recEnd = pos; recKind = 55;
				if (ch == '=') {AddCh(); goto case 109;}
				else {t.kind = 55; break;}
			case 126:
				recEnd = pos; recKind = 57;
				if (ch == '=') {AddCh(); goto case 110;}
				else {t.kind = 57; break;}
			case 127:
				recEnd = pos; recKind = 61;
				if (ch == '=') {AddCh(); goto case 112;}
				else {t.kind = 61; break;}
			case 128:
				if (ch == '"' || ch == 39 || ch == '0' || ch == 92 || ch >= 'a' && ch <= 'b' || ch == 'f' || ch == 'n' || ch == 'r' || ch == 't' || ch == 'v') {AddCh(); goto case 44;}
				else if (ch == 'x') {AddCh(); goto case 45;}
				else if (ch == 'u') {AddCh(); goto case 47;}
				else if (ch == 'U') {AddCh(); goto case 51;}
				else {goto case 0;}
			case 129:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 130;}
				else if (ch == 39) {AddCh(); goto case 59;}
				else {goto case 0;}
			case 130:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 44;}
				else if (ch == 39) {AddCh(); goto case 59;}
				else {goto case 0;}
			case 131:
				if (ch == '"' || ch == 39 || ch == '0' || ch == 92 || ch >= 'a' && ch <= 'b' || ch == 'f' || ch == 'n' || ch == 'r' || ch == 't' || ch == 'v') {AddCh(); goto case 60;}
				else if (ch == 'x') {AddCh(); goto case 61;}
				else if (ch == 'u') {AddCh(); goto case 63;}
				else if (ch == 'U') {AddCh(); goto case 67;}
				else {goto case 0;}
			case 132:
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 133;}
				else if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= '!' || ch >= '#' && ch <= '/' || ch >= ':' && ch <= '@' || ch >= 'G' && ch <= '[' || ch >= ']' && ch <= '`' || ch >= 'g' && ch <= 65535) {AddCh(); goto case 60;}
				else if (ch == '"') {AddCh(); goto case 76;}
				else if (ch == 92) {AddCh(); goto case 131;}
				else {goto case 0;}
			case 133:
				if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= '!' || ch >= '#' && ch <= '[' || ch >= ']' && ch <= 65535) {AddCh(); goto case 60;}
				else if (ch == '"') {AddCh(); goto case 76;}
				else if (ch == 92) {AddCh(); goto case 131;}
				else {goto case 0;}
			case 134:
				recEnd = pos; recKind = 5;
				if (ch == '"') {AddCh(); goto case 75;}
				else {t.kind = 5; break;}
			case 135:
				if (ch <= '/' || ch >= ':' && ch <= 65535) {apx++; AddCh(); goto case 30;}
				else if (ch >= '0' && ch <= '9') {apx = 0; AddCh(); goto case 35;}
				else {goto case 0;}
			case 136:
				recEnd = pos; recKind = 8;
				if (ch == '.') {AddCh(); goto case 78;}
				else {t.kind = 8; break;}
			case 137:
				recEnd = pos; recKind = 24;
				if (ch == '=') {AddCh(); goto case 88;}
				else {t.kind = 24; break;}
			case 138:
				recEnd = pos; recKind = 26;
				if (ch == '=') {AddCh(); goto case 89;}
				else {t.kind = 26; break;}
			case 139:
				recEnd = pos; recKind = 32;
				if (ch == '=') {AddCh(); goto case 91;}
				else if (ch == '>') {AddCh(); goto case 142;}
				else {t.kind = 32; break;}
			case 140:
				recEnd = pos; recKind = 38;
				if (ch == '>') {AddCh(); goto case 143;}
				else if (ch == '=') {AddCh(); goto case 95;}
				else {t.kind = 38; break;}
			case 141:
				recEnd = pos; recKind = 40;
				if (ch == '=') {AddCh(); goto case 96;}
				else {t.kind = 40; break;}
			case 142:
				recEnd = pos; recKind = 33;
				if (ch == '=') {AddCh(); goto case 92;}
				else {t.kind = 33; break;}
			case 143:
				recEnd = pos; recKind = 36;
				if (ch == '=') {AddCh(); goto case 94;}
				else {t.kind = 36; break;}
			case 144:
				if (ch == '+') {AddCh(); goto case 145;}
				else {goto case 0;}
			case 145:
				{t.kind = 185; break;}
			case 146:
				{t.kind = 190; break;}
			case 147:
				recEnd = pos; recKind = 180;
				if (ch >= 'A' && ch <= 'Z' || ch == '_' || ch >= 'a' && ch <= 'z' || ch == 170 || ch == 181 || ch == 186 || ch >= 192 && ch <= 214 || ch >= 216 && ch <= 246 || ch >= 248 && ch <= 255) {AddCh(); goto case 1;}
				else if (ch == 92) {AddCh(); goto case 15;}
				else {t.kind = 180; break;}
			case 148:
				recEnd = pos; recKind = 191;
				if (ch == '=') {AddCh(); goto case 77;}
				else {t.kind = 191; break;}
			case 149:
				recEnd = pos; recKind = 59;
				if (ch == '=') {AddCh(); goto case 111;}
				else if (ch == '^') {AddCh(); goto case 151;}
				else {t.kind = 59; break;}
			case 150:
				recEnd = pos; recKind = 1;
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'Z' || ch == '_' || ch >= 'a' && ch <= 'z' || ch == 128 || ch >= 160 && ch <= 179 || ch == 181 || ch == 186 || ch >= 192 && ch <= 214 || ch >= 216 && ch <= 246 || ch >= 248 && ch <= 255) {AddCh(); goto case 1;}
				else if (ch == 92) {AddCh(); goto case 2;}
				else if (ch == '+') {AddCh(); goto case 144;}
				else {t.kind = 1; t.val = new String(tval, 0, tlen); CheckLiteral(); return t;}
			case 151:
				recEnd = pos; recKind = 192;
				if (ch == '=') {AddCh(); goto case 146;}
				else {t.kind = 192; break;}

		}
		t.val = new String(tval, 0, tlen);
		return t;
	}

	private void SetScannerBehindT() {
		buffer.Pos = t.pos;
		NextCh();
		line = t.line; col = t.col;
		for (int i = 0; i < tlen; i++) NextCh();
	}

	// get the next token (possibly a token already seen during peeking)
	public Token Scan () {
		if (tokens.next == null) {
			return NextToken();
		} else {
			pt = tokens = tokens.next;
			return tokens;
		}
	}

	// peek for the next token, ignore pragmas
	public Token Peek () {
		do {
			if (pt.next == null) {
				pt.next = NextToken();
			}
			pt = pt.next;
		} while (pt.kind > maxT); // skip pragmas

		return pt;
	}

	// make sure that peeking starts at the current scan position
	public void ResetPeek () { pt = tokens; }

} // end Scanner
*/

}