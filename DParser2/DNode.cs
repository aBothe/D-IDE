using System;
using System.Collections.Generic;
using System.Text;
using D_Parser.Core;

namespace D_Parser
{
    public abstract class DNode :AbstractNode
    {
        public ITemplateParameter[] TemplateParameters=null; // Functions, Templates

		public bool ContainsTemplateParameter(string Name)
		{
			if (TemplateParameters != null)
				foreach (var tp in TemplateParameters)
					if (tp.Name == Name)
						return true;

			return false;
		}

		public IEnumerable<TemplateParameterNode> TemplateParameterNodes
		{
			get {
				if (TemplateParameters != null)
					foreach (var p in TemplateParameters)
						yield return new TemplateParameterNode(p);
			}
		}

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
		
        public override void Assign(INode other)
        {
			if (other is DNode)
			{
				TemplateParameters = (other as DNode).TemplateParameters;
				Attributes = (other as DNode).Attributes;
			}
			base.Assign(other);
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
        public override string ToString(bool Attributes,bool IncludePath)
        {
			string s = ""; 
				
			if(Attributes)
				s=AttributeString+" ";

			s += base.ToString(Attributes,IncludePath);

            // Template parameters
            if (TemplateParameters!=null && TemplateParameters.Length > 0)
            {
				if (this is DVariable)
					s += '!';

                s += "(";
				foreach (var p in TemplateParameters)
					s += p.ToString() + ",";

                s = s.Trim(',')+ ")";
            }
            
            return s.Trim();
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
