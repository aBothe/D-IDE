using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_Parser.Dom.Expressions;
using D_Parser.Parser;
using D_Parser.Dom;

namespace D_Parser.Resolver
{
	public partial class ExpressionResolver
	{
		public static ResolveResult[] ResolveExpression(IExpression ex, ResolverContextStack ctxt)
		{
			if (ex is TokenExpression)
			{
				var token=(ex as TokenExpression).Token;

				// References current class scope
				if (token == DTokens.This)
				{
					var classDef = ctxt.ScopedBlock;

					while (!(classDef is DClassLike) && classDef != null)
						classDef = classDef.Parent as IBlockNode;

					if (classDef is DClassLike)
					{
						var res = HandleNodeMatch(classDef, ctxt, typeBase: declaration);

						if (res != null)
							returnedResults.Add(res);
					}
				}
				// References super type of currently scoped class declaration
				else if (token == DTokens.Super)
				{
					var classDef = ctxt.ScopedBlock;

					while (!(classDef is DClassLike) && classDef != null)
						classDef = classDef.Parent as IBlockNode;

					if (classDef != null)
					{
						var baseClassDefs = ResolveBaseClass(classDef as DClassLike, ctxt);

						if (baseClassDefs != null)
						{
							// Important: Overwrite type decl base with 'super' token
							foreach (var bc in baseClassDefs)
								bc.DeclarationOrExpressionBase = declaration;

							returnedResults.AddRange(baseClassDefs);
						}
					}
				}
			}

			return null;
		}

		public static ResolveResult[] ResolveTemplateInstance(TemplateInstanceExpression tix, ResolverContextStack ctxt)
		{

		}
	}
}
