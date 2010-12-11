using System;
using System.Collections.Generic;
using System.Text;

namespace Parser.Core
{
	/*
	public abstract class BlockNode:Node,IBlockNode
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
			if (_Children.Contains(Node))
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
			get { if (Count > i)return _Children[i]; else return null; }
			set { if (Count > i) _Children[i] = value; }
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



		public void Assign(IBlockNode other)
		{
			BlockStartLocation = other.BlockStartLocation;
			Clear();
			AddRange(other);

			base.Assign(other);
		}

		public override string ToString()
		{
			return Name;
		}
	}
*/}
