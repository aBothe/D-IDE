using System.Collections.Generic;
using D_Parser.Dom;
using D_Parser.Dom.Expressions;
using D_Parser.Dom.Statements;
using D_Parser.Parser;
using D_Parser.Resolver;

namespace D_Parser.Completion
{
	public class AbstractCompletionSupport
	{
		public readonly ICompletionDataGenerator CompletionDataGenerator;

		public AbstractCompletionSupport(ICompletionDataGenerator CompletionDataGenerator)
		{
			this.CompletionDataGenerator = CompletionDataGenerator;
		}

		#region Helper Methods
		public static bool IsIdentifierChar(char key)
		{
			return char.IsLetterOrDigit(key) || key == '_';
		}

		public enum ItemVisibility
		{
			All,
			Static,
			PublicAndStatic,
			PublicOrStatic,
			Protected
		}

		public static bool CanItemBeShownGenerally(INode dn)
		{
			if (dn == null || string.IsNullOrEmpty(dn.Name))
				return false;

			if (dn is DMethod)
			{
				var dm = dn as DMethod;

				if (dm.SpecialType == DMethod.MethodType.Unittest ||
					dm.SpecialType == DMethod.MethodType.Destructor ||
					dm.SpecialType == DMethod.MethodType.Constructor)
					return false;
			}

			return true;
		}

		public static bool HaveSameAncestors(INode higherLeveledNode, INode lowerLeveledNode)
		{
			var curPar = higherLeveledNode;

			while (curPar != null)
			{
				if (curPar == lowerLeveledNode)
					return true;

				curPar = curPar.Parent;
			}
			return false;
		}
		#endregion

