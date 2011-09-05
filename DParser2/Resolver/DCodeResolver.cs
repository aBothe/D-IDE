using System;
using System.Collections.Generic;
using System.Text;
using D_Parser.Dom;
using System.IO;
using System.Collections;
using System.Collections.ObjectModel;
using D_Parser.Parser;
using D_Parser.Dom.Expressions;
using D_Parser.Dom.Statements;

/*
/// Code completion rules:
///
/// - If a letter has been typed:
///		- Show the popup if:
///			- there's a "*" in front of the identifier, what makes us assume (TODO: ensure it!) that it is meant to be an expression, not a type
///			- there is no type, show the popup
///			- a preceding () belong to:
///				if while for foreach foreach_reverse with try catch finally cast
///		- Do not show the popup if:
///			- "]" or an other identifier (includes keywords) is located (after at least one whitespace) in front of the identifier
///			- If the caret is already located within an identifier
///		-> When a new identifier has begun to be typed, the after-space completion data gets shown
///	
/// - If a dot has been typed:
///		- resolve the type of the expression in front of the dot
///			- if the type is a class-like, show all its static public members
///			- if it's an enum, show all its items
///			- if it's a variable or method, resolve its base type and show all its public members (also those of the type's base classes)
///				- if the variable is declared within the base type itself, show all items
*/

namespace D_Parser.Resolver
{
	/// <summary>
	/// Generic class for resolve module relations and/or declarations
	/// </summary>
	public class DCodeResolver
	{
		/// <summary>
		/// Returns a list of all items that can be accessed in the current scope.
		/// </summary>
		/// <param name="ScopedBlock"></param>
		/// <param name="ImportCache"></param>
		/// <returns></returns>
		public static IEnumerable<INode> EnumAllAvailableMembers(IBlockNode ScopedBlock, IStatement ScopedStatement, CodeLocation Caret, IEnumerable<IAbstractSyntaxTree> CodeCache)
		{
			/* First walk through the current scope.
			 * Walk up the node hierarchy and add all their items (private as well as public members).
			 * Resolve base classes and add their non-private|static members.
			 * 
			 * Then add public members of the imported modules 
			 */
			var ret = new List<INode>();
			var ImportCache = ResolveImports(ScopedBlock.NodeRoot as DModule, CodeCache);

			#region Current module/scope related members

			if (ScopedStatement != null)
			{
				ret.AddRange(BlockStatement.GetItemHierarchy(ScopedStatement, Caret));
			}


			var curScope = ScopedBlock;

			while (curScope != null)
			{
				// Walk up inheritance hierarchy
				if (curScope is DClassLike)
				{
					var curWatchedClass = curScope as DClassLike;
					// MyClass > BaseA > BaseB > Object
					while (curWatchedClass != null)
					{
						if (curWatchedClass.TemplateParameters != null)
							ret.AddRange(curWatchedClass.TemplateParameterNodes as IEnumerable<INode>);

						foreach (var m in curWatchedClass)
						{
							var dm2 = m as DNode;
							var dm3 = m as DMethod; // Only show normal & delegate methods
							if (dm2 == null ||
								(dm3 != null && !(dm3.SpecialType == DMethod.MethodType.Normal || dm3.SpecialType == DMethod.MethodType.Delegate))
								)
								continue;

							// Add static and non-private members of all base classes; add everything if we're still handling the currently scoped class
							if (curWatchedClass == curScope || dm2.IsStatic || !dm2.ContainsAttribute(DTokens.Private))
								ret.Add(m);
						}

						// Stop adding if Object class level got reached
						if (!string.IsNullOrEmpty(curWatchedClass.Name) && curWatchedClass.Name.ToLower() == "object")
							break;



						var baseclassDefs = DResolver.ResolveBaseClass(curWatchedClass, ImportCache);

						if (baseclassDefs == null)
							break;
						if (curWatchedClass == baseclassDefs[0].ResolvedTypeDefinition)
							break;
						curWatchedClass = baseclassDefs[0].ResolvedTypeDefinition as DClassLike;
					}
				}
				else if (curScope is DMethod)
				{
					var dm = curScope as DMethod;
					ret.AddRange(dm.Parameters);

					if (dm.TemplateParameters != null)
						ret.AddRange(dm.TemplateParameterNodes as IEnumerable<INode>);

					// The method's declaration children are handled above already via BlockStatement.GetItemHierarchy().
				}
				else foreach (var n in curScope)
						{
							var dm3 = n as DMethod; // Only show normal & delegate methods
							if ((dm3 != null && !(dm3.SpecialType == DMethod.MethodType.Normal || dm3.SpecialType == DMethod.MethodType.Delegate)))
								continue;

							//TODO: (More parser-related!) Add anonymous blocks (e.g. delegates) to the syntax tree
							ret.Add(n);
						}

				curScope = curScope.Parent as IBlockNode;
			}
			#endregion

			#region Global members
			// Add all non-private and non-package-only nodes
			foreach (var mod in ImportCache)
			{
				if (mod.FileName == (ScopedBlock.NodeRoot as IAbstractSyntaxTree).FileName)
					continue;

				foreach (var i in mod)
				{
					var dn = i as DNode;
					if (dn != null)
					{
						if (dn.IsPublic && !dn.ContainsAttribute(DTokens.Package))
							ret.Add(dn);
					}
					else ret.Add(i);
				}
			}
			#endregion

			if (ret.Count < 1)
				return null;
			return ret;
		}

