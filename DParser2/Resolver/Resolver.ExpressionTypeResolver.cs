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
				ex is IdentifierExpression || // a is T
				ex is RelExpression) // a <= b
				return new[] { TypeDeclarationResolver.Resolve(new DTokenDeclaration(DTokens.Bool)) };

			else if (ex is InExpression) // a in b
			{
				// The return value of the InExpression is null if the element is not in the array; 
				// if it is in the array it is a pointer to the element.

				return ResolveExpression((ex as InExpression).RightOperand, ctxt);
			}

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

			else if (ex is PostfixExpression)
			{
				var baseExpression = ResolveExpression((ex as PostfixExpression).PostfixForeExpression, ctxt);

				if (ex is PostfixExpression_Increment || ex is PostfixExpression_Decrement)
					return baseExpression;

				else if (ex is PostfixExpression_MethodCall)
				{

				}

				else if (ex is PostfixExpression_Access)
				{
					var acc = ex as PostfixExpression_Access;

					if (acc.Identifier != null)
					{

					}
					else if (acc.TemplateInstance != null)
					{

					}
					else if (acc.NewExpression != null)
					{

					}
				}

				else if (ex is PostfixExpression_Index)
				{

				}
				else if (ex is PostfixExpression_Slice)
				{

				}
			}

			else if (ex is IdentifierExpression)
			{
				/*
				 * Can contain (char/string/integer/float) literals,too!
				 */
			}

			else if (ex is TemplateInstanceExpression)
			{

			}

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
						var res = DResolver.HandleNodeMatch(classDef, ctxt, null, ex);

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

			}

			else if (ex is AssocArrayExpression)
			{

			}

			else if (ex is FunctionLiteral)
			{

			}

			else if (ex is AssertExpression)
			{

			}

			else if (ex is MixinExpression)
			{

			}

			else if (ex is ImportExpression)
			{

			}

			else if (ex is TypeDeclarationExpression) // should be containing a typeof() only
				return TypeDeclarationResolver.Resolve((ex as TypeDeclarationExpression).Declaration, ctxt);

			else if (ex is TypeidExpression)
				return TypeDeclarationResolver.Resolve(new IdentifierDeclaration("TypeInfo") { InnerDeclaration = new IdentifierDeclaration("object") }, ctxt);

			else if (ex is IsExpression)
				return new[]{ TypeDeclarationResolver.Resolve(new DTokenDeclaration(DTokens.Int)) };

			else if (ex is TraitsExpression)
			{
				// TODO: Return either bools, strings, array pointers
			}

			return null;
		}

		public static ResolveResult[] ResolveTemplateInstance(TemplateInstanceExpression tix, ResolverContextStack ctxt, ResolveResult[] resolvedBases=null)
		{
			return null;
		}
	}
}
