using System;
using System.Collections.Generic;
using System.Text;

namespace D_Parser
{
    public class DNode : IEnumerable<DNode>
    {
        public FieldType fieldtype;
        public int TypeToken;
        public TypeDeclaration Type;
        public string name;
        public string module;

        public List<DNode> TemplateParameters=new List<DNode>(); // Functions, Templates

        public List<DNode> children = new List<DNode>(); // Functions, Templates
        public DNode Parent; // Functions, Templates

        public string desc;

        public List<int> modifiers;

        public CodeLocation startLoc, BlockStartLocation, endLoc;

        public DNode(FieldType ftype)
        {
            Init();
            fieldtype = ftype;
        }

        public DNode Assign(DNode other)
        {
            //fieldtype = other.fieldtype;
            TypeToken = other.TypeToken;
            Type = other.Type;
            name = other.name;
            module = other.module;
            TemplateParameters = other.TemplateParameters;

            children = other.Children;
            Parent = other.Parent;
            desc = other.desc;
            modifiers = other.modifiers;
            startLoc = other.startLoc;
            BlockStartLocation = other.BlockStartLocation;
            endLoc = other.endLoc;
            return this;
        }

        protected void Init()
        {
            name = "";
            TypeToken = 0;

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
            get { if (children.Count > i)return (DNode)children[i]; else return null; }
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
                    for (int i = 0; i < Count; i++)
                    {
                        if (this[i].name == name) this[i] = value;
                    }
                }
            }
        }


        public override string ToString()
        {
            return "[" + fieldtype.ToString() + "] " + (Type!=null?Type.ToString():"") + " " + name;
        }

        public void Add(DNode v)
        {
            children.Add(v);
        }

        public List<DNode> Children
        {
            get { return children; }
        }

        public Location EndLocation
        {
            get
            {
                return new Location(endLoc.Column, endLoc.Line);
            }
            set
            {
                endLoc = new CodeLocation(value.Column, value.Line);
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

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return children.GetEnumerator();
        }

        IEnumerator<DNode> IEnumerable<DNode>.GetEnumerator()
        {
            return children.GetEnumerator();
        }
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
