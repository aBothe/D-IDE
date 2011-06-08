using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using D_Parser.Core;

namespace D_Parser
{
    /// <summary>
    /// Parser for D Code
    /// </summary>
    public partial class DParser:DTokens
    {
		/// <summary>
		/// Finds the last import statement and returns its end location (the position after the semicolon).
		/// If no import but module statement was found, the end location of this module statement will be returned.
		/// </summary>
		/// <param name="Code"></param>
		/// <returns></returns>
		public static CodeLocation FindLastImportStatementEndLocation(string Code)
		{
			var p = Create(new StringReader(Code));
			p.doc = new DModule();// create dummy module to prevent crash at ImportDeclaration();
			p.Step();

			if (p.LA(Module))
				p.ModuleDeclaration();

			while (p.LA(Import))
				p.ImportDeclaration();

			return p.t.EndLocation;
		}

        public static IExpression ParseExpression(string Code)
        {
            var p = Create(new StringReader(Code));
            p.Step();
            return p.Expression();
        }

        public static ITypeDeclaration ParseBasicType(string Code,out DToken OptionalToken)
        {
            OptionalToken = null;

            var p = Create(new StringReader(Code));
            p.Step();
            // Exception: If we haven't got any basic types as our first token, return this token via OptionalToken
            if (!p.IsBasicType() || p.LA(__LINE__) || p.LA(__FILE__))
            {
                p.Step();
                p.Peek(1);
                OptionalToken = p.t;

                // Only if a dot follows a 'this' or 'super' token we go on parsing; Return otherwise
                if (!((p.t.Kind == This || p.t.Kind == Super) && p.la.Kind == Dot))
                    return null;
            }
            
            var bt= p.BasicType();
            while (p.IsBasicType2())
            {
                var bt2 = p.BasicType2();
                bt2.InnerMost = bt;
                bt = bt2;
            }
            return bt;
        }

        public static IAbstractSyntaxTree ParseString(string ModuleCode)
        {
            return ParseString(ModuleCode,false);
        }

        public static IAbstractSyntaxTree ParseString(string ModuleCode,bool SkipFunctionBodies)
        {
            var p = Create(new StringReader(ModuleCode));
            return p.Parse(SkipFunctionBodies);
        }

        public static IAbstractSyntaxTree ParseFile(string File)
        {
            return ParseFile(File, false);
        }

        public static IAbstractSyntaxTree ParseFile(string File, bool SkipFunctionBodies)
        {
            var s=new FileStream(File,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);
            var p=Create(new StreamReader(s));
            var m = p.Parse(SkipFunctionBodies);
            m.FileName = File;
			m.ModuleName = Path.GetFileNameWithoutExtension(File);
            s.Close();
            return m;
        }

        /// <summary>
        /// Parses the module again
        /// </summary>
        /// <param name="Module"></param>
        public static void UpdateModule(IAbstractSyntaxTree Module)
        {
            var m = DParser.ParseFile(Module.FileName);
			Module.ParseErrors = m.ParseErrors;
            Module.Assign(m);
        }

        public static void UpdateModuleFromText(IAbstractSyntaxTree Module, string Code)
        {
            var m = DParser.ParseString(Code);
			Module.ParseErrors = m.ParseErrors;
            Module.Assign(m);
        }

        public static DParser Create(TextReader tr)
        {
            return new DParser(new DLexer(tr));
        }

        /// <summary>
        /// Encapsules whole document structure
        /// </summary>
        IAbstractSyntaxTree doc;

        /// <summary>
        /// Modifiers for entire block
        /// </summary>
        Stack<DAttribute> BlockAttributes=new Stack<DAttribute>();
        /// <summary>
        /// Modifiers for current expression only
        /// </summary>
        Stack<DAttribute> DeclarationAttributes=new Stack<DAttribute>();

		void PushAttribute(DAttribute attr, bool BlockAttributes)
		{
			var stk=BlockAttributes?this.BlockAttributes:this.DeclarationAttributes;

			// If attr would change the accessability of an item, remove all previously found (so the most near attribute that's next to the item is significant)
			if (DTokens.VisModifiers[attr.Token])
				DAttribute.CleanupAccessorAttributes(stk);

			stk.Push(attr);
		}

        void ApplyAttributes(DNode n)
        {
            foreach (var attr in BlockAttributes.ToArray())
                n.Attributes.Add(attr);

            while (DeclarationAttributes.Count > 0)
            {
                var attr = DeclarationAttributes.Pop();

				// If accessor already in attribute array, remove it
				if (DTokens.VisModifiers[attr.Token])
					DAttribute.CleanupAccessorAttributes(n.Attributes);

                if (!DAttribute.ContainsAttribute(n.Attributes.ToArray(),attr.Token))
                    n.Attributes.Add(attr);
            }
        }

        public IAbstractSyntaxTree Document
        {
            get { return doc; }
        }
        bool ParseStructureOnly = false;
        public DLexer Lexer;
        public DParser(DLexer lexer)
        {
            this.Lexer = lexer;
			Lexer.LexerErrors = ParseErrors;
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
                return (DToken)Lexer.CurrentToken;
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
            while (Lexer.CurrentPeekToken.Kind != EOF)
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
                return (DToken)Lexer.LookAhead;
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
                Lexer.NextToken();
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
            return Lexer.CurrentPeekToken.Kind == n;
        }

        private bool Expect(int n)
        {
			if (la.Kind == n)
			{ Step(); return true; }
			else
			{
				SynErr(n, DTokens.GetTokenString(n) + " expected, "+DTokens.GetTokenString(la.Kind)+" found!");
			}
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
            Lexer.StartPeek();
        }

        DToken Peek()
        {
            return Lexer.Peek();
        }

        DToken Peek(int n)
        {
            Lexer.StartPeek();
            DToken x = la;
            while (n > 0)
            {
                x = Lexer.Peek();
                n--;
            }
            return x;
        }

        bool IsEOF
        {
            get { return la == null || la.Kind == EOF || la.Kind == __EOF__; }
        }

        DToken Step() { Lexer.NextToken(); Peek(1); return t; }

        [DebuggerStepThrough()]
        public IAbstractSyntaxTree Parse()
        {
            return Parse(false);
        }

        /// <summary>
        /// Initializes and proceed parse procedure
        /// </summary>
        /// <param name="imports">List of imports in the module</param>
        /// <param name="ParseStructureOnly">If true, all statements and non-declarations are ignored - useful for analysing libraries</param>
        /// <returns>Completely parsed module structure</returns>
        public IAbstractSyntaxTree Parse(bool ParseStructureOnly)
        {
            this.ParseStructureOnly = ParseStructureOnly;
            doc=Root();
			doc.ParseErrors = new System.Collections.ObjectModel.ReadOnlyCollection<ParserError>(ParseErrors);
            return doc;
        }
        
        #region Error handlers
		public IList<ParserError> ParseErrors = new List<ParserError>();

        void SynErr(int n, string msg)
        {
			ParseErrors.Add(new ParserError(false,msg,n,t.EndLocation));
        }
        void SynErr(int n)
		{
			ParseErrors.Add(new ParserError(false, DTokens.GetTokenString(n) + " expected", n,t!=null? t.EndLocation:new CodeLocation()));
        }

        void SemErr(int n, string msg)
        {
			ParseErrors.Add(new ParserError(true, msg, n, t.EndLocation));
        }
        void SemErr(int n)
        {
			ParseErrors.Add(new ParserError(true, DTokens.GetTokenString(n) + " expected", n, t != null ? t.EndLocation : new CodeLocation()));
        }
        #endregion
    }
}
