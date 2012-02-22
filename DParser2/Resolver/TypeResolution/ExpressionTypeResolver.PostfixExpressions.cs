using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_Parser.Dom.Expressions;
using D_Parser.Dom;

namespace D_Parser.Resolver.TypeResolution
{
	public partial class ExpressionTypeResolver
	{
		public static ResolveResult[] Resolve(PostfixExpression ex, ResolverContextStack ctxt)
		{
			var baseExpression = Resolve(ex.PostfixForeExpression, ctxt);

			if (baseExpression == null)
				return null;

			// Important: To ensure correct behaviour, aliases must be removed before further handling
			baseExpression = DResolver.TryRemoveAliasesFromResult(baseExpression);

			if (baseExpression == null ||
				ex is PostfixExpression_Increment || // myInt++ is still of type 'int'
				ex is PostfixExpression_Decrement)
				return baseExpression;


			if (ex is PostfixExpression_Access)
				return Resolve(ex as PostfixExpression_Access, ctxt, baseExpression);

			else if (ex is PostfixExpression_MethodCall)
			{
				var call = ex as PostfixExpression_MethodCall;

				/*
				 * int a() { return 1+2; }
				 * 
				 * int result = a() // a is member method -- return the method's base type
				 * 
				 */

				var resolvedCallArguments = new List<ResolveResult[]>();

				// Note: If an arg wasn't able to be resolved (returns null) - add it anyway to keep the indexes parallel
				if (call.Arguments != null)
					foreach (var arg in call.Arguments)
						resolvedCallArguments.Add(ExpressionTypeResolver.Resolve(arg, ctxt));

				/*
				 * std.stdio.writeln(123) does actually contain
				 * a template instance argument: writeln!int(123);
				 * So although there's no explicit type given, 
				 * TemplateParameters will still contain static type int!
				 * 
				 * Therefore, and only if no writeln!int was given as foreexpression like in writeln!int(123),
				 * try to match the call arguments with template parameters.
				 * 
				 * If no template parameters were required, baseExpression will remain untouched.
				 */

				var resultsWithResolvedTemplateArgs =
					call.PostfixForeExpression is TemplateInstanceExpression ?
					baseExpression :
					TemplateInstanceParameterHandler.ResolveAndFilterTemplateResults(resolvedCallArguments, baseExpression, ctxt);

				//TODO: Compare arguments' types with parameter types to whitelist legal method overloads

				return resultsWithResolvedTemplateArgs;
			}

			var r = new List<ResolveResult>(baseExpression.Length);
			foreach (var b in baseExpression)
			{
				if (ex is PostfixExpression_MethodCall)
				{
					if (b is MemberResult)
					{
						var mr = b as MemberResult;

						/*
						 * opCall overloads are possible
						 */

						if (mr.MemberBaseTypes != null)
							r.AddRange(mr.MemberBaseTypes);
					}
					else if (b is DelegateResult)
					{
						var dg = b as DelegateResult;

						// Should never happen
						if (dg.IsDelegateDeclaration)
							return null;

						/*
						 * int a = delegate(x) { return x*2; } (12); // a is 24 after execution
						 * dg() , where as dg is a delegate
						 */

						return dg.ReturnType;
					}
					else if (b is TypeResult)
					{
						/*
						 * auto a = MyStruct(); -- opCall-Overloads can be used
						 */
						var classDef = (b as TypeResult).ResolvedTypeDefinition as DClassLike;

						if (classDef == null)
							continue;

						//TODO: Regard protection attributes for opCall members
						foreach (var i in classDef)
							if (i.Name == "opCall" &&
								i is DMethod &&
								(i as DNode).IsStatic)
								r.Add(TypeDeclarationResolver.HandleNodeMatch(i, ctxt, b, ex));
					}
				}

				else if (ex is PostfixExpression_Index)
				{
					/*
					 * return the value type of a given array result
					 */
					//TODO
				}
				else if (ex is PostfixExpression_Slice)
				{
					/*
					 * like above 
					 */
					//TODO
				}
			}

			if (r.Count > 0)
				return r.ToArray();
			return null;
		}



		public static ResolveResult[] Resolve(PostfixExpression_Access acc, ResolverContextStack ctxt, IEnumerable<ResolveResult> resultBases = null)
		{
			var baseExpression = resultBases ?? Resolve(acc.PostfixForeExpression, ctxt);

			if (acc.TemplateInstance != null)
				return Resolve(acc.TemplateInstance, ctxt, baseExpression);
			else if (acc.NewExpression != null)
			{
				/*
				 * This can be both a normal new-Expression as well as an anonymous class declaration!
				 */
				//TODO!
			}
			else if (acc.Identifier != null)
			{
				/*
				 * First off, try to resolve the identifier as it was a type declaration's identifer list part
				 */
				var results = TypeDeclarationResolver.ResolveFurtherTypeIdentifier(
					acc.Identifier,
					baseExpression,
					ctxt,
					acc);

				if (results != null)
					return results;

				/*
				 * Handle cases which can occur in an expression context only
				 */

				foreach (var b in baseExpression)
				{
					/*
					 * 1) Static properties
					 * 2) ??
					 */
					var staticTypeProperty = StaticPropertyResolver.TryResolveStaticProperties(b, acc.Identifier, ctxt);

					if (staticTypeProperty != null)
						return new[] { staticTypeProperty };
				}
			}
			else
				return baseExpression.ToArray();

			return null;
		}

	}
}