		public static IBlockNode SearchBlockAt(IBlockNode Parent, CodeLocation Where, out IStatement ScopedStatement)
		{
			ScopedStatement = null;

			if(Parent!=null && Parent.Count>0)
				foreach (var n in Parent)
				{
					if (!(n is IBlockNode)) continue;

					var b = n as IBlockNode;
					if (Where >= b.StartLocation && Where <= b.EndLocation)
						return SearchBlockAt(b, Where, out ScopedStatement);
				}

			if (Parent is DMethod)
			{
				var dm = Parent as DMethod;

				// First search the deepest statement under the caret
				if (dm.In != null)
					ScopedStatement = dm.In.SearchStatementDeeply(Where);

				if (dm.Out != null && ScopedStatement == null)
					ScopedStatement = dm.Out.SearchStatementDeeply(Where);

				if(dm.Body!=null && ScopedStatement==null)
					ScopedStatement = dm.Body.SearchStatementDeeply(Where);
			}

			return Parent;
		}

		public static IBlockNode SearchClassLikeAt(IBlockNode Parent, CodeLocation Where)
		{
			if (Parent != null && Parent.Count > 0)
				foreach (var n in Parent)
				{
					if (!(n is DClassLike)) continue;

					var b = n as IBlockNode;
					if (Where >= b.BlockStartLocation && Where <= b.EndLocation)
						return SearchClassLikeAt(b, Where);
				}

			return Parent;
		}

		#region Import path resolving
		/// <summary>
		/// Returns all imports of a module and those public ones of the imported modules
		/// </summary>
		/// <param name="cc"></param>
		/// <param name="ActualModule"></param>
		/// <returns></returns>
		public static IEnumerable<IAbstractSyntaxTree> ResolveImports(DModule ActualModule, IEnumerable<IAbstractSyntaxTree> CodeCache)
		{
			var ret = new List<IAbstractSyntaxTree>();
			if (CodeCache == null || ActualModule == null) return ret;

			// Try to add the 'object' module
			var objmod = SearchModuleInCache(CodeCache, "object");
			if (objmod != null && !ret.Contains(objmod))
				ret.Add(objmod);

			/* 
			 * dmd-feature: public imports only affect the directly superior module
			 *
			 * Module A:
			 * import B;
			 * 
			 * foo(); // Will fail, because foo wasn't found
			 * 
			 * Module B:
			 * import C;
			 * 
			 * Module C:
			 * public import D;
			 * 
			 * Module D:
			 * void foo() {}
			 * 
			 * 
			 * Whereas
			 * Module B:
			 * public import C;
			 * 
			 * will succeed because we have a closed import hierarchy in which all imports are public.
			 * 
			 */

            /*
             * Procedure:
             * 
             * 1) Take the imports of the current module
             * 2) Add the respective modules
             * 3) If that imported module got public imports, also make that module to the current one and repeat Step 1) recursively
             * 
             */
			
			foreach (var kv in ActualModule.Imports)
				if (kv.IsSimpleBinding && !kv.IsStatic)
				{
					var impMod = SearchModuleInCache(CodeCache, kv.ModuleIdentifier) as DModule;

					if (impMod != null && !ret.Contains(impMod))
					{
						ret.Add(impMod);

                        ScanForPublicImports(ret, impMod, CodeCache);
					}

				}

			return ret;
		}

        static void ScanForPublicImports(List<IAbstractSyntaxTree> ret, DModule currentlyWatchedImport, IEnumerable<IAbstractSyntaxTree> CodeCache)
        {
            if(currentlyWatchedImport!=null && currentlyWatchedImport.Imports!=null)
                foreach (var kv2 in currentlyWatchedImport.Imports)
                    if (kv2.IsSimpleBinding && !kv2.IsStatic && kv2.IsPublic)
                    {
                        var impMod2 = SearchModuleInCache(CodeCache, kv2.ModuleIdentifier) as DModule;

                        if (impMod2 != null && !ret.Contains(impMod2))
                        {
                            ret.Add(impMod2);

                            ScanForPublicImports(ret, impMod2, CodeCache);
                        }
                    }
        }
		#endregion

		public static IAbstractSyntaxTree SearchModuleInCache(IEnumerable<IAbstractSyntaxTree> HayStack, string ModuleName)
		{
			foreach (var m in HayStack)
			{
				if (m.Name == ModuleName) 
                    return m;
			}
			return null;
		}

