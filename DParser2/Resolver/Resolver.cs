using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using D_Parser.Completion;
using D_Parser.Dom;
using D_Parser.Dom.Expressions;
using D_Parser.Dom.Statements;
using D_Parser.Parser;

namespace D_Parser.Resolver
{
	/// <summary>
	/// Generic class for resolve module relations and/or declarations
	/// </summary>
	public partial class DResolver
	{
		#region ResolveType
		public static ResolveResult[] ResolveType(IEditorData editor,
			ResolverContextStack ctxt,
			bool alsoParseBeyondCaret = false,
			bool onlyAssumeIdentifierList = false)
		{
			var code = editor.ModuleCode;

			int start = 0;
			CodeLocation startLocation=CodeLocation.Empty;
			bool IsExpression = false;
			
			if (ctxt.CurrentContext.ScopedStatement is IExpressionContainingStatement)
			{
				var exprs=(ctxt.CurrentContext.ScopedStatement as IExpressionContainingStatement).SubExpressions;
				IExpression targetExpr = null;

				if(exprs!=null)
					foreach (var ex in exprs)
						if ((targetExpr = ExpressionHelper.SearchExpressionDeeply(ex, editor.CaretLocation))
							!=ex)
							break;

				if (targetExpr != null && editor.CaretLocation >= targetExpr.Location && editor.CaretLocation <= targetExpr.EndLocation)
				{
					startLocation = targetExpr.Location;
					start = DocumentHelper.LocationToOffset(editor.ModuleCode, startLocation);
					IsExpression = true;
				}
			}
			
			if(!IsExpression)
			{
				// First check if caret is inside a comment/string etc.
				int lastNonNormalStart = 0;
				int lastNonNormalEnd = 0;
				var caretContext = CaretContextAnalyzer.GetTokenContext(code, editor.CaretOffset, out lastNonNormalStart, out lastNonNormalEnd);

				// Return if comment etc. found
				if (caretContext != TokenContext.None)
					return null;

				start = CaretContextAnalyzer.SearchExpressionStart(code, editor.CaretOffset - 1,
					(lastNonNormalEnd > 0 && lastNonNormalEnd < editor.CaretOffset) ? lastNonNormalEnd : 0);
				startLocation = DocumentHelper.OffsetToLocation(editor.ModuleCode, start);
			}

			if (start < 0 || editor.CaretOffset<=start)
				return null;

			var expressionCode = code.Substring(start, alsoParseBeyondCaret ? code.Length - start : editor.CaretOffset - start);

			var parser = DParser.Create(new StringReader(expressionCode));
			parser.Lexer.SetInitialLocation(startLocation);
			parser.Step();

			if (!IsExpression && onlyAssumeIdentifierList && parser.Lexer.LookAhead.Kind == DTokens.Identifier)
				return TypeDeclarationResolver.Resolve(parser.IdentifierList(), ctxt);
			else if (IsExpression || parser.IsAssignExpression())
			{
				var expr = parser.AssignExpression();

				if (expr != null)
				{
					// Do not accept number literals but (100.0) etc.
					if (expr is IdentifierExpression && (expr as IdentifierExpression).Format.HasFlag(LiteralFormat.Scalar))
						return null;

					expr = ExpressionHelper.SearchExpressionDeeply(expr, editor.CaretLocation);

					return ExpressionTypeResolver.ResolveExpression(expr, ctxt);
				}
			}
			else
				return TypeDeclarationResolver.Resolve(parser.Type(), ctxt);

			return null;
		}

