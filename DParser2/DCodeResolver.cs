using System;
using System.Collections.Generic;
using System.Text;
using D_Parser.Core;
using System.IO;

namespace D_Parser
{
	/// <summary>
	/// Generic class for resolve module relations and/or declarations
	/// </summary>
	public class DCodeResolver
	{
		#region Util
		public static string ReverseString(string s)
		{
			if (s.Length < 1) return s;
			char[] ret = new char[s.Length];
			for (int i = s.Length; i > 0; i--)
			{
				ret[s.Length - i] = s[i - 1];
			}
			return new string(ret);
		}

		#endregion


		public static IEnumerable<INode> ResolveTypeDeclarations(IAbstractSyntaxTree Module, 
			string Text, 
			int CaretOffset, 
			CodeLocation CaretLocation, bool EnableVariableTypeResolving, 
			IEnumerable<IAbstractSyntaxTree> CodeCache, bool IsCompleteIdentifier)
		{
			ITypeDeclaration id = null;
			DToken tk = null;
			return ResolveTypeDeclarations(Module,Text,CaretOffset,CaretLocation,EnableVariableTypeResolving,CodeCache,out id,IsCompleteIdentifier,out tk);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Module"></param>
		/// <param name="Text"></param>
		/// <param name="CaretOffset"></param>
		/// <param name="CaretLocation"
		/// <param name="ImportCache"></param>
		/// <param name="IsCompleteIdentifier">True if Text is not only the beginning of a type name - instead, it's handled as the complete one</param>
		/// <returns></returns>
		public static IEnumerable<INode> ResolveTypeDeclarations(IAbstractSyntaxTree Module, 
			string Text, 
			int CaretOffset, 
			CodeLocation CaretLocation, 
			bool EnableVariableTypeResolving,
			IEnumerable<IAbstractSyntaxTree> CodeCache,
			out ITypeDeclaration optIdentifierList,
			bool IsCompleteIdentifier,
			out DToken optToken)
		{
			optIdentifierList = DCodeResolver.BuildIdentifierList(Text,
				CaretOffset, /*true,*/ out optToken);

			if (optIdentifierList == null && optToken==null)
				return null;

			IBlockNode SearchParent = null;

			if (optToken != null && (optToken.Kind == DTokens.This || optToken.Kind == DTokens.Super)) // this.myProp; super.baseProp;
				SearchParent = SearchClassLikeAt(Module, CaretLocation);
			else 
				SearchParent = SearchBlockAt(Module, CaretLocation);

			if (optToken != null)
			{
				if (optToken.Kind == DTokens.Super) // super.baseProp
					SearchParent = ResolveBaseClass(SearchParent as DClassLike, CodeCache);
				else if (optToken.Kind == DTokens.__FILE__)
				{
					var n = new DVariable()
					{
						Parent = SearchParent,
						Type = new NormalDeclaration("string"),
						Name = "__FILE__",
						Initializer = new IdentExpression(Module.FileName),
						Description = "Module file name"
					};
					return new[] { n };
				}
				else if (optToken.Kind == DTokens.__LINE__)
				{
					var n = new DVariable()
					{
						Parent = SearchParent,
						Type = new NormalDeclaration("int"),
						Name = "__LINE__",
						Initializer = new IdentExpression(CaretLocation.Line),
						Description = "Code line"
					};
					return new[] { n };
				}
			}

			// If no addtitional identifiers are given, return immediately
			if (optIdentifierList == null || SearchParent==null)
				return new[]{ SearchParent};

			try
			{
				var ret= DCodeResolver.ResolveTypeDeclarations(
					SearchParent,
					optIdentifierList, CodeCache,IsCompleteIdentifier);

				if (EnableVariableTypeResolving && ret != null && ret.Length > 0 && (ret[0] is DVariable || ret[0] is DMethod))
				{
					var ntype = GetDNodeType(ret[0]);
					if (ntype != null)
					{
						var ret2 = DCodeResolver.ResolveTypeDeclarations(SearchParent, ntype, CodeCache,IsCompleteIdentifier);
						if (ret2 != null && ret2.Length > 0)
							return ret2;
					}
				}

				return ret;
			}
			catch { }
			return null;
		}

		/// <summary>
		/// Returns a list of all items that can be accessed in the current scope.
		/// </summary>
		/// <param name="ScopedBlock"></param>
		/// <param name="ImportCache"></param>
		/// <returns></returns>
		public static IEnumerable<INode> EnumAllAvailableMembers(IBlockNode ScopedBlock, IEnumerable<IAbstractSyntaxTree> CodeCache)
		{
			/* First walk through the current scope.
			 * Walk up the node hierarchy and add all their items (private as well as public members).
			 * Resolve base classes and add their non-private|static members.
			 * 
			 * Then add public members of the imported modules 
			 */
			var ret = new List<INode>();
			var ImportCache = ResolveImports(ScopedBlock.NodeRoot as IAbstractSyntaxTree, CodeCache);

			#region Current module/scope related members
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
							ret.AddRange(curWatchedClass.TemplateParameters);

						foreach (var m in curWatchedClass)
						{
							var dm2 = m as DNode;
							var dm3 = m as DMethod; // Only show normal & delegate methods
							if (dm2 == null || 
								(dm3!=null && !(dm3.SpecialType==DMethod.MethodType.Normal || dm3.SpecialType==DMethod.MethodType.Delegate))
								)
								continue;

							// Add static and non-private members of all base classes; add everything if we're still handling the currently scoped class
							if (curWatchedClass==curScope|| dm2.IsStatic || !dm2.ContainsAttribute(DTokens.Private))
								ret.Add(m);
						}

						// Stop adding if Object class level got reached
						if (!string.IsNullOrEmpty(curWatchedClass.Name) && curWatchedClass.Name.ToLower() == "object")
							break;

						curWatchedClass = ResolveBaseClass(curWatchedClass, ImportCache);
					}
				}
				else if (curScope is DMethod)
				{
					var dm = curScope as DMethod;
					ret.AddRange(dm.Parameters);

					if (dm.TemplateParameters != null)
						ret.AddRange(dm.TemplateParameters);

					foreach (var n in dm)
						if (!(n is DStatementBlock))
							ret.Add(n);
				}
				else foreach (var n in curScope)
						if (!(n is DStatementBlock))
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

