using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_Parser.Dom.Expressions;
using D_Parser.Parser;
using D_Parser.Dom;

namespace D_Parser.Resolver
{
	public partial class ExpressionTypeResolver
	{
		public static ResolveResult[] ResolveExpression(IExpression ex, ResolverContextStack ctxt)
		{
			#region Operand based/Trivial expressions
			if (ex is Expression) // a,b,c;
			{
				return null;
			}

			else if (ex is SurroundingParenthesesExpression)
				return ResolveExpression((ex as SurroundingParenthesesExpression).Expression, ctxt);

			else if (ex is AssignExpression || // a = b
				ex is XorExpression || // a ^ b
				ex is OrExpression || // a | b
				ex is AndExpression || // a & b
				ex is ShiftExpression || // a << 8
				ex is AddExpression || // a += b; a -= b;
				ex is MulExpression || // a *= b; a /= b; a %= b;
				ex is CatExpression || // a ~= b;
				ex is PowExpression) // a ^^ b;
				return ResolveExpression((ex as OperatorBasedExpression).LeftOperand, ctxt);

			else if (ex is ConditionalExpression) // a ? b : c
				return ResolveExpression((ex as ConditionalExpression).TrueCaseExpression, ctxt);

			else if (ex is OrOrExpression || // a || b
				ex is AndAndExpression || // a && b
				ex is EqualExpression || // a==b
				ex is IdendityExpression || // a is T
				ex is RelExpression) // a <= b
				return new[] { TypeDeclarationResolver.Resolve(new DTokenDeclaration(DTokens.Bool)) };

			else if (ex is InExpression) // a in b
			{
				// The return value of the InExpression is null if the element is not in the array; 
				// if it is in the array it is a pointer to the element.

				return ResolveExpression((ex as InExpression).RightOperand, ctxt);
			}
			#endregion

			#region UnaryExpressions
			else if (ex is UnaryExpression)
			{
				if (ex is UnaryExpression_Cat) // a = ~b;
					return ResolveExpression((ex as SimpleUnaryExpression).UnaryExpression, ctxt);

				else if (ex is NewExpression)
				{
					// http://www.d-programming-language.org/expression.html#NewExpression
					var nex = ex as NewExpression;

					/*
					 * TODO: Determine argument types and select respective ctor method
					 */

					return TypeDeclarationResolver.Resolve(nex.Type, ctxt);
				}


				else if (ex is CastExpression)
				{
					var ce = ex as CastExpression;

					ResolveResult[] castedType = null;

					if (ce.Type != null)
						castedType = TypeDeclarationResolver.Resolve(ce.Type, ctxt);
					else
					{
						castedType = ResolveExpression(ce.UnaryExpression, ctxt);

						if (castedType != null && ce.CastParamTokens != null && ce.CastParamTokens.Length > 0)
						{
							//TODO: Wrap resolved type with member function attributes
						}
					}
				}

				else if (ex is UnaryExpression_Add ||
					ex is UnaryExpression_Decrement ||
					ex is UnaryExpression_Increment ||
					ex is UnaryExpression_Sub ||
					ex is UnaryExpression_Not ||
					ex is UnaryExpression_Mul)
					return ResolveExpression((ex as SimpleUnaryExpression).UnaryExpression, ctxt);

				else if (ex is UnaryExpression_And)
				{
					var baseTypes = ResolveExpression((ex as UnaryExpression_And).UnaryExpression, ctxt);

					// TODO: Wrap resolved type with pointer declaration

					return baseTypes;
				}
				else if (ex is DeleteExpression)
					return null;
				else if (ex is UnaryExpression_Type)
				{
					var uat = ex as UnaryExpression_Type;

					if (uat.Type == null)
						return null;

					var type = TypeDeclarationResolver.Resolve(uat.Type, ctxt);
					var id = new IdentifierDeclaration(uat.AccessIdentifier);

					foreach (var t in type)
					{
						var statProp = StaticPropertyResolver.TryResolveStaticProperties(t, id, ctxt);

						if (statProp != null)
							return new[] { statProp };
					}

					return TypeDeclarationResolver.Resolve(id, ctxt, type);
				}
			}
			#endregion

			#region PostfixExpressions
			else if (ex is PostfixExpression)
			{
				var baseExpression = ResolveExpression((ex as PostfixExpression).PostfixForeExpression, ctxt);

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
							resolvedCallArguments.Add(ExpressionTypeResolver.ResolveExpression(arg, ctxt));

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
						baseExpression:
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
								if (i.Name == "opCall" && i is DMethod)
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
			}
			#endregion

			#region PrimaryExpressions
			else if (ex is IdentifierExpression)
			{
				var id = ex as IdentifierExpression;

				if (id.IsIdentifier)
					return TypeDeclarationResolver.ResolveIdentifier(id.Value as string, ctxt, id);
				//TODO: Recognize correct scalar format, i.e. 0.5 => double; 0.5f => float; char, wchar, dchar
				else if (id.Format == LiteralFormat.CharLiteral)
					return new[] { TypeDeclarationResolver.Resolve(new DTokenDeclaration(DTokens.Char)) };
				else if (id.Format.HasFlag(LiteralFormat.FloatingPoint))
					return new[] { TypeDeclarationResolver.Resolve(new DTokenDeclaration(DTokens.Float)) };
				else if (id.Format == LiteralFormat.StringLiteral || id.Format.HasFlag(LiteralFormat.VerbatimStringLiteral))
					return TypeDeclarationResolver.ResolveIdentifier("string", ctxt, ex);
			}

			else if (ex is TemplateInstanceExpression)
				return ResolveTemplateInstance(ex as TemplateInstanceExpression, ctxt);

			else if (ex is TokenExpression)
			{
				var token = (ex as TokenExpression).Token;

				// References current class scope
				if (token == DTokens.This)
				{
					var classDef = ctxt.ScopedBlock;

					while (!(classDef is DClassLike) && classDef != null)
						classDef = classDef.Parent as IBlockNode;

					if (classDef is DClassLike)
					{
						var res = TypeDeclarationResolver.HandleNodeMatch(classDef, ctxt, null, ex);

						if (res != null)
							return new[] { res };
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
						var baseClassDefs = DResolver.ResolveBaseClass(classDef as DClassLike, ctxt);

						if (baseClassDefs != null)
						{
							// Important: Overwrite type decl base with 'super' token
							foreach (var bc in baseClassDefs)
								bc.DeclarationOrExpressionBase = ex;

							return baseClassDefs;
						}
					}
				}
			}

			else if (ex is ArrayLiteralExpression)
			{
				//TODO
			}

			else if (ex is AssocArrayExpression)
			{
				//TODO
			}

			else if (ex is FunctionLiteral)
			{
				//TODO Return simple DelegateResult
			}

			else if (ex is AssertExpression)
				return new[] { TypeDeclarationResolver.Resolve(new DTokenDeclaration(DTokens.Void)) };

			else if (ex is MixinExpression)
			{
				/*
				 * 1) Evaluate the mixin expression
				 * 2) Parse it as an expression
				 * 3) Evaluate the expression's type
				 */
				//TODO
			}

			else if (ex is ImportExpression)
				return TypeDeclarationResolver.ResolveIdentifier("string", ctxt, null);

			else if (ex is TypeDeclarationExpression) // should be containing a typeof() only
				return TypeDeclarationResolver.Resolve((ex as TypeDeclarationExpression).Declaration, ctxt);

			else if (ex is TypeidExpression)
				return TypeDeclarationResolver.Resolve(new IdentifierDeclaration("TypeInfo") { InnerDeclaration = new IdentifierDeclaration("object") }, ctxt);

			else if (ex is IsExpression)
				return new[] { TypeDeclarationResolver.Resolve(new DTokenDeclaration(DTokens.Int)) };

			else if (ex is TraitsExpression)
			{
				// TODO: Return either bools, strings, array (pointers) to members or stuff
			}
			#endregion

			else if (ex is TypeDeclarationExpression)
				return TypeDeclarationResolver.Resolve((ex as TypeDeclarationExpression).Declaration, ctxt);

			return null;
		}

		public static ResolveResult[] Resolve(PostfixExpression_Access acc, ResolverContextStack ctxt, IEnumerable<ResolveResult> resultBases=null)
		{
			var baseExpression = resultBases ?? ResolveExpression(acc.PostfixForeExpression, ctxt);

			if (acc.TemplateInstance != null)
				return ResolveTemplateInstance(acc.TemplateInstance, ctxt, baseExpression);
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

		public static ResolveResult[] ResolveTemplateInstance(
			TemplateInstanceExpression tix, 
			ResolverContextStack ctxt, 
			IEnumerable<ResolveResult> resultBases=null)
		{
			ResolveResult[] r = null;

			if (resultBases == null)
				r = TypeDeclarationResolver.ResolveIdentifier(tix.TemplateIdentifier.Id, ctxt,tix);
			else
				r = TypeDeclarationResolver.ResolveFurtherTypeIdentifier(tix.TemplateIdentifier.Id, resultBases, ctxt, tix);

			return TemplateInstanceParameterHandler.ResolveAndFilterTemplateResults(tix, r, ctxt);
		}
	}
}