		static ResolveResult[] ResolveTypesss(ITypeDeclaration declaration,
		                                          ResolverContextStack ctxt)
		{
			if (ctxt == null || declaration == null)
				return null;

			return TypeDeclarationResolver.Resolve(declaration, ctxt);



			// Check if already resolved once
			ResolveResult[] preRes = null;
			if (ctxt.TryGetAlreadyResolvedType(declaration.ToString(), out preRes))
				return preRes;

			var returnedResults = new List<ResolveResult>();

			// Walk down recursively to resolve everything from the very first to declaration's base type declaration.
			ResolveResult[] rbases = null;
			if (declaration.InnerDeclaration != null)
			{
				rbases = TypeDeclarationResolver.Resolve(declaration.InnerDeclaration, ctxt);

				if (rbases != null)
					rbases = FilterOutByResultPriority(ctxt, rbases);
			}



			#region Search in further, deeper levels
			else foreach (var rbase in rbases)
				{
					if (declaration is DExpressionDecl)
					{
						var expr = (declaration as DExpressionDecl).Expression;

						/* 
						 * Note: Assume e.g. foo.bar.myArray in foo.bar.myArray[0] has been resolved!
						 * So, we just have to take the last postfix expression
						 */

						/*
						 * After we've done this, we reduce the stack..
						 * Target of this action is to retrieve the value type:
						 * 
						 * int[string][] myArray; // Is an array that holds an associative array, whereas the value type is 'int', and key type is 'string'
						 * 
						 * auto mySubArray=myArray[0]; // returns a reference to an int[string] array
						 * 
						 * auto myElement=mySubArray["abcd"]; // returns the most basic value type: 'int'
						 */
						if (rbase is StaticTypeResult)
						{
							var str = rbase as StaticTypeResult;

							if (str.DeclarationOrExpressionBase is ArrayDecl && expr is PostfixExpression_Index)
							{
								returnedResults.Add(new StaticTypeResult() { DeclarationOrExpressionBase = (str.DeclarationOrExpressionBase as ArrayDecl).ValueType });
							}
						}
						else if (rbase is MemberResult)
						{
							var mr = rbase as MemberResult;
							if (mr.MemberBaseTypes != null && mr.MemberBaseTypes.Length > 0)
								foreach (var memberType in TryRemoveAliasesFromResult(mr.MemberBaseTypes))
								{
									if (expr is PostfixExpression_Index)
									{
										if (memberType is StaticTypeResult)
										{
											var str = memberType as StaticTypeResult;
											/*
											 * If the member's type is an array, and if our expression contains an index-expression (e.g. myArray[0]),
											 * take the value type of the 
											 */
											// For array and pointer declarations, the StaticTypeResult object contains the array's value type / pointer base type.
											if (str != null && (str.DeclarationOrExpressionBase is ArrayDecl || str.DeclarationOrExpressionBase is PointerDecl))
											{
												returnedResults.AddRange(TryRemoveAliasesFromResult(str.ResultBase));
												continue;
											}
										}
									}
									
									returnedResults.Add(memberType);
								}
						}
					}
				}
			#endregion



			if (returnedResults.Count > 0)
			{
				ctxt.TryAddResults(declaration.ToString(), returnedResults.ToArray());

				return FilterOutByResultPriority(ctxt, returnedResults.ToArray());
			}

			return null;
		}
		#endregion

