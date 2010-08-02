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

        void OverPeekRoundBrackets()
        {
            int roundBrackets = 0;
            while (roundBrackets >= 0 && lexer.CurrentPeekToken.Kind != EOF && !PK(CloseParenthesis))
            {
                if (PK(OpenParenthesis))
                    roundBrackets++;
                if (PK(CloseParenthesis))
                    roundBrackets--;
                Peek();
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
            return lexer.CurrentPeekToken.Kind == n;
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
        /// Retrieve string value of current token
        /// </summary>
        protected string strVal
        {
            get
            {
                if (t.Kind == DTokens.Identifier || t.Kind == DTokens.Literal)
                    return t.Value;
                return DTokens.GetTokenString(t.Kind);
            }
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

        DToken Step() { lexer.NextToken(); Peek(1); return t; }

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
