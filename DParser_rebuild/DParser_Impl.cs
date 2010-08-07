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
            doc = module;
            // Only one module declaration possible possible!
            if (la.Kind==(Module))
                module.ModuleName = ModuleDeclaration();

            DBlockStatement _block = module as DBlockStatement;
            // Now only declarations or other statements are allowed!
            while (!IsEOF)
            {
                DeclDef(ref _block);
            }
            return module;
        }

        void DeclDef(ref DBlockStatement module)
        {
            //AttributeSpecifier
            if (IsAttributeSpecifier())
                AttributeSpecifier();

            //ImportDeclaration
            else if (la.Kind==(Import))
                ImportDeclaration();

            //Constructor
            else if (la.Kind==(This))
                module.Add(Constructor(module.fieldtype == FieldType.Struct));

            //Destructor
            else if (la.Kind==(Tilde) && lexer.CurrentPeekToken.Kind==(This))
                module.Add(Destructor());

            //Invariant
            else if (la.Kind==(Invariant))
                module.Add(_Invariant());

            //UnitTest
            else if (la.Kind==(Unittest))
            {
                Step();
                DBlockStatement dbs = new DBlockStatement(FieldType.Function);
                dbs.Type = new DTokenDeclaration(Unittest);
                dbs.StartLocation = t.Location;
                FunctionBody(ref dbs);
                dbs.EndLocation = t.EndLocation;
            }

            //ConditionalDeclaration
            else if (la.Kind==(Version) || la.Kind==(Debug) || la.Kind==(If))
            {
                Step();
                string n = t.ToString();

                if (t.Kind==(If))
                {
                    Expect(OpenParenthesis);
                    AssignExpression();
                    Expect(CloseParenthesis);
                }
                else if (la.Kind==(Assign))
                {
                    Step();
                    Step();
                    Expect(Semicolon);
                }
                else if (t.Kind==(Version))
                {
                    Expect(OpenParenthesis);
                    n += "(";
                    Step();
                    n += t.ToString();
                    Expect(CloseParenthesis);
                    n += ")";
                }
                else if (t.Kind==(Debug) && la.Kind==(OpenParenthesis))
                {
                    Expect(OpenParenthesis);
                    n += "(";
                    Step();
                    n += t.ToString();
                    Expect(CloseParenthesis);
                    n += ")";
                }

                if (la.Kind==(Colon))
                    Step();
            }

            //TODO
            else if (la.Kind==(Else))
            {
                Step();
            }

            //StaticAssert
            else if (la.Kind==(Assert))
            {
                Step();
                Expect(OpenParenthesis);
                AssignExpression();
                if (la.Kind==(Comma))
                {
                    Step();
                    AssignExpression();
                }
                Expect(CloseParenthesis);
                Expect(Semicolon);
            }
            //TemplateMixin

            //MixinDeclaration
            else if (la.Kind==(Mixin))
                MixinDeclaration();

            //;
            else if (la.Kind==(Semicolon))
                Step();

            // {
            else if (la.Kind==(OpenCurlyBrace))
            {
                // Due to having a new attribute scope, we'll have use a new attribute stack here
                Stack<int> AttrBackup = BlockAttributes;
                BlockAttributes = new Stack<int>();

                while (DeclarationAttributes.Count > 0)
                    BlockAttributes.Push(DeclarationAttributes.Pop());

                ClassBody(ref module);

                // After the block ended, restore the previous block attributes
                BlockAttributes = AttrBackup;
            }

            // Class Allocators
            // Note: Although occuring in global scope, parse it anyway but declare it as semantic nonsense;)
            else if (la.Kind==(New))
            {
                Step();

                DMethod dm = new DMethod();
                dm.Name = "new";
                DNode dn = dm as DNode;
                ApplyAttributes(ref dn);

                dm.Parameters = Parameters();
                DBlockStatement dbs = dm as DBlockStatement;
                FunctionBody(ref dbs);

            }

            // else:
            else Declaration(ref module);
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

        void ImportDeclaration()
        {
            Expect(Import);

            _Import();

            // ImportBindings
            if (la.Kind==(Colon))
            {
                Step();
                ImportBind();
                while (la.Kind==(Comma))
                {
                    Step();
                    ImportBind();
                }
            }
            else
                while (la.Kind==(Comma))
                {
                    Step();
                    doc.Imports.Add(_Import());
                }

            Expect(Semicolon);
        }

        string _Import()
        {
            // ModuleAliasIdentifier
            if (lexer.CurrentPeekToken.Kind==(Assign))
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

            if (la.Kind==(Assign))
            {
                Step();
                Expect(Identifier);
                imbBindDef = t.Value;
            }
        }


        DNode MixinDeclaration()
        {
            Expect(Mixin);

            if (LA(OpenParenthesis))
            {
                Step();
                AssignExpression();
                Expect(CloseParenthesis);
            }
            else
            {
                // TemplateMixinDeclaration
                if (LA(Template))
                    return TemplateDeclaration();

                // TemplateMixin
                else if (LA(Identifier))
                {
                    if (PK(Not))
                        TemplateInstance();
                    else
                        Expect(Identifier);

                    // MixinIdentifier
                    if (LA(Identifier))
                        Step();
                }
            }
            Expect(Semicolon);
            return null;
        }
        #endregion

        #region Declarations
        // http://www.digitalmars.com/d/2.0/declaration.html

        bool IsDeclaration()
        {
            return la.Kind==(Alias) || IsStorageClass || IsBasicType();
        }

        void Declaration(ref DBlockStatement par)
        {
            if (la.Kind==(Alias))
            {
                Step();
                DBlockStatement _t = new DBlockStatement();
                DNode dn = _t as DNode;
                ApplyAttributes(ref dn);

                // AliasThis
                if (la.Kind == Identifier && PK(This))
                {
                    Step();
                    DVariable dv = new DVariable();
                    dv.StartLocation = lexer.LastToken.Location;
                    dv.fieldtype = FieldType.AliasDecl;
                    dv.Name = "this";
                    dv.Type = new NormalDeclaration(t.Value);
                    dv.EndLocation = t.EndLocation;
                    Step();
                    Expect(Semicolon);
                    return;
                }

                Decl(ref _t);
                foreach (DNode n in _t)
                    n.fieldtype = FieldType.AliasDecl;

                par.Children.AddRange(_t);
            }
            else if (la.Kind==(Struct) || la.Kind==(Union))
                par.Add(AggregateDeclaration());
            else if (la.Kind==(Enum))
                EnumDeclaration(ref par);
            else if (la.Kind==(Class))
                par.Add(ClassDeclaration());
            else if (la.Kind==(Template))
                par.Add(TemplateDeclaration());
            else if (la.Kind==(Interface))
                par.Add(InterfaceDeclaration());
            else
                Decl(ref par);
        }

        void Decl(ref DBlockStatement par)
        {
            // Skip ref token
            if (la.Kind == (Ref))
            {
                DeclarationAttributes.Push(Ref);
                Step();
            }

            // Enum possible storage class attributes
            bool HasStorageClassModifiers = false;
            while (IsStorageClass)
            {
                Step();
                if (!DeclarationAttributes.Contains(t.Kind))
                    DeclarationAttributes.Push(t.Kind);
                HasStorageClassModifiers = true;
            }

            TypeDeclaration ttd =null;

            // Skip ref token
            if (la.Kind==(Ref) && !DeclarationAttributes.Contains(Ref))
            {
                DeclarationAttributes.Push(Ref);
                Step();
            }

            // Autodeclaration
            if (HasStorageClassModifiers && la.Kind==(Identifier) && DeclarationAttributes.Count > 0 &&
                (PK(Assign) || PK(OpenParenthesis)))
            {
                ttd = new DTokenDeclaration(DeclarationAttributes.Pop());
            }
            else 
                ttd= BasicType();

            // Declarators
            DNode firstNode = Declarator(false);

            if (firstNode.Type == null)
                firstNode.Type = ttd;
            else
                firstNode.Type.MostBasic.Base = ttd;

            ApplyAttributes(ref firstNode);

            // BasicType Declarators ;
            bool ExpectFunctionBody = !(la.Kind==(Assign) || la.Kind==(Comma) || la.Kind==(Semicolon));
            if (!ExpectFunctionBody)
            {
                // DeclaratorInitializer
                if (la.Kind==(Assign))
                    (firstNode as DVariable).Initializer = Initializer();

                par.Add(firstNode);

                // DeclaratorIdentifierList
                while (la.Kind==(Comma))
                {
                    Step();
                    Expect(Identifier);

                    DVariable otherNode = new DVariable();
                    otherNode.Assign(firstNode);
                    otherNode.Name = t.Value;

                    if (la.Kind==(Assign))
                        otherNode.Initializer = Initializer();

                    par.Add(otherNode);
                }

                Expect(Semicolon);
            }

            // BasicType Declarator FunctionBody
            else if (firstNode is DBlockStatement)
            {
                DBlockStatement _block = firstNode as DBlockStatement;
                if (la.Kind==(If))
                    Constraint();
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
            return BasicTypes[la.Kind] || la.Kind==(Typeof) || MemberFunctionAttribute[la.Kind] || (la.Kind==(Dot) && lexer.CurrentPeekToken.Kind==(Identifier)) || la.Kind==(Identifier);
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

            //TODO
            if (la.Kind == Ref)
                Step();
            
            if (la.Kind==(Typeof))
            {
                td = TypeOf();
                if (la.Kind!=(Dot)) return td;
            }

            if (la.Kind==(Dot))
                Step();

            if (td == null)
                td = IdentifierList();
            else
                td.Base = IdentifierList();

            return td;
        }

        bool IsBasicType2()
        {
            return la.Kind==(Times) || la.Kind==(OpenSquareBracket) || la.Kind==(Delegate) || la.Kind==(Function);
        }

        TypeDeclaration BasicType2()
        {
            // *
            if (la.Kind==(Times))
            {
                Step();
                return new PointerDecl();
            }

            // [ ... ]
            else if (la.Kind==(OpenSquareBracket))
            {
                Step();
                // [ ]
                if (la.Kind==(CloseSquareBracket)) { Step(); return new ClampDecl(); }

                TypeDeclaration ret = null;

                // [ Type ]
                if (IsBasicType())
                    ret = Type();
                else
                {
                    ret = new DExpressionDecl(AssignExpression());

                    // [ AssignExpression .. AssignExpression ]
                    if (la.Kind==(Dot))
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
            else if (la.Kind==(Delegate) || la.Kind==(Function))
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
            if (la.Kind==(OpenParenthesis))
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
                if (la.Kind!=(CloseParenthesis))
                {
                    if (IsParam && la.Kind!=(Identifier))
                    {
                        /* If this Declarator is a parameter of a function, don't expect anything here
                         * exept a '*' that means that here's an anonymous function pointer
                         */
                        if (t.Kind!=(Times))
                            SynErr(Times);
                    }
                    else
                    {
                        Expect(Identifier);
                        ret.Name = t.Value;

                        /*
                         * Just here suffixes can follow!
                         */
                        if (la.Kind!=(CloseParenthesis))
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
                if (IsParam && la.Kind!=(Identifier))
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
            get { return la.Kind==(OpenSquareBracket) || la.Kind==(OpenParenthesis); }
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

            while (la.Kind==(OpenSquareBracket))
            {
                Step();
                ClampDecl ad = new ClampDecl(td);
                if (la.Kind!=(CloseSquareBracket))
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

            if (la.Kind==(OpenParenthesis))
            {
                if (IsTemplateParameterList())
                {
                    TemplateParameters = TemplateParameterList();
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

            if (la.Kind!=(Identifier))
                SynErr(Identifier);

            // Template instancing
            if (lexer.CurrentPeekToken.Kind==(Not))
                td = TemplateInstance();

            // Identifier
            else
            {
                Step();
                td = new NormalDeclaration(t.Value);
            }

            // IdentifierList
            while (la.Kind==(Dot))
            {
                Step();
                DotCombinedDeclaration dcd = new DotCombinedDeclaration(td);
                // Template instancing
                if (lexer.CurrentPeekToken.Kind==(Not))
                    dcd.AccessedMember = TemplateInstance();
                // Identifier
                else
                {
                    Expect(Identifier);
                    dcd.AccessedMember = new NormalDeclaration(t.Value);
                }
                td = dcd;
            }

            return td;
        }

        bool IsStorageClass
        {
            get
            {
                return la.Kind==(Abstract) ||
            la.Kind==(Auto) ||
            ((MemberFunctionAttribute[la.Kind]) && lexer.CurrentPeekToken.Kind!=(OpenParenthesis)) ||
            la.Kind==(Deprecated) ||
            la.Kind==(Extern) ||
            la.Kind==(Final) ||
            la.Kind==(Override) ||
            la.Kind==(Scope) ||
            la.Kind==(Static) ||
            la.Kind==(Synchronized);
            }
        }

        TypeDeclaration Type()
        {
            TypeDeclaration td = BasicType();

            if (IsDeclarator2())
            {
                TypeDeclaration ttd = Declarator2();
                if (ttd != null)
                {
                    ttd.Base = td;
                    td = ttd;
                }
            }

            return td;
        }

        bool IsDeclarator2()
        {
            return IsBasicType2() || la.Kind==(OpenParenthesis);
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
            if (la.Kind==(OpenParenthesis))
            {
                Step();
                td = Declarator2();
                Expect(CloseParenthesis);

                // DeclaratorSuffixes
                if (la.Kind==(OpenSquareBracket))
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

            return td;
        }

        /// <summary>
        /// Parse parameters
        /// </summary>
        List<DNode> Parameters()
        {
            List<DNode> ret = new List<DNode>();
            Expect(OpenParenthesis);

            // Empty parameter list
            if (la.Kind==(CloseParenthesis))
            {
                Step();
                return ret;
            }

            if (!IsTripleDot())
                ret.Add(Parameter());

            while (la.Kind==(Comma))
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
                bool HadComma = t.Kind==(Comma);

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
            return la.Kind==(Dot) && lexer.CurrentPeekToken.Kind==(Dot) && Peek().Kind == Dot;
        }

        private DNode Parameter()
        {
            List<int> attr = new List<int>();
            if(IsInOut())
            {
                Step();
                attr.Add(t.Kind);
            }

            if (MemberFunctionAttribute[la.Kind] && !PK(OpenParenthesis))
            {
                Step();
                attr.Add(t.Kind);
            }

            TypeDeclaration td = BasicType();

            DNode ret = Declarator(true);
            if (attr.Count > 0) ret.Attributes.AddRange(attr);
            if (ret.Type == null)
                ret.Type = td;
            else
                ret.Type.Base = td;

            // DefaultInitializerExpression
            if (la.Kind==(Assign))
            {
                Step();
                DExpression defInit = null;
                if (la.Kind==(Identifier) && (la.Value == "__FILE__" || la.Value == "__LINE__"))
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
            return la.Kind==(In) || la.Kind==(Out) || la.Kind==(Ref) || la.Kind==(Lazy);
        }


        private DExpression Initializer()
        {
            Expect(Assign);

            // VoidInitializer
            if (la.Kind==(Void))
            {
                Step();
                return new TokenExpression(Void);
            }

            return NonVoidInitializer();
        }

        DExpression NonVoidInitializer()
        {
            // ArrayInitializer | StructInitializer
            if (la.Kind==(OpenSquareBracket) || la.Kind==(OpenCurlyBrace))
            {
                Step();
                bool IsStructInit = t.Kind==(OpenCurlyBrace);
                if (IsStructInit ? la.Kind==(CloseCurlyBrace) : la.Kind==(CloseSquareBracket))
                {
                    Step();
                    return new ClampExpression(IsStructInit ? ClampExpression.ClampType.Curly : ClampExpression.ClampType.Square);
                }

                // ArrayMemberInitializations
                ArrayExpression ae = new ArrayExpression(IsStructInit ? ClampExpression.ClampType.Curly : ClampExpression.ClampType.Square);
                DExpression element = null;

                bool IsInit = true;
                while (IsInit || la.Kind==(Comma))
                {
                    if (!IsInit) Step();
                    IsInit = false;

                    // Allow empty post-comma expression IF the following token finishes the initializer expression
                    // int[] a=[1,2,3,4,];
                    if (la.Kind == (IsStructInit ? CloseCurlyBrace : CloseSquareBracket))
                        break;

                    if (IsStructInit)
                    {
                        // Identifier : NonVoidInitializer
                        if (la.Kind==(Identifier) && lexer.CurrentPeekToken.Kind==(Colon))
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
                        bool HasBeenAssExpr = !(t.Kind==(CloseSquareBracket) || t.Kind==(CloseCurlyBrace));

                        // AssignExpression : NonVoidInitializer
                        if (HasBeenAssExpr && la.Kind==(Colon))
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

                Expect(IsStructInit? CloseCurlyBrace:CloseSquareBracket);
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
            if (la.Kind == (Return))
            {
                Step();
                md.InnerType = new DTokenDeclaration(Return);
            }
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

            if (la.Kind==(Comma))
            {
                Step();
                ArgumentList();
            }
            Expect(CloseParenthesis);
        }

        bool IsAttributeSpecifier()
        {
            return (la.Kind==(Extern) || la.Kind==(Export) || la.Kind==(Align) || la.Kind==(Pragma) || la.Kind==(Deprecated) || IsProtectionAttribute()
                || la.Kind==(Static) || la.Kind==(Final) || la.Kind==(Override) || la.Kind==(Abstract) || la.Kind==(Scope) || la.Kind==(__gshared)
                || ((la.Kind==(Auto) || MemberFunctionAttribute[la.Kind]) && (lexer.CurrentPeekToken.Kind!=(OpenParenthesis) && lexer.CurrentPeekToken.Kind!=(Identifier)))
                || Attributes[la.Kind]);
        }

        bool IsProtectionAttribute()
        {
            return la.Kind==(Public) || la.Kind==(Private) || la.Kind==(Protected) || la.Kind==(Extern) || la.Kind==(Package);
        }

        private void AttributeSpecifier()
        {
            int attr = la.Kind;
            if (la.Kind==(Extern) && lexer.CurrentPeekToken.Kind==(OpenParenthesis))
            {
                Step();
                Step();
                while (!IsEOF && la.Kind!=(CloseParenthesis))
                    Step();
                Expect(CloseParenthesis);
            }
            else if (la.Kind==(Align) && lexer.CurrentPeekToken.Kind==(OpenParenthesis))
            {
                Step();
                Step();
                Expect(Literal);
                Expect(CloseParenthesis);
            }
            else if (la.Kind==(Pragma))
                _Pragma();
            else
                Step();

            if (la.Kind==(Colon))
            {
                BlockAttributes.Push(attr);
                Step();
            }

            else DeclarationAttributes.Push(attr);
        }
        #endregion

        #region Expressions
        DExpression Expression()
        {
            // AssignExpression
            DExpression ass = AssignExpression();
            if (la.Kind!=(Comma))
                return ass;

            /*
             * The following is a leftover of C syntax and proably cause some errors when parsing arguments etc.
             */
            // AssignExpression , Expression
            ArrayExpression ae = new ArrayExpression(ClampExpression.ClampType.Round);
            ae.Expressions.Add(ass);
            while (la.Kind==(Comma))
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
                // uint[]** MyArray;
                if (!BasicTypes[la.Kind])
                {
                    // Skip initial dot
                    if (la.Kind == Dot)
                        Step();

                    if (lexer.CurrentPeekToken.Kind != Identifier)
                    {
                        if (la.Kind == Identifier)
                        {
                            // Skip initial identifier list
                            bool init = true;
                            bool HadTemplateInst = false;
                            while (init || lexer.CurrentPeekToken.Kind == (Dot))
                            {
                                HadTemplateInst = false;
                                if (!init) Peek();
                                init = false;

                                if (lexer.CurrentPeekToken.Kind == (Not))
                                {
                                    HadTemplateInst = true;
                                    Peek();
                                    if (lexer.CurrentPeekToken.Kind == (OpenParenthesis))
                                        OverPeekBrackets(OpenParenthesis);
                                    else Peek();
                                }
                            }
                            if (!init && !HadTemplateInst && lexer.CurrentPeekToken.Kind == (Identifier)) Peek();
                        }
                        else if (la.Kind == (Typeof) || MemberFunctionAttribute[la.Kind])
                        {
                            if (lexer.CurrentPeekToken.Kind == (OpenParenthesis))
                                OverPeekBrackets(OpenParenthesis);
                        }
                    }
                }
                // Skip basictype2's
                while (lexer.CurrentPeekToken.Kind==(Times) || lexer.CurrentPeekToken.Kind==(OpenSquareBracket))
                {
                    if (lexer.CurrentPeekToken.Kind==(OpenSquareBracket))
                        OverPeekBrackets(OpenSquareBracket);
                    else Peek();
                }

                // And now, after having skipped the basictype and possible trailing basictype2's,
                // we check for an identifier or delegate declaration to ensure that there's a declaration and not an expression
                if (lexer.CurrentPeekToken.Kind==(Identifier) || lexer.CurrentPeekToken.Kind==(Delegate) || lexer.CurrentPeekToken.Kind==(Function))
                {
                    Peek(1);
                    return false;
                }
            }
            else if (IsStorageClass)
                return false;

            Peek(1);
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
            if (la.Kind!=(Question))
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
            if (!(la.Kind==(LogicalOr) || la.Kind==(LogicalAnd) || la.Kind==(BitwiseOr) || la.Kind==(BitwiseAnd) || la.Kind==(Xor)))
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
                la.Kind==(Equal) || la.Kind==(NotEqual) ||
                // IdentityExpression | InExpression
                la.Kind==(Is) || la.Kind==(In) || (la.Kind==(Not) && (lexer.CurrentPeekToken.Kind==(Is) || lexer.CurrentPeekToken.Kind == In)) ||
                // ShiftExpression
                la.Kind==(ShiftLeft) || la.Kind==(ShiftRight) || la.Kind==(ShiftRightUnsigned);

            if (!CanProceed)
                return left;

            // If we have a !in or !is
            if (la.Kind==(Not)) Step();
            Step();
            AssignTokenExpression ae = new AssignTokenExpression(t.Kind);
            ae.PrevExpression = left;
            // When a shift expression occurs, an AddExpression is required to follow
            if (t.Kind==(ShiftLeft) || t.Kind==(ShiftRight) || t.Kind==(ShiftRightUnsigned))
                ae.FollowingExpression = AddExpression();
            else
                ae.FollowingExpression = OrOrExpression();
            return ae;
        }

        private DExpression AddExpression()
        {
            DExpression left = MulExpression();

            if (!(la.Kind==(Plus) || la.Kind==(Minus) || la.Kind==(Tilde)))
                return left;

            Step();
            AssignTokenExpression ae = new AssignTokenExpression(t.Kind);
            ae.PrevExpression = left;
            ae.FollowingExpression = AddExpression();
            return ae;
        }

        DExpression MulExpression()
        {
            DExpression left = PowExpression();

            if (!(la.Kind==(Times) || la.Kind==(Div) || la.Kind==(Mod)))
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

            if (!(la.Kind==(Pow)))
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
            if (la.Kind==(Cast))
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

            if (la.Kind==(BitwiseAnd) || la.Kind==(Increment) || la.Kind==(Decrement) || la.Kind==(Times) || la.Kind==(Minus) || la.Kind==(Plus) ||
                la.Kind==(Not) || la.Kind==(Tilde))
            {
                Step();
                AssignTokenExpression ae = new AssignTokenExpression(t.Kind);
                ae.FollowingExpression = UnaryExpression();
                return ae;
            }

            // NewExpression
            if (la.Kind==(New))
                return NewExpression();

            // DeleteExpression
            if (la.Kind==(Delete))
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
            if (la.Kind==(OpenParenthesis))
            {
                Step();
                if (la.Kind==(CloseParenthesis))
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
            if (la.Kind==(Class))
            {
                Step();
                DExpression ex2 = new TokenExpression(Class);
                ex2.Base = ex;
                ex = ex2;

                // ClassArguments
                if (la.Kind==(OpenParenthesis))
                {
                    if (la.Kind==(CloseParenthesis))
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
                if (la.Kind==(Colon))
                {
                    //TODO : Add base classes to expression somehow ;-)
                    BaseClassList();
                }

                DBlockStatement _block = new DClassLike();
                _block.fieldtype = FieldType.Class;
                ClassBody(ref _block);

                return ex;
            }

            // NewArguments Type
            else
            {
                ex = new TypeDeclarationExpression(BasicType());

                while (la.Kind==(OpenSquareBracket))
                {
                    Step();
                    ClampExpression ce = new ClampExpression();
                    ce.Base = ex;
                    if(la.Kind!=CloseSquareBracket)
                        ce.InnerExpression = AssignExpression();
                    ex = ce;
                    Expect(CloseSquareBracket);
                }
                
                if (la.Kind==(OpenParenthesis))
                {
                    Step();
                    ArrayExpression ae = new ArrayExpression(ClampExpression.ClampType.Round);
                    ae.Base = ex;
                    if(la.Kind!=(CloseParenthesis))
                        ae.Expressions = ArgumentList();
                    ex = ae;
                    Expect(CloseParenthesis);
                }
            }
            return ex;
        }

        List<DExpression> ArgumentList()
        {
            List<DExpression> ret = new List<DExpression>();

            ret.Add(AssignExpression());

            while (la.Kind==(Comma))
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
            while (la.Kind==(Dot) || la.Kind==(Increment) || la.Kind==(Decrement) || la.Kind==(OpenParenthesis) || la.Kind==(OpenSquareBracket))
            {
                // Function call
                if (la.Kind==(OpenParenthesis))
                {
                    Step();
                    ArrayExpression ae = new ArrayExpression(ClampExpression.ClampType.Round);
                    ae.Base = retEx;
                    if (la.Kind!=(CloseParenthesis))
                        ae.Expressions = ArgumentList();
                    Step();

                    retEx = ae;
                }

                // IndexExpression | SliceExpression
                else if (la.Kind==(OpenSquareBracket))
                {
                    Step();

                    if (la.Kind!=(CloseSquareBracket))
                    {
                        DExpression firstEx = AssignExpression();
                        // [ AssignExpression .. AssignExpression ]
                        if (la.Kind==(Dot) && lexer.CurrentPeekToken.Kind==(Dot))
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
                        else if (la.Kind==(Comma))
                        {
                            ArrayExpression ae = new ArrayExpression();
                            ae.Expressions.Add(firstEx);
                            while (la.Kind==(Comma))
                            {
                                Step();
                                ae.Expressions.Add(AssignExpression());
                            }
                        }
                        else if (la.Kind!=(CloseSquareBracket))
                            SynErr(CloseSquareBracket);
                    }

                    Expect(CloseSquareBracket);
                }

                else if (la.Kind==(Dot))
                {
                    // Ignore slicing operators
                    if (PK(Dot))
                        break;
                    Step();
                    AssignTokenExpression ae = new AssignTokenExpression(Dot);
                    ae.PrevExpression = retEx;
                    if (la.Kind==(New))
                        ae.FollowingExpression = NewExpression();
                    else if (la.Kind==(Identifier))
                        ae.FollowingExpression = PrimaryExpression();
                    else
                        SynErr(Identifier, "Identifier or new expected");

                    retEx = ae;
                }
                else if (la.Kind==(Increment) || la.Kind==(Decrement))
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
            if (la.Kind == Dot)
                Step();

            // Handle token string here
            if (la.Kind == Identifier && la.Value == "q" && lexer.CurrentPeekToken.Kind == OpenCurlyBrace)
            {
                Step();
                Step();
                string val = "";

                while (!IsEOF && la.Kind != CloseCurlyBrace)
                {
                    Step();
                    val += t.ToString();
                }

                Expect(CloseCurlyBrace);
                return new IdentExpression(val);
            }

            // TemplateInstance
            if (la.Kind==(Identifier) && lexer.CurrentPeekToken.Kind==(Not))
                return new TypeDeclarationExpression(TemplateInstance());

            // Identifier
            if (la.Kind==(Identifier))
            {
                Step();
                return new IdentExpression(t.Value);
            }

            // SpecialTokens (this,super,null,true,false,$)
            if (la.Kind==(This) || la.Kind==(Super) || la.Kind==(Null) || la.Kind==(True) || la.Kind==(False) || la.Kind==(Dollar))
            {
                Step();
                return new TokenExpression(t.Kind);
            }

            // Literal
            if (la.Kind==(Literal))
            {
                Step();
                // Concatenate multiple string literals here
                string a = "";
                while (la.LiteralFormat == LiteralFormat.StringLiteral || la.LiteralFormat == LiteralFormat.StringLiteral)
                {
                    Step();
                    a += la.Value;
                }
                return new IdentExpression(String.IsNullOrEmpty(a)?t.LiteralValue:a);
            }

            // ArrayLiteral | AssocArrayLiteral
            if (la.Kind==(OpenSquareBracket))
            {
                Step();
                ArrayExpression arre = new ArrayExpression();

                if (LA(CloseSquareBracket)) { Step(); return arre; }

                DExpression firstCondExpr = ConditionalExpression();
                // Can be an associative array only
                if (la.Kind==(Colon))
                {
                    Step();
                    AssignTokenExpression ae = new AssignTokenExpression(Colon);
                    ae.PrevExpression = firstCondExpr;
                    ae.FollowingExpression = ConditionalExpression();
                    arre.Expressions.Add(ae);

                    while (la.Kind==(Comma))
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

                    while (la.Kind==(Comma))
                    {
                        Step();
                        arre.Expressions.Add(AssignExpression());
                    }
                }

                Expect(CloseSquareBracket);
                return arre;
            }

            // FunctionLiteral
            if (la.Kind==Delegate || la.Kind==Function|| la.Kind == OpenCurlyBrace || (la.Kind==OpenParenthesis && IsFunctionLiteral()))
            {
                FunctionLiteral fl = new FunctionLiteral();

                if (la.Kind == Delegate || la.Kind == Function)
                {
                    Step();
                    fl.LiteralToken = t.Kind;
                }

                if (la.Kind != OpenParenthesis && la.Kind != OpenCurlyBrace)
                    fl.AnonymousMethod.Type = Type();

                if (la.Kind == OpenParenthesis)
                    fl.AnonymousMethod.Parameters = Parameters();

                DBlockStatement dbs = fl.AnonymousMethod as DBlockStatement;
                FunctionBody(ref dbs);
                return fl;
            }

            // AssertExpression
            if (la.Kind==(Assert))
            {
                Step();
                Expect(OpenParenthesis);
                ClampExpression ce = new ClampExpression(ClampExpression.ClampType.Round);
                ce.FrontExpression = new TokenExpression(Assert);
                ce.InnerExpression = AssignExpression();

                if (la.Kind==(Comma))
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
            if (la.Kind==(Mixin) || la.Kind==(Import))
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
            if (la.Kind==(Typeof))
            {
                return new TypeDeclarationExpression(TypeOf());
            }

            // TypeidExpression
            if (la.Kind==(Typeid))
            {
                //TODO
            }
            // IsExpression
            if (la.Kind==(Is))
            {
                Step();
                Expect(OpenParenthesis);
                ClampExpression ce = new ClampExpression(ClampExpression.ClampType.Round);
                ce.FrontExpression = new TokenExpression(Is);

                AssignTokenExpression ate = new AssignTokenExpression();
                ce.InnerExpression=ate;

                // Type is required!
                ate.PrevExpression = new TypeDeclarationExpression( Type());

                if(la.Kind==CloseParenthesis)
                {
                    Step();
                    return ce;
                }

                // Ignore optional following identifier
                //TODO: Make this identifier become a type which can be used to declare something in the following block
                /*
                 * alias int A;
                 * static if(is(A myInt == int))
                 * {
                 *      myInt i=0; // allowed here!
                 * }
                 */
                if (la.Kind == Identifier)
                    Step();

                // Require a == or : as operator!
                if (!(la.Kind == Colon || la.Kind == Equal))
                    SynErr(Equal);
                else
                {
                    Step();
                    ate.Token = t.Kind;
                }

                if (ClassLike[la.Kind] || LA(Enum) || LA(Delegate) || LA(Function) || LA(Super) || LA(Shared)
                    || (MemberFunctionAttribute[la.Kind] && !PK(OpenParenthesis)))
                {
                    Step();
                    ate.FollowingExpression = new TokenExpression(t.Kind);
                }
                else
                    ate.FollowingExpression = new TypeDeclarationExpression(Type());

                if (la.Kind == Comma)
                {
                    Step();
                    TemplateParameterList(false);
                }

                Expect(CloseParenthesis);
                return ce;
            }
            // ( Expression )
            if (la.Kind==(OpenParenthesis))
            {
                Step();
                DExpression ret = Expression();
                Expect(CloseParenthesis);
                return ret;
            }

            // TraitsExpression
            if (la.Kind==(__traits))
                return TraitsExpression();

            // BasicType . Identifier
            if (la.Kind==(Const) || la.Kind==(Immutable) || la.Kind==(Shared) || la.Kind==(InOut) || BasicTypes[la.Kind])
            {
                Step();
                DExpression before = null;
                if (!BasicTypes[t.Kind])
                {
                    int tk = t.Kind;
                    Expect(OpenParenthesis);
                    ClampExpression ce = new ClampExpression(ClampExpression.ClampType.Round);
                    ce.FrontExpression = new TokenExpression(tk);
                    ce.InnerExpression = new TypeDeclarationExpression(Type());
                    Expect(CloseParenthesis);
                    before = ce;
                }
                else
                    before = new TokenExpression(t.Kind);

                if (la.Kind==(Dot))
                {
                    Step();
                    Expect(Identifier);
                    AssignTokenExpression ate = new AssignTokenExpression(Dot);
                    ate.PrevExpression = before;
                    ate.FollowingExpression = new IdentExpression(t.Value);
                    return ate;
                }
                return before;
            }

            SynErr(Identifier);
            Step();
            return new TokenExpression(t.Kind);
        }

        bool IsFunctionLiteral()
        {
            if (la.Kind != OpenParenthesis)
                return false;

            OverPeekBrackets(OpenParenthesis,true);

            return lexer.CurrentPeekToken.Kind == OpenCurlyBrace;
        }
        #endregion

        #region Statements
        void Statement(ref DBlockStatement par, bool CanBeEmpty, bool BlocksAllowed)
        {
            if (CanBeEmpty && la.Kind == (Semicolon))
            {
                Step();
                return;
            }

            else if (BlocksAllowed && la.Kind == (OpenCurlyBrace))
            {
                BlockStatement(ref par);
                return;
            }

            // LabeledStatement
            else if (la.Kind == (Identifier) && lexer.CurrentPeekToken.Kind == (Colon))
            {
                Step();
                Step();
                return;
            }

            // IfStatement
            else if (la.Kind == (If) || (la.Kind == Static && lexer.CurrentPeekToken.Kind == If))
            {
                if (la.Kind == Static)
                    Step();
                Step();
                DBlockStatement dbs = new DBlockStatement();
                dbs.StartLocation = t.Location;
                Expect(OpenParenthesis);

                // IfCondition
                if (la.Kind == (Auto))
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
                if (la.Kind == (Else))
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
            else if (la.Kind == (While))
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
            else if (la.Kind == (Do))
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
            else if (la.Kind == (For))
            {
                Step();

                DBlockStatement dbs = new DBlockStatement();
                dbs.StartLocation = t.Location;

                Expect(OpenParenthesis);

                // Initialize
                if (la.Kind == (Semicolon))
                    Step();
                else
                    Statement(ref dbs, false, true);

                // Test
                if (la.Kind != (Semicolon))
                    Expression();

                Expect(Semicolon);

                // Increment
                if (la.Kind != (CloseParenthesis))
                    Expression();

                Expect(CloseParenthesis);

                Statement(ref dbs, false, true);
                dbs.EndLocation = t.EndLocation;
                if (dbs.Count > 0) par.Add(dbs);
            }

            // ForeachStatement
            else if (la.Kind == (Foreach) || la.Kind == (Foreach_Reverse))
            {
                Step();

                DBlockStatement dbs = new DBlockStatement();
                dbs.StartLocation = t.Location;

                Expect(OpenParenthesis);

                bool init = true;
                while (init || la.Kind == (Comma))
                {
                    if (!init) Step();
                    init = false;

                    DVariable forEachVar = new DVariable();
                    forEachVar.StartLocation = la.Location;

                    if (la.Kind == (Ref))
                    {
                        Step();
                        forEachVar.Attributes.Add(Ref);
                    }
                    if (la.Kind == (Identifier) && (lexer.CurrentPeekToken.Kind == (Semicolon) || lexer.CurrentPeekToken.Kind == Comma))
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
                if (la.Kind == (Dot) && lexer.CurrentPeekToken.Kind == (Dot))
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
            else if ((la.Kind == (Final) && lexer.CurrentPeekToken.Kind == (Switch)) || la.Kind == (Switch))
            {
                DBlockStatement dbs = new DBlockStatement();
                dbs.StartLocation = la.Location;

                if (la.Kind == (Final))
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
            else if (la.Kind == (Case))
            {
                Step();

                DBlockStatement dbs = new DBlockStatement();
                dbs.StartLocation = la.Location;

                AssignExpression();

                if (!(la.Kind == (Colon) && lexer.CurrentPeekToken.Kind == (Dot) && Peek().Kind == Dot))
                    while (la.Kind == (Comma))
                        AssignExpression();

                Expect(Colon);

                // CaseRangeStatement
                if (la.Kind == (Dot) && lexer.CurrentPeekToken.Kind == (Dot))
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
            else if (la.Kind == (Default))
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
            else if (la.Kind == (Continue) || la.Kind == (Break))
            {
                Step();
                if (la.Kind == (Identifier))
                    Step();
                Expect(Semicolon);
            }

            // Return
            else if (la.Kind == (Return))
            {
                Step();
                if (la.Kind != (Semicolon))
                    Expression();
                Expect(Semicolon);
            }

            // Goto
            else if (la.Kind == (Goto))
            {
                Step();
                if (la.Kind == (Identifier) || la.Kind == (Default))
                {
                    Step();
                }
                else if (la.Kind == (Case))
                {
                    Step();
                    if (la.Kind != (Semicolon))
                        Expression();
                }

                Expect(Semicolon);
            }

            // WithStatement
            else if (la.Kind == (With))
            {
                Step();

                DBlockStatement dbs = new DBlockStatement();
                dbs.StartLocation = t.Location;

                Expect(OpenParenthesis);

                // Symbol
                if (la.Kind == (Identifier))
                    IdentifierList();
                else
                    Expression();

                Expect(CloseParenthesis);
                Statement(ref dbs, false, true);
                dbs.EndLocation = t.EndLocation;

                if (dbs.Count > 0) par.Add(dbs);
            }

            // SynchronizedStatement
            else if (la.Kind == (Synchronized))
            {
                Step();
                DBlockStatement dbs = new DBlockStatement();
                dbs.StartLocation = t.Location;

                if (la.Kind == (OpenParenthesis))
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
            else if (la.Kind == (Try))
            {
                Step();

                DBlockStatement dbs = new DBlockStatement();
                dbs.StartLocation = t.Location;
                Statement(ref dbs, false, true);
                dbs.EndLocation = t.EndLocation;
                if (dbs.Count > 0) par.Add(dbs);

                if (!(la.Kind == (Catch) || la.Kind == (Finally)))
                    SynErr(Catch, "catch or finally expected");

                // Catches
            do_catch:
                if (la.Kind == (Catch))
                {
                    Step();
                    dbs = new DBlockStatement();
                    dbs.StartLocation = t.Location;

                    // CatchParameter
                    if (la.Kind == (OpenParenthesis))
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

                    if (la.Kind == (Catch))
                        goto do_catch;
                }

                if (la.Kind == (Finally))
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
            else if (la.Kind == (Throw))
            {
                Step();
                Expression();
                Expect(Semicolon);
            }

            // ScopeGuardStatement
            else if (la.Kind == (Scope))
            {
                Step();
                Expect(OpenParenthesis);
                Expect(Identifier); // exit, failure, success
                Expect(CloseParenthesis);
                Statement(ref par, false, true);
            }

            // AsmStatement
            else if (la.Kind == (Asm))
            {
                Step();
                Expect(OpenCurlyBrace);

                while (!IsEOF && la.Kind != (CloseCurlyBrace))
                {
                    Step();
                }

                Expect(CloseCurlyBrace);
            }

            // PragmaStatement
            else if (la.Kind == (Pragma))
            {
                _Pragma();
                Statement(ref par, true, true);
            }

            // MixinStatement
            //TODO: Handle this one in terms of adding it to the node structure
            else if (la.Kind == (Mixin))
            {
                    MixinDeclaration();
            }

            // (Static) AssertExpression
            else if (la.Kind==Assert || (la.Kind == Static && PK(Assert)))
            {
                if (LA(Static))
                    Step();
                AssignExpression();
                Expect(Semicolon);
            }

                // Blockstatement
            else if (la.Kind == (OpenCurlyBrace))
                BlockStatement(ref par);

            else if (!(ClassLike[la.Kind] || la.Kind==Enum || MemberFunctionAttribute[la.Kind] || la.Kind == Alias) && IsAssignExpression())
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

            if (ParseStructureOnly)
                lexer.SkipCurrentBlock();
            else
                while (!IsEOF && la.Kind != (CloseCurlyBrace))
                {
                    Statement(ref par, true, true);
                }
            Expect(CloseCurlyBrace);
            par.EndLocation = t.EndLocation;
        }
        #endregion

        #region Structs & Unions
        private DNode AggregateDeclaration()
        {
            if (!(la.Kind==(Union) || la.Kind==(Struct)))
                SynErr(t.Kind, "union or struct required");
            Step();

            DBlockStatement ret = new DClassLike();
            ret.fieldtype = FieldType.Struct;
            ret.Type = new DTokenDeclaration(t.Kind);
            ret.TypeToken = t.Kind;

            Expect(Identifier);
            ret.Name = t.Value;

            if (la.Kind==(Semicolon))
            {
                Step();
                return ret;
            }

            // StructTemplateDeclaration
            if (la.Kind==(OpenParenthesis))
            {
                ret.TemplateParameters = TemplateParameterList();

                // Constraint[opt]
                if (la.Kind==(If))
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
            DBlockStatement dc = new DClassLike();
            dc.StartLocation = t.Location;

            Expect(Identifier);
            dc.Name = t.Value;

            if (la.Kind==(OpenParenthesis))
                dc.TemplateParameters = TemplateParameterList();

            if (la.Kind==(Colon))
                (dc as DClassLike).BaseClasses = BaseClassList();

            ClassBody(ref dc);

            dc.EndLocation = t.EndLocation;
            return dc;
        }

        private List<TypeDeclaration> BaseClassList()
        {
            Expect(Colon);

            List<TypeDeclaration> ret = new List<TypeDeclaration>();

            bool init = true;
            while (init || la.Kind==(Comma))
            {
                if (!init) Step();
                init = false;
                if (IsProtectionAttribute() && la.Kind!=(Protected))
                    Step();

                ret.Add(IdentifierList());
            }
            return ret;
        }

        private void ClassBody(ref DBlockStatement ret)
        {
            Expect(OpenCurlyBrace);
            ret.BlockStartLocation = t.Location;
            while (!IsEOF && la.Kind!=(CloseCurlyBrace))
            {
                DeclDef(ref ret);
            }
            Expect(CloseCurlyBrace);
            ret.EndLocation = t.EndLocation;
        }

        DNode Constructor(bool IsStruct)
        {
            Expect(This);
            DMethod dm = new DMethod();
            dm.Type = new NormalDeclaration("this");
            dm.Name = "this";

            if (IsStruct && lexer.CurrentPeekToken.Kind==(This) && la.Kind==(OpenParenthesis))
            {
                DVariable dv = new DVariable();
                dv.Name = "this";
                dm.Parameters.Add(dv);
                Step();
                Step();
                Expect(CloseParenthesis);
            }
            else
            {
                if (IsTemplateParameterList())
                    dm.TemplateParameters = TemplateParameterList();

                dm.Parameters = Parameters();
            }

            if (la.Kind==(If))
                Constraint();

            DBlockStatement dm_ = dm as DBlockStatement;
            FunctionBody(ref dm_);
            return dm;
        }

        DNode Destructor()
        {
            Expect(Tilde);
            Expect(This);
            DMethod dm = new DMethod();
            dm.Type = new NormalDeclaration("~this");
            dm.Name = "~this";

            if (IsTemplateParameterList())
                dm.TemplateParameters = TemplateParameterList();

            dm.Parameters = Parameters();

            if (la.Kind==(If))
                Constraint();

            DBlockStatement dm_ = dm as DBlockStatement;
            FunctionBody(ref dm_);
            return dm;
        }
        #endregion

        #region Interfaces
        private DBlockStatement InterfaceDeclaration()
        {
            Expect(Interface);
            DBlockStatement dc = new DClassLike();
            dc.StartLocation = t.Location;
            DNode n = dc as DNode;
            ApplyAttributes(ref n);

            Expect(Identifier);
            dc.Name = t.Value;

            if (la.Kind==(OpenParenthesis))
                dc.TemplateParameters = TemplateParameterList();

            if (la.Kind==(If))
                Constraint();

            if (la.Kind==(Colon))
                (dc as DClassLike).BaseClasses = BaseClassList();

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
        private void EnumDeclaration(ref DBlockStatement par)
        {
            Expect(Enum);

            DEnum mye = new DEnum();
            mye.StartLocation = t.Location;
            DNode n = mye as DNode;
            ApplyAttributes(ref n);

            if (IsBasicType() && la.Kind != Identifier)
                mye.EnumBaseType = Type();

            if (la.Kind==(Identifier))
            {
                if (lexer.CurrentPeekToken.Kind==(Assign) || lexer.CurrentPeekToken.Kind==(OpenCurlyBrace) || lexer.CurrentPeekToken.Kind==(Semicolon) || lexer.CurrentPeekToken.Kind==Colon)
                {
                    Step();
                    mye.Name = t.Value;
                }
                else
                {
                    mye.EnumBaseType = Type();

                    Expect(Identifier);
                    mye.Name = t.Value;
                }
            }

            if (la.Kind==(Colon))
            {
                Step();
                mye.EnumBaseType = Type();
            }

            if (la.Kind==(Assign) || la.Kind==(Semicolon))
            {
            another_enumvalue:
                DVariable enumVar = new DVariable();
                enumVar.Assign(mye);
                if (mye.EnumBaseType != null)
                    enumVar.Type = mye.EnumBaseType;
                else
                    enumVar.Type = new DTokenDeclaration(Enum);

                if (la.Kind==(Comma))
                {
                    Step();
                    Expect(Identifier);
                    enumVar.Name = t.Value;
                }

                if (la.Kind==(Assign))
                {
                    Step();
                    enumVar.Initializer = AssignExpression();
                }
                enumVar.EndLocation = t.Location;
                par.Add(enumVar);

                if (la.Kind==(Comma))
                    goto another_enumvalue;

                Expect(Semicolon);
            }
            else
            {
                Expect(OpenCurlyBrace);
                mye.BlockStartLocation = t.Location;

                bool init = true;
                while ((init && la.Kind!=(Comma)) || la.Kind==(Comma))
                {
                    if (!init) Step();
                    init = false;

                    if (la.Kind==(CloseCurlyBrace)) break;

                    DEnumValue ev = new DEnumValue();
                    ev.StartLocation = t.Location;
                    if (la.Kind==(Identifier) && (lexer.CurrentPeekToken.Kind==(Assign) || lexer.CurrentPeekToken.Kind==(Comma) || lexer.CurrentPeekToken.Kind==(CloseCurlyBrace)))
                    {
                        Step();
                        ev.Name = t.Value;
                    }
                    else
                    {
                        ev.Type = Type();
                        Expect(Identifier);
                        ev.Name = t.Value;
                    }

                    if (la.Kind==(Assign))
                    {
                        Step();
                        ev.Initializer = AssignExpression();
                    }

                    ev.EndLocation = t.EndLocation;

                    if (String.IsNullOrEmpty(mye.Name))
                        par.Add(ev);
                    else
                        mye.Add(ev);
                }
                Expect(CloseCurlyBrace);
                mye.EndLocation = t.Location;
                if (!String.IsNullOrEmpty(mye.Name))
                    par.Add(mye);
            }
        }
        #endregion

        #region Functions
        void FunctionBody(ref DBlockStatement par)
        {
            bool HadIn = false, HadOut = false;

        check_again:
            if (!HadIn && la.Kind==(In))
            {
                HadIn = true;
                Step();
                BlockStatement(ref par);

                if (!HadOut && la.Kind==(Out))
                    goto check_again;
            }

            if (!HadOut && la.Kind==(Out))
            {
                HadOut = true;
                Step();

                if (la.Kind==(OpenParenthesis))
                {
                    Step();
                    Expect(Identifier);
                    Expect(CloseParenthesis);
                }

                BlockStatement(ref par);

                if (!HadIn && la.Kind==(In))
                    goto check_again;
            }

            if (HadIn || HadOut)
                Expect(Body);
            else if (la.Kind==(Body))
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
            DBlockStatement dc = new DClassLike();
            dc.fieldtype = FieldType.Template;
            DNode n = dc as DNode;
            ApplyAttributes(ref n);
            dc.StartLocation = t.Location;

            Expect(Identifier);
            dc.Name = t.Value;

            dc.TemplateParameters = TemplateParameterList();

            if (la.Kind==(If))
                Constraint();

            if (la.Kind==(Colon))
                (dc as DClassLike).BaseClasses = BaseClassList();

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
            return TemplateParameterList(true);
        }

        private List<DNode> TemplateParameterList(bool MustHaveSurroundingBrackets)
        {
            if(MustHaveSurroundingBrackets) Expect(OpenParenthesis);

            List<DNode> ret = new List<DNode>();

            if (la.Kind==(CloseParenthesis))
            {
                Step();
                return ret;
            }

            bool init = true;
            while (init || la.Kind==(Comma))
            {
                if (!init) Step();
                init = false;

                DNode dv = new DVariable();

                // TemplateThisParameter
                if (la.Kind==(This))
                    Step();

                // TemplateTupleParameter
                if (la.Kind==(Identifier) && lexer.CurrentPeekToken.Kind==(Dot) && Peek().Kind == Dot && Peek().Kind == Dot)
                {
                    Step();
                    dv.Type = new VarArgDecl();
                    dv.Name = t.Value;
                    Step();
                    Step();
                    Step();
                }

                // TemplateAliasParameter
                else if (la.Kind==(Alias))
                {
                    Step();
                    dv.Type = new DTokenDeclaration(Alias);
                    Expect(Identifier);
                    dv.Name = t.Value;

                    // TemplateAliasParameterSpecialization
                    if (la.Kind==(Colon))
                    {
                        Step();

                        dv.Type = new InheritanceDecl(dv.Type);
                        (dv.Type as InheritanceDecl).InheritedClass = Type();
                    }

                    // TemplateAliasParameterDefault
                    if (la.Kind==(Assign))
                    {
                        Step();
                        if (la.Kind == Literal)
                        {
                            Step();
                            (dv as DVariable).Initializer = new IdentExpression(t.Value);
                        }else
                        (dv as DVariable).Initializer = new TypeDeclarationExpression(Type());
                    }
                }

                // TemplateTypeParameter
                else if (la.Kind==(Identifier) && (lexer.CurrentPeekToken.Kind==(Colon) || lexer.CurrentPeekToken.Kind==(Assign) || lexer.CurrentPeekToken.Kind==(Comma) || lexer.CurrentPeekToken.Kind==(CloseParenthesis)))
                {
                    Step();
                    dv.Name = t.Value;

                    if (la.Kind==(Colon))
                    {
                        Step();
                        dv.Type = new InheritanceDecl(dv.Type);
                        (dv.Type as InheritanceDecl).InheritedClass = Type();
                    }

                    if (la.Kind==(Assign))
                    {
                        Step();
                        (dv as DVariable).Initializer = new TypeDeclarationExpression(Type());
                    }
                }

                else
                {
                    TypeDeclaration bt = BasicType();
                    dv = Declarator(false);

                    if (dv.Type == null)
                        dv.Type = bt;
                    else
                        dv.Type.Base = bt;

                    if (la.Kind==(Colon))
                    {
                        Step();
                        ConditionalExpression();
                    }

                    if (la.Kind==(Assign))
                        (dv as DVariable).Initializer = Initializer();
                }
                ret.Add(dv);
            }

            if (MustHaveSurroundingBrackets) Expect(CloseParenthesis);

            return ret;
        }

        private TypeDeclaration TemplateInstance()
        {
            if (la.line == 1013) { }
            Expect(Identifier);
            TemplateDecl td = new TemplateDecl(new NormalDeclaration(t.Value));
            Expect(Not);
            if (la.Kind==(OpenParenthesis))
            {
                Step();
                if (la.Kind != CloseParenthesis)
                {
                    bool init = true;
                    while (init || la.Kind == (Comma))
                    {
                        if (!init) Step();
                        init = false;

                        if (IsAssignExpression())
                            td.Template.Add(new DExpressionDecl(AssignExpression()));
                        else
                            td.Template.Add(Type());
                    }
                }
                Expect(CloseParenthesis);
            }
            else
            {
                Step();
                if (t.Kind==(Identifier) || t.Kind==(Literal))
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