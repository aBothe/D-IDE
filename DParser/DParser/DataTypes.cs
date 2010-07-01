using System;
using System.Collections.Generic;
using System.Text;

namespace D_Parser
{
	[Serializable()]
    public class DNode:IEnumerable<DNode>
    {
        public FieldType fieldtype;
        public int TypeToken;
        public string type;
        public string name;
		public string module;

        public string value; // Variable
        public List<DNode> param; // Functions, Templates
        public string superClass, implementedInterface; // Class-Like; superClass also represents enum's base type

		public List<DNode> children=new List<DNode>(); // Functions, Templates
		public DNode parent; // Functions, Templates

        public string desc;

        public List<int> modifiers;

        [NonSerialized()]
		public CodeLocation startLoc, BlockStartLocation, endLoc;

        public DNode(FieldType ftype)
        {
            Init();
            fieldtype = ftype;
        }

        protected void Init()
        {
            name = "";
            TypeToken = 0;
            this.type = "";

            value = "";
            param = new List<DNode>();
            superClass = "";
            implementedInterface = "";

            desc = "";

            modifiers = new List<int>();
            
            startLoc = new CodeLocation();
            BlockStartLocation = new CodeLocation();
            endLoc = new CodeLocation();
        }

        public DNode()
        {
            Init();
            this.fieldtype = FieldType.Variable;
        }

        public int Count
        {
            get { return children.Count; }
        }

        public DNode this[int i]
        {
			get { if(children.Count > i)return (DNode)children[i]; else return null; }
			set { if (children.Count > i) children[i] = value; }
        }

		public DNode this[string name]
		{
			get
			{
				if (children.Count > 1)
				{
					foreach (DNode n in Children)
					{
						if ((n as DNode).name == name) return (n as DNode);
					}
				}
				return null;
			}
			set
			{
				if (children.Count > 1)
				{
					for (int i = 0; i < Count;i++ )
					{
						if(this[i].name == name) this[i]=value;
					}
				}
			}
		}


		public override string ToString()
		{
			return "["+fieldtype.ToString()+"] "+type+" "+name;
		}

        public void Add(DNode v)
        {
            children.Add(v);
        }

		#region IEnumerable<DataType> Member

		public IEnumerator<DNode> GetEnumerator()
		{
			return children.GetEnumerator();
		}

		#endregion

		#region IEnumerable Member

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return new List<DNode>.Enumerator();
		}

		#endregion

		#region INode Member
        /*
		public object AcceptChildren(ICSharpCode.NRefactory.IAstVisitor visitor, object data)
		{
			return null;
		}

		public object AcceptVisitor(ICSharpCode.NRefactory.IAstVisitor visitor, object data)
		{
			return null;
		}
        */
		public List<DNode> Children
		{
			get { return children; }
		}

		public /*ICSharpCode.NRefactory.*/Location EndLocation
		{
			get
			{
                return new /*ICSharpCode.NRefactory.*/Location(endLoc.Column, endLoc.Line);
			}
			set
			{
				endLoc = new CodeLocation(value.Column,value.Line);
			}
		}

		public DNode Parent
		{
			get
			{
				return parent;
			}
			set
			{
				parent = value;
			}
		}

        public Location StartLocation
		{
			get
			{
                return new /*ICSharpCode.NRefactory.*/Location(startLoc.Column, startLoc.Line);
			}
			set
			{
				startLoc = new CodeLocation(value.Column, value.Line);
			}
		}

		public object UserData
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		#endregion
	}

    public enum FieldType
    {
        AliasDecl,
		Constructor,

        Variable,
        Function,

        Root, // root element
        Class,
        Template,
        Struct,
        Enum,
        EnumValue, // enum item
        Interface,

		Delegate
    }
}
