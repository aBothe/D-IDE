﻿using System;
using System.Collections.Generic;
using System.Text;
using D_Parser.Parser;

namespace D_Parser.Dom
{
    /// <summary>
    /// Encapsules an entire document and represents the root node
    /// </summary>
    public class DModule : DBlockStatement, IAbstractSyntaxTree
    {
		public bool ContainsImport(ITypeDeclaration type)
		{
			foreach (var kv in _Imports)
				if (kv.Key.ToString() == type.ToString())
					return true;
			return false;
		}

		public bool ContainsImport(string ImportIdentifier)
		{
			foreach (var kv in _Imports)
				if (kv.Key.ToString() == ImportIdentifier)
					return true;
			return false;
		}

        /// <summary>
        /// Applies file name, children and imports from an other module instance
         /// </summary>
        /// <param name="Other"></param>
        public override void Assign(INode Other)
        {
			if (Other is IAbstractSyntaxTree)
			{
				var ast = Other as IAbstractSyntaxTree;
				_Imports = new Dictionary<ITypeDeclaration, bool>(ast.Imports);
				ParseErrors = ast.ParseErrors;
				//FileName = ast.FileName;
			}

			base.Assign(Other);
        }

		string _FileName;
		Dictionary<ITypeDeclaration, bool> _Imports = new Dictionary<ITypeDeclaration, bool>();

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

		public System.Collections.ObjectModel.ReadOnlyCollection<ParserError> ParseErrors
		{
			get;
			set;
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

		public override string ToString(bool Attributes, bool IncludePath)
		{
			if (!IncludePath)
			{
				var parts = ModuleName.Split('.');
				return parts[parts.Length-1];
			}

			return ModuleName;
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

		public override void Assign(INode other)
		{
			if (other is IBlockNode)
			{
				BlockStartLocation = (other as IBlockNode).BlockStartLocation;
				Clear();
				AddRange(other as IBlockNode);
			}

			base.Assign(other);
		}
	}

    public class DVariable : DNode
    {
        public IExpression Initializer; // Variable
        public bool IsAlias = false;

		public override string ToString(bool Attributes, bool IncludePath)
        {
            return (IsAlias?"alias ":"")+base.ToString(Attributes,IncludePath)+(Initializer!=null?(" = "+Initializer.ToString()):"");
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
			Allocator,
            Destructor,
			Deallocator,
            Unittest
        }

        public DMethod() { }
        public DMethod(MethodType Type) { SpecialType = Type; }

		public override string ToString(bool Attributes, bool IncludePath)
        {
            var s= base.ToString(Attributes,IncludePath)+"(";
            foreach (var p in Parameters)
                s += (p is AbstractNode? (p as AbstractNode).ToString(false):p.ToString())+",";
            return s.Trim(',')+")";
        }
    }

    public class DStatementBlock : DBlockStatement
    {
        public int Token;
        public IExpression Expression;

        public DStatementBlock(int Token)
        {
            this.Token = Token;
        }

        public DStatementBlock() { }

		public override string ToString(bool Attributes, bool IncludePath)
        {
            return DTokens.GetTokenString(Token)+(Expression!=null?("("+Expression.ToString()+")"):"");
        }
    }

    public class DClassLike : DBlockStatement
    {
        public List<ITypeDeclaration> BaseClasses=new List<ITypeDeclaration>();
        public int ClassType=DTokens.Class;

		/// <summary>
		/// A constraint expression for template and class declarations
		/// </summary>
		public IExpression Constraint;

        public DClassLike() { }
        public DClassLike(int ClassType)
        {
            this.ClassType = ClassType;
        }

		public override string ToString(bool Attributes, bool IncludePath)
        {
            var ret = (Attributes? (AttributeString + " "):"") + DTokens.GetTokenString(ClassType) + " ";

			if (IncludePath)
				ret += GetNodePath(this, true);
			else
				ret += Name;

			if (TemplateParameters != null && TemplateParameters.Length>0)
			{
				ret += "(";
				foreach (var tp in TemplateParameters)
					ret += tp.ToString()+",";
				ret = ret.TrimEnd(',')+")";
			}

            if (BaseClasses.Count > 0)
                ret += ":";
            foreach (var c in BaseClasses)
                ret += c.ToString()+", ";

            return ret.Trim().TrimEnd(',');
        }
    }

    public class DEnum : DBlockStatement
    {
		public override string ToString(bool Attributes, bool IncludePath)
        {
			return (Attributes ? (AttributeString + " ") : "") + "enum " + (IncludePath?GetNodePath(this,true):Name);
        }
    }

    public class DEnumValue : DVariable
    {
    }
}