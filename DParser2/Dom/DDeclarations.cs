using System;
using System.Collections.Generic;
using System.Text;
using D_Parser.Parser;

namespace D_Parser.Dom
{
	public interface ITypeDeclaration
	{
		ITypeDeclaration InnerDeclaration { get; set; }
		ITypeDeclaration InnerMost { get; set; }

		string ToString();
		string ToString(bool IncludesBase);
	}

	public abstract class AbstractTypeDeclaration : ITypeDeclaration
	{
		public ITypeDeclaration InnerMost
		{
			get
			{
				if (InnerDeclaration == null)
					return this;
				else
					return InnerDeclaration.InnerMost;
			}
			set
			{
				if (InnerDeclaration == null)
					InnerDeclaration = value;
				else
					InnerDeclaration.InnerMost = value;
			}
		}

		public ITypeDeclaration InnerDeclaration
		{
			get;
			set;
		}

		public override string ToString()
		{
			return ToString(true);
		}

		public abstract string ToString(bool IncludesBase);

		public static implicit operator String(AbstractTypeDeclaration d)
		{
			return d.ToString(false);
		}
	}

    /// <summary>
    /// Identifier, e.g. "foo"
    /// </summary>
    public class IdentifierDeclaration : AbstractTypeDeclaration
    {
		public virtual object Value { get; set; }

        public IdentifierDeclaration() { }
        public IdentifierDeclaration(object Value)
        { this.Value = Value; }

		public override string ToString(bool IncludesBase)
		{
			return (IncludesBase&& InnerDeclaration != null ? (InnerDeclaration.ToString() + ".") : "") +Convert.ToString(Value);
		}
	}

    public class DTokenDeclaration : IdentifierDeclaration
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
            InnerDeclaration = td;
        }

        public override object Value
        {
            get { return Token >= 3 ? DTokens.GetTokenString(Token) : ""; }
			set { }
        }
    }

    /// <summary>
    /// Extends an identifier by an array literal.
    /// </summary>
    public class ArrayDecl : AbstractTypeDeclaration
    {
        public ITypeDeclaration KeyType;

        public ArrayDecl() { }

		public override string ToString(bool IncludesBase)
        {
            return (IncludesBase&& InnerDeclaration != null ? InnerDeclaration.ToString() : "")+ "["+(KeyType != null ? KeyType.ToString() : "")+"]";
        }
    }

    public class DelegateDeclaration : AbstractTypeDeclaration
    {
        public ITypeDeclaration ReturnType
        {
            get { return InnerDeclaration; }
            set { InnerDeclaration = value; }
        }
        /// <summary>
        /// Is it a function(), not a delegate() ?
        /// </summary>
        public bool IsFunction = false;

        public List<INode> Parameters = new List<INode>();

		public override string ToString(bool IncludesBase)
        {
            string ret = (IncludesBase? ReturnType.ToString():"") + (IsFunction ? " function" : " delegate") + "(";

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
        public PointerDecl(ITypeDeclaration BaseType) { InnerDeclaration = BaseType; }

		public override string ToString(bool IncludesBase)
        {
            return (IncludesBase&& InnerDeclaration != null ? InnerDeclaration.ToString() : "") + "*";
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

		public override string ToString(bool IncludesBase)
        {
            return (IncludesBase&& InnerDeclaration != null ? (InnerDeclaration.ToString()+" ") : "") +Value + "(" + (InnerType != null ? InnerType.ToString() : "") + ")";
        }
    }

    public class VarArgDecl : AbstractTypeDeclaration
    {
        public VarArgDecl() { }
        public VarArgDecl(ITypeDeclaration BaseIdentifier) { InnerDeclaration = BaseIdentifier; }

		public override string ToString(bool IncludesBase)
        {
            return (IncludesBase&& InnerDeclaration != null ? InnerDeclaration.ToString() : "") + "...";
        }
    }

    /// <summary>
    /// List&lt;T:base&gt; myList;
    /// </summary>
    public class TemplateDecl : AbstractTypeDeclaration
    {
		public string TemplateIdentifier = string.Empty;
        public List<ITypeDeclaration> Template=new List<ITypeDeclaration>();

        public TemplateDecl() { }
        public TemplateDecl(ITypeDeclaration Base)
        {
            this.InnerDeclaration = Base;
        }

		public override string ToString(bool IncludesBase)
        {
            string s = (IncludesBase&& InnerDeclaration != null ? (InnerDeclaration.ToString()+".") : "").ToString() +TemplateIdentifier+ "!(";

            foreach (var t in Template)
                s += t.ToString()+",";
            s=s.TrimEnd(',',' ');
            s+=")";
            return s;
        }
    }
}