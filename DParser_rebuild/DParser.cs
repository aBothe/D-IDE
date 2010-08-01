using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace D_Parser
{
    /// <summary>
    /// Parser for D Code
    /// </summary>
    public partial class DParser:DTokens
    {
        public string PhysFileName;

        public static CodeLocation GetCodeLocation(DToken t) { return new CodeLocation(t.Location.Column, t.Location.Line); }
        public static CodeLocation ToCodeEndLocation(DToken t) { return new CodeLocation(t.EndLocation.Column, t.EndLocation.Line); }

        /// <summary>
        /// Parses D source file
        /// </summary>
        /// <param name="fn"></param>
        /// <param name="imports"></param>
        /// <param name="folds"></param>
        /// <returns>Module structure</returns>
        public static DNode ParseFile(string moduleName, string fn, out List<string> imports)
        {
            if (!File.Exists(fn)) { imports = new List<string>(); return null; }
            DNode ret = new DNode(FieldType.Root);

            FileStream fs;
            try
            {
                fs = new FileStream(fn, FileMode.Open);
            }
            catch (IOException iox) { imports = new List<string>(); return null; }
            TextReader tr = new StreamReader(fs);

            DLexer dl = new DLexer(tr);

            DParser p = new DParser(dl);
            //dl.Errors.SemErr = p.SemErr;
            //dl.Errors.SynErr = p.SynErr;
            p.PhysFileName = fn;
            //p.SemErr(DTokens.Short);
            if (fs.Length > (1024 * 1024 * 2))
            {
                OnError(fn, moduleName, 0, 0, DTokens.EOF, "DParser only parses files that are smaller than 2 MBytes!");
                imports = new List<string>();
                return ret;
            }

            ret = p.Parse(moduleName, out imports);

            fs.Close();


            return ret;
        }

        /// <summary>
        /// Parses D source text.
        /// See also <seealso cref="ParseFile"/>
        /// </summary>
        /// <param name="cont"></param>
        /// <param name="imports"></param>
        /// <param name="folds"></param>
        /// <returns>Module structure</returns>
        public static DNode ParseText(string file, string moduleName, string cont, out List<string> imports)
        {
            if (cont == null || cont.Length < 1) { imports = null; return null; }
            DNode ret = new DNode(FieldType.Root);

            TextReader tr = new StringReader(cont);

            DParser p = new DParser(new DLexer(tr));
            p.PhysFileName = file;
            if (cont.Length > (1024 * 1024 * 2))
            {
                p.SemErr(DTokens.EOF, 0, 0, "DParser only parses files that are smaller than 2 MBytes!");
                imports = new List<string>();
                return ret;
            }
            ret = p.Parse(moduleName, out imports);
            tr.Close();

            return ret;
        }

        public static DParser Create(TextReader tr)
        {
            DLexer dl = new DLexer(tr);
            return new DParser(dl);
        }

        /// <summary>
        /// Encapsules whole document structure
        /// </summary>
        DNode doc;

        public List<string> import;

        /// <summary>
        /// Modifiers for entire block
        /// </summary>
        List<int> BlockModifiers;
        /// <summary>
        /// Modifiers for current expression only
        /// </summary>
        List<int> ExpressionModifiers;

        public DNode Document
        {
            get { return doc; }
        }

        public bool CheckForAssignOps()
        {
            lexer.StartPeek();
            int psb = 0,sqb=0;
            while (lexer.CurrentPeekToken.Kind != DTokens.EOF)
            {
                if (lexer.CurrentPeekToken.Kind == DTokens.OpenParenthesis) psb++;
                else if (lexer.CurrentPeekToken.Kind == DTokens.CloseParenthesis) psb--;
                else if (lexer.CurrentPeekToken.Kind == DTokens.OpenSquareBracket) sqb++;
                else if (lexer.CurrentPeekToken.Kind == DTokens.CloseSquareBracket) sqb--;

                else if (DTokens.AssignOps[lexer.CurrentPeekToken.Kind] && psb < 1 && sqb < 1)
                {
                    // Here's a point of discussion: Is it an assignment or a variable initializer
                    if (lexer.CurrentPeekToken.Kind == DTokens.Assign)
                        return false;
                    
                    return true;
                }

                else if (psb < 0
                    || lexer.CurrentPeekToken.Kind == DTokens.OpenCurlyBrace
                    || lexer.CurrentPeekToken.Kind == DTokens.Semicolon
                    || lexer.CurrentPeekToken.Kind == DTokens.CloseCurlyBrace)
                    break;

                lexer.Peek();
            }
            return false;
        }

        public string SkipToSemicolon()
        {
            string ret = "";

            int mbrace = 0, par = 0;
            while (!ThrowIfEOF(DTokens.Semicolon))
            {
                if (la.Kind == DTokens.OpenCurlyBrace) mbrace++;
                else if (la.Kind == DTokens.CloseCurlyBrace) mbrace--;

                else if (la.Kind == DTokens.OpenParenthesis) par++;
                else if (la.Kind == DTokens.CloseParenthesis) par--;

                if (mbrace < 1 && par < 1 && la.Kind != DTokens.Semicolon && Peek(1).Kind == DTokens.CloseCurlyBrace)
                {
                    ret += strVal;
                    SynErr(la.Kind, "Check for missing semicolon!");
                    break;
                }
                else if (mbrace < 1 && par < 1 && la.Kind == DTokens.Semicolon)
                {
                    break;
                }
                if (ret.Length < 2000) ret += ((la.Kind == DTokens.Identifier && t.Kind != DTokens.Dot) ? " " : "") + strVal;
                lexer.NextToken();
            }
            return ret;
        }

        public void SkipToClosingBrace()
        {
            int mbrace = 0;
            while (!ThrowIfEOF(DTokens.CloseCurlyBrace))
            {
                if (la.Kind == DTokens.OpenCurlyBrace)
                {
                    mbrace++;
                }
                else if (la.Kind == DTokens.CloseCurlyBrace)
                {
                    mbrace--;
                    if (mbrace <= 0) { break; }
                }
                lexer.NextToken();
            }
        }

        [DebuggerStepThrough()]
        public string SkipToClosingParenthesis()
        {
            return SkipToClosingParenthesis(true);
        }

        public string SkipToClosingParenthesis(bool SkipLastClosingParenthesis)
        {
            string ret = "";
            int mbrace = 0, round = 0;
            bool b = true;
            while (b && !ThrowIfEOF(DTokens.CloseParenthesis))
            {
                switch (la.Kind)
                {
                    case DTokens.OpenCurlyBrace: mbrace++; break;
                    case DTokens.CloseCurlyBrace: mbrace--; break;

                    case DTokens.OpenParenthesis:
                        round++;
                        break;

                    case DTokens.CloseParenthesis:
                        round--;
                        if (mbrace < 1 && round < 1) { b = false; ret += ")"; continue; }
                        break;
                }
                if (ret.Length < 2000) ret += ((la.Kind == DTokens.Identifier && (t.Kind == DTokens.Identifier || DTokens.BasicTypes[t.Kind])) ? " " : "") + strVal;
                    lexer.NextToken();
            }

            if (SkipLastClosingParenthesis && round < 1 && la.Kind == DTokens.CloseParenthesis)
            {
                    lexer.NextToken();
            }

            return ret;
        }

        public string SkipToClosingSquares()
        {
            string ret = "";
            int mbrace = 0, round = 0;
            while (!ThrowIfEOF(DTokens.CloseSquareBracket))
            {
                if (la.Kind == DTokens.OpenCurlyBrace) mbrace++;
                if (la.Kind == DTokens.CloseCurlyBrace) mbrace--;

                if (la.Kind == DTokens.OpenSquareBracket)
                {
                    round++;
                    lexer.NextToken(); continue;
                }
                if (la.Kind == DTokens.CloseSquareBracket)
                {
                    round--;
                    if (mbrace < 1 && round < 1) { break; }
                }
                if (ret.Length < 2000) ret += strVal;

                lexer.NextToken();
            }
            return ret;
        }

        public DLexer lexer;
        //public Errors errors;
        public DParser(DLexer lexer)
        {
            this.lexer = lexer;
            //errors = lexer.Errors;
            //errors.SynErr = new ErrorCodeProc(SynErr);
            lexer.OnComment += new AbstractLexer.CommentHandler(lexer_OnComment);
        }

        #region DDoc handling

        public DNode LastElement = null;
        string LastDescription = ""; // This is needed if some later comments are 'ditto'
        string CurrentDescription = "";
        bool HadEmptyCommentBefore = false;

        void lexer_OnComment(Comment comment)
        {
            if (comment.CommentType == Comment.Type.Documentation)
            {
                if (comment.CommentText != "ditto")
                {
                    HadEmptyCommentBefore = (CurrentDescription == "" && comment.CommentText == "");
                    CurrentDescription += (CurrentDescription == "" ? "" : "\r\n") + comment.CommentText;
                }
                else
                    CurrentDescription = LastDescription;

                /*
                 * /// start description
                 * void foo() /// description for foo()
                 * {}
                 */
                if (LastElement != null && LastElement.StartLocation.Line == comment.StartPosition.Line && comment.StartPosition.Column > LastElement.StartLocation.Column)
                {
                    LastElement.desc += (LastElement.desc == "" ? "" : "\r\n") + CurrentDescription;
                    LastDescription = CurrentDescription;
                    CurrentDescription = "";
                }
            }
        }

        #endregion

        StringBuilder qualidentBuilder = new StringBuilder();

        DToken t
        {
            [System.Diagnostics.DebuggerStepThrough]
            get
            {
                return (DToken)lexer.CurrentToken;
            }
        }

        /// <summary>
        /// lookAhead token
        /// </summary>
        DToken la
        {
            [System.Diagnostics.DebuggerStepThrough]
            get
            {
                return (DToken)lexer.LookAhead;
            }
        }

        public string CheckForDocComments()
        {
            string ret = CurrentDescription;
            if (CurrentDescription != "" || HadEmptyCommentBefore)
                LastDescription = CurrentDescription;
            CurrentDescription = "";
            return ret;
        }

        /// <summary>
        /// Check if current Token equals to n and skip that token.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        protected bool Expect(int n, string reason)
        {
            if (la.Kind == n)
            {
                lexer.NextToken();
                return true;
            }
            else
            {
                SynErr(n, reason);
                return false;
            }
        }

        /// <summary>
        /// LookAhead token check
        /// </summary>
        bool LA(int n)
        {
            return la.Kind == n;
        }
        /// <summary>
        /// Currenttoken check
        /// </summary>
        bool T(int n)
        {
            return t.Kind == n;
        }
        /// <summary>
        /// Peek token check
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        bool PK(int n)
        {
            return Peek(1).Kind == n;
        }

        private bool Expect(int n)
        {
            if (la.Kind == n)
            { Step(); return true; }
            else 
                SynErr(n, DTokens.GetTokenString(n) + " expected!");
            return false;
        }

        /// <summary>
        /// Retrieve string value of current lookAhead token
        /// </summary>
        protected string strVal
        {
            get
            {
                if (la.Kind == DTokens.Identifier || la.Kind == DTokens.Literal)
                    return la.Value;
                return DTokens.GetTokenString(la.Kind);
            }
        }

        protected bool ThrowIfEOF(int n)
        {
            if (la.Kind == DTokens.EOF)
            {
                SynErr(n, "End of file reached!");
                return true;
            }
            return false;
        }

        protected bool PeekMustBe(int n, string reason)
        {
            if (Peek(1).Kind == n)
            {
                lexer.NextToken();
            }
            else
            {
                SynErr(n, reason);
                return false;
            }
            return true;
        }

        /* Return the n-th token after the current lookahead token */
        void StartPeek()
        {
            lexer.StartPeek();
        }

        DToken Peek()
        {
            return lexer.Peek();
        }

        DToken Peek(int n)
        {
            lexer.StartPeek();
            DToken x = la;
            while (n > 0)
            {
                x = lexer.Peek();
                n--;
            }
            return x;
        }

        /* True, if ident is followed by ",", "=", "[" or ";"*/
        bool IsVarDecl()
        {
            int peek = Peek(1).Kind;
            return la.Kind == DTokens.Identifier &&
                (peek == DTokens.Comma || peek == DTokens.Assign || peek == DTokens.Semicolon);
        }

        bool IsEOF
        {
            get { return la == null || la.Kind == EOF; }
        }

        DToken Step() { return lexer.NextToken(); }

        /// <summary>
        /// Initializes and proceed parse procedure
        /// </summary>
        /// <param name="imports">List of imports in the module</param>
        /// <param name="fl">TODO: Folding marks</param>
        /// <returns>Completely parsed module structure</returns>
        public DNode Parse(string moduleName, out List<string> imports)
        {
            import = new List<string>();
            imports = import;

            BlockModifiers = new List<int>();
            BlockModifiers.Add(DTokens.Public);
            ExpressionModifiers = new List<int>();

            doc = new DNode(FieldType.Root);
            doc.name = moduleName;
            doc.startLoc = CodeLocation.Empty;
            doc.module = moduleName;
            //ParseBlock(ref doc, false);

            Root();

            doc.EndLocation=la.EndLocation;
            return doc;
        }
        
        #region Error handlers
        public delegate void ErrorHandler(string file, string module, int line, int col, int kindOf, string message);
        static public event ErrorHandler OnError, OnSemanticError;


        void SynErr(int n, int col, int ln)
        {
            OnError(PhysFileName, Document.module, ln, col, n, "");
            //errors.SynErr(ln, col, n);
        }
        void SynErr(int n, int col, int ln, string msg)
        {
            OnError(PhysFileName, Document.module, ln, col, n, msg);
            //errors.Error(ln, col, msg);
        }
        void SynErr(int n, string msg)
        {
            OnError(PhysFileName, Document.module, la.Location.Line, la.Location.Column, n, msg);
            //errors.Error(la.Location.Line, la.Location.Column, msg);
        }
        void SynErr(int n)
        {
            OnError(PhysFileName, Document != null ? Document.module : null, la != null ? la.Location.Line : 0, la != null ? la.Location.Column : 0, n, DTokens.GetTokenString(n)+" expected");
            //errors.SynErr(la != null ? la.Location.Line : 0, la != null ? la.Location.Column : 0, n);
        }

        void SemErr(int n, int col, int ln)
        {
            OnSemanticError(PhysFileName, Document.module, ln, col, n, "");
            //errors.SemErr(ln, col, n);
        }
        void SemErr(int n, int col, int ln, string msg)
        {
            OnSemanticError(PhysFileName, Document.module, ln, col, n, msg);
            //errors.Error(ln, col, msg);
        }
        void SemErr(int n, string msg)
        {
            OnSemanticError(PhysFileName, Document.module, la.Location.Line, la.Location.Column, n, msg);
            //errors.Error(la.Location.Line, la.Location.Column, msg);
        }
        void SemErr(int n)
        {
            OnSemanticError(PhysFileName, Document != null ? Document.module : null, la != null ? la.Location.Line : 0, la != null ? la.Location.Column : 0, n, "");
            //errors.SemErr(la != null ? la.Location.Line : 0, la != null ? la.Location.Column : 0, n);
        }
        #endregion
    }
}
