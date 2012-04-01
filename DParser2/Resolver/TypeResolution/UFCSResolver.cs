using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_Parser.Dom.Expressions;
using D_Parser.Resolver.ASTScanner;
using D_Parser.Dom;

namespace D_Parser.Resolver.TypeResolution
{
	/// <summary>
	/// UFCS: User function call syntax;
	/// A base expression will be used as a method'd first call parameter 
	/// so it looks like the first expression had a respective sub-method.
	/// Example:
	/// assert("fdas".reverse() == "asdf"); -- reverse() will be called with "fdas" as the first argument.
	/// 
	/// </summary>
	public class UFCSResolver
	{
		public static ResolveResult[] TryResolveUFCS(
			ResolveResult firstArgument, 
			PostfixExpression_Access acc, 
			ResolverContextStack ctxt)
		{
			var name="";

			if (acc.AccessExpression is IdentifierExpression)
				name = ((IdentifierExpression)acc.AccessExpression).Value as string;
			else if (acc.AccessExpression is TemplateInstanceExpression)
				name = ((TemplateInstanceExpression)acc.AccessExpression).TemplateIdentifier.Id;
			else
				return null;


			var vis = new UFCSVisitor(ctxt) {
				FirstParamToCompareWith=firstArgument,
				NameToSearch=name
			};

			vis.IterateThroughScopeLayers(acc.Location);

			if (vis.Match != null)
				return new[] {TypeDeclarationResolver.HandleNodeMatch(vis.Match, ctxt, null, acc)};

			return null;
		}

		public class UFCSVisitor : AbstractVisitor
		{
			public UFCSVisitor(ResolverContextStack ctxt) : base(ctxt) { }

			public string NameToSearch;
			public ResolveResult FirstParamToCompareWith;

			public DMethod Match;

			protected override bool HandleItem(INode n)
			{
				if (n.Name == NameToSearch && n is DMethod)
				{
					var dm = (DMethod)n;

					if (dm.Parameters.Count != 0)
					{
						var firstParam = TypeResolution.TypeDeclarationResolver.Resolve(dm.Parameters[0].Type,Context);

						//TODO: Compare the resolved parameter with the first parameter given
						if (true)
						{
							Match = dm;
							return true;
						}
					}
				}

				return false;
			}
		}
	}
}