		public void BuildCompletionData(IEditorData Editor,
			string EnteredText,
			out string lastResultPath)
		{
			lastResultPath = null;

			IStatement curStmt = null;
			var curBlock = DResolver.SearchBlockAt(Editor.SyntaxTree, Editor.CaretLocation, out curStmt);

			if (curBlock == null)
				return;

			IEnumerable<INode> listedItems = null;

			// Usually shows variable members
			#region DotCompletion
			if (EnteredText == ".")
			{
				alreadyAddedModuleNameParts.Clear();

				var resolveResults = DResolver.ResolveType(
					Editor.ModuleCode,
					Editor.CaretOffset - 1,
					Editor.CaretLocation,
					new ResolverContext
					{
						ScopedBlock = curBlock,
						ParseCache = Editor.ParseCache,
						ImportCache = Editor. ImportCache
					}
					);

				if (resolveResults == null) //TODO: Add after-space list creation when an unbound . (Dot) was entered which means to access the global scope
					return;
				/*
				 * Note: When having entered a module name stub only (e.g. "std." or "core.") it's needed to show all packages that belong to that root namespace
				 */

				foreach (var rr in resolveResults)
				{
					lastResultPath = ResolveResult.GetResolveResultString(rr);
					BuildCompletionData(rr, curBlock);
				}
			}
			#endregion

			else if (EnteredText==null || EnteredText==" " || EnteredText.Length<1 || IsIdentifierChar(EnteredText[0]))
			{
				// 1) Get current context the caret is at.
				ParserTrackerVariables trackVars = null;

				var parsedBlock = DResolver.FindCurrentCaretContext(
					Editor.ModuleCode,
					curBlock,
					Editor.CaretOffset,
					Editor.CaretLocation,
					out trackVars);

				// 2) If in declaration and if node identifier is expected, do not show any data
				if (trackVars==null ||
					(trackVars.LastParsedObject is INode && trackVars.ExpectingIdentifier) ||
					(trackVars.LastParsedObject is TokenExpression && DTokens.BasicTypes[(trackVars.LastParsedObject as TokenExpression).Token] &&
					!string.IsNullOrEmpty(EnteredText) &&
					IsIdentifierChar(EnteredText[0]))
					)
					return;

				var visibleMembers = DResolver.MemberTypes.All;

				if (trackVars.LastParsedObject is ImportStatement)
				{
					visibleMembers = DResolver.MemberTypes.Imports;
				}
				else if (trackVars.LastParsedObject is NewExpression)
				{
					visibleMembers = DResolver.MemberTypes.Imports | DResolver.MemberTypes.Types;
				}
				else if (EnteredText == " ")
					return;

				if (!(parsedBlock is BlockStatement) && !trackVars.IsParsingInitializer)
					visibleMembers = DResolver.MemberTypes.Imports | DResolver.MemberTypes.Types | DResolver.MemberTypes.Keywords;

				// In a method, parse from the method's start until the actual caret position to get an updated insight
				if (DResolver.CanShowMemberType(visibleMembers,DResolver.MemberTypes.Variables) && curBlock is DMethod)
				{
					if (parsedBlock is BlockStatement)
					{
						var decls = BlockStatement.GetItemHierarchy(parsedBlock as BlockStatement, Editor.CaretLocation);

						foreach (var n in decls)
							CompletionDataGenerator.Add(n);
					}
				}

				if (visibleMembers != DResolver.MemberTypes.Imports)
					listedItems = DResolver.EnumAllAvailableMembers(curBlock/*, curStmt*/, Editor.CaretLocation, Editor.ImportCache, visibleMembers);
				
				//TODO: Split the keywords into such that are allowed within block statements and non-block statements
				if(DResolver.CanShowMemberType(visibleMembers, DResolver.MemberTypes.Keywords))
					foreach (var kv in DTokens.Keywords)
						CompletionDataGenerator.Add(kv.Key);

				#region Add module name stubs of importable modules
				if (DResolver.CanShowMemberType(visibleMembers, DResolver.MemberTypes.Imports))
				{
					var nameStubs = new Dictionary<string, string>();
					var availModules = new List<IAbstractSyntaxTree>();
					foreach (var mod in Editor.ParseCache)
					{
						if (string.IsNullOrEmpty(mod.ModuleName))
							continue;

						var parts = mod.ModuleName.Split('.');

						if (!nameStubs.ContainsKey(parts[0]) && !availModules.Contains(mod))
						{
							if (parts[0] == mod.ModuleName)
								availModules.Add(mod);
							else
								nameStubs.Add(parts[0], GetModulePath(mod.FileName, parts.Length, 1));
						}
					}

					foreach (var kv in nameStubs)
						CompletionDataGenerator.Add(kv.Key, PathOverride: kv.Value);

					foreach (var mod in availModules)
						CompletionDataGenerator.Add(mod.ModuleName, mod);
				}
				#endregion
			}

			// Add all found items to the referenced list
			if (listedItems != null)
				foreach (var i in listedItems)
				{
					if (CanItemBeShownGenerally(i as DNode)/* && dm.IsStatic*/)
						CompletionDataGenerator.Add(i);
				}
		}

		readonly List<string> alreadyAddedModuleNameParts = new List<string>();