		/// <summary>
		/// Trivial class which cares about locating Comments and other non-code blocks within a code file
		/// </summary>
		public class Commenting
		{
			public static int IndexOf(string HayStack, bool Nested, int Start)
			{
				string Needle = Nested ? "+/" : "*/";
				char cur = '\0';
				int off = Start;
				bool IsInString = false;
				int block = 0, nested = 0;

				while (off < HayStack.Length - 1)
				{
					cur = HayStack[off];

					// String check
					if (cur == '\"' && (off < 1 || HayStack[off - 1] != '\\'))
					{
						IsInString = !IsInString;
					}

					if (!IsInString && (cur == '/') && (HayStack[off + 1] == '*' || HayStack[off + 1] == '+'))
					{
						if (HayStack[off + 1] == '*')
							block++;
						else
							nested++;

						off += 2;
						continue;
					}

					if (!IsInString && cur == Needle[0])
					{
						if (off + Needle.Length - 1 >= HayStack.Length)
							return -1;

						if (HayStack.Substring(off, Needle.Length) == Needle)
						{
							if (Nested) nested--; else block--;

							if ((Nested ? nested : block) < 0) // that value has to be -1 because we started to count at 0
								return off;

							off++; // Skip + or *
						}

						if (HayStack.Substring(off, 2) == (Nested ? "*/" : "+/"))
						{
							if (Nested) block--; else nested--;
							off++;
						}
					}

					off++;
				}
				return -1;
			}

			public static int LastIndexOf(string HayStack, bool Nested, int Start)
			{
				string Needle = Nested ? "/+" : "/*";
				char cur = '\0', prev = '\0';
				int off = Start;
				bool IsInString = false;
				int block = 0, nested = 0;

				while (off >= 0)
				{
					cur = HayStack[off];
					if (off > 0) prev = HayStack[off - 1];

					// String check
					if (cur == '\"' && (off < 1 || HayStack[off - 1] != '\\'))
					{
						IsInString = !IsInString;
					}

					if (!IsInString && (cur == '+' || cur == '*') && HayStack[off + 1] == '/')
					{
						if (cur == '*')
							block--;
						else
							nested--;

						off -= 2;
						continue;
					}

					if (!IsInString && cur == '/')
					{
						if (HayStack.Substring(off, Needle.Length) == Needle)
						{
							if (Nested) nested++; else block++;

							if ((Nested ? nested : block) >= 1)
								return off;
						}

						if (HayStack.Substring(off, 2) == (Nested ? "/*" : "/+"))
						{
							if (Nested) block++; else nested++;
							off--;
						}
					}

					off--;
				}
				return -1;
			}

			public static void IsInCommentAreaOrString(string Text, int Offset, out bool IsInString, out bool IsInLineComment, out bool IsInBlockComment, out bool IsInNestedBlockComment)
			{
				char cur = '\0', peekChar = '\0';
				int off = 0;
				IsInString = IsInLineComment = IsInBlockComment = IsInNestedBlockComment = false;

				while (off < Offset - 1)
				{
					cur = Text[off];
					if (off < Text.Length - 1) peekChar = Text[off + 1];

					// String check
					if (!IsInLineComment && !IsInBlockComment && !IsInNestedBlockComment && cur == '\"' && (off < 1 || Text[off - 1] != '\\'))
						IsInString = !IsInString;

					if (!IsInString)
					{
						// Line comment check
						if (!IsInBlockComment && !IsInNestedBlockComment)
						{
							if (cur == '/' && peekChar == '/')
								IsInLineComment = true;
							if (IsInLineComment && cur == '\n')
								IsInLineComment = false;
						}

						// Block comment check
						if (cur == '/' && peekChar == '*')
							IsInBlockComment = true;
						if (IsInBlockComment && cur == '*' && peekChar == '/')
							IsInBlockComment = false;

						// Nested comment check
						if (!IsInString && cur == '/' && peekChar == '+')
							IsInNestedBlockComment = true;
						if (IsInNestedBlockComment && cur == '+' && peekChar == '/')
							IsInNestedBlockComment = false;
					}

					off++;
				}
			}

			public static bool IsInCommentAreaOrString(string Text, int Offset)
			{
				bool a, b, c, d;
				IsInCommentAreaOrString(Text, Offset, out a, out b, out c, out d);

				return a || b || c || d;
			}
		}
	}

