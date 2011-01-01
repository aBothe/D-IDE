using System;
using System.Collections.Generic;
using System.Text;

namespace Parser.Core
{
	public abstract class AbstractNode:INode
	{
		ITypeDeclaration _Type;
		string _Name="";
		INode _Parent;
		string _Description="";
		CodeLocation _StartLocation;
		CodeLocation _EndLocation;

		public CodeLocation EndLocation
		{
			get { return _EndLocation; }
			set { _EndLocation = value; }
		}

		public CodeLocation StartLocation
		{
			get { return _StartLocation; }
			set { _StartLocation = value; }
		}

		public string Description
		{
			get { return _Description; }
			set { _Description = value; }
		}

		public ITypeDeclaration Type
		{
			get { return _Type; }
			set { _Type = value; }
		}

		public string Name
		{
			get { return _Name; }
			set { _Name = value; }
		}

		public INode Parent
		{
			get { return _Parent; }
			set { _Parent = value; }
		}

		public override string ToString()
		{
			string s = "";
			// Type
			if (Type != null)
				s += Type.ToString() + " ";

			// Path + Name
			string path = "";
			var curParent = this as INode;
			while (curParent != null)
			{
				// Also include module path
				if (curParent is IAbstractSyntaxTree)
					path = (curParent as IAbstractSyntaxTree).ModuleName + "." + path;
				else
					path = curParent.Name + "." + path;
				curParent = curParent.Parent;
			}
			s += path.Trim('.');

			return s.Trim();
		}

		public void Assign(INode other)
		{
			Type = other.Type;
			Name = other.Name;

			Parent = other.Parent;
			Description = other.Description;
			StartLocation = other.StartLocation;
			EndLocation = other.EndLocation;
		}

		public INode NodeRoot
		{
			get
			{
				if (Parent == null)
					return this;
				else return Parent.NodeRoot;
			}
			set
			{
				if (Parent == null)
					Parent = value;
				else Parent.NodeRoot = value;
			}
		}
	}
}
