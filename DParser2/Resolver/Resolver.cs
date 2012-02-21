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
	}
}
