using System;
using System.Collections.Generic;
using System.Text;

namespace D_Parser
{
    public abstract class TypeDeclaration
    {
        public abstract byte TypeId { get; }

        public TypeDeclaration Base;
        public TypeDeclaration ParentDecl { get { return Base; } set { Base = value; } }

        public TypeDeclaration MostBasic
        {
            get { if (Base == null) return this; else return Base.MostBasic; }
        }

        /// <summary>
        /// Returns a string which represents the current type
        /// </summary>
        /// <returns></returns>
        public new abstract string ToString();
    }

    /// <summary>
    /// Basic type, e.g. &gt;int&lt;
    /// </summary>
    public class NormalDeclaration : TypeDeclaration
    {
        public static byte GetDeclarationClassTypeId { get { return 1; } }
        public override byte TypeId { get { return GetDeclarationClassTypeId; } }
        public string Name;

        public NormalDeclaration() { }
        public NormalDeclaration(string Identifier)
        { Name = Identifier.Trim('(', ')'); }

        public override string ToString()
        {
            return Name + (Base != null ? (" " + Base.ToString()) : "");
        }
    }

    public class DTokenDeclaration : NormalDeclaration
    {
        public static new byte GetDeclarationClassTypeId { get { return 2; } }
        public override byte TypeId { get { return GetDeclarationClassTypeId; } }
        public int Token;

        public DTokenDeclaration() { }
        public DTokenDeclaration(int Token)
        { this.Token = Token; }

        public DTokenDeclaration(int p, TypeDeclaration td)
        {
            Token = p;
            Base = td;
        }

        public new string Name
        {
            get { return Token >= 3 ? DTokens.GetTokenString(Token) : ""; }
            set { Token = DTokens.GetTokenID(value); }
        }

        public override string ToString()
        {
            return Name + (Base != null ? (" " + Base.ToString()) : "");
        }
    }

    /// <summary>
    /// Array decl, e.g. &gt;int[string]&lt; myArray;
    /// </summary>
    public class ClampDecl : TypeDeclaration
    {
        public static byte GetDeclarationClassTypeId { get { return 3; } }
        public override byte TypeId { get { return GetDeclarationClassTypeId; } }
        /// <summary>
        /// Equals <see cref="Base" />
        /// </summary>
        public TypeDeclaration ValueType
        {
            get { return Base; }
            set { Base = value; }
        }
        public TypeDeclaration KeyType;
        public enum ClampType
        {
            Round=0,
            Square=1,
            Curly=2
        }
        public ClampType Clamps=ClampType.Square;
        public bool IsArrayDecl
        {
            get { return Clamps == ClampType.Square; }
        }

        public ClampDecl() { }
        public ClampDecl(TypeDeclaration ValueType) { this.ValueType = ValueType; }
        public ClampDecl(TypeDeclaration ValueType, ClampType clamps) { this.ValueType = ValueType; Clamps = clamps; }

        public override string ToString()
        {
            string s = (ValueType != null ? ValueType.ToString() : "");
            switch (Clamps)
            {
                case ClampType.Round:
                    s += "(";
                    break;
                case ClampType.Square:
                    s += "[";
                    break;
                case ClampType.Curly:
                    s += "{";
                    break;
            }
            s+=(KeyType != null ? KeyType.ToString() : "");
            switch (Clamps)
            {
                case ClampType.Round:
                    s += ")";
                    break;
                case ClampType.Square:
                    s += "]";
                    break;
                case ClampType.Curly:
                    s += "}";
                    break;
            }
            return s;
        }
    }

    public class DelegateDeclaration : TypeDeclaration
    {
        public static byte GetDeclarationClassTypeId { get { return 4; } }
        public override byte TypeId { get { return GetDeclarationClassTypeId; } }
        public TypeDeclaration ReturnType
        {
            get { return Base; }
            set { Base = value; }
        }
        /// <summary>
        /// Is it a function(), not a delegate() ?
        /// </summary>
        public bool IsFunction = false;

        public List<DNode> Parameters = new List<DNode>();

        public override string ToString()
        {
            string ret = ReturnType.ToString() + (IsFunction ? " function" : " delegate") + "(";

            foreach (DVariable n in Parameters)
            {
                if (n.Type != null)
                    ret += n.Type.ToString();

                if (!String.IsNullOrEmpty(n.name))
                    ret += (" " + n.name);

                if (n.Initializer != null)
                    ret += "= " + n.Initializer.ToString();

                ret += ", ";
            }
            ret = ret.TrimEnd(',', ' ') + ")";
            return ret;
        }
    }