		public static ITypeDeclaration BuildIdentifierList(string Text, int CaretOffset, /*bool BackwardOnly,*/ out DToken OptionalInitToken)
		{
			OptionalInitToken = null;

			#region Step 1: Walk along the code to find the declaration's beginning
			if (String.IsNullOrEmpty(Text) || CaretOffset >= Text.Length) return null;
			// At first we only want to find the beginning of our identifier list
			// later we will pass the text beyond the beginning to the parser - there we parse all needed expressions from it
			int IdentListStart = -1;

			/*
			T!(...)>.<
			 */

			int isComment = 0;
			bool isString = false, expectDot = false, hadDot = true;
			var bracketStack = new Stack<char>();
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
				if (isComment < 1 && c == '"' && p != '\\')
					isString = !isString;

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
								if (p == '!') // Skip template stuff
									i--;
							}
							else
							{
								// Stop if we reached the most left existing bracket
								// e.g. foo>(< bar| )
								stopSeeking = true;
								IdentListStart++;
							}
							continue;
					}

				// whitespace check
				if (Char.IsWhiteSpace(c)) { if (hadDot) expectDot = false; else expectDot = true; continue; }

				if (c == '.')
				{
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
						continue;
					else
						IdentListStart++;
				}

				stopSeeking = true;
			}

			#endregion

			#region 2: Init the parser
			if (!stopSeeking || IdentListStart < 0)
				return null;

			// If code e.g. starts with a bracket, increment IdentListStart
			var ch = Text[IdentListStart];
			while (IdentListStart < Text.Length - 1 && !Char.IsLetterOrDigit(ch) && ch != '_' && ch != '.')
			{
				IdentListStart++;
				ch = Text[IdentListStart];
			}

			//if (BackwardOnly && IdentListStart >= CaretOffset)return null;

			var psr = DParser.ParseBasicType(Text.Substring(IdentListStart), out OptionalInitToken);
			#endregion

			return psr;
		}

		public static IBlockNode SearchBlockAt(IBlockNode Parent, CodeLocation Where)
		{
			foreach (var n in Parent)
			{
				if (!(n is IBlockNode)) continue;

				var b = n as IBlockNode;
				if (Where > b.StartLocation && Where < b.EndLocation)
					return SearchBlockAt(b, Where);
			}

			return Parent;
		}

		public static IBlockNode SearchClassLikeAt(IBlockNode Parent, CodeLocation Where)
		{
			foreach (var n in Parent)
			{
				if (!(n is DClassLike)) continue;

				var b = n as IBlockNode;
				if (Where > b.BlockStartLocation && Where < b.EndLocation)
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
		public static IEnumerable<IAbstractSyntaxTree> ResolveImports(IAbstractSyntaxTree ActualModule, IEnumerable<IAbstractSyntaxTree> CodeCache)
		{
			var ret = new List<IAbstractSyntaxTree>();
			if (CodeCache == null || ActualModule == null) return ret;

			// First add all local imports
			var localImps = new List<string>();
			foreach (var kv in ActualModule.Imports)
				localImps.Add(kv.Key.ToString());

			// Then try to add the 'object' module
			var objmod = SearchModuleInCache(CodeCache, "object");
			if (objmod != null && !ret.Contains(objmod))
				ret.Add(objmod);

			foreach (var m in CodeCache)
				if (localImps.Contains(m.Name) && !ret.Contains(m))
				{
					ret.Add(m);
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

					// So if there aren't any public imports in our import, continue without doing anything
					ResolveImports(ret, m, CodeCache);
				}

			return ret;
		}

		public static void ResolveImports(List<IAbstractSyntaxTree> ImportModules,
			IAbstractSyntaxTree ActualModule, IEnumerable< IAbstractSyntaxTree> CodeCache)
		{
			var localImps = new List<string>();
			foreach (var kv in ActualModule.Imports)
				if (kv.Value)
					localImps.Add(kv.Key.ToString());

			foreach (var m in CodeCache)
				if (localImps.Contains(m.Name) && !ImportModules.Contains(m))
				{
					ImportModules.Add(m);
					ResolveImports(ImportModules, m, CodeCache);
				}
		}
		#endregion

		#region Declaration resolving
		public enum NodeFilter
		{
			/// <summary>
			/// Returns all static and public children of a node
			/// </summary>
			PublicStaticOnly,
			/// <summary>
			/// Returns all public members e.g. of an object
			/// </summary>
			PublicOnly,

			/// <summary>
			/// e.g. in class hierarchies: Returns all public and protected members
			/// </summary>
			NonPrivate,
			/// <summary>
			/// Returns all members
			/// </summary>
			All
		}

		public static bool MatchesFilter(NodeFilter Filter, INode n)
		{
			switch (Filter)
			{
				case NodeFilter.All:
					return true;
				case NodeFilter.PublicOnly:
					return (n as DNode).IsPublic;
				case NodeFilter.NonPrivate:
					return !(n as DNode).ContainsAttribute(DTokens.Private);
				case NodeFilter.PublicStaticOnly:
					return (n as DNode).IsPublic && (n as DNode).IsStatic;
			}
			return false;
		}

		public static IAbstractSyntaxTree SearchModuleInCache(IEnumerable<IAbstractSyntaxTree> HayStack, string ModuleName)
		{
			foreach (var m in HayStack)
			{
				if (m.Name == ModuleName) return m;
			}
			return null;
		}

		public static ITypeDeclaration GetDNodeType(INode VariableOrMethod)
		{
			var ret = VariableOrMethod.Type;

			// If our node contains an auto attribute, expect that there is an initializer that implies its type
			if (VariableOrMethod is DVariable && (VariableOrMethod as DNode).ContainsAttribute(DTokens.Auto))
			{
				var init = (VariableOrMethod as DVariable).Initializer;
				if (init is InitializerExpression)
				{
					var tex = (init as InitializerExpression).Initializer;
					while (tex != null)
					{
						if (tex is TypeDeclarationExpression)
						{
							ret = (tex as TypeDeclarationExpression).Declaration;

							// If variable initializer contains template arguments, skip those and pass the raw name only
							if (ret is TemplateDecl)
								ret = (ret as TemplateDecl).Base;
							break;
						}

						tex = tex.Base;
					}
				}
			}


			return ret;
		}

		/// <summary>
		/// Finds the location (module node) where a type (TypeExpression) has been declared.
		/// Note: This function only searches within 1 module only!
		/// </summary>
		/// <param name="Module"></param>
		/// <param name="IdentifierList"></param>
		/// <returns>When a type was found, the declaration entry will be returned. Otherwise, it'll return null.</returns>
		public static INode[] ResolveTypeDeclarations_ModuleOnly(IBlockNode BlockNode, ITypeDeclaration IdentifierList, NodeFilter Filter,IEnumerable<IAbstractSyntaxTree> ImportCache)
		{
			var ret = new List<INode>();

			if (BlockNode == null || IdentifierList == null) return ret.ToArray();

			// Modules    Declaration
			// |---------|-----|
			// std.stdio.writeln();
			if (IdentifierList is IdentifierList)
			{
				var il = IdentifierList as IdentifierList;
				var skippedIds = 0;

				// Now search the entire block
				var istr = il.ToString();

				var mod = BlockNode is DModule?BlockNode as DModule: BlockNode.NodeRoot as DModule;
				/* If the id list start with the name of BlockNode's root module, 
				 * skip those identifiers first to proceed seeking the rest of the list
				 */
				if (mod != null && !string.IsNullOrEmpty(mod.ModuleName) && istr.StartsWith(mod.ModuleName))
				{
					skippedIds += mod.ModuleName.Split('.').Length;
					istr = il.ToString(skippedIds);
				}

				// Now move stepwise deeper calling ResolveTypeDeclaration recursively
				ret.Add(BlockNode); // Temporarily add the block node to the return array - it gets proceeded in the next while loop

				var tFilter = Filter;
				while (skippedIds < il.Parts.Count && ret.Count > 0)
				{
					var DeeperLevel = new List<INode>();
					// As long as our node(s) can contain other nodes, scan it
					foreach (var n in ret)
						DeeperLevel.AddRange(ResolveTypeDeclarations_ModuleOnly(n as IBlockNode, il[skippedIds], tFilter, ImportCache));

					// If a variable is given and if it's not the last identifier, return it's definition type
					// If a method is given, search for its return type
					if (DeeperLevel.Count > 0 && skippedIds < il.Parts.Count - 1)
					{
						if (DeeperLevel[0] is DVariable || DeeperLevel[0] is DMethod)
						{
							// If we retrieve deeper levels, we are only allowed to scan for public members
							tFilter = NodeFilter.PublicOnly;
							var v = DeeperLevel[0];
							DeeperLevel.Clear();

							DeeperLevel.AddRange(ResolveTypeDeclarations_ModuleOnly(v.Parent as IBlockNode, GetDNodeType(v), Filter, ImportCache));
						}
					}

					skippedIds++;
					ret = DeeperLevel;
				}

				return ret.ToArray();
			}

			//HACK: Scan the type declaration list for any NormalDeclarations
			var td = IdentifierList;
			while (td != null && !(td is NormalDeclaration))
				td = td.Base;

			var baseTypes = new List<INode>();

			if (td is NormalDeclaration)
			{
				var nameIdent = td as NormalDeclaration;

				// Scan from the inner to the outer level
				var currentParent = BlockNode;
				while (currentParent != null)
				{
					// Scan the node's children for a match - return if we found one
					foreach (var ch in currentParent)
					{
						if (nameIdent.Name == ch.Name && !ret.Contains(ch) && MatchesFilter(Filter, ch))
							baseTypes.Add(ch);
					}

					// If our current Level node is a class-like, also attempt to search in its baseclass!
					if (currentParent is DClassLike)
					{
						var baseClass = ResolveBaseClass(currentParent as DClassLike, ImportCache);
						if (baseClass != null)
							baseTypes.AddRange(ResolveTypeDeclarations_ModuleOnly(baseClass, nameIdent, NodeFilter.NonPrivate, ImportCache));
					}

					// Check parameters
					if (currentParent is DMethod)
						foreach (var ch in (currentParent as DMethod).Parameters)
						{
							if (nameIdent.Name == ch.Name)
								baseTypes.Add(ch);
						}

					// and template parameters
					if (currentParent is DNode && (currentParent as DNode).TemplateParameters != null)
						foreach (var ch in (currentParent as DNode).TemplateParameters)
						{
							if (nameIdent.Name == ch.Name)
								baseTypes.Add(ch);
						}

					// Move root-ward
					currentParent = currentParent.Parent as IBlockNode;
				}

				if (td == IdentifierList)
					ret.AddRange(baseTypes);
				else
				{
					
				}
			}

			//TODO: Here a lot of additional checks and more detailed type evaluations are missing!

			return ret.ToArray();
		}



		/// <summary>
		/// Search a type within an entire Module Cache.
		/// </summary>
		/// <param name="ImportCache"></param>
		/// <param name="CurrentlyScopedBlock"></param>
		/// <param name="IdentifierList"></param>
		/// <returns></returns>
		public static INode[] ResolveTypeDeclarations(IBlockNode CurrentlyScopedBlock, ITypeDeclaration IdentifierList, IEnumerable<IAbstractSyntaxTree> ImportCache,
			bool IsCompleteIdentifier)
		{
			var ret = new List<INode>();

			var ThisModule = CurrentlyScopedBlock.NodeRoot as DModule;

			// Of course it's needed to scan our own module at first
			if (ThisModule != null)
				ret.AddRange(ResolveTypeDeclarations_ModuleOnly(CurrentlyScopedBlock, IdentifierList, NodeFilter.All, ImportCache));

			// Then search within the imports for our IdentifierList
			if(ImportCache!=null)
				foreach (var m in ImportCache)
				{
					// Add the module itself to the returned list if its name starts with the identifierlist
					if (IsCompleteIdentifier? (m.Name.StartsWith(IdentifierList.ToString()+".") || m.Name==IdentifierList.ToString()) // If entire identifer was typed, check if the entire id is part of the module name
						: m.Name.StartsWith(IdentifierList.ToString()))
						ret.Add(m);

					else if (m.FileName != ThisModule.FileName) // We already parsed this module
						ret.AddRange(ResolveTypeDeclarations_ModuleOnly(m, IdentifierList, NodeFilter.PublicOnly, ImportCache));
				}

			return ret.ToArray();
		}

		/// <summary>
		/// Resolves all base classes of a class, struct, template or interface
		/// </summary>
		/// <param name="ModuleCache"></param>
		/// <param name="ActualClass"></param>
		/// <returns></returns>
		public static DClassLike ResolveBaseClass(DClassLike ActualClass, IEnumerable<IAbstractSyntaxTree> ModuleCache)
		{
			// Implicitly set the object class to the inherited class if no explicit one was done
			if (ActualClass.BaseClasses.Count < 1)
			{
				var ObjectClass = ResolveTypeDeclarations(ActualClass.NodeRoot as IBlockNode, new NormalDeclaration("Object"), ModuleCache,true);
				if (ObjectClass.Length > 0 && ObjectClass[0] != ActualClass) // Yes, it can be null - like the Object class which can't inherit itself
					return ObjectClass[0] as DClassLike;
			}
			else // Take the first only (since D forces single inheritance)
			{
				var ClassMatches = ResolveTypeDeclarations(ActualClass.NodeRoot as IBlockNode, ActualClass.BaseClasses[0], ModuleCache,true);
				if (ClassMatches.Length > 0)
					return ClassMatches[0] as DClassLike;
			}

			return null;
		}

		#endregion

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

				while (off < HayStack.Length-1)
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
						if (off + Needle.Length-1 >= HayStack.Length)
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

		/// <summary>
		/// Helper class for e.g. finding the initial offset of a statement.
		/// </summary>
		public class ReverseParsing
		{
			/// <summary>
			/// Parses/Skips through the code backward to find the beginning of the method call statement.
			/// 
			/// ModuleA.StaticClass.MyFoo!(int,bool)(1,true);
			/// </summary>
			/// <param name="Code"></param>
			/// <param name="CaretOffset"></param>
			/// <param name="TriggerChar">The key the user recently typed (usually '!', '(' or ',')</param>
			/// <returns>The call-statement's start offset</returns>
			public static int ResolveMethodCallStatementOffset(
				string Code,
				int CaretOffset,
				char TriggerChar,
				
				out bool IsTemplateParameter,
				out int ParameterNumber)
			{
				IsTemplateParameter = false;
				ParameterNumber = 0;

				var startOffset = 0;
				var curChar='\0';
				int i = CaretOffset;
				while (i >= 0)
				{
					curChar = Code[i];



					i--;
				}

				return startOffset;
			}
		}
	}
}