		static int bcStack = 0;
		public static TypeResult[] ResolveBaseClass(DClassLike ActualClass, ResolverContextStack ctxt)
		{
			if (bcStack > 8)
			{
				bcStack--;
				return null;
			}

			if (ActualClass == null || ((ActualClass.BaseClasses == null || ActualClass.BaseClasses.Count < 1) && ActualClass.Name != null && ActualClass.Name.ToLower() == "object"))
				return null;

			var ret = new List<TypeResult>();
			// Implicitly set the object class to the inherited class if no explicit one was done
			var type = (ActualClass.BaseClasses == null || ActualClass.BaseClasses.Count < 1) ? new IdentifierDeclaration("Object") : ActualClass.BaseClasses[0];

			// A class cannot inherit itself
			if (type == null || type.ToString(false) == ActualClass.Name || ActualClass.NodeRoot == ActualClass)
				return null;

			bcStack++;

			/*
			 * If the ActualClass is defined in an other module (so not in where the type resolution has been started),
			 * we have to enable access to the ActualClass's module's imports!
			 * 
			 * module modA:
			 * import modB;
			 * 
			 * class A:B{
			 * 
			 *		void bar()
			 *		{
			 *			fooC(); // Note that modC wasn't imported publically! Anyway, we're still able to access this method!
			 *			// So, the resolver must know that there is a class C.
			 *		}
			 * }
			 * 
			 * -----------------
			 * module modB:
			 * import modC;
			 * 
			 * // --> When being about to resolve B's base class C, we have to use the imports of modB(!), not modA
			 * class B:C{}
			 * -----------------
			 * module modC:
			 * 
			 * class C{
			 * 
			 * void fooC();
			 * 
			 * }
			 */
			ctxt.PushNewScope(ActualClass.Parent as IBlockNode);

			var results = TypeDeclarationResolver.Resolve(type, ctxt);

			ctxt.Pop();

			if (results != null)
				foreach (var i in results)
					if (i is TypeResult)
						ret.Add(i as TypeResult);
			bcStack--;

			return ret.Count > 0 ? ret.ToArray() : null;
		}

		/// <summary>
		/// The variable's or method's base type will be resolved (if auto type, the intializer's type will be taken).
		/// A class' base class will be searched.
		/// etc..
		/// </summary>
		public static ResolveResult HandleNodeMatch(
			INode m,
			ResolverContextStack ctxt,
			ResolveResult resultBase = null, 
			object typeBase = null)
		{
			stackNum_HandleNodeMatch++;

			//HACK: Really dirty stack overflow prevention via manually counting call depth
			var DoResolveBaseType =
				stackNum_HandleNodeMatch > 5 ?
				false : ctxt.CurrentContext.ResolveBaseTypes;

			// Prevent infinite recursion if the type accidently equals the node's name
			if (m.Type != null && m.Type.ToString(false) == m.Name)
				DoResolveBaseType = false;

