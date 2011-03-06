using System;
using System.Collections.Generic;
using System.Text;
using Parser.Core;
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

		

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Module"></param>
		/// <param name="Text"></param>
		/// <param name="CaretOffset"></param>
		/// <param name="CaretLocation"
		/// <param name="ImportCache"></param>
		/// <returns></returns>
		public static IEnumerable<INode> ResolveTypeDeclarations(IAbstractSyntaxTree Module, string Text, int CaretOffset, CodeLocation CaretLocation, bool EnableVariableTypeResolving, IEnumerable<IAbstractSyntaxTree> ImportCache)
		{
			DToken tk = null;
			var id = DCodeResolver.BuildIdentifierList(Text,
				CaretOffset, /*true,*/ out tk);

			if (id == null && tk==null)
				return null;

			IBlockNode SearchParent = null;

			if (tk!=null &&( tk.Kind == DTokens.This || tk.Kind == DTokens.Super)) // this.myProp; super.baseProp;
				SearchParent = SearchClassLikeAt(Module, CaretLocation);
			else 
				SearchParent = SearchBlockAt(Module, CaretLocation);

			if (tk != null)
			{
				if (tk.Kind == DTokens.Super) // super.baseProp
					SearchParent = ResolveBaseClass(SearchParent as DClassLike, ImportCache);
				else if (tk.Kind == DTokens.__FILE__)
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
				else if (tk.Kind == DTokens.__LINE__)
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
			if (id == null || SearchParent==null)
				return new[]{ SearchParent};

			try
			{
				var ret= DCodeResolver.ResolveTypeDeclarations(
					SearchParent,
					id, ImportCache);

				if (EnableVariableTypeResolving && ret != null && ret.Length > 0 && (ret[0] is DVariable || ret[0] is DMethod))
				{
					var ntype = GetDNodeType(ret[0]);
					if (ntype != null)
					{
						var ret2 = DCodeResolver.ResolveTypeDeclarations(SearchParent, ntype, ImportCache);
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
						foreach (var m in curWatchedClass)
						{
							var dm2 = m as DNode;
							if (dm2 == null)
								continue;

							// Add static and non-private members of all base classes
							if (dm2.IsStatic || !dm2.ContainsAttribute(DTokens.Private))
								ret.Add(m);
						}

						// Stop adding if Object class level got reached
						if (curWatchedClass.Name.ToLower() == "object")
							break;

						curWatchedClass = ResolveBaseClass(curWatchedClass, ImportCache);
					}
				}
				else foreach (var n in curScope)
				{
					//TODO: Skip on anonymous blocks like if-blocks or for-loops
					//TODO: (More parser-related!) Add anonymous blocks (e.g. delegates) to the syntax tree
					
					ret.Add(n);
					if (n is DNode)
					{
						var dn = n as DNode;
						// Add function params as well as DNode-specific template params
						if (n is DMethod)
							ret.AddRange((n as DMethod).Parameters);

						if (dn.TemplateParameters != null)
							ret.AddRange(dn.TemplateParameters);
					}
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

							DeeperLevel.AddRange(ResolveTypeDeclarations_ModuleOnly(v.Parent as IBlockNode, GetDNodeType(v), NodeFilter.PublicOnly, ImportCache));
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
							ret.Add(ch);
					}

					// If our current Level node is a class-like, also attempt to search in its baseclass!
					if (currentParent is DClassLike)
					{
						var baseClass = ResolveBaseClass(currentParent as DClassLike, ImportCache);
						if (baseClass != null)
							ret.AddRange(ResolveTypeDeclarations_ModuleOnly(baseClass, nameIdent, NodeFilter.NonPrivate, ImportCache));
					}

					// Check parameters
					if (currentParent is DMethod)
						foreach (var ch in (currentParent as DMethod).Parameters)
						{
							if (nameIdent.Name == ch.Name)
								ret.Add(ch);
						}

					// and template parameters
					if (currentParent is DNode && (currentParent as DNode).TemplateParameters != null)
						foreach (var ch in (currentParent as DNode).TemplateParameters)
						{
							if (nameIdent.Name == ch.Name)
								ret.Add(ch);
						}

					// Move root-ward
					currentParent = currentParent.Parent as IBlockNode;
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
		public static INode[] ResolveTypeDeclarations(IBlockNode CurrentlyScopedBlock, ITypeDeclaration IdentifierList, IEnumerable<IAbstractSyntaxTree> ImportCache)
		{
			var ret = new List<INode>();

			var ThisModule = CurrentlyScopedBlock.NodeRoot as DModule;

			// Of course it's needed to scan our own module at first
			if (ThisModule != null)
				ret.AddRange(ResolveTypeDeclarations_ModuleOnly(CurrentlyScopedBlock, IdentifierList, NodeFilter.All, ImportCache));

			// Then search within the imports for our IdentifierList
			foreach (var m in ImportCache)
			{
				// Add the module itself to the returned list if its name starts with the identifierlist
				if (m.Name.StartsWith(IdentifierList.ToString()))
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
				var ObjectClass = ResolveTypeDeclarations(ActualClass.NodeRoot as IBlockNode, new NormalDeclaration("Object"), ModuleCache);
				if (ObjectClass.Length > 0 && ObjectClass[0] != ActualClass) // Yes, it can be null - like the Object class which can't inherit itself
					return ObjectClass[0] as DClassLike;
			}
			else // Take the first only (since D forces single inheritance)
			{
				var ClassMatches = ResolveTypeDeclarations(ActualClass.NodeRoot as IBlockNode, ActualClass.BaseClasses[0], ModuleCache);
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

				while (off < HayStack.Length)
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
						if (off + Needle.Length >= HayStack.Length)
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


		/*
		/// <summary>
		/// Reinterpretes all given expression strings to scan the global class hierarchy and find the member called like the last given expression
		/// </summary>
		/// <param name="local"></param>
		/// <param name="expressions"></param>
		/// <returns></returns>
		public static DNode FindActualExpression(DProject prj, DModule local, CodeLocation caretLocation, string[] expressions, bool dotPressed, bool ResolveBaseType, out bool isSuper, out bool isInstance, out bool isNameSpace, out DModule module)
		{
			CompilerConfiguration cc = prj != null ? prj.Compiler : D_IDE_Properties.Default.DefaultCompiler;
			module = local;
			isSuper = false;
			isInstance = false;
			isNameSpace = false;
			try
			{
				int i = 0;
				if (expressions == null || expressions.Length < 1) return null;

				DNode seldt = null, seldd = null; // Selected DNode - Will be returned later

				if (expressions[0] == "this")
				{
					seldt = GetClassAt(local.dom, caretLocation);
					i++;
				}
				else if (expressions[0] == "super")
				{
					seldt = GetClassAt(local.dom, caretLocation);
					if (seldt is DClassLike && (seldt as DClassLike).BaseClasses.Count > 0)
					{
						seldt = SearchGlobalExpr(prj, local, (seldt as DClassLike).BaseClasses[0].ToString(), false, out module);
					}
					isSuper = true;
					i++;
				}
				else
				{
					// Search expression in all superior blocks
					DNode cblock = GetBlockAt(local.dom, caretLocation);
					seldt = SearchExprInClassHierarchyBackward(cc, cblock, RemoveTemplatePartFromDecl(expressions[0]));
					// Search expression in current module root first
					if (seldt == null) seldt = SearchGlobalExpr(prj, local, RemoveTemplatePartFromDecl(expressions[0]), true, out module);
					// If there wasn't found anything, search deeper and recursive
					//if (seldt == null) seldt = SearchExprInClassHierarchy(local.dom, GetBlockAt(local.dom, caretLocation), RemoveArrayOrTemplatePartFromDecl(expressions[0]));
					// EDIT: Don't search recursively in all blocks of local.dom because you'd resolve something you couldn't access...

					// If seldt is a variable, resolve its basic type such as a class etc

					seldd = seldt;
					bool IsLastInChain = i >= expressions.Length - 1;
					if ((ResolveBaseType && IsLastInChain) || !IsLastInChain) seldt = ResolveReturnOrBaseType(prj, local, seldt, IsLastInChain);
					if (seldt != seldd) isInstance = true;
					if (seldt != null) i++;

					#region Seek in global and local(project) namespace names
					if (seldt == null) // if there wasn't still anything found in global space
					{
						string modpath = "";
						string[] modpath_packages;
						List<DModule> dmods = new List<DModule>(cc.GlobalModules),
							dmods2 = new List<DModule>();
						if (prj != null) dmods.AddRange(prj.files);// Very important: add the project's files to the search list

						i = expressions.Length;
						/*
						 * i=0	i=1			i=2			i=3
						 * std.
						 * std.	socket.
						 * std. socketstream
						 * std.	windows.	windows.
						 * std.	c.			stdio.		printf();
						 * std.	complex
						 * /
						while (i > 0)
						{
							modpath = "";
							for (int _i = 0; _i < i; _i++) modpath += (_i > 0 ? "." : "") + expressions[_i];
							modpath_packages = modpath.Split('.');
							module = null;
							seldt = null;

							foreach (DModule gpf in dmods)
							{
								if (gpf.ModuleName.StartsWith(modpath, StringComparison.Ordinal))
								{
									string[] path_packages = gpf.ModuleName.Split('.');
									dmods2.Add(gpf);
									module = gpf;
									seldt = gpf.dom;
									if (gpf.ModuleName == modpath) // if this module has the same path as equally typed in the editor, take this as the only one
									{
										dmods2.Clear();
										dmods2.Add(gpf);
										break;
									}
								}
							}

							if (dmods2.Count < 1) { i--; continue; }
							isNameSpace = true;

							if (prj == null || (module = prj.FileDataByFile(modpath)) == null)
								module = D_IDE_Properties.Default.GetModule(D_IDE_Properties.Default.DefaultCompiler, modpath);

							if (dmods2.Count == 1 && dmods2[0].ModuleName == modpath)
							{
								break;
							}

							//Create a synthetic node which only contains module names
							seldt = new DNode(FieldType.Root);
							seldt.module = modpath;
							if (module != null)
							{
								seldt.module = module.ModuleName;
								seldt.children = module.Children;
								seldt.endLoc = module.dom.endLoc;
							}

							foreach (DModule dm in dmods2)
							{
								seldt.Add(dm.dom);
							}
							break;
						}
					}
					#endregion
				}

				for (; i < expressions.Length && seldt != null; i++)
				{
					isInstance = false;
					seldt = SearchExprInClassHierarchy(cc, seldt, null, RemoveTemplatePartFromDecl(expressions[i]));
					if (seldt == null) break;

					seldd = seldt;
					bool IsLastInChain = i == expressions.Length - 1;
					if ((ResolveBaseType && IsLastInChain) || !IsLastInChain) seldt = ResolveReturnOrBaseType(prj, local, seldt, IsLastInChain);
					if (seldt != seldd) isInstance = true;
				}

				return seldt;
			}
			catch (Exception ex)
			{
				D_IDEForm.thisForm.Log(ex.Message);
			}
			return null;
		}
		*/

		/*
		public ICompletionData[] GenerateCompletionData(string fn, TextArea ta, char ch)
		{
			ImageList icons = D_IDEForm.icons;

			List<ICompletionData> rl = new List<ICompletionData>();
			List<string> expressions = new List<string>();
			try
			{
				DocumentInstanceWindow diw = D_IDEForm.SelectedTabPage;
				DProject project = diw.project;
				if (project != null) cc = project.Compiler;
				DModule pf = diw.fileData;

				CodeLocation tl = new CodeLocation(ta.Caret.Column + 1, ta.Caret.Line + 1);
				DNode seldt, seldd;

				int off = ta.Caret.Offset;

				bool isInst = false; // Here the return type of a function is the base type for which the data will be generated
				bool isSuper = false;
				bool isNameSpace = false;

				#region Compute expressions based on caret location
				char tch;
				string texpr = "";
				int KeyWord = -1, psb = 0;
				for (int i = off - 1; i > 0; i--)
				{
					tch = ta.Document.GetCharAt(i);

					if (tch == ']') psb++;

					if (char.IsLetterOrDigit(tch) || tch == '_' || psb > 0) texpr += tch;

					if (!char.IsLetterOrDigit(tch) && tch != '_' && psb < 1)
					{
						if (texpr == "") break;
						texpr = ReverseString(texpr);
						if (KeyWord < 0 && (KeyWord = DKeywords.GetToken(texpr)) >= 0 && texpr != "this" && texpr != "super")
						{
							break;
						}
						else
						{
							expressions.Add(texpr);
						}
						texpr = "";

						if (!char.IsWhiteSpace(tch) && tch != ';' && tch != '.')
						{
							break;
						}
						off = i;
					}
					if (tch == '[') psb--;
				}

				if (KeyWord == DTokens.New && expressions.Count < 1)
				{
					rl.AddRange(cc.GlobalCompletionList);
					presel = null;
					return rl.ToArray();
				}

				if (expressions.Count < 1 && ch != '\0') return rl.ToArray();

				expressions.Reverse();
				#endregion

				if (expressions.Count < 1)
				{
					if (ch == '\0') expressions.Add("");
					else return null;
				}

				if (ch != '.' && (expressions.Count == 1 && expressions[0].Length < 2) && KeyWord < 0) // Reflect entire cache content including D KeyWords
				{
					if (expressions.Count > 0) presel = expressions[expressions.Count - 1];
					else presel = null;
					rl.AddRange(diw.CurrentCompletionData);

					//rl.Sort();
					return rl.ToArray();
				}

				if (ch == '.')
				{
					#region A.B.c>.<
					presel = null; // Important: After a typed dot ".", set previous selection string to null!
					DModule gpf = null;

					seldt = FindActualExpression(project, pf, tl, expressions.ToArray(), ch == '.', true, out isSuper, out isInst, out isNameSpace, out gpf);

					if (seldt == null) return rl.ToArray();
					//Debugger.Log(0,"parsing", DCompletionData.BuildDescriptionString(seldt.Parent) + " " + DCompletionData.BuildDescriptionString(seldt));

					//seldd = seldt;
					//seldt = ResolveReturnOrBaseType(prj, pf, seldt, expressions.Count==2);
					if (seldt.fieldtype == FieldType.Function	//||(seldt.fieldtype == FieldType.Variable && !DTokens.BasicTypes[(int)seldt.TypeToken])
					   )
					{
						seldd = seldt;
						seldt = SearchGlobalExpr(cc, pf.dom, seldt.Type.ToString());
						isInst = true;
					}

					if (seldt != null)
					{
						if (expressions[0] == "this" && expressions.Count < 2) // this.
						{
							AddAllClassMembers(cc, seldt, ref rl, true);

							foreach (DNode arg in (seldt as DMethod).Parameters)
							{
								if (arg.Type == null || arg.name == null) continue;
								rl.Add(new DCompletionData(arg, seldt, icons.Images.IndexOfKey("Icons.16x16.Parameter.png")));
							}
						}
						else if (expressions[0] == "super" && expressions.Count < 2) // super.
						{
							if (seldt is DClassLike && (seldt as DClassLike).BaseClasses.Count > 0)
							{
								foreach (D_Parser.TypeDeclaration td in (seldt as DClassLike).BaseClasses)
								{
									seldd = SearchGlobalExpr(cc, pf.dom, td.ToString());
									if (seldd != null)
									{
										AddAllClassMembers(cc, seldd, ref rl, true);

										foreach (DNode arg in (seldt as DMethod).Parameters)
										{
											if (arg.Type == null || arg.name == null) continue;
											rl.Add(new DCompletionData(arg, seldd, icons.Images.IndexOfKey("Icons.16x16.Parameter.png")));
										}
									}
								}
							}
						}
						else if (seldt.fieldtype == FieldType.Enum && seldt.Count > 0) // Flags.
						{
							foreach (DNode dt in seldt)
							{
								rl.Add(new DCompletionData(dt, seldt));
							}
						}
						else if (seldt.fieldtype == FieldType.Variable) // myVar.
						{
							AddAllClassMembers(cc, seldt, ref rl, false);
							AddTypeStd(seldt, ref rl);
						}
						else // e.g. MessageBox>.<
						{
							if (isInst || isNameSpace)
							{
								AddAllClassMembers(cc, seldt, ref rl, !isNameSpace);
							}
							else
							{
								foreach (DNode dt in seldt)
								{
									if (
										//showAll ||
										(isSuper && dt.modifiers.Contains(DTokens.Protected)) || // super.ProtectedMember
										(isInst && dt.modifiers.Contains(DTokens.Public)) || // (MyType) foo().MyMember
										(dt.modifiers.Contains(DTokens.Static) && // 
										(dt.modifiers.Contains(DTokens.Public)  // 
										|| dt.modifiers.Count < 2)) ||
										(dt.fieldtype == FieldType.EnumValue && // 
											(dt.modifiers.Contains(DTokens.Public)  // 
											|| dt.modifiers.Count < 2)
										)
										) // int a;
										rl.Add(new DCompletionData(dt, seldt));
								}
							}
							if (!isNameSpace) AddTypeStd(seldt, ref rl);

							foreach (DNode arg in seldt.TemplateParameters)
							{
								if (arg.Type == null || arg.name == null) continue;
								rl.Add(new DCompletionData(arg, seldt, icons.Images.IndexOfKey("Icons.16x16.Parameter.png")));
							}
							if (seldt is DMethod)
								foreach (DNode arg in (seldt as DMethod).Parameters)
								{
									if (arg.Type == null || arg.name == null) continue;
									rl.Add(new DCompletionData(arg, seldt, icons.Images.IndexOfKey("Icons.16x16.Parameter.png")));
								}
						}
					}
					#endregion
				}
			}
			catch (Exception ex)
			{
				D_IDEForm.thisForm.Log(ex.Message);
			}
			//rl.Sort();
			return rl.ToArray();
		}
		*/

		/*

		/// <summary>
		/// Resolves either the return type of a method or the base type of a variable
		/// </summary>
		/// <param name="IsLastInExpressionChain">This value is needed for resolving functions because if this parameter is true then it returns the owner node</param>
		/// <returns>The base type or return type of a node</returns>
		public static INode ResolveReturnOrBaseType(INode Node, bool IsLastInExpressionChain,params IAbstractSyntaxTree[] Cache)
		{
			return null;
		}

		/// <summary>
		/// A subroutine for ResolveMultipleNodes
		/// </summary><see cref="ResolveMultipleNodes"/>
		/// <param name="prj"></param>
		/// <param name="local"></param>
		/// <param name="parent"></param>
		/// <param name="i"></param>
		/// <param name="expressions"></param>
		/// <returns></returns>
		static List<DNode> _res(DProject prj, DModule local, DNode parent, int i, string[] expressions)
		{
			List<DNode> tl = new List<DNode>();
			if (expressions == null || i >= expressions.Length)
			{
				tl.Add(parent);
				return tl;
			}

			foreach (DNode dt in GetExprsByName(parent, expressions[i], true))
			{
				DNode seldt = ResolveReturnOrBaseType(prj, local, dt, i >= expressions.Length - 1);

				if (seldt == null) seldt = dt;

				tl.AddRange(_res(prj, local, seldt, i + 1, expressions));
			}
			return tl;
		}

		/// <summary>
		/// Searches nodes in global space which have the same name and returns all of these
		/// </summary>
		/// <param name="prj"></param>
		/// <param name="local"></param>
		/// <param name="expressions"></param>
		/// <returns></returns>
		public static List<DNode> ResolveMultipleNodes(DProject prj, DModule local, string[] expressions)
		{
			if (expressions == null || expressions.Length < 1) return new List<DNode>();

			List<DNode> rl = SearchGlobalExprs(prj, local.dom, expressions[0]);
			if (expressions.Length < 2 || rl.Count < 1) return rl;

			List<DNode> ret = new List<DNode>();
			foreach (DNode dt in rl)
			{
				DNode seldt = ResolveReturnOrBaseType(prj, local, dt, expressions.Length == 2);
				if (seldt == null) seldt = dt;
				ret.AddRange(_res(prj, local, seldt, 1, expressions));
			}
			return ret;
		}

		*/
	}
}