    /// <summary>
    /// int* ptr;
    /// </summary>
    public class PointerDecl : TypeDeclaration
    {
        public static byte GetDeclarationClassTypeId { get { return 5; } }
        public override byte TypeId { get { return GetDeclarationClassTypeId; } }
        public PointerDecl() { }
        public PointerDecl(TypeDeclaration BaseType) { Base = BaseType; }

        public override string ToString()
        {
            return (Base != null ? Base.ToString() : "") + "*";
        }
    }

    /// <summary>
    /// const(char)
    /// </summary>
    public class MemberFunctionAttributeDecl : DTokenDeclaration
    {
        public static byte GetDeclarationClassTypeId { get { return 6; } }
        public override byte TypeId { get { return GetDeclarationClassTypeId; } }
        /// <summary>
        /// Equals <see cref="Token"/>
        /// </summary>
        public int Modifier
        {
            get { return Token; }
            set { Token = value; }
        }

        public MemberFunctionAttributeDecl() { }
        public MemberFunctionAttributeDecl(int ModifierToken) { this.Modifier = ModifierToken; }

        public override string ToString()
        {
            return Name + "(" + (Base != null ? Base.ToString() : "") + ")";
        }
    }

    public class VarArgDecl : TypeDeclaration
    {
        public static byte GetDeclarationClassTypeId { get { return 7; } }
        public override byte TypeId { get { return GetDeclarationClassTypeId; } }
        public VarArgDecl() { }
        public VarArgDecl(TypeDeclaration BaseIdentifier) { Base = BaseIdentifier; }

        public override string ToString()
        {
            return (Base != null ? Base.ToString() : "") + "...";
        }
    }

    // Secondary importance
    /// <summary>
    /// class ABC: &gt;A, C&lt;
    /// </summary>
    public class InheritanceDecl : TypeDeclaration
    {
        public static byte GetDeclarationClassTypeId { get { return 8; } }
        public override byte TypeId { get { return GetDeclarationClassTypeId; } }
        public TypeDeclaration InheritedClass;
        public TypeDeclaration InheritedInterface;

        public InheritanceDecl() { }
        public InheritanceDecl(TypeDeclaration Base) { this.Base = Base; }

        public override string ToString()
        {
            string ret = "";

            if (Base != null) ret += Base.ToString();

            if (InheritedClass != null || InheritedInterface != null) ret += ":";
            if (InheritedClass != null) ret += InheritedClass.ToString();
            if (InheritedClass != null && InheritedInterface != null) ret += ", ";
            if (InheritedInterface != null) ret += InheritedInterface.ToString();

            return ret;
        }
    }

    /// <summary>
    /// List&lt;T:base&gt; myList;
    /// </summary>
    public class TemplateDecl : TypeDeclaration
    {
        public static byte GetDeclarationClassTypeId { get { return 9; } }
        public override byte TypeId { get { return GetDeclarationClassTypeId; } }
        public TypeDeclaration Template;

        public TemplateDecl() { }
        public TemplateDecl(TypeDeclaration Base)
        {
            this.Base = Base;
        }

        public override string ToString()
        {
            return (Base != null ? Base.ToString() : "").ToString() + "!(" + (Template != null ? Template.ToString() : "") + ")";
        }
    }

    /// <summary>
    /// Base.AccessedMember
    /// </summary>
    public class DotCombinedDeclaration : TypeDeclaration
    {
        public static byte GetDeclarationClassTypeId { get { return 10; } }
        public override byte TypeId { get { return GetDeclarationClassTypeId; } }
        public TypeDeclaration AccessedMember;

        public DotCombinedDeclaration() { }
        public DotCombinedDeclaration(TypeDeclaration Base) { this.Base = Base; }

        public override string ToString()
        {
            return (Base != null ? Base.ToString() : "") + "." + (AccessedMember != null ? AccessedMember.ToString() : "");
        }
    }


    #region Expressions
    public class DExpressionDecl : TypeDeclaration
    {
        public static byte GetDeclarationClassTypeId { get { return 11; } }
        public override byte TypeId { get { return GetDeclarationClassTypeId; } }

        public DExpression Expression;

        public DExpressionDecl() { }

        public DExpressionDecl(DExpression dExpression)
        {
            this.Expression = dExpression;
        }

        public override string ToString()
        {
            return Expression.ToString();
        }
    }


    public abstract class DExpression
    {
        public DExpression() { }

        public abstract string ToString();
    }

    public class IdentExpression : DExpression
    {
        public string Value="";

        public IdentExpression() { }
        public IdentExpression(string Val) { Value = Val; }

        public override string ToString()
        {
            return Value;
        }
    }
    #endregion


}