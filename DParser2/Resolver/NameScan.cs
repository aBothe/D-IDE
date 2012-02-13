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

		public static IEnumerable<INode> SearchMatchesAlongNodeHierarchy(ResolverContext ctxt, CodeLocation caret, string name)
		{
			var scan = new NameScan { filterId=name };

			scan.IterateThroughScopeLayers(ctxt, caret);

			if (ctxt.ParseCache != null)
				foreach (var mod in ctxt.ParseCache)
				{
					var modNameParts = mod.ModuleName.Split('.');

					if (modNameParts[0] == name)
						scan.Matches.Add(mod);
				}

			return scan.Matches;
		}

		protected override void HandleItem(INode n)
		{
			if (n != null && n.Name == filterId)
				Matches.Add(n);
		}
	}
}
