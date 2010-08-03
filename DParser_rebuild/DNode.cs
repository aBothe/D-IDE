using System;
using System.Collections.Generic;
using System.Text;

namespace D_Parser
{
    public class DNode
    {
        public FieldType fieldtype;
        public int TypeToken=0;
        public TypeDeclaration Type;
        public string Name;

        public List<DNode> TemplateParameters=new List<DNode>(); // Functions, Templates

        public DNode Parent; // Functions, Templates

        public string Description="";

        public List<string> Attributes=new List<string>();

        public CodeLocation startLoc, endLoc;

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
            Name = other.Name;
            TemplateParameters = other.TemplateParameters;

            Parent = other.Parent;
            Description = other.Description;
            Attributes = other.Attributes;
            startLoc = other.startLoc;
            endLoc = other.endLoc;
            return this;
        }

        protected void Init()
        {
            startLoc = new CodeLocation();
            endLoc = new CodeLocation();
        }

        public DNode()
        {
            Init();
            this.fieldtype = FieldType.Variable;
        }

        public override string ToString()
        {
            return "[" + fieldtype.ToString() + "] " + (Type!=null?Type.ToString():"") + " " + Name;
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
    }

    public enum FieldType
    {
        AliasDecl,
        Constructor,

        EnumValue, // enum item
        Delegate,

        Variable,
        Function, // is treated as a block statement

        Root, // root element
        Block, // a field that is able to contain some children

        // Special kinds of blocks
        Class,
        Template,
        Struct,
        Enum,
        Interface
    }
}
