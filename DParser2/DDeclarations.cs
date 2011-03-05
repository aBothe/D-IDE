using System;
using System.Collections.Generic;
using System.Text;
using Parser.Core;

namespace D_Parser
{
    /// <summary>
    /// Basic type, e.g. &gt;int&lt;
    /// </summary>
    public class NormalDeclaration : AbstractTypeDeclaration
    {
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
        public int Token;

        public DTokenDeclaration() { }
        public DTokenDeclaration(int Token)
        { this.Token = Token; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p">The token</param>
        /// <param name="td">Its base token</param>
        public DTokenDeclaration(int p, ITypeDeclaration td)
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
    public class ClampDecl : AbstractTypeDeclaration
    {
        /// <summary>
        /// Equals <see cref="Base" />
        /// </summary>
        public ITypeDeclaration ValueType
        {
            get { return Base; }
            set { Base = value; }
        }
        public ITypeDeclaration KeyType;
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
        public ClampDecl(ITypeDeclaration ValueType) { this.ValueType = ValueType; }
        public ClampDecl(ITypeDeclaration ValueType, ClampType clamps) { this.ValueType = ValueType; Clamps = clamps; }

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

    public class DelegateDeclaration : AbstractTypeDeclaration
    {
        public ITypeDeclaration ReturnType
        {
            get { return Base; }
            set { Base = value; }
        }
        /// <summary>
        /// Is it a function(), not a delegate() ?
        /// </summary>
        public bool IsFunction = false;

        public List<INode> Parameters = new List<INode>();

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
    public class PointerDecl : AbstractTypeDeclaration
    {
        public PointerDecl() { }
        public PointerDecl(ITypeDeclaration BaseType) { Base = BaseType; }

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
        /// <summary>
        /// Equals <see cref="Token"/>
        /// </summary>
        public int Modifier
        {
            get { return Token; }
            set { Token = value; }
        }

        public ITypeDeclaration InnerType;

        public MemberFunctionAttributeDecl() { }
        public MemberFunctionAttributeDecl(int ModifierToken) { this.Modifier = ModifierToken; }

        public override string ToString()
        {
            return (Base != null ? (Base.ToString()+" ") : "") +Name + "(" + (InnerType != null ? InnerType.ToString() : "") + ")";
        }
    }

    public class VarArgDecl : AbstractTypeDeclaration
    {
        public VarArgDecl() { }
        public VarArgDecl(ITypeDeclaration BaseIdentifier) { Base = BaseIdentifier; }

        public override string ToString()
        {
            return (Base != null ? Base.ToString() : "") + "...";
        }
    }

    // Secondary importance
    /// <summary>
    /// class ABC: &gt;A, C&lt;
    /// </summary>
    public class InheritanceDecl : AbstractTypeDeclaration
    {
        public ITypeDeclaration InheritedClass;
        public ITypeDeclaration InheritedInterface;

        public InheritanceDecl() { }
        public InheritanceDecl(ITypeDeclaration Base) { this.Base = Base; }

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
    public class TemplateDecl : AbstractTypeDeclaration
    {
        public List<ITypeDeclaration> Template=new List<ITypeDeclaration>();

        public TemplateDecl() { }
        public TemplateDecl(ITypeDeclaration Base)
        {
            this.Base = Base;
        }

        public override string ToString()
        {
            string s = (Base != null ? Base.ToString() : "").ToString() + "!(";

            foreach (var t in Template)
                s += t.ToString()+",";
            s=s.TrimEnd(',',' ');
            s+=")";
            return s;
        }
    }

    /// <summary>
    /// Probably a more efficient way to store identifier lists like a.b.c.d
    /// </summary>
    public class IdentifierList : AbstractTypeDeclaration
    {
        public readonly List<ITypeDeclaration> Parts = new List<ITypeDeclaration>();

        public ITypeDeclaration this[int i]
        {
            get { return Parts[i]; }
            set { Parts[i] = value;}
        }

        public void Add(ITypeDeclaration Part)
        {
            Parts.Add(Part);
        }

        public void Add(string Identifier)
        {
            Parts.Add(new NormalDeclaration(Identifier));
        }

        public override string ToString()
        {
            var s = "";
            foreach (var p in Parts)
                s += "." + p.ToString();
            return s.TrimStart('.');
        }

        public string ToString(int start)
        {
            return ToString(start,Parts.Count-start);
        }

        public string ToString(int start, int length)
        {
            var s = "";
            if (start <= 0 || length < 0) 
                throw new ArgumentNullException("Parameter must not be 0 or less");
            if (start > Parts.Count || (start + length) > Parts.Count) 
                throw new ArgumentOutOfRangeException();

            for (int i = start; i < (start + length);i++ )
                s += "." + Parts[i].ToString();

            return s.TrimStart('.');
        }
    }


    #region Expressions
    public class DExpressionDecl : AbstractTypeDeclaration
    {
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
            return 
                ((Base != null) ? 
                    (Base.ToString() + " ") : 
                    string.Empty) + 
                ((Value is string) ? 
                    Value as String :
                    ((Value == null) ? 
                        string.Empty : 
                        Value.ToString()));
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
        public ITypeDeclaration Declaration;

        public TypeDeclarationExpression() { }
        public TypeDeclarationExpression(ITypeDeclaration td) { Declaration = td; }

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

    /// <summary>
    /// auto arr= [1,2,3,4,5,6];
    /// </summary>
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

    public class InitializerExpression : DExpression
    {
        public DExpression Initializer;
        public DExpression[] NewArguments = null;

        public InitializerExpression(DExpression InitExpression)
        {
            Initializer = InitExpression;
        }

        public override string ToString()
        {
            return "new "+Initializer.ToString();
        }
    }
    #endregion


}