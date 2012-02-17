using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_Parser.Dom;
using D_Parser.Parser;
using D_Parser.Dom.Expressions;

namespace D_Parser.Resolver
{
	public partial class TypeDeclarationResolver
	{
		public static ResolveResult[] ResolveBasicType(ITypeDeclaration declaration, ResolverContextStack ctxt)
		{
			if (declaration is DTokenDeclaration)
			{
				var token = (declaration as DTokenDeclaration).Token;

				if (DTokens.BasicTypes[token])
					return new[]{
						new StaticTypeResult
						{
							BaseTypeToken = token,
							DeclarationOrExpressionBase = declaration
						}
					};

				// Anything else is just a keyword, not a type
				return null;
			}
			else if (declaration is IdentifierDeclaration)
			{
				var id = declaration as IdentifierDeclaration;

				var matches = NameScan.SearchMatchesAlongNodeHierarchy(ctxt, declaration.Location, id);

				return DResolver.HandleNodeMatches(matches, ctxt, null, declaration);
			}
			else if (declaration is TemplateInstanceExpression)
				return ExpressionResolver.ResolveTemplateInstance(declaration as TemplateInstanceExpression, ctxt);
			else if (declaration is TypeOfDeclaration)
			{
				var typeOf = declaration as TypeOfDeclaration;

				// typeof(return)
				if (typeOf.InstanceId is TokenExpression && (typeOf.InstanceId as TokenExpression).Token == DTokens.Return)
				{
					var m = DResolver.HandleNodeMatch(ctxt.ScopedBlock, ctxt, null, declaration);
					if (m != null)
						return new[] { m };
				}
				// typeOf(myInt) === int
				else if (typeOf.InstanceId != null)
				{
					var wantedTypes = ExpressionResolver.ResolveExpression(typeOf.InstanceId, ctxt);

					if (wantedTypes == null)
						return null;

					// Scan down for variable's base types
					var c1 = new List<ResolveResult>(wantedTypes);
					var c2 = new List<ResolveResult>();
					var ret = new List<ResolveResult>();

					while (c1.Count > 0)
					{
						foreach (var t in c1)
						{
							if (t is MemberResult)
							{
								if ((t as MemberResult).MemberBaseTypes != null)
									c2.AddRange((t as MemberResult).MemberBaseTypes);
							}
							else
								ret.Add(t);
						}

						c1.Clear();
						c1.AddRange(c2);
						c2.Clear();
					}

					return ret.ToArray();
				}
			}

			return null;
		}
	}
}
