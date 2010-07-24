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
    public partial class DParser
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

            if (SkipLastClosingParenthesis && round<1 && la.Kind == DTokens.CloseParenthesis) lexer.NextToken();

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
        /// Check if current lookAhead DToken equals to n and skip that token.
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

        bool EOF
        {
            get { return la == null || la.Kind == DTokens.EOF; }
        }



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
            ParseBlock(ref doc, false);

            doc.endLoc = GetCodeLocation(la);
            return doc;
        }

        /// <summary>
        /// Parses complete block from current lookahead DToken "{" until the last "}" on the same depth
        /// </summary>
        /// <param name="ret">Parent node</param>
        void ParseBlock(ref DNode ret, bool isFunctionBody)
        {
            int curbrace = 0;
            if (String.IsNullOrEmpty(ret.desc)) ret.desc = CheckForDocComments();
            List<int> prevBlockModifiers = new List<int>(BlockModifiers);
            ExpressionModifiers.Clear();
            BlockModifiers.Clear();
            BlockModifiers.Add(DTokens.Public);

            //Debug.Print("ParseBlock started ("+ret.name+")");

            if (la != null) ret.startLoc = ret.BlockStartLocation = GetCodeLocation(la);

            while (la == null || la.Kind != DTokens.EOF)
            {
                lexer.NextToken();
            blockcont:
                if (la.Kind == DTokens.EOF) { if (curbrace > 1) SynErr(DTokens.CloseCurlyBrace); break; }
                BlockModifiers = prevBlockModifiers;

                if (la.Kind == DTokens.Scope)
                {
                    if (Peek(1).Kind == DTokens.OpenParenthesis)
                    {
                        SkipToClosingParenthesis();
                        goto blockcont;
                    }
                }

                #region Modifiers
                if (DTokens.Modifiers[la.Kind] || DTokens.MemberFunctionAttributes[la.Kind])
                {
                    DToken pt = Peek(1);
                    int mod = la.Kind;

                    if (pt.Kind == DTokens.OpenParenthesis && Peek().Kind == DTokens.CloseParenthesis && Peek().Kind == DTokens.OpenCurlyBrace) // invariant() {...} - whatever this shall mean...something like that is possible in D!
                    {
                        lexer.NextToken(); // Skip modifier ID
                        lexer.NextToken(); // Skip "("
                        if (Peek(1).Kind == DTokens.OpenCurlyBrace)
                        {
                            SkipToClosingBrace();
                        }
                        continue;
                    }
                    else if (DTokens.MemberFunctionAttributes[la.Kind] && pt.Kind == DTokens.OpenParenthesis)
                    {
                        goto go_on;
                    }

                    if (DTokens.MemberFunctionAttributes[la.Kind] && pt.Kind == DTokens.Identifier && (Peek().Kind == DTokens.Assign || lexer.CurrentPeekToken.Kind == DTokens.Comma || lexer.CurrentPeekToken.Kind == DTokens.Semicolon)) // const abc=45;
                    {
                        DVariable cnst = new DVariable();
                        cnst.desc = CheckForDocComments();
                        cnst.StartLocation = la.Location;
                        cnst.Type = new DTokenDeclaration(la.Kind);
                        lexer.NextToken(); // Skip 'const'
                        cnst.name = strVal;

                        if (Peek(1).Kind == DTokens.Assign)
                        {
                            lexer.NextToken();
                            cnst.Value = ParseAssignIdent(DTokens.Semicolon, DTokens.Comma);
                            if (Peek(1).Kind == DTokens.Semicolon || lexer.CurrentPeekToken.Kind == DTokens.Comma) lexer.NextToken();
                        }
                        cnst.EndLocation = la.EndLocation;
                        LastElement = cnst;
                        ret.Add(cnst);
                        continue;
                    }

                    if (pt.Kind == DTokens.Import) // private import...
                        continue;
                    else if (pt.Kind == DTokens.Colon)// private>:<
                    {
                        if (!BlockModifiers.Contains(mod))
                        {
                            if (DTokens.VisModifiers[mod]) DTokens.RemoveVisMod(BlockModifiers);
                            BlockModifiers.Add(mod);
                        }
                        continue;
                    }
                    else if (pt.Kind == DTokens.OpenCurlyBrace) // public >{<...}
                    {
                        lexer.NextToken(); // Skip modifier
                        DNode tblock = new DNode(ret.fieldtype);
                        ParseBlock(ref tblock, isFunctionBody);

                        foreach (DNode dt in tblock) // Apply modifier to parsed children
                        {
                            if (!dt.modifiers.Contains(mod)) // static package int a;
                            {
                                if (DTokens.VisModifiers[mod]) DTokens.RemoveVisMod(dt.modifiers);
                                dt.modifiers.Add(mod);
                            }
                        }

                        ret.Children.AddRange(tblock.Children);
                        continue;
                    }
                    else
                    {
                        DToken pt2 = pt;
                        pt = lexer.Peek();
                        bool hasFollowingMods = false;
                        while (pt.Kind != DTokens.EOF)
                        {
                            if (DTokens.Modifiers[pt.Kind] || DTokens.MemberFunctionAttributes[pt.Kind]) // static >const<
                            {
                                pt = lexer.Peek();
                                if (pt.Kind == DTokens.OpenCurlyBrace) // static const >{<...}
                                {
                                    hasFollowingMods = true;
                                    break;
                                }
                            }
                            else
                                break;
                            pt = lexer.Peek();
                        }

                        if (!hasFollowingMods && DTokens.MemberFunctionAttributes[pt.Kind] && pt2.Kind == DTokens.Identifier && pt.Kind == DTokens.Assign) // const >MyCnst2< = 2; // similar to enum MyCnst = 1;
                        {
                            DNode cdt = ParseEnum();
                            cdt.Type = new DTokenDeclaration(DTokens.Int);
                            cdt.modifiers.Add(DTokens.Const);
                            cdt.TypeToken = DTokens.Int;
                            cdt.Parent = ret;
                            ret.Children.Add(cdt);
                        }

                        if (!ExpressionModifiers.Contains(mod) && !hasFollowingMods) // static package int a;
                        {
                            if (DTokens.VisModifiers[mod]) DTokens.RemoveVisMod(ExpressionModifiers);
                            ExpressionModifiers.Add(mod);
                        }
                        continue;
                    }

                }
                #endregion
            go_on:
                #region Normal Expressions
                if (DTokens.BasicTypes[la.Kind] ||
                    la.Kind == DTokens.Identifier ||
                    DTokens.MemberFunctionAttributes[la.Kind] ||
                    la.Kind == DTokens.Typeof ||
                    la.Kind == DTokens.Auto)
                {

                    DToken pk = Peek(1);
                    switch (pk.Kind)
                    {
                        /*
                     * Could be function call (foo>(<))
                     * but it may be function pointer declaration (int >(<*foo)();)
                     * don't confuse it with a function call that contains a * as one of the first arguments like foo(*pointer);
                     */
                        case DTokens.OpenParenthesis:
                            if (!DTokens.BasicTypes[la.Kind] && la.Kind != DTokens.Identifier)
                                break;
                            pk = Peek();
                            if (pk.Kind != DTokens.Times)
                            {
                                SkipToSemicolon();
                                continue;
                            }
                            else
                            {
                                #region Search for a possible function pointer definition
                                int par = 0;
                                bool IsFunctionDefinition = false;
                                while ((pk = Peek()).Kind != DTokens.EOF)
                                {
                                    if (pk.Kind == DTokens.OpenParenthesis)
                                    {
                                        if (par < 0)
                                        {
                                            IsFunctionDefinition = true;
                                            break;
                                        }
                                        par++;
                                    }
                                    if (pk.Kind == DTokens.CloseParenthesis) par--;

                                    if (pk.Kind == DTokens.Semicolon || pk.Kind == DTokens.CloseCurlyBrace || pk.Kind == DTokens.OpenCurlyBrace) break;
                                }
                                if (!IsFunctionDefinition)
                                {
                                    SkipToSemicolon();
                                    continue;
                                }
                                #endregion
                            }
                            break;
                    }

                    #region Within Function Body
                    if (isFunctionBody)
                    {
                        switch (pk.Kind)
                        {/*
                            case DTokens.Dot: // Package.foo();
                                continue;*/

                            case DTokens.Not: // Type!(int,b)();
                                int par = 0;
                                bool isCall = false;
                                Peek(); // skip peeked '!'
                                while ((pk = Peek()).Kind != DTokens.EOF)
                                {
                                    if (pk.Kind == DTokens.OpenParenthesis)
                                    {
                                        if (par < 0) // Template!( |Here we start; par=0|...(.|par=1|.). |par=0|.. ) |par=-1| >(<
                                        {
                                            isCall = true; break;
                                        }
                                        par++;
                                    }
                                    if (pk.Kind == DTokens.CloseParenthesis) par--;

                                    if (pk.Kind == DTokens.Semicolon || pk.Kind == DTokens.OpenCurlyBrace) { isCall = false; break; }
                                }
                                if (!isCall) break;
                                SkipToSemicolon();
                                continue;

                            case DTokens.OpenSquareBracket: // array[0]+=5; char[] buf;
                                #region Check if Var Decl is done
                                int mbrace2 = 0;
                                bool isDecl = false;
                                while ((pk = Peek()).Kind != DTokens.EOF)
                                {
                                    switch (pk.Kind)
                                    {
                                        case DTokens.OpenSquareBracket:
                                            mbrace2++;
                                            break;
                                        case DTokens.CloseSquareBracket:
                                            mbrace2--;
                                            break;
                                        case DTokens.Dot:
                                            if (mbrace2 > 0) continue;
                                            pk = Peek();
                                            if (pk.Kind == DTokens.Identifier) // array[i].foo(); array[i].x=2;
                                            {
                                                continue;
                                            }
                                            break;
                                    }

                                    if (mbrace2 < 1)
                                    {
                                        if (DTokens.AssignOps[pk.Kind])
                                        {
                                            isDecl = pk.Kind == DTokens.Assign;
                                            break;
                                        }

                                        if (pk.Kind == DTokens.Identifier)
                                        {
                                            pk = Peek();
                                            if (pk.Kind == DTokens.Comma || // string[] a,b;
                                                pk.Kind == DTokens.Assign || // string[] stringArray=...;
                                                pk.Kind == DTokens.Semicolon || // string[] stringArray;
                                                pk.Kind == DTokens.OpenSquareBracket) // char[] ID=value;
                                            { isDecl = true; goto parseexpr; }
                                        }
                                    }
                                }
                                #endregion
                                if (isDecl) goto default;
                            sqbracket:
                                if (la.Kind == DTokens.OpenSquareBracket)
                                    SkipToClosingSquares();
                                if (la.Kind == DTokens.CloseSquareBracket)
                                {
                                    pk = Peek(1);
                                    if (pk.Kind == DTokens.OpenSquareBracket) // matrix[0][4]=1;
                                    {
                                        lexer.NextToken(); // Skip last "]"
                                        goto sqbracket;
                                    }
                                }
                                goto default;

                            case DTokens.Colon: // part:
                                lexer.NextToken();
                                continue;

                            case DTokens.OpenParenthesis: // becomes handled few lines below!
                                break;

                            default:
                                if (pk.Kind == DTokens.Increment ||  // a++;
                                    pk.Kind == DTokens.Decrement)
                                {
                                    lexer.NextToken();
                                    continue;
                                }
                                if (DTokens.AssignOps[pk.Kind])// b<<=4;
                                {
                                    SkipToSemicolon();//ParseAssignIdent(ref ret, true);
                                    continue;
                                }
                                if (pk.Kind == DTokens.Semicolon) continue;

                                if (DTokens.Conditions[pk.Kind]) // p!= null || p<1
                                {
                                    SkipToSemicolon();
                                    continue;
                                }
                                break;
                        }
                    }
                    #endregion
                parseexpr:
                    if (DTokens.ClassLike[Peek(1).Kind]) break;


                    #region Modifier assessment
                    bool cvm = DTokens.ContainsVisMod(ExpressionModifiers);
                    foreach (int m in BlockModifiers)
                    {
                        if (!ExpressionModifiers.Contains(m))
                        {
                            if (cvm && DTokens.VisModifiers[m]) continue;
                            ExpressionModifiers.Add(m);
                        }
                    }
                    List<int> TExprMods = new List<int>(ExpressionModifiers);
                    ExpressionModifiers.Clear();

                    #endregion

                    DNode tv = ParseExpression();
                    if (tv != null)
                    {
                        LastElement = tv;
                        tv.modifiers.AddRange(TExprMods);
                        tv.module = ret.module;
                        tv.Parent = ret;
                        ret.Add(tv);

                        if (la.Kind == DTokens.Comma) goto blockcont;
                    }
                    continue;
                }
                #endregion

                #region Special and other Tokens
                switch (la.Kind)
                {
                    case DTokens.Times: // *ptr=123;
                        if (!isFunctionBody)
                            SynErr(la.Kind, "'*' not allowed here; Identifier expected");
                        SkipToSemicolon();
                        break;

                    case DTokens.OpenParenthesis: // (...);
                        if (!isFunctionBody)
                            SynErr(la.Kind, "C-style cast not allowed here");
                        SkipToSemicolon();
                        break;

                    #region Custom Allocators
                    case DTokens.Delete:
                    case DTokens.New:
                        bool IsAlloc = la.Kind == DTokens.New;
                        if (isFunctionBody) break;

                        // This is for handling custom allocators (new(uint size){...})
                        if (!isFunctionBody)
                        {
                            if (!PeekMustBe(DTokens.OpenParenthesis, "Expected \"(\" for declaring a custom (de-)allocator!"))
                            {
                                SkipToClosingBrace();
                                break;
                            }
                            DMethod custAlloc = new DMethod();
                            if (IsAlloc)
                            {
                                custAlloc.name = "new";
                                custAlloc.Type = new PointerDecl(new DTokenDeclaration(DTokens.Void));
                            }
                            else
                            {
                                custAlloc.name = "delete";
                                custAlloc.Type = new DTokenDeclaration(DTokens.Void);
                            }
                            custAlloc.TypeToken = DTokens.New;
                            lexer.NextToken();
                            ParseFunctionArguments(ref custAlloc);
                            if (!Expect(DTokens.CloseParenthesis, "Expected \")\" for declaring a custom (de-)allocator!"))
                            {
                                SkipToClosingBrace();
                                break;
                            }
                            custAlloc.modifiers.Add(DTokens.Private);
                            DNode _custAlloc = custAlloc;
                            ParseBlock(ref _custAlloc, true);

                            custAlloc.module = ret.module;
                            custAlloc.Parent = ret;
                            ret.Add(custAlloc);
                        }
                        break;
                    #endregion
                    case DTokens.Cast:
                        //SynErr(la.Kind, "Cast cannot be done at front of a statement");
                        SkipToSemicolon();
                        break;
                    case DTokens.With:
                        if (PeekMustBe(DTokens.OpenParenthesis, "Error parsing \"with()\" Expression: \"(\" expected!"))
                        {
                            SkipToClosingParenthesis();
                            if (t.Kind != DTokens.CloseParenthesis) SynErr(DTokens.CloseParenthesis, "Error parsing \"with()\" Expression: \")\" expected!");
                            goto blockcont;
                        }
                        break;
                    case DTokens.Asm: // Inline Assembler
                        SkipToClosingBrace();
                        break;
                    case DTokens.Case:
                        while (!EOF)
                        {
                            lexer.NextToken();
                            if (la.Kind == DTokens.Colon) break;
                        }
                        break;
                    case DTokens.Catch:
                        if (Peek(1).Kind == DTokens.OpenParenthesis)
                        {
                            SkipToClosingParenthesis();
                            goto blockcont;
                        }
                        break;
                    case DTokens.Debug:
                        if (Peek(1).Kind == DTokens.OpenParenthesis)
                        {
                            SkipToClosingParenthesis();
                            goto blockcont;
                        }
                        break;
                    case DTokens.Goto:
                        SkipToSemicolon();
                        break;
                    case DTokens.Throw:
                    case DTokens.Return:
                        SkipToSemicolon();
                        break;
                    case DTokens.Unittest:
                        if (Peek(1).Kind == DTokens.OpenCurlyBrace)
                        {
                            SkipToClosingBrace();
                        }
                        break;
                    case DTokens.Do: // do {...} while(...);
                        if (Peek(1).Kind == DTokens.OpenCurlyBrace)
                        {
                            SkipToClosingBrace();
                            Expect(DTokens.CloseCurlyBrace, "Error parsing do Expression: \"}\" after \"do\" block expected!");
                        }
                        else
                        {
                            SkipToSemicolon();
                            Expect(DTokens.Semicolon, "Semicolon after statement expected!");
                        }

                        if (Expect(DTokens.While, "while() expected!"))
                        {
                            SkipToSemicolon();
                        }
                        break;
                    case DTokens.For:
                    case DTokens.Foreach:
                    case DTokens.Foreach_Reverse:
                    case DTokens.Switch:
                    case DTokens.While:
                    case DTokens.If: // static if(...) {}else if(...) {} else{}
                        if (PeekMustBe(DTokens.OpenParenthesis, "'(' expected!"))
                        {
                            SkipToClosingParenthesis();
                            goto blockcont;
                        }
                        break;
                    case DTokens.Else:
                        break;
                    case DTokens.Comma:
                        if (ret.Count < 1) break;
                        if (!PeekMustBe(DTokens.Identifier, "Expected variable identifier!"))
                        {
                            SkipToSemicolon();
                            break;
                        }
                        // MyType a,b,c,d;
                        DNode prevExpr = (DNode)ret.Children[ret.Count - 1];
                        if (prevExpr is DVariable)
                        {
                            DVariable tv = new DVariable();
                            if (tv == null) continue;
                            tv.Assign(prevExpr);
                            tv.desc = CheckForDocComments();
                            tv.StartLocation = la.Location;
                            tv.fieldtype = prevExpr.fieldtype;
                            if (la.Kind != DTokens.Identifier)
                            {
                                SynErr(DTokens.Comma, "Identifier for var enumeration expected!");
                                continue;
                            }
                            tv.name = strVal;
                            tv.EndLocation = la.EndLocation;
                            lexer.NextToken(); // Skip var id

                            if (Peek(1).Kind == DTokens.Assign) lexer.NextToken();
                            if (la.Kind == DTokens.Assign) tv.Value = ParseAssignIdent(DTokens.Semicolon, DTokens.Comma);

                            LastElement = tv;
                            ret.Add(tv);

                            if (la.Kind == DTokens.Comma) goto blockcont; // Another declaration is directly following
                        }
                        break;
                    case DTokens.EOF:
                        if (t.Kind != DTokens.CloseCurlyBrace) SynErr(DTokens.CloseCurlyBrace);
                        ret.endLoc = GetCodeLocation(la);
                        BlockModifiers = prevBlockModifiers;
                        return;
                    case DTokens.Align:
                    case DTokens.Version:
                        lexer.NextToken(); // Skip "version"

                        if (la.Kind == DTokens.Assign) // version=xxx
                        {
                            SkipToSemicolon();
                            break;
                        }

                        Expect(DTokens.OpenParenthesis, "'(' expected!");
                        string version = strVal; // version(xxx)
                        if (version == "Posix" && Peek(1).Kind == DTokens.OpenCurlyBrace) SkipToClosingBrace();
                        break;
                    case DTokens.Extern:
                        if (Peek(1).Kind == DTokens.OpenParenthesis)
                        {
                            SkipToClosingParenthesis();
                            goto blockcont;
                        }
                        break;
                    case DTokens.CloseCurlyBrace: // }
                        curbrace--;
                        if (curbrace < 0)
                        {
                            ret.endLoc = GetCodeLocation(la);
                            BlockModifiers = prevBlockModifiers;
                            ExpressionModifiers.Clear();

                            CurrentDescription = "";
                            return;
                        }
                        break;
                    case DTokens.OpenCurlyBrace: // {
                        curbrace++;
                        break;
                    case DTokens.Enum:
                        DNode mye = ParseEnum();
                        if (mye != null)
                        {
                            mye.Parent = ret;
                            mye.module = ret.module;

                            if (mye.name != "")
                            {
                                LastElement = mye;
                                ret.Add(mye);
                            }
                            else
                            {
                                foreach (DNode ch in mye)
                                {
                                    ch.Parent = ret;
                                    ch.module = ret.module;
                                }
                                ret.Children.AddRange(mye.Children);
                            }
                            if (la.Kind == DTokens.Comma) // enum int abc>,< def=45;
                                goto blockcont;
                        }
                        break;
                    case DTokens.Super:
                        if (isFunctionBody) // Every "super" in a function body can only be a call....
                        {
                            SkipToSemicolon();
                            break;
                        }
                        else SynErr(DTokens.Super);
                        break;
                    case DTokens.This:
                        if (isFunctionBody) // Every "this" in a function body can only be a call....
                        {
                            SkipToSemicolon();
                            break;
                        }

                        #region Modifiers
                        bool cvm = DTokens.ContainsVisMod(ExpressionModifiers);
                        foreach (int m in BlockModifiers)
                        {
                            if (!ExpressionModifiers.Contains(m))
                            {
                                if (cvm && DTokens.VisModifiers[m]) continue;
                                ExpressionModifiers.Add(m);
                            }
                        }
                        List<int> TExprMods = new List<int>(ExpressionModifiers);
                        ExpressionModifiers.Clear();

                        #endregion

                        bool IsDestructor = t.Kind == DTokens.Tilde;

                        DNode ctor = ParseExpression();
                        if (ctor != null)
                        {
                            if (ret.fieldtype == FieldType.Root && !TExprMods.Contains(DTokens.Static))
                            {
                                SemErr(DTokens.This, ctor.startLoc.Column, ctor.startLoc.Line, "Module Constructors must be static!");
                            }

                            ctor.modifiers.AddRange(TExprMods);
                            ctor.name = (IsDestructor ? "~" : "") + "this";
                            ctor.fieldtype = FieldType.Constructor;
                            ctor.EndLocation = la.EndLocation;

                            ctor.Parent = ret;
                            ctor.module = ret.module;

                            LastElement = ctor;
                            ret.Add(ctor);
                        }
                        break;
                    case DTokens.Class:
                    case DTokens.Template:
                    case DTokens.Struct:
                    case DTokens.Union:
                    case DTokens.Interface:

                        if (Peek(1).Kind == DTokens.OpenCurlyBrace) // struct {...}
                        {
                            break;
                        }

                        DNode myc = ParseClass();
                        if (myc != null)
                        {
                            LastElement = myc;
                            myc.module = ret.module;
                            myc.Parent = ret;
                            ret.Add(myc);
                        }
                        continue;
                    case DTokens.Synchronized:
                        if (Peek(1).Kind==DTokens.OpenParenthesis)
                        {
                            lexer.NextToken();
                            SkipToClosingParenthesis();
                        }
                        break;
                    case DTokens.Module:
                        lexer.NextToken();
                        ret.module = SkipToSemicolon();
                        break;
                    case DTokens.Typedef:
                    case DTokens.Alias:
                        // typedef void* function(int a) foo;
                        lexer.NextToken(); // Skip alias|typedef
                        DNode aliasType = ParseExpression();
                        if (aliasType == null) break;
                        aliasType.desc = CheckForDocComments();
                        aliasType.fieldtype = FieldType.AliasDecl;
                        LastElement = aliasType;
                        ret.Add(aliasType);

                        if (la.Kind == DTokens.Comma)
                            goto blockcont;

                        break;
                    case DTokens.Import:
                        ParseImport();
                        continue;
                    case DTokens.Decrement: // --a;
                    case DTokens.Increment: // ++a;
                    case DTokens.Mixin:
                    case DTokens.Assert:
                    case DTokens.Pragma:
                        SkipToSemicolon();
                        break;
                    default:
                        break;
                }
                #endregion
            }

            // Debug.Print("ParseBlock ended (" + ret.name + ")");
        }

        /// <summary>
        /// import abc.def, efg.hij, xyz;
        /// </summary>
        void ParseImport()
        {
            if (la.Kind != DTokens.Import)
                SynErr(DTokens.Import);
            string ts = "";

            List<string> tl = new List<string>();
            while (!EOF && la.Kind != DTokens.Semicolon)
            {
                lexer.NextToken();
                if (ThrowIfEOF(DTokens.Semicolon)) return;
                switch (la.Kind)
                {
                    default:
                        ts += strVal;
                        break;
                    case DTokens.Comma:
                    case DTokens.Semicolon:
                        tl.Add(ts);
                        ts = "";
                        break;
                    case DTokens.Colon:
                    case DTokens.Assign:
                        ts = "";
                        break;
                }
            }

            if (la.Kind == DTokens.Semicolon) import.AddRange(tl);
        }


        List<DNode> ParseTemplateArguments()
        {
            List<DNode> ret = new List<DNode>();
            DVariable targ = null;
            if (la.Kind == DTokens.OpenParenthesis) lexer.NextToken();

            while (!ThrowIfEOF(DTokens.CloseParenthesis))
            {
                switch (la.Kind)
                {
                    case DTokens.CloseParenthesis:
                        if (targ != null)
                        {
                            targ.EndLocation = la.Location;
                            ret.Add(targ);
                        }
                        return ret;

                    case DTokens.This: // this abc:def=ghi
                    case DTokens.Alias: // alias abc:char
                        targ = new DVariable();
                        targ.StartLocation = la.Location;
                        targ.Type = new DTokenDeclaration(la.Kind);

                        if (!PeekMustBe(DTokens.Identifier, "Identifier expected after alias declaration"))
                        {
                            SkipToClosingParenthesis();
                            return ret;
                        }

                        targ.name = strVal;
                        break;

                    case DTokens.Assign:
                        if (targ != null) targ.Value = ParseAssignIdent(DTokens.Comma, DTokens.CloseParenthesis);
                        continue;

                    case DTokens.Colon: // here a conditional expression can follow
                        string condExpr = "";
                        int psb = 0;
                        lexer.NextToken();
                        while (!ThrowIfEOF(DTokens.CloseParenthesis))
                        {
                            if (la.Kind == DTokens.Comma) break;
                            else if (la.Kind == DTokens.OpenParenthesis) psb++;
                            else if (la.Kind == DTokens.CloseParenthesis)
                            {
                                psb--;
                                if (psb < 0)
                                    break;
                            }

                            condExpr += strVal;
                            lexer.NextToken();
                        }
                        targ.Type = new InheritanceDecl(targ.Type);
                        (targ.Type as InheritanceDecl).InheritedClass = new NormalDeclaration(condExpr);
                        continue;

                    case DTokens.Comma:
                        if (targ == null) { SkipToClosingBrace(); break; }
                        targ.EndLocation = la.EndLocation;
                        ret.Add(targ);
                        targ = null;
                        break;

                    default:
                        targ = new DVariable();

                        if (la.Kind == DTokens.Identifier)
                        {
                            DToken pk = Peek(1);

                            if (pk.Kind == DTokens.Dot && Peek().Kind == DTokens.Dot && Peek().Kind == DTokens.Dot) // abc...
                            {
                                targ = new DVariable();
                                targ.name = strVal;
                                targ.Type = new VarArgDecl(new NormalDeclaration(strVal));

                                lexer.NextToken(); // Skip id
                                lexer.NextToken(); // Skip 1st '.'
                                lexer.NextToken(); // Skip 2nd '.'
                                break;
                            }

                            if (pk.Kind == DTokens.Comma || pk.Kind == DTokens.CloseParenthesis || pk.Kind == DTokens.Colon || pk.Kind == DTokens.Assign) // (T,U:char,A="a<b",CC)
                            {
                                targ.name = strVal;
                                break;
                            }
                        }

                        if (la.Kind == DTokens.Identifier || DTokens.BasicTypes[la.Kind] || DTokens.MemberFunctionAttributes[la.Kind] || DTokens.ParamModifiers[la.Kind])
                            targ.Type = ParseTypeIdent(out targ.name);

                        break;
                }
                lexer.NextToken();
            }
            return ret;
        }

        /// <summary>
        /// Parses all variable declarations when "(" is the lookahead DToken and retrieves them into v.param. 
        /// Thereafter ")" will be lookahead
        /// </summary>
        /// <param name="v"></param>
        void ParseFunctionArguments(ref DMethod v)
        {
            int psb = 0;// ()
            DNode targ = null;
            // int[]* MyFunction(in string[]* arg, uint function(aa[]) funcarg, ref MyType b)
            while (la.Kind != DTokens.EOF)
            {
                if (ThrowIfEOF(DTokens.CloseParenthesis))
                    return;

                switch (la.Kind)
                {
                    case DTokens.OpenParenthesis:
                        psb++;
                        break;
                    case DTokens.CloseParenthesis:
                        psb--;
                        if (psb < 1)
                        {
                            if (targ != null)
                            {
                                targ.EndLocation=t.EndLocation;
                                v.Parameters.Add(targ);
                            }
                            return;
                        }
                        break;
                    case DTokens.Comma:
                        if (targ == null) { SkipToClosingBrace(); break; }
                        targ.endLoc = GetCodeLocation(la);
                        v.Parameters.Add(targ);
                        targ = null;
                        break;
                    case DTokens.Dot:
                        if (Peek(1).Kind == DTokens.Dot && Peek(2).Kind == DTokens.Dot) // "..."
                        {
                            if (targ == null) targ = new DVariable();

                            targ.Type = new VarArgDecl();
                            targ.name = "...";

                            targ.startLoc = GetCodeLocation(la);
                            targ.endLoc = GetCodeLocation(la);
                            targ.endLoc.Column += 3; // three dots (...)

                            v.Parameters.Add(targ);
                            targ = null;
                        }
                        break;
                    case DTokens.Alias:
                        if (targ == null) targ = new DVariable();
                        targ.modifiers.Add(la.Kind);
                        break;
                    default:
                        if (DTokens.Modifiers[la.Kind] && Peek(1).Kind != DTokens.OpenParenthesis) // const int a
                        {
                            if (targ == null) targ = new DVariable();
                            targ.modifiers.Add(la.Kind);
                            break;
                        }
                        if (DTokens.BasicTypes[la.Kind] || la.Kind == DTokens.Identifier || la.Kind == DTokens.Typeof || DTokens.MemberFunctionAttributes[la.Kind])
                        {
                            if (targ == null) targ = new DVariable();
                            if (Peek(1).Kind == DTokens.Dot) break;

                            targ.StartLocation = la.Location;
                            targ.TypeToken = la.Kind;
                            bool IsCFunction = false;
                            targ.Type = ParseTypeIdent(out targ.name,out IsCFunction);

                            if (IsCFunction) // int (*>fp)<
                            {
                                DMethod dm = new DMethod();
                                dm.fieldtype = FieldType.Delegate;
                                dm.Assign(targ);
                                targ = dm;
                                lexer.NextToken(); // Skip ')'
                                ParseFunctionArguments(ref dm);
                                break;
                            }

                            if (la.Kind == DTokens.Comma || (la.Kind == DTokens.CloseParenthesis && (targ.name == null || Peek(1).Kind == DTokens.Semicolon || lexer.CurrentPeekToken.Kind == DTokens.CloseCurlyBrace)))// size_t wcslen(in wchar *>);<
                            {
                                continue;
                            }
                            /*
                            if (la.Kind == DTokens.Colon) // void foo(T>:<Object[],S[],U,V) {...}
                            {
                                lexer.NextToken(); // Skip :
                                targ.Type = new InheritanceDecl(targ.Type);
                                (targ.Type as InheritanceDecl).InheritedClass = ParseTypeIdent();
                                DToken pk2 = Peek(1);
                            }

                            if (la.Kind == DTokens.Identifier) targ.name = strVal;*/

                            if (Peek(1).Kind == DTokens.Assign) // Argument has default argument
                            {
                                if (targ is DVariable)
                                    (targ as DVariable).Value = ParseAssignIdent(DTokens.Comma, DTokens.CloseParenthesis);
                                else
                                    ParseAssignIdent(DTokens.Comma, DTokens.CloseParenthesis);

                                if (la.Kind == DTokens.CloseParenthesis && (Peek(1).Kind == DTokens.Comma || Peek(1).Kind == DTokens.CloseParenthesis)) lexer.NextToken(); // void foo(int a=bar(>),<bool b)
                                continue;
                            }

                            if (la.Kind == DTokens.OpenCurlyBrace || la.Kind == DTokens.Semicolon) { v.Parameters.Add(targ); return; }
                        }
                        break;
                }
                lexer.NextToken();
            }
        }

        /// <summary><![CDATA[
        /// enum{
        /// a>=1<,
        /// b=2,
        /// c
        /// }
        /// =1;
        /// =-1234;
        /// =&a;
        /// =*b;
        /// =cast(uint) -1
        /// =cast(char*) "",
        /// =*(cast(int[]*)b);
        /// =delegate void(int i) {...};
        /// =MyType.ConstVal
        /// =1+5;
        /// =(EnumVal1 + EnumVal2);
        /// =EnumVal1 | EnumVal2;
        /// ]]></summary>
        /// <returns></returns>
        string ParseAssignIdent()
        {
            return ParseAssignIdent(DTokens.Semicolon, DTokens.Comma, DTokens.CloseCurlyBrace, DTokens.CloseParenthesis);
        }
        string ParseAssignIdent(params int[] EndTokens)
        {
            string ret = "";
            bool b = true;
            while (b && !ThrowIfEOF(DTokens.Semicolon))
            {
                lexer.NextToken();
            cont:
                foreach (int tk in EndTokens)
                    if (la.Kind == tk)
                    {
                        return ret.Trim();
                    }

                if (la.Kind == DTokens.OpenCurlyBrace) { ret += "{..."; SkipToClosingBrace(); }
                if (la.Kind == DTokens.OpenParenthesis) { ret += SkipToClosingParenthesis(); goto cont; }
                if (la.Kind == DTokens.OpenSquareBracket) { ret += "[" + SkipToClosingSquares(); }

                ret += (la.Kind == DTokens.Identifier || la.Kind == DTokens.Delegate || la.Kind == DTokens.Function ? " " : "") + strVal;
            }
            return ret.Trim();
        }

        TypeDeclaration ParseTypeIdent()
        {
            string _unused = null;
            return ParseTypeIdent(out _unused);
        }

        TypeDeclaration ParseTypeIdent(out string VarName)
        {
            bool _unused = false;
            return ParseTypeIdent(out VarName, out _unused);
        }

        /// <summary>
        /// MyType
        /// uint[]*
        /// const(char)*
        /// invariant(char)[]
        /// int[] function(char[int[]], int function() mf, ref string y)[]
        /// immutable(char)[] 
        /// int ABC;
        /// int* ABC;
        /// int[] ABC;
        /// int[]* ABC;
        /// myclass!(...) ABC;
        /// myclass.staticType ABC;
        /// int[]* delegate(...)[] ABC;
        /// </summary>
        /// <param name="identifierOnly"></param>
        /// <param name="hasClampMod"></param>
        /// <returns></returns>
        TypeDeclaration ParseTypeIdent(out string VariableName,out bool IsCStyleFunctionDeclaration)
        {
            /*
             * This functions ends when la.Kind== ; or , or ) or { or if it has found an identifier (probably a function or var ident.)
             */
            VariableName = null;
            IsCStyleFunctionDeclaration = false;

            Stack<TypeDeclaration> declStack = new Stack<TypeDeclaration>();
            bool IsInit = true;
            bool IsAliasDecl = (t != null && (t.Kind == DTokens.Alias || t.Kind == DTokens.Typedef));
            bool IsBaseTypeAnalysis = ((t != null && t.Kind == DTokens.Colon) || la.Kind == DTokens.Colon); // class ABC>:<Object {}
            if (la.Kind == DTokens.Colon) lexer.NextToken(); // Skip ':'
            bool IsCStyleDeclaration = false; // char abc>[<30];

            DToken pk = null;
            while (!ThrowIfEOF(DTokens.Semicolon))
            {
                pk = Peek(1);

                if (IsInit && la.Kind == DTokens.Dot) { lexer.NextToken(); continue; }

                if (IsInit && (DTokens.Modifiers[la.Kind] || la.Kind==DTokens.Extern || la.Kind==DTokens.Export) && !DTokens.MemberFunctionAttributes[la.Kind]) // ref int
                {
                    declStack.Push(new DTokenDeclaration(la.Kind));
                    if (Peek(1).Kind == DTokens.OpenParenthesis) // extern>(<...)
                    {
                        lexer.NextToken();
                        SkipToClosingParenthesis();
                        continue;
                    }
                    lexer.NextToken(); // Skip ref, inout, out ,in
                    continue;
                }

                if ((DTokens.MemberFunctionAttributes[la.Kind] || la.Kind == DTokens.Typeof)) // const(...)
                {
                    bool IsTypeOf = la.Kind == DTokens.Typeof;
                    if (IsTypeOf && pk.Kind != DTokens.OpenParenthesis) // typeof abcd --> illegal
                    {
                        SynErr(DTokens.OpenParenthesis, "'(' after a typeof expression expected");
                        return null;
                    }
                    else if (pk.Kind == DTokens.OpenParenthesis) // const>(>char)
                    {
                        declStack.Push(new MemberFunctionAttributeDecl(la.Kind));
                        lexer.NextToken(); // Skip const
                        IsInit = false;

                        if (IsTypeOf)
                            (declStack.Peek() as MemberFunctionAttributeDecl).Base = new NormalDeclaration(SkipToClosingParenthesis().Trim('(', ')'));
                        else
                        {
                            lexer.NextToken(); // Skip '('
                            (declStack.Peek() as MemberFunctionAttributeDecl).Base = ParseTypeIdent();
                            if (la.Kind != DTokens.CloseParenthesis) lexer.NextToken(); // Skip last ident. parsed by ParseTypeIdent()
                            lexer.NextToken(); // Skip ')'
                        }
                    }
                    else // const >char<
                    {
                        declStack.Push(new MemberFunctionAttributeDecl(la.Kind));
                        lexer.NextToken(); // Skip const
                    }
                    continue;
                }

                if (IsInit && la.Kind == DTokens.Auto && pk.Kind == DTokens.Ref) // auto ref foo()...
                {
                    lexer.NextToken(); // Skip 'auto'
                    declStack.Push(new NormalDeclaration("auto ref"));
                    IsInit = false;
                    continue;
                }

                if (la.Kind == DTokens.Literal) // int[>3<];
                {
                    if (t.Kind != DTokens.OpenSquareBracket)
                    {
                        SynErr(DTokens.OpenSquareBracket, "Literals only allowed in array declarations");
                        goto do_return;
                    }

                    declStack.Push(new NormalDeclaration(strVal));
                    lexer.NextToken(); // Skip literal
                    continue;
                }

                if (la.Kind == DTokens.Identifier) // int* >ABC<;
                {
                    if (!IsInit && pk.Kind == DTokens.OpenSquareBracket) // int ABC>[<1234];
                    {
                        VariableName = strVal;
                        IsCStyleDeclaration = true;
                    }
                    else if (pk.Kind == DTokens.OpenParenthesis ||  // void foo>(<...) {...}
                            pk.Kind == DTokens.Comma ||             // void foo(bool a>,<int b)
                            pk.Kind == DTokens.CloseParenthesis ||  // void foo(bool a,int b>)< {}
                            pk.Kind == DTokens.Semicolon ||         // int abc>;<
                            pk.Kind == DTokens.Colon ||             // class ABC>:<Object {...}
                            pk.Kind == DTokens.OpenCurlyBrace ||    // class ABC:Object>{<...}
                            pk.Kind == DTokens.Assign               // int[] foo>=<...;
                       )
                    {
                        if (!IsInit)
                        {
                            if (pk.Kind == DTokens.OpenParenthesis) // w.foo>(<...);
                            {
                                if (t.Kind == DTokens.Dot || declStack.Count < 1) // .doit(); // foo();
                                {
                                    return null;
                                }
                            }
                            VariableName = strVal;
                        }
                        else
                            declStack.Push(new NormalDeclaration(strVal));
                        goto do_return;
                    }
                }

                /*
                 *  This will happen only if a identifier is needed. 
                 */
                if (IsInit /* int* */ ||
                    (t != null && t.Kind == DTokens.Not) || /* List!(ABC...) */
                    (t != null && t.Kind == DTokens.OpenParenthesis && la.Kind != DTokens.Dot) /* Template!>(>abc) */ ||
                    (t != null && t.Kind == DTokens.Dot) || // >.<MyIdent
                    (t != null && t.Kind == DTokens.OpenSquareBracket && la.Kind != DTokens.CloseSquareBracket) /* int[><] */)
                {
                    if (!DTokens.BasicTypes[la.Kind] && la.Kind != DTokens.Identifier && la.Kind != DTokens.This && la.Kind != DTokens.Super /* >this<.ABC abc; */)
                    {
                        SynErr(la.Kind, "Expected identifier or base type!");
                        goto do_return;
                    }
                    //lexer.NextToken(); // Skip token that is in front of the identifier

                    if (la.Kind != DTokens.Identifier)
                        declStack.Push(new DTokenDeclaration(la.Kind));
                    else declStack.Push(new NormalDeclaration(strVal));
                }

                if (DTokens.Conditions[la.Kind] || (VariableName == null && DTokens.AssignOps[pk.Kind]))
                    return null;

                switch (la.Kind)
                {

                    case DTokens.Delegate: // myType*[] >delegate<(...) asdf;
                    case DTokens.Function:
                        DelegateDeclaration dd = new DelegateDeclaration();
                        if (declStack.Count < 1)// int a; >delegate<(...) asdf;
                        {
                            SynErr(la.Kind, "Declaration expected, not '" + strVal + "'!");
                            goto do_return;
                        }
                        dd.ReturnType = declStack.Pop();
                        declStack.Push(dd);

                        if (!PeekMustBe(DTokens.OpenParenthesis, "Expected '('!"))
                            goto do_return;

                        lexer.NextToken(); // Skip '('
                        #region Parse delegate parameters
                        if (la.Kind == DTokens.CloseParenthesis) break;//  void delegate(>)< asdf;
                        while (!ThrowIfEOF(DTokens.CloseParenthesis))
                        {
                            DVariable dv = new DVariable();
                            dv.Type = ParseTypeIdent(out dv.name);

                            if (Peek(1).Kind == DTokens.Comma || lexer.CurrentPeekToken.Kind == DTokens.CloseParenthesis || lexer.CurrentPeekToken.Kind == DTokens.Assign) lexer.NextToken(); // Skip last token parsed, can theoretically only be an identifier

                            // Do not expect a parameter id here!

                            if (la.Kind == DTokens.Assign) // void delegate(int i>=<5, bool a=false)
                                dv.Value = ParseAssignIdent(DTokens.Comma, DTokens.CloseParenthesis);

                            dd.Parameters.Add(dv);


                            if (la.Kind == DTokens.Comma)
                            {
                                lexer.NextToken();
                                continue;
                            }
                            break;
                        }
                        #endregion
                        break;

                    case DTokens.OpenParenthesis:
                        if (pk.Kind != DTokens.Times)// void >(<*foo)();
                        {
                            if (t.Kind == DTokens.CloseSquareBracket) // foo[0>]<(asdf);
                                return null;
                            else
                                goto do_return;
                        }
                        IsCStyleFunctionDeclaration = true;
                        lexer.NextToken(); // Skip '('
                        lexer.NextToken(); // Skip '*'
                        //TODO: possible but rare array declaration | void (*>[<]foo)();
                        if (la.Kind == DTokens.Identifier)
                        {
                            VariableName = strVal; // void (*>foo<)();
                            lexer.NextToken(); // Skip id
                        }
                        else if (la.Kind == DTokens.CloseParenthesis) // void (*>)<(...)
                        {
                            // Do nothing here
                        }
                        else SynErr(la.Kind,"Identifier expected");
                        goto do_return;

                    case DTokens.Assign:
                    case DTokens.Colon:
                    case DTokens.Semicolon: // int;
                        if (IsAliasDecl && VariableName == null)
                            VariableName = DTokens.GetTokenString(t.Kind);
                        else if (!IsCStyleDeclaration)
                            return null;
                        goto do_return;

                    case DTokens.CloseParenthesis: // void foo(T,U>)<()
                    case DTokens.Comma:// void foo(T>,< U)()
                        goto do_return;
                    case DTokens.OpenCurlyBrace: // enum abc >{< ... }
                        goto do_return;
                    case DTokens.CloseCurlyBrace: // int asdf; aaa>}<
                        if (t.Kind == DTokens.Identifier)
                            SynErr(la.Kind, "Found '}' when expecting ';'");
                        else // int aaa}
                            SynErr(la.Kind, "Found '}' when expecting identifier");
                        goto do_return;

                    case DTokens.CloseSquareBracket:
                        TypeDeclaration keyType = new DTokenDeclaration(DTokens.Int); // default key type is int
                        if (t.Kind != DTokens.OpenSquareBracket) // int>[<] abc;
                        {
                            keyType = declStack.Pop();
                            if (declStack.Count < 1 || !(declStack.Peek() is ArrayDecl))
                            {
                                SynErr(DTokens.CloseParenthesis, "Type declaration parsing error! Perhaps there are too much closing parentheses (']')");
                                return null;
                            }
                        }
                        ArrayDecl arrDecl = declStack.Pop() as ArrayDecl;
                        if (arrDecl == null)
                        {
                            SynErr(DTokens.CloseSquareBracket, "Error, check array syntax");
                            break;
                        }
                        arrDecl.KeyType = keyType;
                        declStack.Push(arrDecl);
                        if (IsCStyleDeclaration)
                            goto do_return;
                        break;

                    case DTokens.Times: // int>*<
                        declStack.Push(new PointerDecl(declStack.Pop()));
                        break;

                    case DTokens.Not: // Template>!<(...)
                        lexer.NextToken(); // Skip !

                        TemplateDecl templDecl_ = new TemplateDecl(declStack.Pop());
                        declStack.Push(templDecl_);

                        if (la.Kind == DTokens.Identifier || DTokens.BasicTypes[la.Kind])
                        {
                            if (la.Kind == DTokens.Identifier)
                                declStack.Push(new NormalDeclaration(strVal));
                            else
                                declStack.Push(new DTokenDeclaration(la.Kind));

                            if (pk.Kind == DTokens.OpenParenthesis) lexer.NextToken();
                        }
                        else if (la.Kind == DTokens.OpenParenthesis) { }
                        else
                        {
                            SynErr(DTokens.OpenParenthesis, "Expected identifier or base type when parsing a template initializer");
                            goto do_return;
                        }

                        if (la.Kind == DTokens.OpenParenthesis)
                        {
                            templDecl_.Template = new NormalDeclaration(SkipToClosingParenthesis(false));
                            if (la.Kind != DTokens.CloseParenthesis) continue;
                        }
                        break;

                    case DTokens.OpenSquareBracket: // int>[<...]
                        declStack.Push(new ArrayDecl(declStack.Pop()));
                        if (pk.Kind != DTokens.CloseSquareBracket) // Here we cheat a bit - otherwise we should mind every kind of other expressions that may occur here...I'm still too lazy to realize this ;-)
                        {
                            declStack.Push(new NormalDeclaration(SkipToClosingSquares()));
                            continue;
                        }
                        break;

                    case DTokens.Dot: // >.<init
                        if (Peek(1).Kind == DTokens.Dot && Peek().Kind == DTokens.Dot) // >...<
                        {
                            if (VariableName == null && t.Kind == DTokens.Identifier)
                                VariableName = t.Value;

                            lexer.NextToken(); // 1st dot
                            lexer.NextToken(); // 2nd dot

                            if (declStack.Count < 1) // void foo(>...<) {}
                                declStack.Push(new VarArgDecl());
                            else
                                declStack.Push(new VarArgDecl(declStack.Pop()));

                            goto do_return;
                        }
                        else
                        {
                            if (Peek(1).Kind != DTokens.Identifier)
                            {
                                SynErr(DTokens.Dot, "Expected identifier after a dot");
                                goto do_return;
                            }

                            declStack.Push(new DotCombinedDeclaration(declStack.Pop()));
                        }
                        break;
                }

                IsInit = false;
                lexer.NextToken();
            }

        do_return:

            while (declStack.Count > 1)
            {
                TypeDeclaration innerType = declStack.Pop();
                if (declStack.Peek() is TemplateDecl)
                    (declStack.Peek() as TemplateDecl).Template = innerType;
                else if (declStack.Peek() is ArrayDecl)
                    (declStack.Peek() as ArrayDecl).KeyType = innerType;
                else if (declStack.Peek() is DotCombinedDeclaration)
                    (declStack.Peek() as DotCombinedDeclaration).AccessedMember = innerType;
                else
                    declStack.Peek().Base = innerType;
            }

            if (declStack.Count > 0)
                return declStack.Pop();

            return null;
        }

        /// <summary>
        /// void main(string[] args) {}
        /// void expFunc();
        /// void delegate(int a,bool b) myDelegate;
        /// int i=45;
        /// MyType[] a;
        /// const(char)[] foo();
        /// this() {}
        /// </summary>
        /// <returns></returns>
        DNode ParseExpression()
        {
            DNode tv = new DNode();
            tv.desc = CheckForDocComments();
            tv.StartLocation = la.Location;
            bool isCTor = (la.Kind == DTokens.This && Peek(1).Kind == DTokens.OpenParenthesis);
            tv.TypeToken = la.Kind;
            if (!isCTor)
            {
                tv.Type = ParseTypeIdent(out tv.name);
                if (tv.Type == null || tv.name == null)
                {
                    SkipToSemicolon();
                    return null;
                }
            }
            else
                tv.Type = new DTokenDeclaration(DTokens.This);

            //if (!isCTor) lexer.NextToken(); // Skip last ID parsed by ParseIdentifier();
            if (tv.name != null && la.Kind == DTokens.CloseSquareBracket) lexer.NextToken(); // char asdf[30>]<;

            if (IsVarDecl() ||// int foo; TypeTuple!(a,T)[] a;
                (tv.name != null && (la.Kind == DTokens.Semicolon || la.Kind == DTokens.Assign || la.Kind == DTokens.Comma))) // char abc[]>;< dchar def[]>=<...;
            {
                DVariable var = new DVariable();
                var.Assign(tv);
                tv = var;

                if (Peek(1).Kind == DTokens.Assign)
                    lexer.NextToken();

                if (la.Kind == DTokens.Assign)
                    var.Value = ParseAssignIdent();

                if (Peek(1).Kind == DTokens.Semicolon || Peek(1).Kind == DTokens.Comma)
                    lexer.NextToken();


                if (la.Kind != DTokens.Comma)
                {
                    if (la.Kind != DTokens.Semicolon)
                    {
                        SynErr(DTokens.Semicolon, "Missing semicolon!");
                        goto expr_ret;
                    }
                }

            }
            else if (la.Kind == DTokens.Identifier || isCTor || (la.Kind == DTokens.CloseParenthesis && Peek(1).Kind == DTokens.OpenParenthesis)) // MyType myfunc() {...}; this()() {...}; int (*foo)>(<int a, bool b);
            {
                if (!isCTor && (String.IsNullOrEmpty(tv.name) || tv.Type == null))
                {
                    SkipToSemicolon();
                    return null;
                }

                DMethod meth = new DMethod();
                meth.Assign(tv);
                tv = meth;
                lexer.NextToken(); // Skip function name

                if (!Expect(DTokens.OpenParenthesis, "Expected '('"))
                {
                    SkipToClosingBrace(); return null;
                }

                bool HasTemplateArgs = false;
                #region Scan for template arguments
                int psb = 0;
                DToken pk = la;
                lexer.StartPeek();
                if (pk.Kind == DTokens.OpenParenthesis) psb = -1;
                for (int i = 0; pk != null && pk.Kind != DTokens.EOF; i++)
                {
                    if (pk.Kind == DTokens.OpenParenthesis) psb++;
                    if (pk.Kind == DTokens.CloseParenthesis)
                    {
                        psb--;
                        if (psb < 0)
                        {
                            if (lexer.Peek().Kind == DTokens.OpenParenthesis) HasTemplateArgs = true;
                            break;
                        }
                    }
                    pk = lexer.Peek();
                }
                #endregion

                if (la.Kind != DTokens.CloseParenthesis) // just if some arguments are given!
                {
                    if (HasTemplateArgs)
                        tv.TemplateParameters = ParseTemplateArguments();
                    else
                        ParseFunctionArguments(ref meth);
                }

                if (HasTemplateArgs) // void templFunc(S,T[],U*) (S s, int b=2) {...}
                {
                    if (!Expect(DTokens.CloseParenthesis, "Expected ')'")) { SkipToClosingBrace(); return null; }
                    if (Peek(1).Kind != DTokens.CloseParenthesis) // If there aren't any args, don't try to parse em' :-D
                        ParseFunctionArguments(ref meth);
                    else
                        lexer.NextToken();// Skip "("
                }

                if (DTokens.Conditions[Peek(1).Kind]) // foo() >||< abc;
                {
                    SkipToSemicolon();
                    return null;
                }

                if (la.Kind == DTokens.CloseParenthesis) lexer.NextToken();

                if (la.Kind == DTokens.Assign) // this() = null;
                {
                    /*tv.value = */
                    SkipToSemicolon();
                    tv.endLoc = GetCodeLocation(la);
                    goto expr_ret;
                }

                #region In|Out|Body regions of a method
                if (DTokens.Modifiers[la.Kind] && la.Kind != DTokens.In && la.Kind != DTokens.Out && la.Kind != DTokens.Body) // void foo() const if(...) {...}
                {
                    lexer.NextToken();
                }

                if (la.Kind == DTokens.If) // void foo() if(...) {}
                {
                    lexer.NextToken(); // Skip "if"
                    SkipToClosingParenthesis();
                }

                if (la.Kind == DTokens.Semicolon) { goto expr_ret; } // void foo()();

                if (DTokens.Modifiers[la.Kind] && la.Kind != DTokens.In && la.Kind != DTokens.Out && la.Kind != DTokens.Body) // void foo() const {...}
                {
                    lexer.NextToken();
                }

                if (la.Kind == DTokens.OpenCurlyBrace)// normal function void foo() >{<}
                {
                    Location sloc = tv.StartLocation;
                    ParseBlock(ref tv, true);
                    tv.StartLocation = sloc;
                    goto expr_ret;
                }

            in_out_body:
                if (la.Kind == DTokens.In || la.Kind == DTokens.Out || la.Kind == DTokens.Body) // void foo() in{}body{}
                {
                    lexer.NextToken();
                    if (t.Kind == DTokens.Out && la.Kind == DTokens.OpenParenthesis)
                    {
                        SkipToClosingParenthesis();
                    }

                    Location sloc = tv.StartLocation;
                    ParseBlock(ref tv, true);
                    tv.StartLocation = sloc;

                    DToken bpk = Peek(1);
                    if (bpk.Kind == DTokens.In || bpk.Kind == DTokens.Out || bpk.Kind == DTokens.Body)
                    {
                        lexer.NextToken(); // Skip "}"
                        goto in_out_body;
                    }
                    goto expr_ret;
                }
                #endregion

                SynErr(la.Kind, "unexpected end of function body!");
                return null;
            }
            else
                return null;

        expr_ret:


            return tv;
        }

        /// <summary>
        /// Parses a complete class, template or struct
        /// public class MyType(T,S*,U[]): public Mybase, MyInterface {...}
        /// </summary>
        /// <returns></returns>
        DNode ParseClass()
        {
            DClassLike myc = new DClassLike(); // >class<
            myc.StartLocation = la.Location;
            DNode _myc = myc;
            myc.desc = CheckForDocComments();
            if (la.Kind == DTokens.Struct || la.kind == DTokens.Union) myc.fieldtype = FieldType.Struct;
            if (la.Kind == DTokens.Template) myc.fieldtype = FieldType.Template;
            if (la.Kind == DTokens.Interface) myc.fieldtype = FieldType.Interface;
            myc.TypeToken = la.Kind;
            lexer.NextToken(); // Skip initial type ID ,e.g. "class"

            #region Apply vis modifiers
            bool cvm = DTokens.ContainsVisMod(ExpressionModifiers);
            foreach (int m in BlockModifiers)
            {
                if (!ExpressionModifiers.Contains(m))
                {
                    if (cvm) if (DTokens.VisModifiers[m]) continue;
                    ExpressionModifiers.Add(m);
                }
            }
            myc.modifiers.AddRange(ExpressionModifiers);
            ExpressionModifiers.Clear();
            #endregion

            if (la.Kind != DTokens.Identifier)
            {
                SynErr(DTokens.Identifier, "Identifier required!");
                return null;
            }

            myc.name = strVal; // >MyType<
            LastElement = myc;
            lexer.NextToken(); // Skip id

            if (la.Kind == DTokens.Semicolon) return myc;

            // >(T,S,U[])<
            if (la.Kind == DTokens.OpenParenthesis) // "(" template declaration
            {
                _myc.TemplateParameters= ParseTemplateArguments();
                if (!Expect(DTokens.CloseParenthesis, "Failure during template paramter parsing - \")\" missing!")) { SkipToClosingBrace(); }
            }

            // >: MyBase, MyInterface< {...}
            if (la.Kind == DTokens.Colon) // : inheritance
            {
                while (!EOF) // Skip modifiers or module paths
                {
                    lexer.NextToken();
                    if (DTokens.Modifiers[la.Kind])
                        continue;// Skip heritage modifier
                    if (Peek(1).Kind == DTokens.Dot)// : std.Class
                        continue;
                    else if (la.Kind != DTokens.Dot)
                        break;
                }
                string _unused = null;
                myc.BaseClasses.Add(ParseTypeIdent(out _unused));
                lexer.NextToken();
                while(la.Kind == DTokens.Comma && !ThrowIfEOF(DTokens.CloseParenthesis))
                {
                    lexer.NextToken(); // Skip ","
                    myc.BaseClasses.Add(ParseTypeIdent(out _unused));
                    lexer.NextToken(); // Skip last id
                }
                if (la.Kind != DTokens.OpenCurlyBrace) lexer.NextToken(); // Skip to "{"
            }

            if (myc.name != "Object" && myc.fieldtype != FieldType.Struct && myc.BaseClasses.Count<1) 
                myc.BaseClasses.Add(new NormalDeclaration("Object")); // Every object except the Object class itself has "Object" as its base class!

            if (myc.BaseClasses.Count>0 && myc.BaseClasses[0].ToString() == myc.name)
            {
                SemErr(DTokens.Colon, "Cannot inherit \"" + myc.name + "\" from itself!");
                myc.BaseClasses.RemoveAt(0);
            }

            if (la.Kind == DTokens.If)
            {
                lexer.NextToken();
                SkipToClosingParenthesis();
            }

            if (la.Kind != DTokens.OpenCurlyBrace)
            {
                SynErr(DTokens.OpenCurlyBrace, "Error parsing " + DTokens.GetTokenString(myc.TypeToken) + " " + myc.name + ": missing {");
                return myc;
            }

            ParseBlock(ref _myc, false);

            myc.endLoc = GetCodeLocation(la);

            return myc;
        }

        /// <summary>
        /// Parses an enumeration
        /// enum myType mt=null;
        /// enum ABC:uint {
        /// a=1,
        /// b=23,
        /// c=2,
        /// d,
        /// }
        /// </summary>
        /// <returns></returns>
        DNode ParseEnum()
        {
            DNode mye = new DEnum();
            mye.StartLocation = la.Location;

            mye.Type = new DTokenDeclaration(la.Kind);
            (mye as DEnum).EnumBaseType = new DTokenDeclaration(DTokens.Int);

            #region Apply vis modifiers
            bool cvm = DTokens.ContainsVisMod(ExpressionModifiers);
            foreach (int m in BlockModifiers)
            {
                if (!ExpressionModifiers.Contains(m))
                {
                    if (cvm) if (DTokens.VisModifiers[m]) continue;
                    ExpressionModifiers.Add(m);
                }
            }
            mye.modifiers.AddRange(ExpressionModifiers);
            ExpressionModifiers.Clear();
            #endregion


            if (la.Kind == DTokens.Enum) lexer.NextToken(); // Skip 'enum'

            if (la.Kind != DTokens.OpenCurlyBrace) // Otherwise it would be enum >{<...}
            {
                DToken pk = Peek(1);
                if (la.Kind == DTokens.Identifier && (pk.Kind == DTokens.OpenCurlyBrace || pk.Kind == DTokens.Colon || pk.Kind == DTokens.Assign)) // enum ABC>{>...}
                {
                    mye.name = strVal;
                    lexer.NextToken(); // Skip enum id
                }

                if (la.Kind == DTokens.Colon) // enum>:<uint | enum ABC>:<uint
                {
                    string _unused = null;
                    (mye as DEnum).EnumBaseType = ParseTypeIdent(out _unused);
                    if (Peek(1).Kind == DTokens.OpenCurlyBrace) lexer.NextToken();
                }
                else if (la.Kind == DTokens.Assign) // enum abc>=< [aaa,bbb,ccc];
                {
                    DVariable enumVar = new DVariable();
                    enumVar.Assign(mye);
                    mye = enumVar;
                    mye.Type = new DTokenDeclaration(DTokens.Int);
                    enumVar.Value = ParseAssignIdent(DTokens.Semicolon);
                    enumVar.EndLocation = la.EndLocation;
                    return enumVar;
                }
                else if (la.Kind != DTokens.OpenCurlyBrace) // enum Type[]** ABC;
                    return ParseExpression();
            }

            if (la.Kind != DTokens.OpenCurlyBrace) // Check beginning "{"
            {
                SynErr(DTokens.OpenCurlyBrace, "Expected '{' when parsing enumeration");
                SkipToClosingBrace();
                mye.EndLocation = la.Location;
                return mye;
            }

            DEnumValue tt = new DEnumValue();
            while (!EOF)
            {
                lexer.NextToken();
            enumcont:
                if (tt == null) tt = new DEnumValue();
                if (ThrowIfEOF(DTokens.CloseCurlyBrace)) break;
                switch (la.Kind)
                {
                    case DTokens.CloseCurlyBrace: // Final "}"
                        if (tt.name != "") mye.Add(tt);
                        mye.EndLocation = la.Location;
                        return mye;
                    case DTokens.Comma: // Next item
                        tt.EndLocation = la.Location;
                        if (tt.name != "") mye.Add(tt);
                        tt = null;
                        break;
                    case DTokens.Assign: // Value assignment
                        tt.Value = ParseAssignIdent(DTokens.Comma, DTokens.CloseCurlyBrace);
                        if (la.Kind != DTokens.Identifier) goto enumcont;
                        break;
                    case DTokens.Identifier: // Can just be a new item
                        tt.Type = (mye as DEnum).EnumBaseType;
                        tt.StartLocation = la.Location;
                        tt.name = strVal;
                        if (la.Kind != DTokens.Identifier) goto enumcont;
                        break;
                    default: break;
                }
            }
            mye.EndLocation = la.Location;
            return mye;
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
            OnError(PhysFileName, Document != null ? Document.module : null, la != null ? la.Location.Line : 0, la != null ? la.Location.Column : 0, n, "");
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