		public void BuildCompletionData(
			ResolveResult rr,
			IBlockNode currentlyScopedBlock,
			bool isVariableInstance = false,
			ResolveResult resultParent = null)
		{
			#region MemberResult
			if (rr is MemberResult)
			{
				var mrr = rr as MemberResult;
				if (mrr.MemberBaseTypes != null)
					foreach (var i in mrr.MemberBaseTypes)
						BuildCompletionData(i, currentlyScopedBlock,
							(mrr.ResolvedMember is DVariable && (mrr.ResolvedMember as DVariable).IsAlias) ?
								isVariableInstance : true, rr); // True if we obviously have a variable handled here. Otherwise depends on the samely-named parameter..

				if (resultParent == null)
					StaticPropertyAddition.AddGenericProperties(rr, CompletionDataGenerator, mrr.ResolvedMember, DontAddInitProperty: mrr.MemberBaseTypes != null);
			}
			#endregion

			// A module path has been typed
			else if (!isVariableInstance && rr is ModuleResult)
				BuildModuleCompletionData(rr as ModuleResult, 0,  alreadyAddedModuleNameParts);

			#region A type was referenced directly
			else if (rr is TypeResult)
			{
				var tr = rr as TypeResult;
				var vis = ItemVisibility.All;

				if (!HaveSameAncestors(currentlyScopedBlock, tr.ResolvedTypeDefinition))
				{
					if (isVariableInstance ||
						(tr.TypeDeclarationBase is DTokenDeclaration &&
						(tr.TypeDeclarationBase as DTokenDeclaration).Token == DTokens.Super))
						vis = ItemVisibility.PublicOrStatic;
					else
						vis = ItemVisibility.PublicAndStatic;
				}

				BuildTypeCompletionData(tr, vis);
				if (resultParent == null)
					StaticPropertyAddition.AddGenericProperties(rr, CompletionDataGenerator, tr.ResolvedTypeDefinition);
			}
			#endregion

			#region Things like int. or char.
			else if (rr is StaticTypeResult)
			{
				var srr = rr as StaticTypeResult;
				if (resultParent == null)
					StaticPropertyAddition.AddGenericProperties(rr, CompletionDataGenerator, null, true);

				var type = srr.TypeDeclarationBase;

				// on things like immutable(char), pass by the surrounding attribute..
				while (type is MemberFunctionAttributeDecl)
					type = (type as MemberFunctionAttributeDecl).InnerType;

				if (type is ArrayDecl)
				{
					var ad = type as ArrayDecl;

					// Normal array
					if (ad.KeyType is DTokenDeclaration && DTokens.BasicTypes_Integral[(ad.KeyType as DTokenDeclaration).Token])
					{
						StaticPropertyAddition.AddArrayProperties(rr, CompletionDataGenerator, ad);
					}
					// Associative array
					else
					{
						StaticPropertyAddition.AddAssocArrayProperties(rr, CompletionDataGenerator, ad);
					}
				}
				// Direct pointer accessing - only generic props are available
				else if (type is PointerDecl)
				{
					// Do nothing
				}
				else
				{
					int TypeToken = srr.BaseTypeToken;

					if (TypeToken <= 0 && type is DTokenDeclaration)
						TypeToken = (type as DTokenDeclaration).Token;

					if (TypeToken > 0)
					{
						// Determine whether float by the var's base type
						bool isFloat = DTokens.BasicTypes_FloatingPoint[srr.BaseTypeToken];

						// Float implies integral props
						if (DTokens.BasicTypes_Integral[srr.BaseTypeToken] || isFloat)
							StaticPropertyAddition.AddIntegralTypeProperties(srr.BaseTypeToken, rr, CompletionDataGenerator, null, isFloat);

						if (isFloat)
							StaticPropertyAddition.AddFloatingTypeProperties(srr.BaseTypeToken, rr, CompletionDataGenerator, null);
					}
				}
			}
			#endregion

			#region "abcd" , (200), (0.123) //, [1,2,3,4], [1:"asdf", 2:"hey", 3:"yeah"]
			else if (rr is ExpressionResult)
			{
				var err = rr as ExpressionResult;
				var expr = err.Expression;

				// 'Skip' surrounding parentheses
				while (expr is SurroundingParenthesesExpression)
					expr = (expr as SurroundingParenthesesExpression).Expression;

				var idExpr = expr as IdentifierExpression;
				if (idExpr != null)
				{
					// Char literals, Integrals types & Floats
					if ((idExpr.Format&LiteralFormat.Scalar)==LiteralFormat.Scalar || idExpr.Format == LiteralFormat.CharLiteral)
					{
						StaticPropertyAddition.AddGenericProperties(rr, CompletionDataGenerator, null, true);
						bool isFloat = (idExpr.Format & LiteralFormat.FloatingPoint) == LiteralFormat.FloatingPoint;
						// Floats also imply integral properties
						StaticPropertyAddition.AddIntegralTypeProperties(DTokens.Int, rr, CompletionDataGenerator, null, isFloat);

						// Float-exclusive props
						if (isFloat)
							StaticPropertyAddition.AddFloatingTypeProperties(DTokens.Float, rr, CompletionDataGenerator);
					}
					// String literals
					else if (idExpr.Format == LiteralFormat.StringLiteral || idExpr.Format == LiteralFormat.VerbatimStringLiteral)
					{
						StaticPropertyAddition.AddGenericProperties(rr, CompletionDataGenerator, DontAddInitProperty: true);
						StaticPropertyAddition.AddArrayProperties(rr, CompletionDataGenerator, new ArrayDecl()
						{
							ValueType =
								new MemberFunctionAttributeDecl(DTokens.Immutable)
								{
									InnerType =
										new DTokenDeclaration(DTokens.Char)
								}
						});
					}
				}
				// Pointer conversions (e.g. (myInt*).sizeof)

			}
			#endregion
		}

