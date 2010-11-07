using System;
using System.Collections.Generic;
using System.Text;

namespace D_Parser
{
    /// <summary>
    /// Encapsules an entire document and represents the root node
    /// </summary>
    public class DModule : DBlockStatement
    {
        public string ModuleName="";
        public string ModuleFileName="";

        public List<string> Imports = new List<string>();

        public DModule():base(FieldType.Root) { }

        public void ApplyFrom(DModule Other)
        {
            ModuleFileName = Other.ModuleFileName;
            Children.Clear();
            Children.AddRange(Other.Children);
            Imports.Clear();
            Imports.AddRange(Other.Imports);
            this.fieldtype = Other.fieldtype;
        }
    }

    public class DVariable : DNode
    {
        public DExpression Initializer; // Variable

        public DVariable()
            :base(FieldType.Variable)
        {
        }

        public override string ToString()
        {
            return base.ToString()+(Initializer!=null?(" = "+Initializer.ToString()):"");
        }
    }

    public class DBlockStatement : DNode, IEnumerable<DNode>
    {
        public CodeLocation BlockStartLocation=new CodeLocation();

        public DBlockStatement() {}
        public DBlockStatement(FieldType Field) { fieldtype = Field; }

        public DNode Assign(DBlockStatement block)
        {
            children = block.children;
            BlockStartLocation = block.BlockStartLocation;
            return base.Assign(block as DNode);
        }

        public List<DNode> children = new List<DNode>();

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
                        if ((n as DNode).Name == name) return (n as DNode);
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
                        if (this[i].Name == name) this[i] = value;
                    }
                }
            }
        }

        public void Add(DNode v)
        {
            children.Add(v);
        }

        public List<DNode> Children
        {
            get { return children; }
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

    public class DMethod : DBlockStatement
    {
        public List<DNode> Parameters=new List<DNode>();

        public DMethod()
            : base(FieldType.Function)
        {

        }
    }

    public class DClassLike : DBlockStatement
    {
        public List<TypeDeclaration> BaseClasses=new List<TypeDeclaration>();

        public DClassLike()
            : base(FieldType.Class)
        {

        }
    }

    public class DEnum : DBlockStatement
    {
        public TypeDeclaration EnumBaseType;

        public DEnum()
            : base(FieldType.Enum)
        {

        }
    }

    public class DEnumValue : DVariable
    {
        public DEnumValue()
        {
            fieldtype = FieldType.EnumValue;
        }
    }
}
