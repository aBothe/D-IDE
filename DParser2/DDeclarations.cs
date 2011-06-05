using System;
using System.Collections.Generic;
using System.Text;
using D_Parser.Core;

namespace D_Parser
{
	public interface ITypeDeclaration
	{
		ITypeDeclaration InnerDeclaration { get; set; }
		ITypeDeclaration InnerMost { get; set; }
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
	}

    /// <summary>
    /// Basic type, e.g. &gt;int&lt;
    /// </summary>
    public class IdentifierDeclaration : AbstractTypeDeclaration
    {
		public virtual string Name { get; set; }

        public IdentifierDeclaration() { }
        public IdentifierDeclaration(string Identifier)
        { Name = Identifier; }

        public override string ToString()
        {
			return (InnerDeclaration != null ? (InnerDeclaration.ToString()+".") : "") + Name;
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

        public override string Name
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

        public override string ToString()
        {
            return (InnerDeclaration != null ? InnerDeclaration.ToString() : "")+ "["+(KeyType != null ? KeyType.ToString() : "")+"]";
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
        public PointerDecl(ITypeDeclaration BaseType) { InnerDeclaration = BaseType; }

        public override string ToString()
        {
            return (InnerDeclaration != null ? InnerDeclaration.ToString() : "") + "*";
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
            return (InnerDeclaration != null ? (InnerDeclaration.ToString()+" ") : "") +Name + "(" + (InnerType != null ? InnerType.ToString() : "") + ")";
        }
    }

    public class VarArgDecl : AbstractTypeDeclaration
    {
        public VarArgDecl() { }
        public VarArgDecl(ITypeDeclaration BaseIdentifier) { InnerDeclaration = BaseIdentifier; }

        public override string ToString()
        {
            return (InnerDeclaration != null ? InnerDeclaration.ToString() : "") + "...";
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
            this.InnerDeclaration = Base;
        }

        public override string ToString()
        {
            string s = (InnerDeclaration != null ? InnerDeclaration.ToString() : "").ToString() + "!(";

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
            Parts.Add(new IdentifierDeclaration(Identifier));
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