		#region Static properties

		public class StaticPropertyAddition
		{
			public class StaticProperty
			{
				public readonly string Name;
				public readonly string Description;
				public readonly ITypeDeclaration OverrideType;

				public StaticProperty(string name, string desc, ITypeDeclaration overrideType = null)
				{ Name = name; Description = desc; OverrideType = overrideType; }
			}

			public static StaticProperty[] GenericProps = new[]{
				new StaticProperty("sizeof","Size of a type or variable in bytes",new IdentifierDeclaration("size_t")),
				new StaticProperty("alignof","Variable offset",new DTokenDeclaration(DTokens.Int)),
				new StaticProperty("mangleof","String representing the ‘mangled’ representation of the type",new IdentifierDeclaration("string")),
				new StaticProperty("stringof","String representing the source representation of the type",new IdentifierDeclaration("string")),
			};

			public static StaticProperty[] IntegralProps = new[] { 
				new StaticProperty("max","Maximum value"),
				new StaticProperty("min","Minimum value")
			};

			public static StaticProperty[] FloatingTypeProps = new[] { 
				new StaticProperty("infinity","Infinity value"),
				new StaticProperty("nan","Not-a-Number value"),
				new StaticProperty("dig","Number of decimal digits of precision",new DTokenDeclaration(DTokens.Int)),
				new StaticProperty("epsilon", "Smallest increment to the value 1"),
				new StaticProperty("mant_dig","Number of bits in mantissa",new DTokenDeclaration(DTokens.Int)),
				new StaticProperty("max_10_exp","Maximum int value such that 10^max_10_exp is representable",new DTokenDeclaration(DTokens.Int)),
				new StaticProperty("max_exp","Maximum int value such that 2^max_exp-1 is representable",new DTokenDeclaration(DTokens.Int)),
				new StaticProperty("min_10_exp","Minimum int value such that 10^max_10_exp is representable",new DTokenDeclaration(DTokens.Int)),
				new StaticProperty("min_exp","Minimum int value such that 2^max_exp-1 is representable",new DTokenDeclaration(DTokens.Int)),
				new StaticProperty("min_normal","Number of decimal digits of precision",new DTokenDeclaration(DTokens.Int)),
				new StaticProperty("re","Real part"),
				new StaticProperty("in","Imaginary part")
			};

			public static StaticProperty[] ClassTypeProps = new[]{
				new StaticProperty("classinfo","Information about the dynamic type of the class", new IdentifierDeclaration("TypeInfo_Class") { InnerDeclaration = new IdentifierDeclaration("object") })
			};

			public static StaticProperty[] ArrayProps = new[] { 
				new StaticProperty("init","Returns an array literal with each element of the literal being the .init property of the array element type. null on dynamic arrays."),
				new StaticProperty("length","Array length",new IdentifierDeclaration("size_t")),
				//new StaticProperty("ptr","Returns pointer to the array",new PointerDecl(){InnerDeclaration=new DTokenDeclaration(DTokens.Void)}),
				new StaticProperty("dup","Create a dynamic array of the same size and copy the contents of the array into it."),
				new StaticProperty("idup","D2.0 only! Creates immutable copy of the array"),
				new StaticProperty("reverse","Reverses in place the order of the elements in the array. Returns the array."),
				new StaticProperty("sort","Sorts in place the order of the elements in the array. Returns the array.")
			};

			// Associative Arrays' properties have to be inserted manually

			static void CreateArtificialProperties(StaticProperty[] Properties, ICompletionDataGenerator cdg, ITypeDeclaration DefaultPropType = null)
			{
				foreach (var prop in Properties)
				{
					var p = new DVariable()
					{
						Name = prop.Name,
						Description = prop.Description,
						Type = prop.OverrideType != null ? prop.OverrideType : DefaultPropType
					};

					cdg.Add(p);
				}
			}

