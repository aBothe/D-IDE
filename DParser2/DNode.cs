using System;
using System.Collections.Generic;
using System.Text;

namespace D_Parser
{
    public abstract class DNode
    {
        public FieldType fieldtype = FieldType.Variable;
        public int TypeToken=0;
        public TypeDeclaration Type;
        public string Name;

        public List<DNode> TemplateParameters=null; // Functions, Templates

        public DNode Parent; // Functions, Templates

        public string Description="";

        public List<DAttribute> Attributes = new List<DAttribute>();
        public bool ContainsAttribute(int Token)
        {
            return DAttribute.ContainsAttribute(Attributes, Token);
        }

        public CodeLocation StartLocation,EndLocation;

        public DNode(FieldType ftype)
        {
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
            StartLocation = other.StartLocation;
            EndLocation = other.EndLocation;
            return this;
        }

        public DNode NodeRoot
        {
            get
            {
                if (this is DModule || Parent==null)
                    return this;
                else return Parent.NodeRoot;
            }
        }

        public DNode()
        {}

        public string AttributeString
        {
            get
            {
                string s = "";
                foreach (var attr in Attributes)
                    s += attr.ToString() + " ";
                s.Trim();
                return s;
            }
        }

        /// <summary>
        /// Returns attributes, type and name combined to one string
        /// </summary>
        /// <returns></returns>
        public string ToDeclarationString(bool Attributes)
        {
            string s = "";
            // Attributes
            if(Attributes)
                s = AttributeString+" ";

            // Type
            if (Type != null)
                s += Type.ToString()+" ";

            // Path + Name
            string path="";
            var curParent=this;
            while (curParent != null)
            {
                path = curParent.Name + "." + path;
                curParent = curParent.Parent;
            }
            s += path;
            
            return s;
        }

        public override string ToString()
        {
            return ToDeclarationString(true);
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
