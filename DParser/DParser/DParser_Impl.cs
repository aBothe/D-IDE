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
        void Root()
        {
            Step();

            // Only one module declaration possible possible!
            if (LA(Module))
                ModuleDeclaration();

            // Now only declarations or other statements are allowed!
            while (!IsEOF)
            {
                DeclDef(ref doc);
            }
        }

        void DeclDef(ref DNode par)
        {
            //AttributeSpecifier
            if (IsAttributeSpecifier())
                AttributeSpecifier();

            //ImportDeclaration
            else if (LA(Import))
                ImportDeclaration();

            //EnumDeclaration
            else if (LA(Enum))
                EnumDeclaration();

            //ClassDeclaration
            else if (LA(Class))
                ClassDeclaration();

            //InterfaceDeclaration
            else if (LA(Interface))
                InterfaceDeclaration();

            //AggregateDeclaration
            else if (LA(Struct) || LA(Union))
                AggregateDeclaration();

            //Declaration
            else if (IsDeclaration())
                Declaration(ref par);

            //Constructor
            else if (LA(This))
                Constructor(ref par);

            //Destructor
            else if (LA(Tilde) && LA(This))
                Destructor(ref par);

            //Invariant
            //UnitTest
            //StaticConstructor
            //StaticDestructor
            //SharedStaticConstructor
            //SharedStaticDestructor
            //ConditionalDeclaration
            //StaticAssert
            //TemplateDeclaration
            //TemplateMixin

            //MixinDeclaration
            else if (LA(Mixin))
                MixinDeclaration();

            //;
            else if (LA(Semicolon))
                Step();

            // else:
            else
                SynErr(t.Kind, "Declaration expected");
        }

        void ModuleDeclaration()
        {
            Expect(Module);
            Document.module = ModuleFullyQualifiedName();
            Expect(Semicolon);
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
                    _Import();
                }

            Expect(Semicolon);
        }

        void _Import()
        {
            string imp = "";

            // ModuleAliasIdentifier
            if (PK(Assign))
            {
                Expect(Identifier);
                string ModuleAliasIdentifier = t.Value;
                Step();
            }

            imp = ModuleFullyQualifiedName();
            import.Add(imp);
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
            return false;
        }

        void Declaration(ref DNode par)
        {
            if (LA(Alias))
            {
                Step();
            }
            Decl(ref par);
        }

        void Decl(ref DNode par)
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
                n.name = t.Value;
                Expect(Assign);
                (n as DVariable).Initializer = AssignExpression();
                Expect(Semicolon);
                par.Add(n);
                return;
            }

            // Declarators
            List<DNode> decls = new List<DNode>();

            DNode firstNode = new DVariable();
            firstNode.Type = BasicType();
            TypeDeclaration ttd = Declarator(out firstNode.name);
            if (ttd != null) { ttd.Base = firstNode.Type; firstNode.Type = ttd; }

            bool IsMethod = false; //TODO: Check if declaration contains parameters or not
            if (IsMethod)
            {
                DMethod dm = new DMethod();
                dm.Assign(firstNode);
                firstNode = dm;
            }

            bool ExpectFunctionBody = !(LA(Assign) || LA(Comma) || LA(Semicolon));

            // BasicType Declarators ;
            if (!ExpectFunctionBody)
            {
                // DeclaratorInitializer
                if (LA(Assign))
                    (firstNode as DVariable).Initializer = Initializer();

                par.Add(firstNode);

                // DeclaratorIdentifierList
                while (LA(Comma))
                {
                    Step();
                    Expect(Identifier);

                    DVariable otherNode = new DVariable();
                    otherNode.Assign(firstNode);
                    otherNode.name = t.Value;

                    if (LA(Assign))
                        otherNode.Initializer = Initializer();

                    par.Add(otherNode);
                }

                Expect(Semicolon);
            }

            // BasicType Declarator FunctionBody
            else
            {
                FunctionBody(ref firstNode);
                par.Add(firstNode);
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
                md.Base = Type();
                Expect(CloseParenthesis);
                return md;
            }

            if (LA(Typeof))
            {
                Step();
                Expect(OpenParenthesis);
                MemberFunctionAttributeDecl md = new MemberFunctionAttributeDecl(Typeof);
                td = md;
                if (LA(Return))
                    md.Base = new DTokenDeclaration(Return);
                else
                    md.Base = Expression();
                Expect(CloseParenthesis);

                if (!LA(Dot)) return md;
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
                return new PointerDecl();

            // [ ... ]
            else if (LA(OpenSquareBracket))
            {
                Step();
                // [ ]
                if (LA(CloseSquareBracket)) { Step(); return new ArrayDecl(); }

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

        TypeDeclaration Declarator(out string VarIdent)
        {
            VarIdent = null;

            TypeDeclaration td = null;

            while (IsBasicType2())
            {
                if (td == null) td = BasicType2();
                else { TypeDeclaration td2 = BasicType2(); td2.Base = td; td = td2; }
            }

            Expect(Identifier);
            VarIdent = t.Value;

            // DeclaratorSuffixes

            /*
             * Note:
             * http://www.digitalmars.com/d/2.0/declaration.html#DeclaratorSuffix
             * The definition of a sequence of declarator suffixes is buggy here! Theoretically template parameters can be declared without a surrounding ( and )!
             * Also, more than one parameter sequences are possible!
             * 
             * TemplateParameterList[opt] Parameters MemberFunctionAttributes[opt]
             */
            while (LA(OpenSquareBracket))
            {
                Step();
                ArrayDecl ad=new ArrayDecl(td);
                if (!LA(CloseSquareBracket))
                {
                    if (IsAssignExpression())
                        ad.KeyType = new DExpressionDecl(AssignExpression());
                    else
                        ad.KeyType = Type();

                    Expect(CloseSquareBracket);
                }
            }

            if (LA(OpenParenthesis))
            {
                if (IsTemplateParameterList())
                {
                    Step();
                    TemplateParameterList();
                    Expect(CloseParenthesis);
                }
                Parameters();
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

            while (LA(Dot))
            {
                Step();

                // Template instancing
                if (PK(Not))
                    td = TemplateInstance();
                // Identifier
                else
                {
                    td = new NormalDeclaration(t.Value);
                }
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
            throw new NotImplementedException();
        }

        List<DNode> Parameters()
        {
            throw new NotImplementedException();
        }

        bool IsInOut()
        {
            return LA(In) || LA(Out) || LA(Ref) || LA(InOut);
        }


        private DExpression Initializer()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Attributes
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
            throw new NotImplementedException();
        }
        #endregion

        #region Expressions
        TypeDeclaration Expression()
        {
            throw new NotImplementedException();
        }

        bool IsAssignExpression()
        {
            return false;
        }

        DExpression AssignExpression()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Statements

        #endregion

        #region Structs & Unions
        private void AggregateDeclaration()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Classes
        private void ClassDeclaration()
        {
            throw new NotImplementedException();
        }

        void Constructor(ref DNode par)
        {

        }

        void Destructor(ref DNode par)
        {

        }
        #endregion

        #region Interfaces
        private void InterfaceDeclaration()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Enums
        private void EnumDeclaration()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Functions
        void FunctionBody(ref DNode par)
        {

        }
        #endregion

        #region Templates

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
                    if (r < 0 && Peek().Kind == OpenParenthesis)
                        return true;
                }
                Peek();
            }
            return false;
        }

        private List<DNode> TemplateParameterList()
        {
            throw new NotImplementedException();
        }

        private TypeDeclaration TemplateInstance()
        {
            throw new NotImplementedException();
        }
        #endregion
    }

}