			/// <summary>
			/// Adds init, sizeof, alignof, mangleof, stringof to the completion list
			/// </summary>
			public static void AddGenericProperties(ResolveResult rr, ICompletionDataGenerator cdg, INode relatedNode = null, bool DontAddInitProperty = false)
			{
				if (!DontAddInitProperty)
				{
					var prop_Init = new DVariable();

					if (relatedNode != null)
						prop_Init.AssignFrom(relatedNode);

					// Override the initializer variable's name and description
					prop_Init.Name = "init";
					prop_Init.Description = "A type's or variable's static initializer expression";

					cdg.Add(prop_Init);
				}

				CreateArtificialProperties(GenericProps, cdg);
			}

			/// <summary>
			/// Adds init, max, min to the completion list
			/// </summary>
			public static void AddIntegralTypeProperties(int TypeToken, ResolveResult rr, ICompletionDataGenerator cdg, INode relatedNode = null, bool DontAddInitProperty = false)
			{
				var intType = new DTokenDeclaration(TypeToken);

				if (!DontAddInitProperty)
				{
					var prop_Init = new DVariable() { Type = intType, Initializer = new IdentifierExpression(0, LiteralFormat.Scalar) };

					if (relatedNode != null)
						prop_Init.AssignFrom(relatedNode);

					// Override the initializer variable's name and description
					prop_Init.Name = "init";
					prop_Init.Description = "A type's or variable's static initializer expression";

					cdg.Add(prop_Init);
				}

				CreateArtificialProperties(IntegralProps, cdg, intType);
			}

			public static void AddFloatingTypeProperties(int TypeToken, ResolveResult rr, ICompletionDataGenerator cdg, INode relatedNode = null, bool DontAddInitProperty = false)
			{
				var intType = new DTokenDeclaration(TypeToken);

				if (!DontAddInitProperty)
				{
					var prop_Init = new DVariable() { Type = intType, Initializer = new PostfixExpression_Access() { PostfixForeExpression = new TokenExpression(TypeToken), TemplateOrIdentifier = new IdentifierDeclaration("nan") } };

					if (relatedNode != null)
						prop_Init.AssignFrom(relatedNode);

					// Override the initializer variable's name and description
					prop_Init.Name = "init";
					prop_Init.Description = "A type's or variable's static initializer expression";

					cdg.Add(prop_Init);
				}

				CreateArtificialProperties(FloatingTypeProps, cdg, intType);
			}

			public static void AddClassTypeProperties(ICompletionDataGenerator cdg, INode relatedNode = null)
			{
				CreateArtificialProperties(ClassTypeProps, cdg);
			}

			public static void AddArrayProperties(ResolveResult rr, ICompletionDataGenerator cdg, ArrayDecl ArrayDecl = null)
			{
				CreateArtificialProperties(ArrayProps, cdg, ArrayDecl);

				cdg.Add(new DVariable
				{
					Name = "ptr",
					Description = "Returns pointer to the array",
					Type = new PointerDecl(ArrayDecl == null ? new DTokenDeclaration(DTokens.Void) : ArrayDecl.ValueType)
				});
			}

