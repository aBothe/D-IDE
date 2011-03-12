using System;
using System.Collections.Generic;
using System.Text;
using Parser.Core;

namespace D_Parser
{
    public abstract class DNode :AbstractNode
    {
        public INode[] TemplateParameters=null; // Functions, Templates

        public new IBlockNode Parent{get{return base.Parent as IBlockNode;}
		set{base.Parent=value;}} // Functions, Templates

        public List<DAttribute> Attributes = new List<DAttribute>();
        public bool ContainsAttribute(params int[] Token)
        {
            return DAttribute.ContainsAttribute(Attributes, Token);
        }

        public DNode()
        {
        }
		
        public new INode Assign(INode other)
        {
			if (other is DNode)
			{
				TemplateParameters = (other as DNode).TemplateParameters;
				if(TemplateParameters!=null)
				foreach (var tp in TemplateParameters)
					tp.Parent = this;
				Attributes = (other as DNode).Attributes;
			}
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
        public string ToString(bool Attributes)
        {
            string s = "";
            // Attributes
            if(Attributes)
                s = AttributeString+" ";

			s += base.ToString();

            // Template parameters
            if (TemplateParameters!=null && TemplateParameters.Length > 0)
            {
                s += "!(";
                foreach (var p in TemplateParameters)
                    s += p.ToString() + ",";
                s = s.Trim(',')+ ")";
            }
            
            return s.Trim();
        }

        public override string ToString()
        {
            return ToString(true);
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