	public class DResolver
	{
		public static ResolveResult[] ResolveType(string code, int caret, CodeLocation caretLocation, IBlockNode currentlyScopedNode, IEnumerable<IAbstractSyntaxTree> parseCache,
			bool alsoParseBeyondCaret = false,
			bool onlyAssumeIdentifierList = false)
		{
			var start = ReverseParsing.SearchExpressionStart(code, caret);

			if (start < 0)
				return null;

			var expressionCode = code.Substring(start, alsoParseBeyondCaret ? code.Length - start : caret - start);

			var parser = DParser.Create(new StringReader(expressionCode));
			parser.Step();

			if (onlyAssumeIdentifierList)
				return ResolveType(parser.IdentifierList(),currentlyScopedNode, parseCache);
			else if (parser.IsAssignExpression())
			{
				var expr=parser.AssignExpression();

				if (expr != null)
				{
					var relativeCaretLocation=DocumentHelper.OffsetToLocation(expressionCode, caret - start);
					expr = ExpressionHelper.SearchExpressionDeeply(expr, relativeCaretLocation);

					var ret = ResolveType(expr.ExpressionTypeRepresentation, currentlyScopedNode, parseCache);

					if (ret == null && expr != null)
						ret = new[] { new ExpressionResult() { Expression = expr } };

					return ret;
				}
			}
			else
				return ResolveType(parser.Type(), currentlyScopedNode, parseCache);

			return null;
		}

		public static ResolveResult[] ResolveType(ITypeDeclaration declaration, IBlockNode currentlyScopedNode, IEnumerable<IAbstractSyntaxTree> parseCache)
		{
			if (declaration == null)
				return null;

			var returnedResults = new List<ResolveResult>();

			// Walk down recursively to resolve everything from the very first to declaration's base type declaration.
			ResolveResult[] rbases = null;
			if (declaration.InnerDeclaration != null)
				rbases = ResolveType(declaration.InnerDeclaration, currentlyScopedNode, parseCache);

			/* 
			 * If there is no parent resolve context (what usually means we are searching the type named like the first identifier in the entire declaration),
			 * search the very first type declaration by walking along the current block scope hierarchy.
			 * If there wasn't any item found in that procedure, search in the global parse cache
			 */
			#region Search initial member/type/module/whatever
			if (rbases == null)
			{
				#region IdentifierDeclaration
				if (declaration is IdentifierDeclaration)
				{
					string searchIdentifier = (declaration as IdentifierDeclaration).Value as string;

					if (string.IsNullOrEmpty(searchIdentifier))
						return null;

					// Try to convert the identifier into a token
					int searchToken = string.IsNullOrEmpty(searchIdentifier) ? 0 : DTokens.GetTokenID(searchIdentifier);

					// References current class scope
					if (searchToken == DTokens.This)
					{
						returnedResults.Add(new TypeResult() { ResolvedTypeDefinition = currentlyScopedNode });
					}
					// References super type of currently scoped class declaration
					else if (searchToken == DTokens.Super)
					{
						var baseClassDefs = ResolveBaseClass(currentlyScopedNode as DClassLike, parseCache);

						if (baseClassDefs != null)
							returnedResults.AddRange(baseClassDefs);
					}
					// If we found a base type, return a static-type-result
					else if (searchToken > 0 && DTokens.BasicTypes[searchToken])
					{
						returnedResults.Add(new StaticTypeResult() { BaseTypeToken = searchToken, Type = new DTokenDeclaration(searchToken) });
					}
					// (As usual) Go on searching in the local&global scope(s)
					else
					{
						var matches = new List<INode>();

						// First search along the hierarchy in the current module
						var curScope = currentlyScopedNode;
						while (curScope != null)
						{
							var m = ScanNodeForIdentifier(curScope, searchIdentifier, parseCache);

							if (m != null)
								matches.AddRange(m);

							var mod=curScope as IAbstractSyntaxTree;
							if (mod!=null && !string.IsNullOrEmpty(mod.ModuleName) && mod.ModuleName.StartsWith(searchIdentifier))
								matches.Add(curScope);

							curScope = curScope.Parent as IBlockNode;
						}

						// Then go on searching in the global scope
						var ThisModule = currentlyScopedNode is IAbstractSyntaxTree ? currentlyScopedNode as IAbstractSyntaxTree : currentlyScopedNode.NodeRoot as IAbstractSyntaxTree;
						if (parseCache != null)
							foreach (var mod in parseCache)
							{
								if (mod == ThisModule)
									continue;

								var modNameParts = mod.ModuleName.Split('.');

								if (modNameParts[0] == searchIdentifier)
									matches.Add(mod);

								var m = ScanNodeForIdentifier(mod, searchIdentifier, null);
								if (m != null)
									matches.AddRange(m);
							}

						var results = HandleNodeMatches(matches, currentlyScopedNode, parseCache/*, searchIdentifier*/);
						if (results != null)
							returnedResults.AddRange(results);
					}
				}
				#endregion

				else
					returnedResults.Add(new StaticTypeResult() { Type=declaration});
			}
			#endregion

			#region Search in further, deeper levels
			else foreach(var rbase in rbases)
				{
					#region Identifier
					if (declaration is IdentifierDeclaration)
					{
						string searchIdentifier = (declaration as IdentifierDeclaration).Value as string;

						//TODO: Scan for static properties
						
						var scanResults = new List<ResolveResult>();
						scanResults.Add(rbase);
						var nextResults=new List<ResolveResult>();

						while (scanResults.Count > 0)
						{
							foreach (var scanResult in scanResults)
							{
								// First filter out all alias and member results..so that there will be only (Static-)Type or Module results left..
								if (scanResult is MemberResult)
								{
									var _m = (scanResult as MemberResult).MemberBaseTypes;
									if(_m!=null)nextResults.AddRange(_m);
								}

								else if (scanResult is TypeResult)
								{
									var results = HandleNodeMatches(
										ScanNodeForIdentifier((scanResult as TypeResult).ResolvedTypeDefinition, searchIdentifier, parseCache),
										currentlyScopedNode, parseCache, rbase);
									if (results != null)
										returnedResults.AddRange(results);
								}
								else if (scanResult is ModuleResult)
								{
									var modRes = (scanResult as ModuleResult);

									if (modRes.IsOnlyModuleNamePartTyped())
									{
										var modNameParts = modRes.ResolvedModule.ModuleName.Split('.');

										if (modNameParts[modRes.AlreadyTypedModuleNameParts] == searchIdentifier)
										{
											returnedResults.Add(new ModuleResult()
											{
												ResolvedModule = modRes.ResolvedModule,
												AlreadyTypedModuleNameParts = modRes.AlreadyTypedModuleNameParts + 1,
												ResultBase = modRes
											});
										}
									}
									else
									{
										var results = HandleNodeMatches(
										ScanNodeForIdentifier((scanResult as ModuleResult).ResolvedModule, searchIdentifier, parseCache),
										currentlyScopedNode, parseCache, rbase);
										if (results != null)
											returnedResults.AddRange(results);
									}
								}
								else if (scanResult is StaticTypeResult)
								{

								}
							}

							scanResults = nextResults;
							nextResults = new List<ResolveResult>();
						}
					}
					#endregion

					else if (declaration is ArrayDecl || declaration is PointerDecl)
					{
						returnedResults.Add(new StaticTypeResult() { Type = declaration, ResultBase = rbase });
					}

					else if (declaration is DExpressionDecl)
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

							if (str.Type is ArrayDecl && expr is PostfixExpression_Index)
							{
								returnedResults.Add(new StaticTypeResult() { Type = (str.Type as ArrayDecl).ValueType });
							}
						}
						else if (rbase is MemberResult)
						{
							var mr = rbase as MemberResult;
							if (mr.MemberBaseTypes != null && mr.MemberBaseTypes.Length > 0)
								foreach (var memberType in mr.MemberBaseTypes)
								{
									var curRes = TryRemoveAliasesFromResult(memberType);

									if (expr is PostfixExpression_Index)
									{
										var str = (curRes as StaticTypeResult);
										/*
										 * If the member's type is an array, and if our expression contains an index-expression (e.g. myArray[0]),
										 * take the value type of the 
										 */
										// For array and pointer declarations, the StaticTypeResult object contains the array's value type / pointer base type.
										if (str != null && (str.Type is ArrayDecl || str.Type is PointerDecl))
											curRes = TryRemoveAliasesFromResult(str.ResultBase);
									}

									if (curRes != null)
										returnedResults.Add(curRes);
								}
						}
					}
					}
			#endregion