			public static void AddAssocArrayProperties(ResolveResult rr, ICompletionDataGenerator cdg, ArrayDecl ad)
			{
				var ll = new List<INode>();

				ll.Add(new DVariable()
				{
					Name = "sizeof",
					Description = "Returns the size of the reference to the associative array; it is typically 8.",
					Type = new IdentifierDeclaration("size_t"),
					Initializer = new IdentifierExpression(8, LiteralFormat.Scalar)
				});

				/*ll.Add(new DVariable() { 
					Name="length",
					Description="Returns number of values in the associative array. Unlike for dynamic arrays, it is read-only.",
					Type=new IdentifierDeclaration("size_t")
				});*/

				if (ad != null)
				{
					ll.Add(new DVariable()
					{
						Name = "keys",
						Description = "Returns dynamic array, the elements of which are the keys in the associative array.",
						Type = new ArrayDecl() { ValueType = ad.KeyType }
					});

					ll.Add(new DVariable()
					{
						Name = "values",
						Description = "Returns dynamic array, the elements of which are the values in the associative array.",
						Type = new ArrayDecl() { ValueType = ad.ValueType }
					});

					ll.Add(new DVariable()
					{
						Name = "rehash",
						Description = "Reorganizes the associative array in place so that lookups are more efficient. rehash is effective when, for example, the program is done loading up a symbol table and now needs fast lookups in it. Returns a reference to the reorganized array.",
						Type = ad
					});

					ll.Add(new DVariable()
					{
						Name = "byKey",
						Description = "Returns a delegate suitable for use as an Aggregate to a ForeachStatement which will iterate over the keys of the associative array.",
						Type = new DelegateDeclaration() { ReturnType = new ArrayDecl() { ValueType = ad.KeyType } }
					});

					ll.Add(new DVariable()
					{
						Name = "byValue",
						Description = "Returns a delegate suitable for use as an Aggregate to a ForeachStatement which will iterate over the values of the associative array.",
						Type = new DelegateDeclaration() { ReturnType = new ArrayDecl() { ValueType = ad.ValueType } }
					});

					ll.Add(new DMethod()
					{
						Name = "get",
						Description = "Looks up key; if it exists returns corresponding value else evaluates and returns defaultValue.",
						Type = ad.ValueType,
						Parameters = new List<INode> {
						new DVariable(){
							Name="key",
							Type=ad.KeyType
						},
						new DVariable(){
							Name="defaultValue",
							Type=ad.ValueType,
							Attributes=new List<DAttribute>{ new DAttribute(DTokens.Lazy)}
						}
					}
					});
				}

				foreach (var prop in ll)
					cdg.Add(prop);
			}
		}

		#endregion

		public void BuildModuleCompletionData(ModuleResult tr, ItemVisibility visMod,
			List<string> alreadyAddedModuleNames)
		{
			if (!tr.IsOnlyModuleNamePartTyped())
				foreach (var i in tr.ResolvedModule)
				{
					var di = i as DNode;
					if (di == null)
					{
						if (i != null)
							CompletionDataGenerator.Add(i);
						continue;
					}

					if (di.IsPublic && CanItemBeShownGenerally(di))
						CompletionDataGenerator.Add(di);
				}
			else
			{
				var modNameParts = tr.ResolvedModule.ModuleName.Split('.');

				string packageDir = modNameParts[0];
				for (int i = 1; i <= tr.AlreadyTypedModuleNameParts; i++)
					packageDir += "." + modNameParts[i];

				if (tr.AlreadyTypedModuleNameParts < modNameParts.Length - 1)
				{
					// Don't add a package name that already has been added before.. so e.g. show only the first module of package "std.c."
					if (alreadyAddedModuleNames.Contains(packageDir))
						return;

					alreadyAddedModuleNames.Add(packageDir);

					CompletionDataGenerator.Add(modNameParts[tr.AlreadyTypedModuleNameParts], PathOverride:packageDir);
				}
				else
					CompletionDataGenerator.Add(modNameParts[modNameParts.Length - 1], tr.ResolvedModule);
			}
		}

		public void BuildTypeCompletionData(TypeResult tr, ItemVisibility visMod)
		{
			var n = tr.ResolvedTypeDefinition;
			if (n is DClassLike) // Add public static members of the class and including all base classes
			{
				var curlevel = tr;
				var tvisMod = visMod;
				while (curlevel != null)
				{
					foreach (var i in curlevel.ResolvedTypeDefinition)
					{
						var dn = i as DNode;

						if (i != null && dn == null)
						{
							CompletionDataGenerator.Add(i);
							continue;
						}

						// If "this." ,add all items, also those of superior classes
						// if "super." , add public items
						// if neither nor, add public static items

						bool IsPublicOrStatic = dn.IsPublic || dn.IsStatic;
						bool IsProtectedOrPublic = IsPublicOrStatic || dn.ContainsAttribute(DTokens.Protected);

						bool add = false;

						switch (tvisMod)
						{
							case ItemVisibility.All:
								add = true;
								break;
							case ItemVisibility.Protected:
								add = IsProtectedOrPublic;
								break;
							case ItemVisibility.PublicAndStatic:
								add = dn.IsPublic && dn.IsStatic;
								break;
							case ItemVisibility.PublicOrStatic:
								add = IsPublicOrStatic;
								break;
							case ItemVisibility.Static:
								add = dn.IsStatic;
								break;
						}

						if (add)
						{
							if (CanItemBeShownGenerally(dn))
								CompletionDataGenerator.Add(dn);

							// Add members of anonymous enums
							else if (dn is DEnum && dn.Name == "")
							{
								foreach (var k in dn as DEnum)
									CompletionDataGenerator.Add(k);
							}
						}
					}
					curlevel = curlevel.BaseClass != null ? curlevel.BaseClass[0] : null;

					// After having shown all items on the current node level,
					// allow showing public (static) and/or protected items in the more basic levels then
					if (tvisMod == ItemVisibility.All)
						tvisMod = ItemVisibility.Protected;
				}
			}
			else if (n is DEnum)
			{
				var de = n as DEnum;

				foreach (var i in de)
					if(i is DEnumValue)
						CompletionDataGenerator.Add(i);
			}
		}

