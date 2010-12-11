using System;
using System.Collections.Generic;
using System.Text;
using Parser.Core;

namespace D_Parser
{
    public abstract class DNode :Node
    {
        public INode[] TemplateParameters=null; // Functions, Templates

        public IBlockNode Parent; // Functions, Templates

        public List<DAttribute> Attributes = new List<DAttribute>();
        public bool ContainsAttribute(params int[] Token)
        {
            return DAttribute.ContainsAttribute(Attributes, Token);
        }

        public DNode()
        {
        }

        public DNode Assign(DNode other)
        {
            TemplateParameters = other.TemplateParameters;
            Attributes = other.Attributes;
			base.Assign(other);
            return this;
        }

        public string AttributeString
        {
            get
            {
                string s = "";
                foreach (var attr in Attributes)
                    s += attr.ToString() + " ";
                return s.Trim();
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
            var curParent=this as INode;
            while (curParent != null)
            {
                // Also include module path
                if (curParent is DModule)
                    path = (curParent as DModule).ModuleName + "." + path;
                else
                    path = curParent.Name + "." + path;
                curParent = curParent.Parent;
            }
            s += path.Trim('.');

            // Template parameters
            if (TemplateParameters!=null && TemplateParameters.Length > 0)
            {
                s += "!(";
                foreach (var p in TemplateParameters)
                    s += p.ToString() + ",";
                s = s.Trim(',')+ ")";
            }
            
            return s;
        }

        public override string ToString()
        {
            return ToDeclarationString(true);
        }

        public bool IsPublic
        {
            get
            {
                return !ContainsAttribute(DTokens.Private, DTokens.Protected);
            }
        }

        public bool IsStatic
        {
            get
            {
                return ContainsAttribute(DTokens.Static);
            }
        }
	}
}
