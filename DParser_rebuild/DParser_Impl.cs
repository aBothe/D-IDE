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
        #region Modules
        // http://www.digitalmars.com/d/2.0/module.html

        /// <summary>
        /// Module entry point
        /// </summary>
        DModule Root()
        {
            Step();

            DModule module = new DModule();
            DBlockStatement _block = module as DBlockStatement;

            // Only one module declaration possible possible!
            if (LA(Module))
                module.ModuleName = ModuleDeclaration();

            // Now only declarations or other statements are allowed!
            while (!IsEOF)
            {
                //AttributeSpecifier
                if (IsAttributeSpecifier())
                    AttributeSpecifier();

                //ImportDeclaration
                else if (LA(Import))
                    ImportDeclaration(ref module);

                //Constructor
                else if (LA(This))
                    module.Add(Constructor());

                //Destructor
                else if (LA(Tilde) && LA(This))
                    module.Add(Destructor());

                //Invariant
                else if (LA(Invariant))
                    module.Add(_Invariant());

                //UnitTest
                //ConditionalDeclaration
                //StaticAssert
                //TemplateMixin

                //MixinDeclaration
                else if (LA(Mixin))
                    MixinDeclaration();

                //;
                else if (LA(Semicolon))
                    Step();

                // else:
                else Declaration(ref _block);
            }
            return module;
        }

        string ModuleDeclaration()
        {
            Expect(Module);
            string ret = ModuleFullyQualifiedName();
            Expect(Semicolon);
            return ret;
        }

        string ModuleFullyQualifiedName()
        {
            Expect(Identifier);
            string ret = t.Value;

            while (la.Kind == Dot)
            {
                Step();
                Expect(Identifier);

                ret += "." + t.Value;
            }
            return ret;
        }

        void ImportDeclaration(ref DModule par)
        {
            Expect(Import);

            _Import();

            // ImportBindings
            if (LA(Colon))
            {
                ImportBind();
                while (LA(Comma))
                {
                    Step();
                    ImportBind();
                }
            }
            else
                while (LA(Comma))
                {
                    Step();
                    par.Imports.Add(_Import());
                }

            Expect(Semicolon);
        }

        string _Import()
        {
            // ModuleAliasIdentifier
            if (PK(Assign))
            {
                Expect(Identifier);
                string ModuleAliasIdentifier = t.Value;
                Step();
            }

            return ModuleFullyQualifiedName();
        }

        void ImportBind()
        {
            Expect(Identifier);
            string imbBind = t.Value;
            string imbBindDef = null;

            if (LA(Assign))
            {
                Step();
                imbBindDef = t.Value;
            }
        }


        void MixinDeclaration()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Declarations
        // http://www.digitalmars.com/d/2.0/declaration.html

        bool IsDeclaration()
        {
            return LA(Alias) || IsStorageClass || IsBasicType();
        }

        void Declaration(ref DBlockStatement par)
        {
            if (LA(Alias))
            {
                Step();
                DBlockStatement _t = new DBlockStatement();
                Decl(ref _t);
                foreach (DNode n in _t)
                    n.fieldtype = FieldType.AliasDecl;

                par.Children.AddRange(_t);
            }
            else if (LA(Struct) || LA(Union))
                par.Add(AggregateDeclaration());
            else if (LA(Enum))
                par.Add(EnumDeclaration());
            else if (LA(Class))
                par.Add(ClassDeclaration());
            else if (LA(Template))
                par.Add(TemplateDeclaration());
            else if (LA(Interface))
                par.Add(InterfaceDeclaration());
            else
                Decl(ref par);
        }

        void Decl(ref DBlockStatement par)
        {
            // Enum possible storage class attributes
            List<int> storAttr = new List<int>();
            while (IsStorageClass)
            {
                Step();
                storAttr.Add(t.Kind);
            }

            // Autodeclaration
            if (storAttr.Count > 0 && PK(Identifier) && Peek().Kind == DTokens.Assign)
            {
                Step();
                DNode n = new DVariable();
                n.Type = new DTokenDeclaration(t.Kind);
                n.TypeToken = t.Kind;
                Expect(Identifier);
                n.Name = t.Value;
                Expect(Assign);
                (n as DVariable).Initializer = AssignExpression();
                Expect(Semicolon);
                par.Add(n);
                return;
            }

            // Declarators
            TypeDeclaration ttd = BasicType();
            DNode firstNode = Declarator(false);

            if (firstNode.Type == null)
                firstNode.Type = ttd;
            else
                firstNode.Type.MostBasic.Base = ttd;


            // BasicType Declarators ;
            bool ExpectFunctionBody = !(LA(Assign) || LA(Comma) || LA(Semicolon));
            if (!ExpectFunctionBody)
            {
                // DeclaratorInitializer
                if (LA(Assign))
                {
                    Step();
                    (firstNode as DVariable).Initializer = Initializer();
                }

                par.Add(firstNode);

                // DeclaratorIdentifierList
                while (LA(Comma))
                {
                    Step();
                    Expect(Identifier);

                    DVariable otherNode = new DVariable();
                    otherNode.Assign(firstNode);
                    otherNode.Name = t.Value;

                    if (LA(Assign))
                        otherNode.Initializer = Initializer();

                    par.Add(otherNode);
                }

                Expect(Semicolon);
            }

            // BasicType Declarator FunctionBody
            else if (firstNode is DBlockStatement)
            {
                DBlockStatement _block = firstNode as DBlockStatement;
                FunctionBody(ref _block);
                par.Add(firstNode);
            }
            else
            {
                SynErr(OpenCurlyBrace, "Function declaration expected in front of block statement");
            }
        }

        bool IsBasicType()
        {
            return BasicTypes[la.Kind] || LA(Typeof) || MemberFunctionAttribute[la.Kind] || (LA(Dot) && PK(Identifier)) || LA(Identifier);
        }

        TypeDeclaration BasicType()
        {
            TypeDeclaration td = null;
            if (BasicTypes[la.Kind])
            {
                Step();
                return new DTokenDeclaration(t.Kind);
            }

            if (MemberFunctionAttribute[la.Kind])
            {
                Step();
                MemberFunctionAttributeDecl md = new MemberFunctionAttributeDecl(t.Kind);
                Expect(OpenParenthesis);
                md.InnerType = Type();
                Expect(CloseParenthesis);
                return md;
            }

            if (LA(Typeof))
            {
                td = TypeOf();
                if (!LA(Dot)) return td;
            }

            if (LA(Dot))
                Step();

            if (td == null)
                td = IdentifierList();
            else
                td.Base = IdentifierList();

            return td;
        }

        bool IsBasicType2()
        {
            return LA(Times) || LA(OpenSquareBracket) || LA(Delegate) || LA(Function);
        }

        TypeDeclaration BasicType2()
        {
            // *
            if (LA(Times))
            {
                Step();
                return new PointerDecl();
            }

            // [ ... ]
            else if (LA(OpenSquareBracket))
            {
                Step();
                // [ ]
                if (LA(CloseSquareBracket)) { Step(); return new ClampDecl(); }

                TypeDeclaration ret = null;

                // [ Type ]
                if (IsBasicType())
                    ret = Type();
                else
                {
                    ret = new DExpressionDecl(AssignExpression());

                    // [ AssignExpression .. AssignExpression ]
                    if (LA(Dot))
                    {
                        Step();
                        Expect(Dot);

                        //TODO: do something with the 2nd expression here
                        AssignExpression();
                    }
                }

                Expect(CloseSquareBracket);
                return ret;
            }

            // delegate | function
            else if (LA(Delegate) || LA(Function))
            {
                Step();
                TypeDeclaration td = null;
                DelegateDeclaration dd = new DelegateDeclaration();
                dd.IsFunction = t.Kind == Function;

                dd.Parameters = Parameters();
                td = dd;
                //TODO: add attributes to declaration
                while (FunctionAttribute[la.Kind])
                {
                    Step();
                    td = new DTokenDeclaration(t.Kind, td);
                }
                return td;
            }
            else
                SynErr(Identifier);
            return null;
        }

        /// <summary>
        /// Parses a type declarator
        /// </summary>
        /// <returns>A dummy node that contains the return type, the variable name and possible parameters of a function declaration</returns>
        DNode Declarator(bool IsParam)
        {
            DNode ret = new DVariable();
            TypeDeclaration ttd = null;

            while (IsBasicType2())
            {
                if (ret.Type == null) ret.Type = BasicType2();
                else { ttd = BasicType2(); ttd.Base = ret.Type; ret.Type = ttd; }
            }
            /*
             * Add some syntax possibilities here
             * like in 
             * int (x);
             * or in
             * int(*foo);
             */
            if (LA(OpenParenthesis))
            {
                Step();
                ClampDecl cd = new ClampDecl(ret.Type, ClampDecl.ClampType.Round);
                ret.Type = cd;

                /* 
                 * Parse all basictype2's that are following the initial '('
                 */
                while (IsBasicType2())
                {
                    ttd = BasicType2();

                    if (cd.KeyType == null) cd.KeyType = ttd;
                    else
                    {
                        ttd.Base = cd.KeyType;
                        cd.KeyType = ttd;
                    }
                }

                /*
                 * Here can be an identifier with some optional DeclaratorSuffixes
                 */
                if (!LA(CloseParenthesis))
                {
                    if (IsParam && !LA(Identifier))
                    {
                        /* If this Declarator is a parameter of a function, don't expect anything here
                         * exept a '*' that means that here's an anonymous function pointer
                         */
                        if (!T(Times))
                            SynErr(Times);
                    }
                    else
                    {
                        Expect(Identifier);
                        ret.Name = t.Value;

                        /*
                         * Just here suffixes can follow!
                         */
                        if (!LA(CloseParenthesis))
                        {
                            List<DNode> _unused = null;
                            ttd = DeclaratorSuffixes(out _unused, out _unused);

                            if (cd.KeyType == null) cd.KeyType = ttd;
                            else
                            {
                                ttd.Base = cd.KeyType;
                                cd.KeyType = ttd;
                            }
                        }
                    }
                }
                ret.Type = cd;
                Expect(CloseParenthesis);
            }
            else
            {
                if (IsParam && !LA(Identifier))
                    return ret;

                Expect(Identifier);
                ret.Name = t.Value;
            }

            if (IsDeclaratorSuffix)
            {
                // DeclaratorSuffixes
                List<DNode> _Parameters;
                ttd = DeclaratorSuffixes(out ret.TemplateParameters, out _Parameters);
                if (ttd != null)
                {
                    ttd.Base = ret.Type;
                    ret.Type = ttd;
                }

                if (_Parameters != null)
                {
                    DMethod dm = new DMethod();
                    dm.Assign(ret);
                    dm.Parameters = _Parameters;
                    ret = dm;
                }
            }

            return ret;
        }

        bool IsDeclaratorSuffix
        {
            get { return LA(OpenSquareBracket) || LA(OpenParenthesis); }
        }

        /// <summary>
        /// Note:
        /// http://www.digitalmars.com/d/2.0/declaration.html#DeclaratorSuffix
        /// The definition of a sequence of declarator suffixes is buggy here! Theoretically template parameters can be declared without a surrounding ( and )!
        /// Also, more than one parameter sequences are possible!
        /// 
        /// TemplateParameterList[opt] Parameters MemberFunctionAttributes[opt]
        /// </summary>
        TypeDeclaration DeclaratorSuffixes(out List<DNode> TemplateParameters, out List<DNode> _Parameters)
        {
            TypeDeclaration td = null;
            TemplateParameters = new List<DNode>();
            _Parameters = null;

            while (LA(OpenSquareBracket))
            {
                Step();
                ClampDecl ad = new ClampDecl(td);
                if (!LA(CloseSquareBracket))
                {
                    if (IsAssignExpression())
                        ad.KeyType = new DExpressionDecl(AssignExpression());
                    else
                        ad.KeyType = Type();
                }
                Expect(CloseSquareBracket);
                ad.ValueType = td;
                td = ad;
            }

            if (LA(OpenParenthesis))
            {
                if (IsTemplateParameterList())
                {
                    Step();
                    TemplateParameters = TemplateParameterList();
                    Expect(CloseParenthesis);
                }
                _Parameters = Parameters();

                //TODO: MemberFunctionAttributes -- add them to the declaration
                while (MemberFunctionAttribute[la.Kind])
                {
                    Step();
                }
            }
            return td;
        }

        TypeDeclaration IdentifierList()
        {
            TypeDeclaration td = null;

            if (!LA(Identifier))
                SynErr(Identifier);

            // Template instancing
            if (PK(Not))
                td = TemplateInstance();

            // Identifier
            else
            {
                Step();
                td = new NormalDeclaration(t.Value);
            }

            // IdentifierList
            while (LA(Dot))
            {
                Step();
                DotCombinedDeclaration dcd = new DotCombinedDeclaration(td);
                // Template instancing
                if (PK(Not))
                    dcd.AccessedMember = TemplateInstance();
                // Identifier
                else
                    dcd.AccessedMember = new NormalDeclaration(t.Value);
                td = dcd;
            }

            return td;
        }

        bool IsStorageClass
        {
            get
            {
                return LA(Abstract) ||
            LA(Auto) ||
            LA(Const) ||
            LA(Deprecated) ||
            LA(Extern) ||
            LA(Final) ||
            LA(Immutable) ||
            LA(InOut) ||
            LA(Shared) ||
        LA(Nothrow) ||
            LA(Override) ||
        LA(Pure) ||
            LA(Scope) ||
            LA(Static) ||
            LA(Synchronized);
            }
        }

        TypeDeclaration Type()
        {
            TypeDeclaration td = BasicType();

            if (IsDeclarator2())
            {
                TypeDeclaration ttd = Declarator2();
                ttd.Base = td;
                td = ttd;
            }

            return td;
        }

        bool IsDeclarator2()
        {
            return IsBasicType2() || LA(OpenParenthesis);
        }

        /// <summary>
        /// http://www.digitalmars.com/d/2.0/declaration.html#Declarator2
        /// The next bug: Following the definition strictly, this function would end up in an endless loop of requesting another Declarator2
        /// 
        /// So here I think that a Declarator2 only consists of a couple of BasicType2's and some DeclaratorSuffixes
        /// </summary>
        /// <returns></returns>
        TypeDeclaration Declarator2()
        {
            TypeDeclaration td = null;
            if (LA(OpenParenthesis))
            {
                Step();
                td = Declarator2();
                Expect(CloseParenthesis);

                // DeclaratorSuffixes
                if (LA(OpenSquareBracket))
                {
                    List<DNode> _unused = null, _unused2 = null;
                    DeclaratorSuffixes(out _unused, out _unused2);
                }
                return td;
            }

            while (IsBasicType2())
            {
                TypeDeclaration ttd = BasicType2();
                ttd.Base = td;
                td = ttd;
            }

            return null;
        }

        /// <summary>
        /// Parse parameters
        /// </summary>
        List<DNode> Parameters()
        {
            List<DNode> ret = new List<DNode>();
            Expect(OpenParenthesis);

            // Empty parameter list
            if (LA(CloseParenthesis))
            {
                Step();
                return ret;
            }

            if (!IsTripleDot())
                ret.Add(Parameter());

            while (LA(Comma))
            {
                Step();
                if (IsTripleDot())
                    break;
                ret.Add(Parameter());
            }

            /*
             * There can be only one '...' in every parameter list
             */
            if (IsTripleDot())
            {
                // If it had not a comma, add a VarArgDecl to the last parameter
                bool HadComma = T(Comma);

                Step();
                Step();
                Step();

                if (!HadComma && ret.Count > 0)
                {
                    ret[ret.Count - 1].Type = new VarArgDecl(ret[ret.Count - 1].Type);
                }
                else
                {
                    DVariable dv = new DVariable();
                    dv.Type = new VarArgDecl();
                    ret.Add(dv);
                }
            }

            Expect(CloseParenthesis);
            return ret;
        }

        bool IsTripleDot()
        {
            return LA(Dot) && PK(Dot) && Peek().Kind == Dot;
        }

        private DNode Parameter()
        {
            int attr = 0;
            if (IsInOut())
            {
                Step();
                attr = t.Kind;
            }

            TypeDeclaration td = BasicType();

            DNode ret = Declarator(true);
            if (attr != 0) ret.Attributes.Add(attr);
            if (ret.Type == null)
                ret.Type = td;
            else
                ret.Type.Base = td;

            // DefaultInitializerExpression
            if (LA(Assign))
            {
                Step();
                DExpression defInit = null;
                if (LA(Identifier) && (la.Value == "__FILE__" || la.Value == "__LINE__"))
                    defInit = new IdentExpression(la.Value);
                else
                    defInit = AssignExpression();

                if (ret is DVariable)
                    (ret as DVariable).Initializer = defInit;
            }

            return ret;
        }

        bool IsInOut()
        {
            return LA(In) || LA(Out) || LA(Ref) || LA(Lazy);
        }


        private DExpression Initializer()
        {
            // VoidInitializer
            if (LA(Void))
            {
                Step();
                return new TokenExpression(Void);
            }

            return NonVoidInitializer();
        }

        DExpression NonVoidInitializer()
        {
            // ArrayInitializer | StructInitializer
            if (LA(OpenSquareBracket) || LA(OpenCurlyBrace))
            {
                Step();
                bool IsStructInit = T(OpenCurlyBrace);
                if (IsStructInit ? LA(CloseCurlyBrace) : LA(CloseSquareBracket))
                {
                    Step();
                    return new ClampExpression(IsStructInit ? ClampExpression.ClampType.Curly : ClampExpression.ClampType.Square);
                }

                // ArrayMemberInitializations
                ArrayExpression ae = new ArrayExpression(IsStructInit ? ClampExpression.ClampType.Curly : ClampExpression.ClampType.Square);
                DExpression element = null;

                bool IsInit = true;
                while (IsInit || LA(Comma))
                {
                    if (!IsInit) Step();
                    IsInit = false;


                    if (IsStructInit)
                    {
                        // Identifier : NonVoidInitializer
                        if (LA(Identifier) && PK(Colon))
                        {
                            Step();
                            AssignTokenExpression inh = new AssignTokenExpression(Colon);
                            inh.PrevExpression = new IdentExpression(t.Value);
                            Step();
                            inh.FollowingExpression = NonVoidInitializer();
                            element = inh;
                        }
                        else
                            element = NonVoidInitializer();
                    }
                    else
                    {
                        // ArrayMemberInitialization
                        element = NonVoidInitializer();
                        bool HasBeenAssExpr = !(T(CloseSquareBracket) || T(CloseCurlyBrace));

                        // AssignExpression : NonVoidInitializer
                        if (HasBeenAssExpr && LA(Colon))
                        {
                            Step();
                            AssignTokenExpression inhExpr = new AssignTokenExpression(Colon);
                            inhExpr.PrevExpression = element;
                            inhExpr.FollowingExpression = NonVoidInitializer();
                            element = inhExpr;
                        }
                    }

                    ae.Expressions.Add(element);
                }

                Expect(CloseSquareBracket);
                return ae;
            }
            else
                return AssignExpression();
        }

        TypeDeclaration TypeOf()
        {
            Expect(Typeof);
            Expect(OpenParenthesis);
            MemberFunctionAttributeDecl md = new MemberFunctionAttributeDecl(Typeof);
            if (LA(Return))
                md.InnerType = new DTokenDeclaration(Return);
            else
                md.InnerType = new DExpressionDecl(Expression());
            Expect(CloseParenthesis);
            return md;
        }

        #endregion

        #region Attributes

        DBlockStatement _Invariant()
        {
            DBlockStatement inv = new DMethod();
            inv.Name = "invariant";

            Expect(Invariant);
            Expect(OpenParenthesis);
            Expect(CloseParenthesis);
            BlockStatement(ref inv);
            return inv;
        }

        void _Pragma()
        {
            Expect(Pragma);
            Expect(OpenParenthesis);
            Expect(Identifier);

            if (LA(Comma))
            {
                Step();
                ArgumentList();
            }
            Expect(CloseParenthesis);
        }

        bool IsAttributeSpecifier()
        {
            return (LA(Extern) || LA(Align) || LA(Pragma) || LA(Deprecated) || IsProtectionAttribute()
                || LA(Static) || LA(Final) || LA(Override) || LA(Abstract) || LA(Const) || LA(Auto) || LA(Scope) || LA(__gshared) || LA(Shared) || LA(Immutable) || LA(InOut)
                || LA(DisabledAttribute));
        }

        bool IsProtectionAttribute()
        {
            return LA(Public) || LA(Private) || LA(Protected) || LA(Extern) || LA(Package);
        }

        private void AttributeSpecifier()
        {
            if (LA(Extern) && PK(OpenParenthesis))
            {
                Step();
                Step();
                Expect(Identifier);
                Expect(CloseParenthesis);
            }
            else if (LA(Align) && PK(OpenParenthesis))
            {
                Step();
                Step();
                Expect(Literal);
                Expect(CloseParenthesis);
            }
            else if (LA(Pragma))
                _Pragma();
            else
                Step();

            if (LA(Colon))
                Step();

            // Unlike the definition we return here because later we'll treat this attribute as a pre-definition of a Declaration
        }
        #endregion

        #region Expressions
        DExpression Expression()
        {
            // AssignExpression
            DExpression ass = AssignExpression();
            if (!LA(Comma))
                return ass;

            /*
             * The following is a leftover of C syntax and proably cause some errors when parsing arguments etc.
             */
            // AssignExpression , Expression
            ArrayExpression ae = new ArrayExpression(ClampExpression.ClampType.Round);
            ae.Expressions.Add(ass);
            while (LA(Comma))
            {
                Step();
                ae.Expressions.Add(AssignExpression());
            }
            return ae;
        }

        /// <summary>
        /// This function has a very high importance because here we decide whether it's a declaration or assignExpression!
        /// </summary>
        bool IsAssignExpression()
        {
            if (IsBasicType())
            {
                if (LA(Identifier) || LA(Dot) || BasicTypes[la.Kind])
                {
                    /*
                     * In the following we are skipping the entire identifierlist that may consists of TemplateDeclarations and/or multiple identifiers
                     */

                    if (LA(Dot))
                        Peek();

                    // Check for template declarations
                    if (PK(Not))
                    {
                        Peek();
                        if (PK(OpenParenthesis))
                        {
                            Peek();
                            OverPeekBrackets(OpenParenthesis);
                        }
                        else
                            Peek();
                    }

                    // Skip possible slicing operators
                    while (PK(OpenSquareBracket))
                    {
                        Peek();
                        OverPeekBrackets(OpenSquareBracket);
                    }

                    while (PK(Dot))
                    {
                        Peek();
                        // pk should be an identifier now
                        if (!PK(Identifier))
                        {
                            SynErr(Identifier);
                            return true;
                        }

                        Peek(); // Skip identifier

                        // Check for template declarations
                        if (PK(Not))
                        {
                            Peek();
                            if (PK(OpenParenthesis))
                            {
                                Peek();
                                OverPeekBrackets(OpenParenthesis);
                            }
                            else
                                Peek();
                        }

                        // Skip possible slicing operators
                        while (PK(OpenSquareBracket))
                        {
                            Peek();
                            OverPeekBrackets(OpenSquareBracket);
                        }
                    }

                    /*
                     * And after we skipped all idents and templDecls, we check for basictype2's or trailing identifiers which represent a declaration
                     */

                    // Check for basictype2's
                    // Important: Do this AFTER we seeked for template declarations...
                    // because we have a peek token that's located after the template decl!
                    if (PK(Identifier) || PK(Times) || PK(Delegate) || PK(Function))
                        return false;
                }

                else if (LA(Typeof) || MemberFunctionAttribute[la.Kind])
                {
                    if (PK(OpenParenthesis))
                    {
                        Peek();
                        OverPeekBrackets(OpenParenthesis);
                    }

                    if (PK(Dot) && !LA(Typeof))
                    {
                        Peek();
                        if (PK(Identifier)) Peek();
                    }

                    if (PK(Identifier) || PK(Times) || PK(OpenSquareBracket) || PK(Delegate) || PK(Function) || PK(Dot))
                        return false;
                }
            }
            else if (IsStorageClass)
                return false;

            return true;
        }

        DExpression AssignExpression()
        {
            DExpression left = ConditionalExpression();
            if (!AssignOps[la.Kind])
                return left;

            Step();
            AssignTokenExpression ate = new AssignTokenExpression(t.Kind);
            ate.PrevExpression = left;
            ate.FollowingExpression = AssignExpression();
            return ate;
        }

        DExpression ConditionalExpression()
        {
            DExpression trigger = OrOrExpression();
            if (!LA(Question))
                return trigger;

            Expect(Question);
            SwitchExpression se = new SwitchExpression(trigger);
            se.TrueCase = AssignExpression();
            Expect(Colon);
            se.FalseCase = ConditionalExpression();
            return se;
        }

        DExpression OrOrExpression()
        {
            DExpression left = CmpExpression();
            if (!(LA(LogicalOr) || LA(LogicalAnd) || LA(BitwiseOr) || LA(BitwiseAnd) || LA(Xor)))
                return left;

            Step();
            AssignTokenExpression ae = new AssignTokenExpression(t.Kind);
            ae.PrevExpression = left;
            ae.FollowingExpression = OrOrExpression();
            return ae;
        }

        DExpression CmpExpression()
        {
            DExpression left = AddExpression();

            bool CanProceed =
                // RelExpression
                RelationalOperators[la.Kind] ||
                // EqualExpression
                LA(Equal) || LA(NotEqual) ||
                // IdentityExpression | InExpression
                LA(Is) || LA(In) || (LA(Not) && (PK(Is) || lexer.CurrentPeekToken.Kind == In)) ||
                // ShiftExpression
                LA(ShiftLeft) || LA(ShiftRight) || LA(ShiftRightUnsigned);

            if (!CanProceed)
                return left;

            // If we have a !in or !is
            if (LA(Not)) Step();
            Step();
            AssignTokenExpression ae = new AssignTokenExpression(t.Kind);
            ae.PrevExpression = left;
            // When a shift expression occurs, an AddExpression is required to follow
            if (T(ShiftLeft) || T(ShiftRight) || T(ShiftRightUnsigned))
                ae.FollowingExpression = AddExpression();
            else
                ae.FollowingExpression = OrOrExpression();
            return ae;
        }

        private DExpression AddExpression()
        {
            DExpression left = MulExpression();

            if (!(LA(Plus) || LA(Minus) || LA(Tilde)))
                return left;

            Step();
            AssignTokenExpression ae = new AssignTokenExpression(t.Kind);
            ae.PrevExpression = left;
            ae.FollowingExpression = MulExpression();
            return ae;
        }

        DExpression MulExpression()
        {
            DExpression left = PowExpression();

            if (!(LA(Times) || LA(Div) || LA(Mod)))
                return left;

            Step();
            AssignTokenExpression ae = new AssignTokenExpression(t.Kind);
            ae.PrevExpression = left;
            ae.FollowingExpression = MulExpression();
            return ae;
        }

        DExpression PowExpression()
        {
            DExpression left = UnaryExpression();

            if (!(LA(Pow)))
                return left;

            Step();
            AssignTokenExpression ae = new AssignTokenExpression(t.Kind);
            ae.PrevExpression = left;
            ae.FollowingExpression = PowExpression();
            return ae;
        }

        DExpression UnaryExpression()
        {
            // CastExpression
            if (LA(Cast))
            {
                Step();
                Expect(OpenParenthesis);
                TypeDeclaration castType = Type();
                Expect(CloseParenthesis);

                DExpression ex = UnaryExpression();
                ClampExpression ce = new ClampExpression(new TokenExpression(Cast), ClampExpression.ClampType.Round);
                ex.Base = ce;
                return ex;
            }

            if (LA(BitwiseAnd) || LA(Increment) || LA(Decrement) || LA(Times) || LA(Minus) || LA(Plus) ||
                LA(Not) || LA(Tilde))
            {
                Step();
                AssignTokenExpression ae = new AssignTokenExpression(t.kind);
                ae.FollowingExpression = UnaryExpression();
                return ae;
            }

            if (LA(OpenParenthesis))
            {
                Step();
                ClampExpression ce = new ClampExpression(ClampExpression.ClampType.Round);
                ce.InnerExpression = new TypeDeclarationExpression(Type());
                Expect(CloseParenthesis);
                Expect(Dot);
                Expect(Identifier);
                AssignTokenExpression ae = new AssignTokenExpression(Dot);
                ae.PrevExpression = ce;
                ae.FollowingExpression = new IdentExpression(t.Value);
                return ae;
            }

            // NewExpression
            if (LA(New))
                return NewExpression();

            // DeleteExpression
            if (LA(Delete))
            {
                Step();
                DExpression ex = UnaryExpression();
                ex.Base = new TokenExpression(Delete);
                return ex;
            }

            return PostfixExpression();
        }

        DExpression NewExpression()
        {
            Expect(New);
            DExpression ex = new TokenExpression(New);

            // NewArguments
            if (LA(OpenParenthesis))
            {
                Step();
                if (LA(CloseParenthesis))
                    Step();
                else
                {
                    ArrayExpression ae = new ArrayExpression(ClampExpression.ClampType.Round);
                    ae.Base = ex;
                    ae.Expressions = ArgumentList();
                    ex = ae;
                }
            }

            /*
             * If here occurs a class keyword, interpretate it as an anonymous class definition
             * NewArguments ClassArguments BaseClasslist[opt] { DeclDefs } 
             */
            if (LA(Class))
            {
                Step();
                DExpression ex2 = new TokenExpression(Class);
                ex2.Base = ex;
                ex = ex2;

                // ClassArguments
                if (LA(OpenParenthesis))
                {
                    if (LA(CloseParenthesis))
                        Step();
                    else
                    {
                        ArrayExpression ae = new ArrayExpression(ClampExpression.ClampType.Round);
                        ae.Base = ex;
                        ae.Expressions = ArgumentList();
                        ex = ae;
                    }
                }

                // BaseClasslist[opt]
                if (LA(Colon))
                {
                    //TODO : Add base classes to expression somehow ;-)
                    BaseClassList();
                }

                DClassLike _block = new DClassLike();
                _block.fieldtype = FieldType.Class;
                ClassBody(ref _block);

                return ex;
            }

            // NewArguments Type
            else
            {
                DExpression ex2 = new TypeDeclarationExpression(Type());
                ex2.Base = ex;
                ex = ex2;

                if (LA(OpenSquareBracket))
                {
                    ClampExpression ce = new ClampExpression();
                    ce.Base = ex;
                    ce.InnerExpression = AssignExpression();
                    Expect(CloseSquareBracket);
                }
                else if (LA(OpenParenthesis))
                {
                    ArrayExpression ae = new ArrayExpression(ClampExpression.ClampType.Round);
                    ae.Base = ex;
                    ae.Expressions = ArgumentList();
                    ex = ae;
                }
            }
            return ex;
        }

        List<DExpression> ArgumentList()
        {
            List<DExpression> ret = new List<DExpression>();

            ret.Add(AssignExpression());

            while (LA(Comma))
            {
                Step();
                ret.Add(AssignExpression());
            }

            return ret;
        }

        DExpression PostfixExpression()
        {
            // PostfixExpression
            DExpression retEx = PrimaryExpression();

            /*
             * A postfixexpression must start with a primaryexpression and can 
             * consist of more than one additional epxression --
             * things like foo()[1] become possible then
             */
            while (LA(Dot) || LA(Increment) || LA(Decrement) || LA(OpenParenthesis) || LA(OpenSquareBracket))
            {
                // Function call
                if (LA(OpenParenthesis))
                {
                    Step();
                    ArrayExpression ae = new ArrayExpression(ClampExpression.ClampType.Round);
                    ae.Base = retEx;
                    if (!LA(CloseParenthesis))
                        ae.Expressions = ArgumentList();
                    Step();

                    retEx = ae;
                }

                // IndexExpression | SliceExpression
                else if (LA(OpenSquareBracket))
                {
                    Step();

                    if (!LA(CloseSquareBracket))
                    {
                        DExpression firstEx = AssignExpression();
                        // [ AssignExpression .. AssignExpression ]
                        if (LA(Dot) && PK(Dot))
                        {
                            Step();
                            Step();
                            TokenExpression tex = new TokenExpression(Dot);
                            tex.Base = firstEx;
                            TokenExpression tex2 = new TokenExpression(Dot);
                            tex2.Base = tex;

                            DExpression second = AssignExpression();
                            second.Base = tex2;

                            retEx = second;
                        }
                        // [ ArgumentList ]
                        else if (LA(Comma))
                        {
                            ArrayExpression ae = new ArrayExpression();
                            ae.Expressions.Add(firstEx);
                            while (LA(Comma))
                            {
                                Step();
                                ae.Expressions.Add(AssignExpression());
                            }
                        }
                        else
                            SynErr(CloseSquareBracket);
                    }

                    Expect(CloseSquareBracket);
                }

                else if (LA(Dot))
                {
                    Step();
                    AssignTokenExpression ae = new AssignTokenExpression(Dot);
                    ae.PrevExpression = retEx;
                    if (LA(New))
                        ae.FollowingExpression = NewExpression();
                    else if (LA(Identifier))
                    {
                        Step();
                        ae.FollowingExpression = new IdentExpression(t.Value);
                    }
                    else
                        SynErr(Identifier, "Identifier or new expected");

                    retEx = ae;
                }
                else if (LA(Increment) || LA(Decrement))
                {
                    Step();
                    DExpression ex2 = new TokenExpression(t.Kind);
                    ex2.Base = retEx;
                    retEx = ex2;
                }
            }

            return retEx;
        }

        DExpression PrimaryExpression()
        {
            if (LA(Identifier) && PK(Not))
                return new TypeDeclarationExpression(TemplateInstance());

            if (LA(Identifier))
            {
                Step();
                return new IdentExpression(t.Value);
            }

            if (LA(This) || LA(Super) || LA(Null) || LA(True) || LA(False) || LA(Dollar))
            {
                Step();
                return new TokenExpression(t.Kind);
            }

            if (LA(Literal))
            {
                Step();
                return new IdentExpression(t.LiteralValue);
            }

            if (LA(Dot))
            {
                Step();
                Expect(Identifier);
                DExpression ret = new IdentExpression(t.Value);
                ret.Base = new TokenExpression(Dot);
                return ret;
            }

            // ArrayLiteral | AssocArrayLiteral
            if (LA(OpenSquareBracket))
            {
                Step();
                ArrayExpression arre = new ArrayExpression();

                DExpression firstCondExpr = ConditionalExpression();
                // Can be an associative array only
                if (LA(Colon))
                {
                    Step();
                    AssignTokenExpression ae = new AssignTokenExpression(Colon);
                    ae.PrevExpression = firstCondExpr;
                    ae.FollowingExpression = ConditionalExpression();
                    arre.Expressions.Add(ae);

                    while (LA(Comma))
                    {
                        Step();
                        ae = new AssignTokenExpression(Colon);
                        ae.PrevExpression = ConditionalExpression();
                        Expect(Colon);
                        ae.FollowingExpression = ConditionalExpression();
                        arre.Expressions.Add(ae);
                    }
                }
                else
                {
                    if (AssignOps[la.Kind])
                    {
                        Step();
                        AssignTokenExpression ae = new AssignTokenExpression(t.Kind);
                        ae.PrevExpression = firstCondExpr;
                        ae.FollowingExpression = AssignExpression();
                        arre.Expressions.Add(ae);
                    }

                    while (LA(Comma))
                    {
                        Step();
                        arre.Expressions.Add(AssignExpression());
                    }
                }

                Expect(CloseSquareBracket);
                return arre;
            }

            //TODO: FunctionLiteral

            // AssertExpression
            if (LA(Assert))
            {
                Step();
                Expect(OpenParenthesis);
                ClampExpression ce = new ClampExpression(ClampExpression.ClampType.Round);
                ce.FrontExpression = new TokenExpression(Assert);
                ce.InnerExpression = AssignExpression();

                if (LA(Comma))
                {
                    Step();
                    AssignTokenExpression ate = new AssignTokenExpression(Comma);
                    ate.PrevExpression = ce.InnerExpression;
                    ate.FollowingExpression = AssignExpression();
                    ce.InnerExpression = ate;
                }
                Expect(CloseParenthesis);
                return ce;
            }

            // MixinExpression | ImportExpression
            if (LA(Mixin) || LA(Import))
            {
                Step();
                int tk = t.Kind;
                Expect(OpenParenthesis);
                ClampExpression ce = new ClampExpression(ClampExpression.ClampType.Round);
                ce.FrontExpression = new TokenExpression(tk);
                ce.InnerExpression = AssignExpression();
                Expect(CloseParenthesis);
                return ce;
            }

            // Typeof
            if (LA(Typeof))
            {
                return new TypeDeclarationExpression(TypeOf());
            }

            // TypeidExpression
            if (LA(Typeid))
            {
                //TODO
            }
            // IsExpression
            if (LA(Is))
            {
                //TODO
            }
            // ( Expression )
            if (LA(OpenParenthesis))
            {
                Step();
                DExpression ret = Expression();
                Expect(CloseParenthesis);
                return ret;
            }
            // TraitsExpression
            if (LA(__traits))
            {
                return TraitsExpression();
            }

            // BasicType . Identifier
            if (LA(Const) || LA(Immutable) || LA(Shared) || LA(InOut) || BasicTypes[la.Kind])
            {
                Step();
                int tk = t.Kind;
                Expect(OpenParenthesis);
                ClampExpression ce = new ClampExpression(ClampExpression.ClampType.Round);
                ce.FrontExpression = new TokenExpression(tk);
                ce.InnerExpression = new TypeDeclarationExpression(Type());
                Expect(CloseParenthesis);

                Expect(Dot);
                Expect(Identifier);
                AssignTokenExpression ate = new AssignTokenExpression(Dot);
                ate.PrevExpression = ce;
                ate.FollowingExpression = new IdentExpression(t.Value);
            }

            SynErr(t.Kind, "Identifier expected when parsing an expression");
            Step();
            return null;
        }
        #endregion

        #region Statements
        void Statement(ref DBlockStatement par, bool CanBeEmpty, bool BlocksAllowed)
        {
            if (CanBeEmpty && LA(Semicolon))
            {
                Step();
                return;
            }

            else if (BlocksAllowed && LA(OpenCurlyBrace))
            {
                BlockStatement(ref par);
                return;
            }

            // LabeledStatement
            else if (LA(Identifier) && PK(Colon))
            {
                Step();
                Step();
                return;
            }

            // IfStatement
            else if (LA(If))
            {
                Step();
                DBlockStatement dbs = new DBlockStatement();
                dbs.StartLocation = t.Location;
                Expect(OpenParenthesis);

                // IfCondition
                if (LA(Auto))
                {
                    Step();
                    Expect(Identifier);
                    Expect(Assign);
                    Expression();
                }
                else if (IsAssignExpression())
                    Expression();
                else
                {
                    Declarator(false);
                    Expect(Assign);
                    Expression();
                }

                Expect(CloseParenthesis);
                // ThenStatement

                Statement(ref dbs, false, true);
                if (dbs.Count > 0) par.Add(dbs);

                // ElseStatement
                if (LA(Else))
                {
                    Step();
                    dbs = new DBlockStatement();
                    dbs.StartLocation = t.Location;
                    Statement(ref dbs, false, true);
                    dbs.EndLocation = t.EndLocation;
                    if (dbs.Count > 0) par.Add(dbs);
                }
            }

            // WhileStatement
            else if (LA(While))
            {
                Step();

                DBlockStatement dbs = new DBlockStatement();
                dbs.StartLocation = t.Location;

                Expect(OpenParenthesis);
                Expression();
                Expect(CloseParenthesis);

                Statement(ref dbs, false, true);
                dbs.EndLocation = t.EndLocation;
                if (dbs.Count > 0) par.Add(dbs);
            }

            // DoStatement
            else if (LA(Do))
            {
                Step();

                DBlockStatement dbs = new DBlockStatement();
                dbs.StartLocation = t.Location;
                Statement(ref dbs, false, true);

                Expect(While);
                Expect(OpenParenthesis);
                Expression();
                Expect(CloseParenthesis);

                dbs.EndLocation = t.EndLocation;
                if (dbs.Count > 0) par.Add(dbs);
            }

            // ForStatement
            else if (LA(For))
            {
                Step();

                DBlockStatement dbs = new DBlockStatement();
                dbs.StartLocation = t.Location;

                Expect(OpenParenthesis);

                // Initialize
                if (LA(Semicolon))
                    Step();
                else
                    Statement(ref dbs, false, true);

                // Test
                if (!LA(Semicolon))
                    Expression();

                Expect(Semicolon);

                // Increment
                if (!LA(CloseParenthesis))
                    Expression();

                Expect(CloseParenthesis);

                Statement(ref dbs, false, true);
                dbs.EndLocation = t.EndLocation;
                if (dbs.Count > 0) par.Add(dbs);
            }

            // ForeachStatement
            else if (LA(Foreach) || LA(Foreach_Reverse))
            {
                Step();

                DBlockStatement dbs = new DBlockStatement();
                dbs.StartLocation = t.Location;

                Expect(OpenParenthesis);

                bool init = true;
                while (init || LA(Comma))
                {
                    if (!init) Step();
                    init = false;

                    DVariable forEachVar = new DVariable();
                    forEachVar.StartLocation = la.Location;

                    if (LA(Ref))
                    {
                        Step();
                        forEachVar.Attributes.Add(Ref);
                    }
                    if (LA(Identifier) && (PK(Semicolon) || lexer.CurrentPeekToken.Kind == Comma))
                    {
                        Step();
                        forEachVar.Name = t.Value;
                    }
                    else
                    {
                        forEachVar.Type = Type();
                        Expect(Identifier);
                        forEachVar.Name = t.Value;
                    }
                    forEachVar.EndLocation = t.EndLocation;
                    dbs.Add(forEachVar);
                }

                Expect(Semicolon);
                Expression();

                // ForeachRangeStatement
                if (LA(Dot) && PK(Dot))
                {
                    Step();
                    Step();
                    Expression();
                }

                Expect(CloseParenthesis);

                Statement(ref dbs, false, true);

                dbs.EndLocation = t.EndLocation;
                if (dbs.Count > 0) par.Add(dbs);
            }

            // [Final] SwitchStatement
            else if ((LA(Final) && PK(Switch)) || LA(Switch))
            {
                DBlockStatement dbs = new DBlockStatement();
                dbs.StartLocation = la.Location;

                if (LA(Final))
                {
                    dbs.Attributes.Add(Final);
                    Step();
                }
                Step();
                Expect(OpenParenthesis);
                Expression();
                Expect(CloseParenthesis);
                Statement(ref dbs, false, true);
                dbs.EndLocation = t.EndLocation;

                if (dbs.Count > 0) par.Add(dbs);
            }

            // CaseStatement
            else if (LA(Case))
            {
                Step();

                DBlockStatement dbs = new DBlockStatement();
                dbs.StartLocation = la.Location;

                AssignExpression();

                if (!(LA(Colon) && PK(Dot) && Peek().Kind == Dot))
                    while (LA(Comma))
                        AssignExpression();

                Expect(Colon);

                // CaseRangeStatement
                if (LA(Dot) && PK(Dot))
                {
                    Step();
                    Step();
                    Expect(Case);
                    AssignExpression();
                    Expect(Colon);
                }

                Statement(ref dbs, true, true);
                dbs.EndLocation = t.EndLocation;

                if (dbs.Count > 0) par.Add(dbs);
            }

            // Default
            else if (LA(Default))
            {
                Step();

                DBlockStatement dbs = new DBlockStatement();
                dbs.StartLocation = la.Location;

                Expect(Colon);
                Statement(ref dbs, true, true);
                dbs.EndLocation = t.EndLocation;

                if (dbs.Count > 0) par.Add(dbs);
            }

            // Continue | Break
            else if (LA(Continue) || LA(Break))
            {
                Step();
                if (LA(Identifier))
                    Step();
                Expect(Semicolon);
            }

            // Return
            else if (LA(Return))
            {
                Step();
                if (!LA(Semicolon))
                    Expression();
                Expect(Semicolon);
            }

            // Goto
            else if (LA(Goto))
            {
                Step();
                if (LA(Identifier) || LA(Default))
                {
                    Step();
                }
                else if (LA(Case))
                {
                    Step();
                    if (!LA(Semicolon))
                        Expression();
                }

                Expect(Semicolon);
            }

            // WithStatement
            else if (LA(With))
            {
                Step();

                DBlockStatement dbs = new DBlockStatement();
                dbs.StartLocation = t.Location;

                Expect(OpenParenthesis);

                // Symbol
                if (LA(Identifier))
                    IdentifierList();
                else
                    Expression();

                Expect(CloseParenthesis);
                Statement(ref dbs, false, true);
                dbs.EndLocation = t.EndLocation;

                if (dbs.Count > 0) par.Add(dbs);
            }

            // SynchronizedStatement
            else if (LA(Synchronized))
            {
                Step();
                DBlockStatement dbs = new DBlockStatement();
                dbs.StartLocation = t.Location;

                if (LA(OpenParenthesis))
                {
                    Step();
                    Expression();
                    Expect(CloseParenthesis);
                }
                Statement(ref dbs, false, true);

                dbs.EndLocation = t.EndLocation;
                if (dbs.Count > 0) par.Add(dbs);
            }

            // TryStatement
            else if (LA(Try))
            {
                Step();

                DBlockStatement dbs = new DBlockStatement();
                dbs.StartLocation = t.Location;
                Statement(ref dbs, false, true);
                dbs.EndLocation = t.EndLocation;
                if (dbs.Count > 0) par.Add(dbs);

                if (!(LA(Catch) || LA(Finally)))
                    SynErr(Catch, "catch or finally expected");

                // Catches
            do_catch:
                if (LA(Catch))
                {
                    Step();
                    dbs = new DBlockStatement();
                    dbs.StartLocation = t.Location;

                    // CatchParameter
                    if (LA(OpenParenthesis))
                    {
                        Step();
                        DVariable catchVar = new DVariable();
                        catchVar.Type = BasicType();
                        Expect(Identifier);
                        catchVar.Name = t.Value;
                        Expect(CloseParenthesis);
                        dbs.Add(catchVar);
                    }

                    Statement(ref dbs, false, true);
                    dbs.EndLocation = t.EndLocation;
                    if (dbs.Count > 0) par.Add(dbs);

                    if (LA(Catch))
                        goto do_catch;
                }

                if (LA(Finally))
                {
                    Step();

                    dbs = new DBlockStatement();
                    dbs.StartLocation = t.Location;
                    Statement(ref dbs, false, true);
                    dbs.EndLocation = t.EndLocation;
                    if (dbs.Count > 0) par.Add(dbs);
                }
            }

            // ThrowStatement
            else if (LA(Throw))
            {
                Step();
                Expression();
                Expect(Semicolon);
            }

            // ScopeGuardStatement
            else if (LA(Scope))
            {
                Step();
                Expect(OpenParenthesis);
                Expect(Identifier); // exit, failure, success
                Expect(CloseParenthesis);
                Statement(ref par, false, true);
            }

            // AsmStatement
            else if (LA(Asm))
            {
                Step();
                Expect(OpenCurlyBrace);

                while (!IsEOF && !LA(CloseCurlyBrace))
                {
                    Step();
                }

                Expect(CloseCurlyBrace);
            }

            // PragmaStatement
            else if (LA(Pragma))
            {
                _Pragma();
                Statement(ref par, true, true);
            }

            // MixinStatement
            else if (LA(Mixin))
            {
                Step();
                Expect(OpenParenthesis);

                AssignExpression();

                Expect(CloseParenthesis);
                Expect(Semicolon);
            }

            else if (IsAssignExpression())
            {
                AssignExpression();
                Expect(Semicolon);
            }
            else
                Declaration(ref par);
        }

        void BlockStatement(ref DBlockStatement par)
        {
            Expect(OpenCurlyBrace);
            par.BlockStartLocation = t.Location;

            while (!IsEOF && !LA(CloseCurlyBrace))
            {
                Statement(ref par, true, true);
            }

            Expect(CloseCurlyBrace);
        }
        #endregion

        #region Structs & Unions
        private DNode AggregateDeclaration()
        {
            if (!(LA(Union) || LA(Struct)))
                SynErr(t.Kind, "union or struct required");
            Step();

            DClassLike ret = new DClassLike();
            ret.fieldtype = FieldType.Struct;
            ret.Type = new DTokenDeclaration(t.Kind);
            ret.TypeToken = t.Kind;

            Expect(Identifier);
            ret.Name = t.Value;

            if (LA(Semicolon))
            {
                Step();
                return ret;
            }

            // StructTemplateDeclaration
            if (LA(OpenParenthesis))
            {
                Step();
                ret.TemplateParameters = TemplateParameterList();
                Expect(CloseParenthesis);

                // Constraint[opt]
                if (LA(If))
                    Constraint();
            }

            ClassBody(ref ret);

            return ret;
        }
        #endregion

        #region Classes
        private DNode ClassDeclaration()
        {
            Expect(Class);
            DClassLike dc = new DClassLike();
            dc.StartLocation = t.Location;

            Expect(Identifier);
            dc.Name = t.Value;

            if (LA(OpenParenthesis))
            {
                Step();
                dc.TemplateParameters = TemplateParameterList();
                Expect(CloseParenthesis);
            }

            if (LA(Colon))
                dc.BaseClasses = BaseClassList();

            ClassBody(ref dc);

            dc.EndLocation = t.EndLocation;
            return dc;
        }

        private List<string> BaseClassList()
        {
            Expect(Colon);

            List<string> ret = new List<string>();

            bool init = true;
            while (init || LA(Comma))
            {
                if (IsProtectionAttribute() && !LA(Protected))
                    Step();

                ret.Add(ModuleFullyQualifiedName());
            }
            return ret;
        }

        private void ClassBody(ref DClassLike ret)
        {
            Expect(OpenCurlyBrace);

            while (!IsEOF && !LA(CloseCurlyBrace))
            {
                if (IsAttributeSpecifier())
                    AttributeSpecifier();

                // StructConstructor | StructDestructor
                else if (LA(This) || (LA(Tilde) && PK(This)))
                {
                    DMethod dm = new DMethod();
                    dm.Type = new NormalDeclaration((LA(Dot) ? "~" : "") + "this");
                    dm.Name = (LA(Dot) ? "~" : "") + "this";

                    if (LA(Dot))
                        Step();
                    Step();

                    Expect(OpenParenthesis);

                    if (LA(This) && ret.fieldtype != FieldType.Struct)
                    {
                        DVariable dv = new DVariable();
                        dv.Name = "this";
                        dm.Parameters.Add(dv);
                        Step();
                    }
                    else
                        dm.Parameters = Parameters();

                    Expect(CloseParenthesis);

                    DBlockStatement dm_ = dm as DBlockStatement;
                    FunctionBody(ref dm_);
                    ret.Add(dm);
                }

                else if (LA(Invariant))
                    ret.Add(_Invariant());
                else if (LA(Semicolon))
                    Step();
                else
                {
                    DBlockStatement _ret = ret as DBlockStatement;
                    Decl(ref _ret);
                }
            }

            Expect(CloseCurlyBrace);
        }

        DNode Constructor()
        {
            throw new NotImplementedException();
        }

        DNode Destructor()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Interfaces
        private DClassLike InterfaceDeclaration()
        {
            Expect(Interface);
            DClassLike dc = new DClassLike();
            dc.StartLocation = t.Location;

            Expect(Identifier);
            dc.Name = t.Value;

            if (LA(OpenParenthesis))
            {
                Step();
                dc.TemplateParameters = TemplateParameterList();
                Expect(CloseParenthesis);

                if (LA(If))
                    Constraint();
            }

            if (LA(Colon))
                dc.BaseClasses = BaseClassList();

            ClassBody(ref dc);

            dc.EndLocation = t.EndLocation;
            return dc;
        }

        void Constraint()
        {
            Expect(If);
            Expect(OpenParenthesis);
            Expression();
            Expect(CloseParenthesis);
        }
        #endregion

        #region Enums
        private DNode EnumDeclaration()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Functions
        void FunctionBody(ref DBlockStatement par)
        {
            bool HadIn = false, HadOut = false;

        check_again:
            if (!HadIn && LA(In))
            {
                HadIn = true;
                Step();
                BlockStatement(ref par);

                if (!HadOut && LA(Out))
                    goto check_again;
            }

            if (!HadOut && LA(Out))
            {
                HadOut = true;
                Step();

                if (LA(OpenParenthesis))
                {
                    Step();
                    Expect(Identifier);
                    Expect(CloseParenthesis);
                }

                BlockStatement(ref par);

                if (!HadIn && LA(In))
                    goto check_again;
            }

            if (HadIn || HadOut)
                Expect(Body);
            else if (LA(Body))
                Step();

            BlockStatement(ref par);

        }
        #endregion

        #region Templates
        /*
         * American beer is like sex on a boat - Fucking close to water;)
         */

        private DNode TemplateDeclaration()
        {
            Expect(Template);
            DClassLike dc = new DClassLike();
            dc.fieldtype = FieldType.Template;
            dc.StartLocation = t.Location;

            Expect(Identifier);
            dc.Name = t.Value;

            Expect(OpenParenthesis);
            dc.TemplateParameters = TemplateParameterList();
            Expect(CloseParenthesis);

            if (LA(If))
                Constraint();

            if (LA(Colon))
                dc.BaseClasses = BaseClassList();

            ClassBody(ref dc);

            dc.EndLocation = t.EndLocation;
            return dc;
        }

        /// <summary>
        /// Be a bit lazy here with checking whether there're templates or not
        /// </summary>
        private bool IsTemplateParameterList()
        {
            lexer.StartPeek();
            int r = 0;
            while (r >= 0 && lexer.CurrentPeekToken.Kind != EOF)
            {
                if (lexer.CurrentPeekToken.Kind == OpenParenthesis) r++;
                else if (lexer.CurrentPeekToken.Kind == CloseParenthesis)
                {
                    r--;
                    if (r <= 0)
                        if (Peek().Kind == OpenParenthesis)
                            return true;
                        else return false;
                }
                Peek();
            }
            return false;
        }

        private List<DNode> TemplateParameterList()
        {
            List<DNode> ret = new List<DNode>();

            bool init = true;
            while (init || LA(Comma))
            {
                if (!init) Step();
                init = false;

                DNode dv=new DVariable();

                // TemplateThisParameter
                if (LA(This))
                    Step();

                // TemplateTupleParameter
                if (LA(Identifier) && PK(Dot) && Peek().Kind == Dot && Peek().Kind == Dot)
                {
                    Step();
                    dv.Type = new VarArgDecl();
                    dv.Name = t.Value;
                    Step();
                    Step();
                    Step();
                }

                // TemplateAliasParameter
                else if (LA(Alias))
                {
                    Step();
                    dv.Type = new DTokenDeclaration(Alias);
                    Expect(Identifier);
                    dv.Name = t.Value;

                    // TemplateAliasParameterSpecialization
                    if (LA(Colon))
                    {
                        Step();

                        dv.Type = new InheritanceDecl(dv.Type);
                        (dv.Type as InheritanceDecl).InheritedClass=Type();
                    }

                    // TemplateAliasParameterDefault
                    if (LA(Assign))
                    {
                        Step();
                        (dv as DVariable).Initializer = new TypeDeclarationExpression(Type());
                    }
                }

                // TemplateTypeParameter
                else if (LA(Identifier) && (PK(Colon) || PK(Assign) || PK(Comma) || PK(CloseParenthesis)))
                {
                    Step();
                    dv.Name = t.Value;

                    if (LA(Colon))
                    {
                        Step();
                        dv.Type = new InheritanceDecl(dv.Type);
                        (dv.Type as InheritanceDecl).InheritedClass = Type();
                    }

                    if (LA(Assign))
                    {
                        Step();
                        (dv as DVariable).Initializer = new TypeDeclarationExpression(Type());
                    }
                }

                else
                {
                    TypeDeclaration bt = BasicType();
                    dv = Declarator(false);
                    dv.Type.Base = bt;

                    if (LA(Colon))
                    {
                        Step();
                        ConditionalExpression();
                    }

                    if (LA(Assign))
                    {
                        Step();
                        if (LA(Identifier))
                            Step();
                        else
                            ConditionalExpression();
                    }
                }
                ret.Add(dv);
            }

            return ret;
        }

        private TypeDeclaration TemplateInstance()
        {
            Expect(Identifier);
            TemplateDecl td = new TemplateDecl(new NormalDeclaration(t.Value));
            Expect(Not);
            if (LA(OpenParenthesis))
            {
                Step();

                bool init=true;
                while(init || LA(Comma))
                {
                    if(!init)Step();
                    init=false;

                    if(IsAssignExpression())
                        td.Template.Add(new DExpressionDecl(AssignExpression()));
                    else
                        td.Template.Add(Type());
                }

                Expect(CloseParenthesis);
            }
            else
            {
                Step();
                if (T(Identifier) || T(Literal))
                    td.Template.Add(new NormalDeclaration(t.Value));
                else
                    td.Template.Add(new DTokenDeclaration(t.Kind));
            }
            return td;
        }
        #endregion

        #region Traits
        DExpression TraitsExpression()
        {
            Expect(__traits);
            Expect(OpenParenthesis);
            ClampExpression ce = new ClampExpression(new TokenExpression(__traits), ClampExpression.ClampType.Round);

            //TODO: traits keywords

            Expect(CloseParenthesis);
            return ce;
        }
        #endregion
    }

}