		/// <summary>
		/// Returns C:\fx\a\b when PhysicalFileName was "C:\fx\a\b\c\Module.d" , ModuleName= "a.b.c.Module" and WantedDirectory= "a.b"
		/// 
		/// Used when formatting package names in BuildCompletionData();
		/// </summary>
		public static string GetModulePath(string PhysicalFileName, string ModuleName, string WantedDirectory)
		{
			return GetModulePath(PhysicalFileName, ModuleName.Split('.').Length, WantedDirectory.Split('.').Length);
		}

		public static string GetModulePath(string PhysicalFileName, int ModuleNamePartAmount, int WantedDirectoryNamePartAmount)
		{
			var ret = "";

			var physFileNameParts = PhysicalFileName.Split('\\');
			for (int i = 0; i < physFileNameParts.Length - ModuleNamePartAmount + WantedDirectoryNamePartAmount; i++)
				ret += physFileNameParts[i] + "\\";

			return ret.TrimEnd('\\');
		}

		public static bool IsInsightWindowTrigger(char key)
		{
			return key == '(' || key == ',';
		}

		#region Tooltip Creation

		public static AbstractTooltipContent[] BuildToolTip(IEditorData Editor)
		{
			if (DResolver.CommentSearching.IsInCommentAreaOrString(Editor.ModuleCode, Editor.CaretOffset))
				return null;

			try
			{
				IStatement curStmt = null;
				var rr = DResolver.ResolveType(Editor.ModuleCode, Editor.CaretOffset, Editor.CaretLocation,
					new ResolverContext
					{
						ScopedBlock = DResolver.SearchBlockAt(Editor.SyntaxTree, Editor.CaretLocation, out curStmt),
						ParseCache = Editor.ParseCache,
						ImportCache = Editor.ImportCache
					}, true, true);

				if (rr.Length < 1)
					return null;

				var l = new List<AbstractTooltipContent>(rr.Length);

				foreach (var res in rr)
				{
					var modRes = res as ModuleResult;
					var memRes = res as MemberResult;
					var typRes = res as TypeResult;

					// Only show one description for items sharing descriptions
					string description = "";

					if (modRes != null)
						description = modRes.ResolvedModule.Description;
					else if (memRes != null)
						description = memRes.ResolvedMember.Description;
					else if (typRes != null)
						description = typRes.ResolvedTypeDefinition.Description;

					l.Add(new AbstractTooltipContent{
						ResolveResult=res,
						Title=(res is ModuleResult ? (res as ModuleResult).ResolvedModule.FileName : res.ToString()),
						Description=description
					});
				}

				return l.ToArray();
			}
			catch { }
			return null;
		}

		#endregion
	}

	public interface ICompletionDataGenerator
	{
		/// <summary>
		/// Adds a token entry
		/// </summary>
		void Add(int Token);

		/// <summary>
		/// Adds a node to the completion data
		/// </summary>
		/// <param name="Node"></param>
		void Add(INode Node);

		/// <summary>
		/// Adds a module (name stub) to the completion data
		/// </summary>
		/// <param name="ModuleName"></param>
		/// <param name="AssocModule"></param>
		void Add(string ModuleName, IAbstractSyntaxTree Module = null, string PathOverride=null);
	}

	/// <summary>
	/// Encapsules tooltip content.
	/// If there are more than one tooltip contents, there are more than one resolve results
	/// </summary>
	public class AbstractTooltipContent
	{
		public ResolveResult ResolveResult;
		public string Title;
		public string Description;
	}
}
