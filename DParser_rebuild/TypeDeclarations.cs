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
        { Name = Identifier; }

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
            Round = 0,
            Square = 1,
            Curly = 2
        }
        public ClampType Clamps = ClampType.Square;
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
            s += (KeyType != null ? KeyType.ToString() : "");
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

                if (!String.IsNullOrEmpty(n.Name))
                    ret += (" " + n.Name);

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
        public static new byte GetDeclarationClassTypeId { get { return 6; } }
        public override byte TypeId { get { return GetDeclarationClassTypeId; } }
        /// <summary>
        /// Equals <see cref="Token"/>
        /// </summary>
        public int Modifier
        {
            get { return Token; }
            set { Token = value; }
        }

        public TypeDeclaration InnerType;

        public MemberFunctionAttributeDecl() { }
        public MemberFunctionAttributeDecl(int ModifierToken) { this.Modifier = ModifierToken; }

        public override string ToString()
        {
            return (Base != null ? (Base.ToString()+" ") : "") +Name + "(" + (InnerType != null ? InnerType.ToString() : "") + ")";
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
        public List<TypeDeclaration> Template=new List<TypeDeclaration>();

        public TemplateDecl() { }
        public TemplateDecl(TypeDeclaration Base)
        {
            this.Base = Base;
        }

        public override string ToString()
        {
            string s = (Base != null ? Base.ToString() : "").ToString() + "!(";

            foreach (TypeDeclaration t in Template)
                s += t.ToString()+",";
            s=s.TrimEnd(',',' ');
            s+=")";
            return s;
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
        public DExpression Base;

        public new abstract string ToString();
    }

    public class IdentExpression : DExpression
    {
        public object Value = "";

        public IdentExpression() { }
        public IdentExpression(object Val) { Value = Val; }

        public override string ToString()
        {
            return (Base != null ? (Base.ToString() + " ") : "") + ((Value is string) ? (string)Value : Value.ToString());
        }
    }

    public class TokenExpression : DExpression
    {
        public int Token;

        public TokenExpression() { }
        public TokenExpression(int T) { Token = T; }

        public override string ToString()
        {
            return (Base != null ? (Base.ToString() + " ") : "") + DParser.GetTokenString(Token);
        }
    }

    public class TypeDeclarationExpression : DExpression
    {
        public TypeDeclaration Declaration;

        public TypeDeclarationExpression() { }
        public TypeDeclarationExpression(TypeDeclaration td) { Declaration = td; }

        public override string ToString()
        {
            return (Base != null ? (Base.ToString() + " ") : "") + Declaration != null ? Declaration.ToString() : "";
        }
    }

    public class ClampExpression : DExpression
    {
        public DExpression FrontExpression
        {
            get { return Base; }
            set { Base = value; }
        }
        public DExpression InnerExpression;

        public enum ClampType
        {
            Round = 0,
            Square = 1,
            Curly = 2
        }
        public ClampType Clamps = ClampType.Square;
        public bool IsArrayDecl
        {
            get { return Clamps == ClampType.Square; }
        }

        public ClampExpression() { }
        public ClampExpression(DExpression frontExpr) { FrontExpression = frontExpr; }
        public ClampExpression(DExpression frontExpr, ClampType clamps) { FrontExpression = frontExpr; Clamps = clamps; }
        public ClampExpression(ClampType clamps) { Clamps = clamps; }

        public override string ToString()
        {
            string s = (FrontExpression != null ? FrontExpression.ToString() : "");
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
            s += (InnerExpression != null ? InnerExpression.ToString() : "");
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

    public class AssignTokenExpression : DExpression
    {
        public int Token;
        public DExpression FollowingExpression;
        public DExpression PrevExpression
        {
            get { return Base; }
            set { Base = value; }
        }

        public AssignTokenExpression() { }
        public AssignTokenExpression(int T) { Token = T; }

        public override string ToString()
        {
            return (PrevExpression != null ? PrevExpression.ToString() : "") + " " + DParser.GetTokenString(Token) + " " + (FollowingExpression != null ? FollowingExpression.ToString() : "");
        }
    }


    public class SwitchExpression : DExpression
    {
        public DExpression TriggerExpression
        {
            get { return Base; }
            set { Base = value; }
        }
        public DExpression TrueCase, FalseCase;

        public SwitchExpression() { }
        public SwitchExpression(DExpression Trigger) { TriggerExpression = Trigger; }

        public override string ToString()
        {
            return (TriggerExpression != null ? TriggerExpression.ToString() : "") + "?" + (TrueCase != null ? TrueCase.ToString() : "") + " : " + (FalseCase != null ? FalseCase.ToString() : "");
        }
    }

    public class ArrayExpression : DExpression
    {
        public ClampExpression.ClampType Clamps = ClampExpression.ClampType.Square;
        public List<DExpression> Expressions = new List<DExpression>();

        public ArrayExpression() { }
        public ArrayExpression(ClampExpression.ClampType clamps) { Clamps = clamps; }

        public override string ToString()
        {
            string s = (Base != null ? Base.ToString() : "");
            switch (Clamps)
            {
                case ClampExpression.ClampType.Round:
                    s += "(";
                    break;
                case ClampExpression.ClampType.Square:
                    s += "[";
                    break;
                case ClampExpression.ClampType.Curly:
                    s += "{";
                    break;
            }
            foreach (DExpression expr in Expressions)
                s += expr.ToString()+", ";
            s = s.TrimEnd(' ', ',');
            switch (Clamps)
            {
                case ClampExpression.ClampType.Round:
                    s += ")";
                    break;
                case ClampExpression.ClampType.Square:
                    s += "]";
                    break;
                case ClampExpression.ClampType.Curly:
                    s += "}";
                    break;
            }
            return s;
        }
    }

    public class FunctionLiteral : DExpression
    {
        public int LiteralToken = DTokens.Delegate;

        public DMethod AnonymousMethod = new DMethod();

        public FunctionLiteral() { }
        public FunctionLiteral(int InitialLiteral) { LiteralToken = InitialLiteral; }

        public override string ToString()
        {
            return (Base != null ? (Base.ToString()+" ") : "") + DTokens.GetTokenString(LiteralToken) + " " + AnonymousMethod.ToString();
        }
    }
    #endregion


}