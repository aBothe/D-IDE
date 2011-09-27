﻿using System.Collections.Generic;
using D_Parser.Dom.Expressions;
using D_Parser.Dom.Statements;
using D_Parser.Parser;

namespace D_Parser.Dom
{
    /// <summary>
    /// Encapsules an entire document and represents the root node
    /// </summary>
    public class DModule : DBlockNode, IAbstractSyntaxTree
    {
        /// <summary>
        /// Applies file name, children and imports from an other module instance
         /// </summary>
        /// <param name="Other"></param>
        public override void AssignFrom(INode Other)
        {
			if (Other is IAbstractSyntaxTree)
			{
				var ast = Other as IAbstractSyntaxTree;
				ParseErrors = ast.ParseErrors;
				//FileName = ast.FileName;
			}

			if (Other is DModule)
			{
				var dm = Other as DModule;
				Imports = dm.Imports;
			}

			base.AssignFrom(Other);
        }

		string _FileName;

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

		public ModuleStatement OptionalModuleStatement;

		ImportStatement[] imports = null;
		public ImportStatement[] Imports
		{
			get { return imports; }
			set {
				imports = value;

				if (imports != null)
					foreach (var imp in value)
						imp.ParentNode = this;
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

        public bool ContainsImport(string p)
        {
            if (Imports != null)
                foreach (var i in Imports)
                    if (i.IsSimpleBinding && i.ModuleIdentifier!=null && i.ModuleIdentifier.ToString() == p)
                        return true;

            return false;
        }
	}

	public class DBlockNode : DNode, IBlockNode
	{
		CodeLocation _BlockStart;
		protected List<INode> _Children = new List<INode>();

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
			if(Nodes!=null)
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

		public override void AssignFrom(INode other)
		{
			if (other is IBlockNode)
			{
				BlockStartLocation = (other as IBlockNode).BlockStartLocation;
				Clear();
				AddRange(other as IBlockNode);
			}

			base.AssignFrom(other);
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

		public override void AssignFrom(INode other)
		{
			if (other is DVariable)
			{
				var dv = other as DVariable;
				Initializer = dv.Initializer;
				IsAlias = dv.IsAlias;
			}

			base.AssignFrom(other);
		}
    }

    public class DMethod : DNode,IBlockNode
    {
        public List<INode> Parameters=new List<INode>();
        public MethodType SpecialType = MethodType.Normal;

		BlockStatement _In;
		BlockStatement _Out;
		BlockStatement _Body;

		public BlockStatement GetSubBlockAt(CodeLocation Where)
		{
			if (_In != null && _In.StartLocation <= Where && _In.EndLocation >= Where)
				return _In;

			if (_Out != null && _Out.StartLocation <= Where && _Out.EndLocation >= Where)
				return _Out;

			if (_Body != null && _Body.StartLocation <= Where && _Body.EndLocation >= Where)
				return _Body;

			return null;
		}

		public override void AssignFrom(INode other)
		{
			if (other is DMethod)
			{
				var dm = other as DMethod;

				Parameters = dm.Parameters;
				SpecialType = dm.SpecialType;
				_In = dm._In;
				_Out = dm._Out;
				_Body = dm._Body;
				UpdateChildrenArray();
			}

			base.AssignFrom(other);
		}

		public BlockStatement In { get { return _In; } set { _In = value; UpdateChildrenArray(); } }
		public BlockStatement Out { get { return _Out; } set { _Out = value; UpdateChildrenArray(); } }
		public BlockStatement Body { get { return _Body; } set { _Body = value; UpdateChildrenArray(); } }

		INode[] children;
		List<INode> additionalChildren = new List<INode>();

		/// <summary>
		/// Children which were added artifically via Add() or AddRange()
		/// In most cases, these are anonymous delegate/class declarations.
		/// </summary>
		public List<INode> AdditionalChildren
		{
			get { return additionalChildren; }
		}

		void UpdateChildrenArray()
		{
			var l = new List<INode>();

			l.AddRange(additionalChildren);

			if (_In != null)
				l.AddRange(_In.Declarations);

			if (_Body != null)
				l.AddRange(_Body.Declarations);

			if (_Out != null)
				l.AddRange(_Out.Declarations);

			children = l.ToArray();
		}

        public enum MethodType
        {
            Normal=0,
			Delegate,
            AnonymousDelegate,
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

		public CodeLocation BlockStartLocation
		{
			get
			{
				if (_In != null && _Out != null)
					return _In.StartLocation < _Out.StartLocation ? _In.StartLocation : _Out.StartLocation;
				else if (_In != null)
					return _In.StartLocation;
				else if (_Out != null)
					return _Out.StartLocation;
				else if (_Body != null)
					return _Body.StartLocation;

				return CodeLocation.Empty;
			}
			set{}
		}

		public INode[] Children
		{
			get { return children; }
		}

		public void Add(INode Node)
		{
			Node.Parent = this;
			additionalChildren.Add(Node);
			/*
			var block = GetSubBlockAt(Node.StartLocation);

			if (block == null)
				return;

			var ds = new DeclarationStatement() { 
				Declarations=new[]{Node},
				StartLocation=Node.StartLocation,
				EndLocation=Node.EndLocation
			};

			block.Add(ds);*/

			UpdateChildrenArray();
		}

		public void AddRange(IEnumerable<INode> Nodes)
		{
			foreach (var n in Nodes)
			{
				n.Parent = this;
				additionalChildren.Add(n);
			}
			/*
			foreach (var Node in Nodes)
			{
				var block = GetSubBlockAt(Node.StartLocation);

				if (block == null)
					continue;

				var ds = new DeclarationStatement()
				{
					Declarations = new[] { Node },
					StartLocation = Node.StartLocation,
					EndLocation = Node.EndLocation
				};

				block.Add(ds);
			}*/

			UpdateChildrenArray();
		}

		public int Count
		{
			get { 
				if (children == null) 
					return 0;
				return children.Length; 
			}
		}

		public INode this[int i]
		{
			get
			{
				if (children != null)
					return children[i];
				return null;
			}
			set
			{
				if (children != null)
					children[i]=value;
			}
		}

		public INode this[string Name]
		{
			get
			{
				if(children!=null)
					foreach (var c in children)
						if (c.Name == Name)
							return c;

				return null;
			}
			set
			{
				if (children != null)
					for(int i=0;i<children.Length;i++)
						if (children[i].Name == Name)
						{
							children[i] = value;
							return;
						}
			}
		}

		public IEnumerator<INode> GetEnumerator()
		{
			if (children == null)
				return null;
			return (children as IEnumerable<INode>).GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			if (children == null)
				return null;
			return children.GetEnumerator();
		}


		public void Clear()
		{
			additionalChildren.Clear();
		}
	}

    public class DClassLike : DBlockNode
    {
		public bool IsAnonymous = false;

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

    public class DEnum : DBlockNode
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
