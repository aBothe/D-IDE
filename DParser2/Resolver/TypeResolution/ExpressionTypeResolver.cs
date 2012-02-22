using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_Parser.Dom.Expressions;
using D_Parser.Parser;
using D_Parser.Dom;

namespace D_Parser.Resolver.TypeResolution
{
	public partial class ExpressionTypeResolver
	{
		public static ResolveResult[] Resolve(IExpression ex, ResolverContextStack ctxt)
		{
			#region Operand based/Trivial expressions
			if (ex is Expression) // a,b,c;
			{
				return null;
			}

			else if (ex is SurroundingParenthesesExpression)
				return Resolve((ex as SurroundingParenthesesExpression).Expression, ctxt);

			else if (ex is AssignExpression || // a = b
				ex is XorExpression || // a ^ b
				ex is OrExpression || // a | b
				ex is AndExpression || // a & b
				ex is ShiftExpression || // a << 8
				ex is AddExpression || // a += b; a -= b;
				ex is MulExpression || // a *= b; a /= b; a %= b;
				ex is CatExpression || // a ~= b;
				ex is PowExpression) // a ^^ b;
				return Resolve((ex as OperatorBasedExpression).LeftOperand, ctxt);

			else if (ex is ConditionalExpression) // a ? b : c
				return Resolve((ex as ConditionalExpression).TrueCaseExpression, ctxt);

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

				return Resolve((ex as InExpression).RightOperand, ctxt);
			}
			#endregion

			#region UnaryExpressions
			else if (ex is UnaryExpression)
			{
				if (ex is UnaryExpression_Cat) // a = ~b;
					return Resolve((ex as SimpleUnaryExpression).UnaryExpression, ctxt);

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
						castedType = Resolve(ce.UnaryExpression, ctxt);

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
					return Resolve((ex as SimpleUnaryExpression).UnaryExpression, ctxt);

				else if (ex is UnaryExpression_And)
				{
					var baseTypes = Resolve((ex as UnaryExpression_And).UnaryExpression, ctxt);

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

			else if (ex is PostfixExpression)
				return Resolve(ex as PostfixExpression, ctxt);

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
				return Resolve(ex as TemplateInstanceExpression, ctxt);

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

			#region Initializers
			//TODO: Handle initializers
			#endregion

			return null;
		}

		public static ResolveResult[] Resolve(
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
