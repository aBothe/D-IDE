using System;
using System.Collections.Generic;
using System.Text;
using Parser.Core;

namespace D_Parser
{
    /// <summary>
    /// Encapsules an entire document and represents the root node
    /// </summary>
    public class DModule : DBlockStatement, ISourceModule
    {
		public bool ContainsImport(ITypeDeclaration type)
		{
			foreach (var kv in _Imports)
				if (kv.Key.ToString() == type.ToString())
					return true;
			return false;
		}

        /// <summary>
        /// Applies file name, children and imports from an other module instance
         /// </summary>
        /// <param name="Other"></param>
        public void Assign(DModule Other)
        {
			FileName = Other.FileName;
			base.Assign(Other as IBlockNode);
        }

		string _FileName;
		Dictionary<ITypeDeclaration, bool> _Imports = new Dictionary<ITypeDeclaration, bool>();
		List<ParserError> LastParseErrors = new List<ParserError>();

		/// <summary>
		/// Name alias
		/// </summary>
		public string ModuleName
		{
			get { return Name; }
			set { Name = value; }
		}

		public string FileName
		{
			get
			{
				return _FileName;
			}
			set
			{
				_FileName = value;
			}
		}

		public List<ParserError> ParseErrors
		{
			get { return LastParseErrors; }
		}

		public Dictionary<ITypeDeclaration, bool> Imports
		{
			get
			{
				return _Imports;
			}
			set
			{
				_Imports = value;
			}
		}
	}

	public class DBlockStatement : DNode, IBlockNode
	{
		CodeLocation _BlockStart;
		List<INode> _Children = new List<INode>();

		public CodeLocation BlockStartLocation
		{
			get
			{
				return _BlockStart;
			}
			set
			{
				_BlockStart = value;
			}
		}

		public INode[] Children
		{
			get { return _Children.ToArray(); }
		}

		public void Add(INode Node)
		{
			Node.Parent = this;
			if (!_Children.Contains(Node))
				_Children.Add(Node);
		}

		public void AddRange(IEnumerable<INode> Nodes)
		{
			foreach (var Node in Nodes)
				Add(Node);
		}

		public int Count
		{
			get { return _Children.Count; }
		}

		public void Clear()
		{
			_Children.Clear();
		}

		public INode this[int i]
		{
			get { if (i>=0 && Count > i)return _Children[i]; else return null; }
			set { if (i >= 0 && Count > i) _Children[i] = value; }
		}

		public INode this[string Name]
		{
			get
			{
				if (Count > 1)
					foreach (var n in _Children)
						if (n.Name == Name) return n;
				return null;
			}
			set
			{
				if (Count > 1)
					for (int i = 0; i < Count; i++)
						if (this[i].Name == Name) this[i] = value;
			}
		}

		public IEnumerator<INode> GetEnumerator()
		{
			return _Children.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return _Children.GetEnumerator();
		}

		public new void Assign(INode other)
		{
			if (other is IBlockNode)
			{
				BlockStartLocation = (other as IBlockNode).BlockStartLocation;
				Clear();
				AddRange(other as IBlockNode);
			}

			base.Assign(other);
		}

		public override string ToString()
		{
			return Name;
		}
	}

    public class DVariable : DNode
    {
        public DExpression Initializer; // Variable
        public bool IsAlias = false;

        public override string ToString()
        {
            return (IsAlias?"alias ":"")+base.ToString()+(Initializer!=null?(" = "+Initializer.ToString()):"");
        }
    }

    public class DMethod : DBlockStatement
    {
        public List<INode> Parameters=new List<INode>();
        public MethodType SpecialType = MethodType.Normal;

        public enum MethodType
        {
            Normal=0,
            Delegate,
            Constructor,
            Destructor,
            Unittest
        }

        public DMethod() { }
        public DMethod(MethodType Type) { SpecialType = Type; }

        public override string ToString()
        {
            var s= base.ToString()+"(";
            foreach (var p in Parameters)
                s += p.ToString()+",";
            return s.Trim(',')+")";
        }
    }

    public class DStatementBlock : DBlockStatement
    {
        public int Token;
        public DExpression Expression;

        public DStatementBlock(int Token)
        {
            this.Token = Token;
        }

        public DStatementBlock() { }

        public override string ToString()
        {
            return DTokens.GetTokenString(Token)+(Expression!=null?("("+Expression.ToString()+")"):"");
        }
    }

    public class DClassLike : DBlockStatement
    {
        public List<ITypeDeclaration> BaseClasses=new List<ITypeDeclaration>();
        public int ClassType=DTokens.Class;

        public DClassLike() { }
        public DClassLike(int ClassType)
        {
            this.ClassType = ClassType;
        }

        public override string ToString()
        {
            string ret = AttributeString + " " + DTokens.GetTokenString(ClassType) + " " + ToString(false);
            if (BaseClasses.Count > 0)
                ret += ":";
            foreach (var c in BaseClasses)
                ret += c.ToString()+", ";

            return ret.Trim().TrimEnd(',');
        }
    }

    public class DEnum : DBlockStatement
    {
        public override string ToString()
        {
            return "enum "+ToString(false);
        }
    }

    public class DEnumValue : DVariable
    {
    }
}
