using System;
using System.Collections.Generic;
using System.Text;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory;

namespace D_Parser
{
	[Serializable()]
    public class DataType:IEnumerable<INode>,INode
    {
        public FieldType fieldtype;
        public int TypeToken;
        public string type;
        public string name;
		public string module;

        public string value; // Variable
        public List<INode> param; // Functions, Templates
        public string superClass, implementedInterface; // Class-Like; superClass also represents enum's base type

		public List<INode> children=new List<INode>(); // Functions, Templates
		public INode parent; // Functions, Templates

        public string desc;

        public List<int> modifiers;

        [NonSerialized()]
		public CodeLocation startLoc, endLoc;

        public DataType(FieldType ftype)
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
            param = new List<INode>();
            superClass = "";
            implementedInterface = "";

            desc = "";

            modifiers = new List<int>();
            
            startLoc = new CodeLocation();
            endLoc = new CodeLocation();
        }

        public DataType()
        {
            Init();
            this.fieldtype = FieldType.Variable;
        }

        public int Count
        {
            get { return children.Count; }
        }

        public DataType this[int i]
        {
			get { if(children.Count > i)return (DataType)children[i]; else return null; }
			set { if (children.Count > i) children[i] = value; }
        }

        public void Add(DataType v)
        {
            children.Add(v);
        }

		#region IEnumerable<DataType> Member

		public IEnumerator<INode> GetEnumerator()
		{
			return children.GetEnumerator();
		}

		#endregion

		#region IEnumerable Member

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return new List<DataType>.Enumerator();
		}

		#endregion

		#region INode Member

		public object AcceptChildren(ICSharpCode.NRefactory.IAstVisitor visitor, object data)
		{
			return null;
		}

		public object AcceptVisitor(ICSharpCode.NRefactory.IAstVisitor visitor, object data)
		{
			return null;
		}

		public List<INode> Children
		{
			get { return children; }
		}

		public Location EndLocation
		{
			get
			{
				return new Location(endLoc.Column,endLoc.Line);
			}
			set
			{
				endLoc = new CodeLocation(value.Column,value.Line);
			}
		}

		public INode Parent
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
				return new Location(startLoc.Column, startLoc.Line);
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
