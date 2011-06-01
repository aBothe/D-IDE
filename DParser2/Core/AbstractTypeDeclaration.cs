using System;
using System.Collections.Generic;
using System.Text;

namespace D_Parser.Core
{
	public abstract class AbstractTypeDeclaration:ITypeDeclaration
	{
		public ITypeDeclaration InnerMost
		{
			get
			{ 
				if (InnerDeclaration == null) 
					return this; 
				else 
					return InnerDeclaration.InnerMost; 
			}
			set
			{
				if (InnerDeclaration == null)
					InnerDeclaration = value; 
				else 
					InnerDeclaration.InnerMost = value;
			}
		}

		public ITypeDeclaration InnerDeclaration
		{
			get;
			set;
		}
	}
}
