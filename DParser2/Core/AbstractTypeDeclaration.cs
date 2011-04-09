using System;
using System.Collections.Generic;
using System.Text;

namespace D_Parser.Core
{
	public abstract class AbstractTypeDeclaration:ITypeDeclaration
	{
		private ITypeDeclaration _Base;
		public new abstract string ToString();

		public ITypeDeclaration MostBasic
		{
			get
			{ 
				if (Base == null) 
					return this; 
				else 
					return Base.MostBasic; 
			}
			set
			{
				if (Base == null)
					Base = value; 
				else 
					Base.MostBasic = value;
			}
		}

		public ITypeDeclaration Base
		{
			get
			{
				return _Base;
			}
			set
			{
				_Base = value;
			}
		}
	}
}
