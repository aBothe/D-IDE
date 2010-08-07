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
        public static DParser Create(TextReader tr)
        {
            DLexer dl = new DLexer(tr);
            return new DParser(dl);
        }

        /// <summary>
        /// Encapsules whole document structure
        /// </summary>
        DModule doc;

        /// <summary>
        /// Modifiers for entire block
        /// </summary>
        Stack<int> BlockAttributes=new Stack<int>();
        /// <summary>
        /// Modifiers for current expression only
        /// </summary>
        Stack<int> DeclarationAttributes=new Stack<int>();

        void ApplyAttributes(ref DNode n)
        {
            foreach (int attr in BlockAttributes.ToArray())
                n.Attributes.Add(attr);

            while (DeclarationAttributes.Count > 0)
            {
                int attr = DeclarationAttributes.Pop();
                if (!n.Attributes.Contains(attr))
                    n.Attributes.Add(attr);
            }
        }

        public DModule Document
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
                    LastElement.Description += (LastElement.Description == "" ? "" : "\r\n") + CurrentDescription;
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

        void OverPeekBrackets(int OpenBracketKind)
        {
            OverPeekBrackets(OpenBracketKind, false);
        }

        void OverPeekBrackets(int OpenBracketKind,bool LAIsOpenBracket)
        {
            int CloseBracket = CloseParenthesis;
            if (OpenBracketKind == OpenSquareBracket) CloseBracket = CloseSquareBracket;
            else if (OpenBracketKind == OpenCurlyBrace) CloseBracket = CloseCurlyBrace;

            int i = LAIsOpenBracket?1:0;
            while (lexer.CurrentPeekToken.Kind != EOF)
            {
                if (PK(OpenBracketKind))
                    i++;
                else if (PK(CloseBracket))
                {
                    i--;
                    if (i <= 0) { Peek(); break; }
                }
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
        public DModule Parse()
        {
            doc=Root();
            return doc;
        }
        
        #region Error handlers
        public delegate void ErrorHandler(DModule tempModule, int line, int col, int kindOf, string message);
        static public event ErrorHandler OnError, OnSemanticError;

        void SynErr(int n, string msg)
        {
            OnError(Document, la.Location.Line, la.Location.Column, n, msg);
            //errors.Error(la.Location.Line, la.Location.Column, msg);
        }
        void SynErr(int n)
        {
            OnError(Document, la != null ? la.Location.Line : 0, la != null ? la.Location.Column : 0, n, DTokens.GetTokenString(n)+" expected");
            //errors.SynErr(la != null ? la.Location.Line : 0, la != null ? la.Location.Column : 0, n);
        }

        void SemErr(int n, string msg)
        {
            OnSemanticError(Document, la.Location.Line, la.Location.Column, n, msg);
            //errors.Error(la.Location.Line, la.Location.Column, msg);
        }
        void SemErr(int n)
        {
            OnSemanticError(Document, la != null ? la.Location.Line : 0, la != null ? la.Location.Column : 0, n, "");
            //errors.SemErr(la != null ? la.Location.Line : 0, la != null ? la.Location.Column : 0, n);
        }
        #endregion
    }
}