			if (m is DVariable)
			{
				var v = m as DVariable;

				var memberbaseTypes = DoResolveBaseType ? TypeDeclarationResolver.Resolve(v.Type, ctxt) : null;

				// For auto variables, use the initializer to get its type
				if (memberbaseTypes == null && DoResolveBaseType && v.Initializer != null)
				{
					memberbaseTypes = ExpressionTypeResolver.ResolveExpression(v.Initializer, ctxt);
				}

				// Resolve aliases if wished
				if (ctxt.CurrentContext.ResolveAliases && memberbaseTypes != null)
				{
					/*
					 * To ensure that absolutely all kinds of alias definitions became resolved (includes aliased alias definitions!), 
					 * loop through the resolution process again, after at least one aliased type has been found.
					 */
					while (memberbaseTypes.Length > 0)
					{
						bool hadAliasResolution = false;
						var memberBaseTypes_Override = new List<ResolveResult>();

						foreach (var type in memberbaseTypes)
						{
							var mr = type as MemberResult;
							if (mr != null && mr.ResolvedMember is DVariable)
							{
								var dv = mr.ResolvedMember as DVariable;
								// Note: Normally, a variable's base type mustn't be an other variable but an alias defintion...
								if (dv.IsAlias)
								{
									var newRes = TypeDeclarationResolver.Resolve(dv.Type, ctxt);
									if (newRes != null)
										memberBaseTypes_Override.AddRange(newRes);
									hadAliasResolution = true;
									continue;
								}
							}

							// If no alias found, re-add it to our override list again
							memberBaseTypes_Override.Add(type);
						}
						memberbaseTypes = memberBaseTypes_Override.ToArray();

						if (!hadAliasResolution)
							break;
					}
				}

				// Note: Also works for aliases! In this case, we simply try to resolve the aliased type, otherwise the variable's base type
				stackNum_HandleNodeMatch--;
				return new MemberResult()
				{
					ResolvedMember = m,
					MemberBaseTypes = memberbaseTypes,
					ResultBase = resultBase,
					DeclarationOrExpressionBase = typeBase
				};
			}
			else if (m is DMethod)
			{
				var method = m as DMethod;
				ResolveResult[] returnTypes=null;

				if (DoResolveBaseType)
				{
					/*
					 * If a method's type equals null, assume that it's an 'auto' function..
					 * 1) Search for a return statement
					 * 2) Resolve the returned expression
					 * 3) Use that one as the method's type
					 */
					if (method.Type != null)
						returnTypes = TypeDeclarationResolver.Resolve(method.Type, ctxt);
					else if (method.Body != null)
					{
						ReturnStatement returnStmt = null;
						var list = new List<IStatement> { method.Body };
						var list2 = new List<IStatement>();

						bool foundMatch = false;
						while (!foundMatch && list.Count > 0)
						{
							foreach (var stmt in list)
							{
								if (stmt is ReturnStatement)
								{
									returnStmt = stmt as ReturnStatement;

									if (!(returnStmt.ReturnExpression is TokenExpression) ||
										(returnStmt.ReturnExpression as TokenExpression).Token != DTokens.Null)
									{
										foundMatch = true;
										break;
									}
								}

								if (stmt is StatementContainingStatement)
									list2.AddRange((stmt as StatementContainingStatement).SubStatements);
							}

							list = list2;
							list2 = new List<IStatement>();
						}

						if (returnStmt != null && returnStmt.ReturnExpression != null)
						{
							ctxt.PushNewScope(method);

							returnTypes = ExpressionTypeResolver.ResolveExpression(returnStmt.ReturnExpression, ctxt);

							ctxt.Pop();
						}
					}
				}

				var ret = new MemberResult()
				{
					ResolvedMember = m,
					MemberBaseTypes = returnTypes,
					ResultBase = resultBase,
					DeclarationOrExpressionBase = typeBase
				};

				stackNum_HandleNodeMatch--;
				return ret;
			}
			else if (m is DClassLike)
			{
				var Class = m as DClassLike;

				var bc = DoResolveBaseType ? ResolveBaseClass(Class, ctxt) : null;

				stackNum_HandleNodeMatch--;
				return new TypeResult()
				{
					ResolvedTypeDefinition = Class,
					BaseClass = bc,
					ResultBase = resultBase,
					DeclarationOrExpressionBase = typeBase
				};
			}
			else if (m is IAbstractSyntaxTree)
			{
				stackNum_HandleNodeMatch--;
				return new ModuleResult()
				{
					ResolvedModule = m as IAbstractSyntaxTree,
					AlreadyTypedModuleNameParts = 1,
					ResultBase = resultBase,
					DeclarationOrExpressionBase = typeBase
				};
			}
			else if (m is DEnum)
			{
				stackNum_HandleNodeMatch--;
				return new TypeResult()
				{
					ResolvedTypeDefinition = m as IBlockNode,
					ResultBase = resultBase,
					DeclarationOrExpressionBase = typeBase
				};
			}
			else if (m is TemplateParameterNode)
			{
				stackNum_HandleNodeMatch--;
				return new MemberResult()
				{
					ResolvedMember = m,
					DeclarationOrExpressionBase = typeBase,
					ResultBase = resultBase
				};
			}

			stackNum_HandleNodeMatch--;
			// This never should happen..
			return null;
		}

		static int stackNum_HandleNodeMatch = 0;
		public static ResolveResult[] HandleNodeMatches(
			IEnumerable<INode> matches,
			ResolverContextStack ctxt,
			ResolveResult resultBase = null,
			object TypeDeclaration = null)
		{
			var rl = new List<ResolveResult>();

			if (matches != null)
				foreach (var m in matches)
				{
					if (m == null)
						continue;

					var res = HandleNodeMatch(m, ctxt, resultBase, TypeDeclaration);
					if (res != null)
						rl.Add(res);
				}
			return rl.ToArray();
		}
	}
}
