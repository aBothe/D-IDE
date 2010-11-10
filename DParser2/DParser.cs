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
        public static DExpression ParseExpression(string Code)
        {
            var p = Create(new StringReader(Code));
            p.Step();
            return p.Expression();
        }

        public static TypeDeclaration ParseBasicType(string Code)
        {
            var p = Create(new StringReader(Code));
            p.Step();

            return p.BasicType();
        }

        public static DModule ParseString(string ModuleCode)
        {
            return ParseString(ModuleCode,false);
        }

        public static DModule ParseString(string ModuleCode,bool SkipFunctionBodies)
        {
            var p = Create(new StringReader(ModuleCode));
            return p.Parse(SkipFunctionBodies);
        }

        public static DModule ParseFile(string File)
        {
            return ParseFile(File, false);
        }

        public static DModule ParseFile(string File, bool SkipFunctionBodies)
        {
            var s=new FileStream(File,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);
            var p=Create(new StreamReader(s));
            var m = p.Parse(SkipFunctionBodies);
            m.ModuleFileName = File;
            s.Close();
            return m;
        }

        /// <summary>
        /// Parses the module again
        /// </summary>
        /// <param name="Module"></param>
        public static void UpdateModule(DModule Module)
        {
            var m = DParser.ParseFile(Module.ModuleFileName);
            Module.ApplyFrom(m);
        }

        public static void UpdateModuleFromText(DModule Module, string Code)
        {
            var m = DParser.ParseString(Code);
            Module.ApplyFrom(m);
        }

        static DParser Create(TextReader tr)
        {
            return new DParser(new DLexer(tr));
        }

        /// <summary>
        /// Encapsules whole document structure
        /// </summary>
        DModule doc;

        /// <summary>
        /// Modifiers for entire block
        /// </summary>
        Stack<DAttribute> BlockAttributes=new Stack<DAttribute>();
        /// <summary>
        /// Modifiers for current expression only
        /// </summary>
        Stack<DAttribute> DeclarationAttributes=new Stack<DAttribute>();

        void ApplyAttributes(ref DNode n)
        {
            foreach (var attr in BlockAttributes.ToArray())
                n.Attributes.Add(attr);

            while (DeclarationAttributes.Count > 0)
            {
                var attr = DeclarationAttributes.Pop();
                if (!DAttribute.ContainsAttribute(n.Attributes.ToArray(),attr.Token))
                    n.Attributes.Add(attr);
            }
        }

        public DModule Document
        {
            get { return doc; }
        }
        bool ParseStructureOnly = false;
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

        string CheckForDocComments()
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

        [DebuggerStepThrough()]
        public DModule Parse()
        {
            return Parse(false);
        }

        /// <summary>
        /// Initializes and proceed parse procedure
        /// </summary>
        /// <param name="imports">List of imports in the module</param>
        /// <param name="ParseStructureOnly">If true, all statements and non-declarations are ignored - useful for analysing libraries</param>
        /// <returns>Completely parsed module structure</returns>
        public DModule Parse(bool ParseStructureOnly)
        {
            this.ParseStructureOnly = ParseStructureOnly;
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
