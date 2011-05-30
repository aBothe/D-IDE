using System;
using System.Collections.Generic;
using System.Text;
using D_Parser.Core;

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
}