			return returnedResults.Count > 0 ? returnedResults.ToArray() : null;
		}

		/// <summary>
		/// If an aliased type result has been passed to this method, it'll return the resolved type.
		/// If aliases were done multiple times, it also tries to skip through these.
		/// 
		/// alias char[] A;
		/// alias A B;
		/// 
		/// var resolvedType=TryRemoveAliasesFromResult(% the member result from B %);
		/// --> resolvedType will be StaticTypeResult from char[]
		/// 
		/// </summary>
		/// <param name="rr"></param>
		/// <returns></returns>
		public static ResolveResult TryRemoveAliasesFromResult(ResolveResult rr)
		{
			ResolveResult ret = rr;
			while (ret is MemberResult && (ret as MemberResult).ResolvedMember is DVariable && ((ret as MemberResult).ResolvedMember as DVariable).IsAlias)
			{
				var mr2 = ret as MemberResult;
				/*
				 * Although it's theoretically possible to ignore multiple definitions, 
				 * only take the first aliased type for performance reasons
				 */
				if (mr2.MemberBaseTypes != null && mr2.MemberBaseTypes.Length > 0)
					ret = mr2.MemberBaseTypes[0];
				else break;
			}
			return ret;
		}

		public static TypeResult[] ResolveBaseClass(DClassLike ActualClass, IEnumerable<IAbstractSyntaxTree> ModuleCache)
		{
			if (ActualClass == null || ((ActualClass.BaseClasses==null ||ActualClass.BaseClasses.Count < 1) && ActualClass.Name!=null && ActualClass.Name.ToLower() == "object"))
				return null;

			var ret = new List<TypeResult>();
			// Implicitly set the object class to the inherited class if no explicit one was done
			var type = (ActualClass.BaseClasses == null || ActualClass.BaseClasses.Count < 1) ? new IdentifierDeclaration("Object") : ActualClass.BaseClasses[0];

			// A class cannot inherit itself
			if (type==null||type.ToString(false) == ActualClass.Name)
				return null;

			var results=ResolveType(type, ActualClass.NodeRoot as IBlockNode, ModuleCache);

			if(results!=null)
				foreach (var i in results)
					if (i is TypeResult)
						ret.Add(i as TypeResult);

			return ret.Count > 0 ? ret.ToArray() : null;
		}

		/// <summary>
		/// Scans through the node. Also checks if n is a DClassLike or an other kind of type node and checks their specific child and/or base class nodes.
		/// </summary>
		/// <param name="n"></param>
		/// <param name="name"></param>
		/// <param name="parseCache">Needed when trying to search base classes</param>
		/// <returns></returns>
		public static INode[] ScanNodeForIdentifier(IBlockNode curScope, string name, IEnumerable<IAbstractSyntaxTree> parseCache)
		{
			var matches = new List<INode>();
			foreach (var n in curScope)
			{
				if (n.Name == name)
					matches.Add(n);
			}

			// If our current Level node is a class-like, also attempt to search in its baseclass!
			if (curScope is DClassLike)
			{
				var baseClasses = ResolveBaseClass(curScope as DClassLike, parseCache);
				if (baseClasses != null)
					foreach (var i in baseClasses)
					{
						var baseClass = i as TypeResult;
						if (baseClass == null)
							continue;
						// Search for items called name in the base class(es)
						var r = ScanNodeForIdentifier(baseClass.ResolvedTypeDefinition, name, parseCache);

						if (r != null)
							matches.AddRange(r);
					}
			}

			// Check parameters
			if (curScope is DMethod)
			{
				foreach (var ch in (curScope as DMethod).Parameters)
				{
					if (name == ch.Name)
						matches.Add(ch);
				}
			}

			// and template parameters
			if (curScope is DNode && (curScope as DNode).TemplateParameters != null)
				foreach (var ch in (curScope as DNode).TemplateParameters)
				{
					if (name == ch.Name)
						matches.Add(new TemplateParameterNode(ch));
				}

			return matches.Count>0? matches.ToArray():null;
		}

		/// <summary>
		/// The variable's or method's base type will be resolved (if auto type, the intializer's type will be taken).
		/// A class' base class will be searched.
		/// etc..
		/// </summary>
		/// <returns></returns>
		static ResolveResult HandleNodeMatch(INode m, 
			IBlockNode currentlyScopedNode,
			IEnumerable<IAbstractSyntaxTree> parseCache,
			ResolveResult resultBase = null, bool ResolveAliases=false)
		{
			bool DoResolveBaseType = true;
			// Prevent infinite recursion if the type accidently equals the node's name
			if (m.Type != null && m.Type.ToString(false) == m.Name)
				DoResolveBaseType = false;

			if (m is DVariable)
			{
				var v = m as DVariable;

				var memberbaseTypes = DoResolveBaseType ? ResolveType(v.Type, currentlyScopedNode, parseCache) : null;

				// For auto variables, use the initializer to get its type
				if (memberbaseTypes == null && DoResolveBaseType && v.ContainsAttribute(DTokens.Auto) && v.Initializer != null)
				{
					var init = v.Initializer;
					memberbaseTypes = ResolveType(init.ExpressionTypeRepresentation, currentlyScopedNode, parseCache);
				}

				// Resolve aliases if wished
				if (ResolveAliases && memberbaseTypes != null)
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
									var newRes = ResolveType(dv.Type, currentlyScopedNode, parseCache);
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
				return new MemberResult()
				{
					ResolvedMember = m,
					MemberBaseTypes = memberbaseTypes,
					ResultBase = resultBase
				};
			}
			else if (m is DMethod)
			{
				var method = m as DMethod;

				return new MemberResult()
				{
					ResolvedMember = m,
					MemberBaseTypes = DoResolveBaseType ? ResolveType(method.Type, currentlyScopedNode, parseCache) : null,
					ResultBase = resultBase
				};
			}
			else if (m is DClassLike)
			{
				var Class = m as DClassLike;

				return new TypeResult()
				{
					ResolvedTypeDefinition = Class,
					BaseClass = ResolveBaseClass(Class, parseCache),
					ResultBase = resultBase
				};
			}
			else if (m is IAbstractSyntaxTree)
				return new ModuleResult()
				{
					ResolvedModule = m as IAbstractSyntaxTree,
					AlreadyTypedModuleNameParts = 1,
					ResultBase = resultBase
				};
			else if (m is DEnum)
				return new TypeResult()
				{
					ResolvedTypeDefinition = m as IBlockNode,
					ResultBase = resultBase
				};

			// This never should happen..
			return null;
		}

		static ResolveResult[] HandleNodeMatches(IEnumerable<INode> matches, 
			IBlockNode currentlyScopedNode, 
			IEnumerable<IAbstractSyntaxTree> parseCache,
			ResolveResult resultBase = null, bool ResolveAliasDefs=false)
		{
			var rl = new List<ResolveResult>();
			if(matches!=null)
			foreach (var m in matches)
			{
				var res = HandleNodeMatch(m, currentlyScopedNode, parseCache, resultBase,ResolveAliasDefs);
				if (res != null)
					rl.Add(res);
			}
			return rl.ToArray();
		}

		static readonly BitArray sigTokens = DTokens.NewSet(
			DTokens.If,
			DTokens.Foreach,
			DTokens.Foreach_Reverse,
			DTokens.With,
			DTokens.Try,
			DTokens.Catch,
			DTokens.Finally,

			DTokens.Cast // cast(...) myType << Show cc popup after a cast
			);

		/// <summary>
		/// Checks if an identifier is about to be typed. Therefore, we assume that this identifier hasn't been typed yet. 
		/// So, we also will assume that the caret location is the start of the identifier;
		/// </summary>
		public static bool IsTypeIdentifier(string code, int caret)
		{
			//try{
				if (caret < 1)
					return false;

				code = code.Insert(caret, " "); // To ensure correct behaviour, insert a phantom ws after the caret

				// Check for preceding letters
				if (char.IsLetter(code[caret]))
					return true;

				int precedingExpressionOrTypeStartOffset = ReverseParsing.SearchExpressionStart(code, caret);

				if (precedingExpressionOrTypeStartOffset >= caret)
					return false;

				var expressionCode = code.Substring(precedingExpressionOrTypeStartOffset, caret - precedingExpressionOrTypeStartOffset);

				if (string.IsNullOrEmpty(expressionCode) || expressionCode.Trim() == string.Empty)
					return false;

				var lx = new Lexer(new StringReader(expressionCode));

				var firstToken = lx.NextToken();

				if (DTokens.ClassLike[firstToken.Kind])
					return true;

				while (lx.LookAhead.Kind != DTokens.EOF)
					lx.NextToken();

				var lastToken = lx.CurrentToken;

				if (lastToken.Kind == DTokens.Times)
					return false; // TODO: Check if it's an expression or not

				if (lastToken.Kind == DTokens.CloseSquareBracket || lastToken.Kind == DTokens.Identifier)
					return true;

				if (lastToken.Kind == DTokens.CloseParenthesis)
				{
					lx.CurrentToken = firstToken;

					while (lx.LookAhead.Kind != DTokens.OpenParenthesis && lx.LookAhead.Kind != DTokens.EOF)
						lx.NextToken();

					if (sigTokens[lx.CurrentToken.Kind])
						return false;
					else
						return true;
				}

			//}catch(Exception ex) { }
			return false;
		}

		public class ArgumentsResolutionResult
		{
			public bool IsMethodArguments;
			public bool IsTemplateInstanceArguments;

			public IExpression ParsedExpression;

			public ResolveResult[] ResolvedTypesOrMethods;

			/// <summary>
			///	Identifies the currently called method overload. Is an index related to <see cref="ResolvedTypesOrMethods"/>
			/// </summary>
			public int CurrentlyCalledMethod;
			public int CurrentlyTypedArgument;
		}

		public static ArgumentsResolutionResult ResolveArgumentContext(string code, int caret, CodeLocation caretLocation, IBlockNode currentlyScopedBlock, IEnumerable<IAbstractSyntaxTree> parseCache)
		{
			// First step: Search the method's call start offset

			int startOffset = ReverseParsing.SearchExpressionStart(code,caret-1);

			if (startOffset < 0)
				return null;

			// Check if it's a method declaration - return null if so
			//if (IsTypeIdentifier(code, startOffset-1))	return null;

			// Parse the expression
			var e = DParser.ParseExpression(code.Substring(startOffset,caret-startOffset));

			/*
			 * There are at least 3 possibilities here:
			 * 1) foo(			-- normal arguments only
			 * 2) foo!(...)(	-- normal arguments + template args
			 * 3) foo!(		-- template args only
			 */
			var res = new ArgumentsResolutionResult()
			{	ParsedExpression=e	};

			ITypeDeclaration methodIdentifier = null;

			// 1), 2)
			if (e is PostfixExpression_MethodCall)
			{
				res.IsMethodArguments = true;
				var call = e as PostfixExpression_MethodCall;

				if(call.Arguments!=null)
					res.CurrentlyTypedArgument = call.Arguments.Length;

				if (call.PostfixForeExpression is TemplateInstanceExpression)
				{
					var templ = call.PostfixForeExpression as TemplateInstanceExpression;

					methodIdentifier = templ.ExpressionTypeRepresentation;
				}
				else
					methodIdentifier = call.PostfixForeExpression.ExpressionTypeRepresentation;

			}
			// 3)
			else if (e is TemplateInstanceExpression)
			{
				var templ = e as TemplateInstanceExpression;

				res.IsTemplateInstanceArguments = true;

				if (templ.Arguments != null)
					res.CurrentlyTypedArgument = templ.Arguments.Length;

				methodIdentifier = templ.ExpressionTypeRepresentation;
			}
			
 			if(methodIdentifier==null)
				return null;

			// Resolve all types, methods etc. which belong to the methodIdentifier
			res.ResolvedTypesOrMethods = ResolveType(methodIdentifier, currentlyScopedBlock, parseCache);

			return res;
		}
	}

	/// <summary>
	/// Helper class for e.g. finding the initial offset of a statement.
	/// </summary>
	public class ReverseParsing
	{
		static IList<string> preParenthesisBreakTokens = new List<string> { "if", "while", "for", "foreach", "foreach_reverse", "with", "try", "catch", "finally", "synchronized", "pragma" };

		public static int SearchExpressionStart(string Text, int CaretOffset)
		{
			if (CaretOffset > Text.Length)
				throw new ArgumentOutOfRangeException("CaretOffset", "Caret offset must be smaller than text length");
			else if (CaretOffset == Text.Length)
				Text += ' ';

			// At first we only want to find the beginning of our identifier list
			// later we will pass the text beyond the beginning to the parser - there we parse all needed expressions from it
			int IdentListStart = -1;

			/*
			T!(...)>.<
			 */

			int isComment = 0;
			bool isString = false, expectDot = false, hadDot = true;
			bool hadString = false;
			var bracketStack = new Stack<char>();

			var identBuffer = "";
			bool hadBraceOpener = false;
			int lastBraceOpenerOffset = 0;

			bool stopSeeking = false;

			// Step backward
			for (int i = CaretOffset; i >= 0 && !stopSeeking; i--)
			{
				IdentListStart = i;
				var c = Text[i];
				var str = Text.Substring(i);
				char p = ' ';
				if (i > 0) p = Text[i - 1];

				// Primitive comment check
				if (!isString && c == '/' && (p == '*' || p == '+'))
					isComment++;
				if (!isString && isComment > 0 && (c == '+' || c == '*') && p == '/')
					isComment--;

				// Primitive string check
				//TODO: "blah">.<
				hadString = false;
				if (isComment < 1 && c == '"' && p != '\\')
				{
					isString = !isString;
					
					if(!isString)
						hadString = true;
				}

				// If string or comment, just continue
				if (isString || isComment > 0)
					continue;

				// If between brackets, skip
				if (bracketStack.Count > 0 && c != bracketStack.Peek())
					continue;

				// Bracket check
				if (hadDot)
					switch (c)
					{
						case ']':
							bracketStack.Push('[');
							continue;
						case ')':
							bracketStack.Push('(');
							continue;
						case '}':
							if (bracketStack.Count < 1)
							{
								IdentListStart++;
								stopSeeking = true;
								continue;
							}
							bracketStack.Push('{');
							continue;

						case '[':
						case '(':
						case '{':
							if (bracketStack.Count > 0 && bracketStack.Peek() == c)
							{
								bracketStack.Pop();
								if (c=='(' && p == '!') // Skip template stuff
									i--;
							}
							else if (c == '{')
							{
								stopSeeking = true;
								IdentListStart++;
							}
							else
							{
								if (c=='(' && p == '!') // Skip template stuff
									i--;

								lastBraceOpenerOffset = IdentListStart;
								// e.g. foo>(< bar| )
								hadBraceOpener = true;
								identBuffer = "";
							}
							continue;
					}

				// whitespace check
				if (Char.IsWhiteSpace(c)) { if (hadDot) expectDot = false; else expectDot = true; continue; }

				if (c == '.')
				{
					hadBraceOpener = false;
					identBuffer = "";
					expectDot = false;
					hadDot = true;
					continue;
				}

				/*
				 * abc
				 * abc . abc
				 * T!().abc[]
				 * def abc.T
				 */
				if (Char.IsLetterOrDigit(c) || c == '_')
				{
					hadDot = false;

					if (!expectDot)
					{
						identBuffer += c;

						if (!hadBraceOpener)
							continue;
						else if (!preParenthesisBreakTokens.Contains(identBuffer))
							continue;
						else
							IdentListStart = lastBraceOpenerOffset;
					}
				}

				// Only re-increase our caret offset if we did not break because of a string..
				// otherwise, we'd return the offset after the initial string quote
				if(!hadString)
					IdentListStart++;
				stopSeeking = true;
			}

			return IdentListStart;
		}
	}

	public class DocumentHelper
	{
		public static CodeLocation OffsetToLocation(string Text, int Offset)
		{
			int line = 1;
			int col = 1;

			char c='\0';
			for (int i = 0; i < Offset;i++ )
			{
				c = Text[i];

				col++;

				if (c == '\n')
				{
					line++;
					col = 1;
				}
			}

			return new CodeLocation(col, line);
		}
	}
}
