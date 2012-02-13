using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_Parser.Dom;

namespace D_Parser.Resolver
{
	public class NameScan : RootsEnum
	{
		string filterId;
		public List<INode> Matches = new List<INode>();

		protected override void HandleItem(INode n)
		{
			throw new NotImplementedException();
		}
	}
}
