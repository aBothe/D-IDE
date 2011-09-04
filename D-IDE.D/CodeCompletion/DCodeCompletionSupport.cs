using System;
using System.Collections.Generic;
using D_IDE.Core;
using ICSharpCode.AvalonEdit.CodeCompletion;
using D_Parser.Dom;
using D_Parser;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using D_Parser.Resolver;
using D_Parser.Parser;
using D_Parser.Dom.Statements;
using D_Parser.Dom.Expressions;

namespace D_IDE.D
{
	public class DCodeCompletionSupport
	{
		public static DCodeCompletionSupport Instance = new DCodeCompletionSupport();

		public DCodeCompletionSupport()
		{
			InitImages();
		}

		public bool IsIdentifierChar(char key)
		{
			return char.IsLetterOrDigit(key) || key == '_';
		}

		public bool CanShowCompletionWindow(DEditorDocument EditorDocument)
		{
			return !DCodeResolver.Commenting.IsInCommentAreaOrString(EditorDocument.Editor.Text, EditorDocument.Editor.CaretOffset);
		}

		public enum ItemVisibility
		{
			All,
			Static,
			PublicAndStatic,
			PublicOrStatic,
			Protected
		}

		public static bool CanItemBeShownGenerally(DNode dn)
		{
			if (dn==null || string.IsNullOrWhiteSpace(dn.Name))
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

		public void BuildCompletionData(DEditorDocument EditorDocument, IList<ICompletionData> l, string EnteredText)
		{
			var caretOffset = EditorDocument.Editor.CaretOffset;
			var caretLocation = new CodeLocation(EditorDocument.Editor.TextArea.Caret.Column,EditorDocument.Editor.TextArea.Caret.Line);

			IStatement curStmt = null;
			var curBlock = DCodeResolver.SearchBlockAt(EditorDocument.SyntaxTree,caretLocation,out curStmt);

			#region Parse the code between the last block opener and the caret

			var blockOpenerLocation = curBlock != null ? curBlock.BlockStartLocation : CodeLocation.Empty;
			var blockOpenerOffset = blockOpenerLocation.Line<=0? blockOpenerLocation.Column:
				EditorDocument.Editor.Document.GetOffset(blockOpenerLocation.Line, blockOpenerLocation.Column);

			if (caretOffset - blockOpenerOffset > 0)
			{
				var codeToParse = EditorDocument.Editor.Document.GetText(blockOpenerOffset, caretOffset - blockOpenerOffset);

				/*
				 * So, if we're inside of a method, we parse all its 'contents' (statements, expressions, declarations etc.)
				 * to achieve a fully updated insight.
				 */
				if (curBlock is DMethod)
				{
					var newStmt = DParser.ParseBlockStatement(codeToParse, blockOpenerLocation, curBlock);

					curStmt = newStmt.SearchStatementDeeply(caretLocation);

					// For adding method members to the return list anyway, make the method's statment block (either 'In', 'Out' or 'Body') scoped.
					if (curStmt == null)
						curStmt = newStmt;
				}
			}
			#endregion

			if (curBlock == null)
				return;

			IEnumerable<INode> listedItems = null;
			var codeCache = EnumAvailableModules(EditorDocument);

			// Usually shows variable members
			if (EnteredText == ".")
			{
				alreadyAddedModuleNameParts.Clear();

				var resolveResults = DResolver.ResolveType(EditorDocument.Editor.Document.Text, caretOffset-1, caretLocation, curBlock, codeCache);

				if (resolveResults == null) //TODO: Add after-space list creation when an unbound . (Dot) was entered which means to access the global scope
					return;
				/*
				 * Note: When having entered a module name stub only (e.g. "std." or "core.") it's needed to show all packages that belong to that root namespace
				 */

				foreach (var rr in resolveResults)
					BuildCompletionData(rr,curBlock, l);
			}

			// Enum all nodes that can be accessed in the current scope
			else if(string.IsNullOrEmpty(EnteredText) || IsIdentifierChar(EnteredText[0]))
			{
				listedItems = DCodeResolver.EnumAllAvailableMembers(curBlock, curStmt,caretLocation,codeCache);

				foreach (var kv in DTokens.Keywords)
					l.Add(new TokenCompletionData(kv.Key));

				// Add module name stubs of importable modules
				var nameStubs=new Dictionary<string,string>();
				foreach (var mod in codeCache)
				{
					if (string.IsNullOrEmpty(mod.ModuleName))
						continue;

					var parts = mod.ModuleName.Split('.');

					if (!nameStubs.ContainsKey(parts[0]))
						nameStubs.Add(parts[0], GetModulePath(mod.FileName, parts.Length, 1));
				}

				foreach (var kv in nameStubs)
					l.Add(new NamespaceCompletionData(kv.Key,kv.Value));
			}

			// Add all found items to the referenced list
			if(listedItems!=null)
				foreach (var i in listedItems)
				{
					if(CanItemBeShownGenerally(i as DNode)/* && dm.IsStatic*/)
						l.Add(new DCompletionData(i));
				}
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

		readonly List<string> alreadyAddedModuleNameParts = new List<string>(); 

		public void BuildCompletionData(ResolveResult rr, IBlockNode currentlyScopedBlock, IList<ICompletionData> l,bool isVariableInstance=false,ResolveResult resultParent=null)
		{
			#region MemberResult
			if (rr is MemberResult)
			{
				var mrr = rr as MemberResult;
				if (mrr.MemberBaseTypes != null)
					foreach (var i in mrr.MemberBaseTypes)
						BuildCompletionData(i,currentlyScopedBlock, l,
							(mrr.ResolvedMember is DVariable&&(mrr.ResolvedMember as DVariable).IsAlias)?
								isVariableInstance:true,rr); // True if we obviously have a variable handled here. Otherwise depends on the samely-named parameter..

				if(resultParent==null)
					StaticPropertyAddition.AddGenericProperties(rr, l, mrr.ResolvedMember);
			}
			#endregion

			// A module path has been typed
			else if (!isVariableInstance && rr is ModuleResult)
				BuildModuleCompletionData(rr as ModuleResult, 0, l, alreadyAddedModuleNameParts);

			#region A type was referenced directly
			else if (rr is TypeResult)
			{
				var tr = rr as TypeResult;
				var vis = ItemVisibility.All;

				if (!HaveSameAncestors(currentlyScopedBlock, tr.ResolvedTypeDefinition))
				{
					if (isVariableInstance)
						vis = ItemVisibility.PublicOrStatic;
					else
						vis = ItemVisibility.PublicAndStatic;
				}

				BuildTypeCompletionData(tr, vis, l);
				if (resultParent == null)
					StaticPropertyAddition.AddGenericProperties(rr, l, tr.ResolvedTypeDefinition);
			}
			#endregion

			#region Things like int. or char.
			else if (rr is StaticTypeResult)
			{
				var srr = rr as StaticTypeResult;
				if (resultParent == null)
					StaticPropertyAddition.AddGenericProperties(rr, l, null,true);

				if (srr.Type is ArrayDecl)
				{
					var ad = srr.Type as ArrayDecl;

					// Normal array
					if (ad.KeyType is DTokenDeclaration && DTokens.BasicTypes_Integral[(ad.KeyType as DTokenDeclaration).Token])
					{
						StaticPropertyAddition.AddArrayProperties(rr, l, ad);
					}
					// Associative array
					else
					{

					}
				}
				else
				{
					// Determine whether float by the var's base type
					bool isFloat = DTokens.BasicTypes_FloatingPoint[srr.BaseTypeToken];

					// Float implies integral props
					if (DTokens.BasicTypes_Integral[srr.BaseTypeToken] || isFloat)
						StaticPropertyAddition.AddIntegralTypeProperties(srr.BaseTypeToken, rr, l, null, isFloat);

					if (isFloat)
						StaticPropertyAddition.AddFloatingTypeProperties(srr.BaseTypeToken, rr, l, null);
				}
			}
			#endregion

			#region "abcd" , (200), (0.123), [1,2,3,4], [1:"asdf", 2:"hey", 3:"yeah"]
			else if (rr is ExpressionResult)
			{
				var err = rr as ExpressionResult;
				var expr = err.Expression;

				// 'Skip' surrounding parentheses
				while (expr is SurroundingParenthesesExpression)
					expr = (expr as SurroundingParenthesesExpression).Expression;

				var idExpr = expr as IdentifierExpression;
				if (idExpr!=null)
				{
					// Char literals, Integrals types & Floats
					if (idExpr.LiteralFormat.HasFlag(LiteralFormat.Scalar) || idExpr.LiteralFormat==LiteralFormat.CharLiteral)
					{
						StaticPropertyAddition.AddGenericProperties(rr, l, null, true);
						bool isFloat=idExpr.LiteralFormat.HasFlag(LiteralFormat.FloatingPoint);
						// Floats also imply integral properties
						StaticPropertyAddition.AddIntegralTypeProperties(DTokens.Int, rr, l, null, isFloat);

						// Float-exclusive props
						if (isFloat)
							StaticPropertyAddition.AddFloatingTypeProperties(DTokens.Float, rr, l);
					}
					// String literals
					else if (idExpr.LiteralFormat == LiteralFormat.StringLiteral || idExpr.LiteralFormat == LiteralFormat.VerbatimStringLiteral)
					{
						StaticPropertyAddition.AddGenericProperties(rr, l, DontAddInitProperty: true);
						StaticPropertyAddition.AddArrayProperties(rr, l,new IdentifierDeclaration("string"));
					}
				}
				// Normal array literals
				else if (expr is ArrayLiteralExpression)
				{
					var arr = expr as ArrayLiteralExpression;

					StaticPropertyAddition.AddGenericProperties(rr, l, null, true);
					StaticPropertyAddition.AddArrayProperties(rr, l);
				}
				// Associative array literals
				else if (expr is AssocArrayExpression)
				{
					var arr = expr as AssocArrayExpression;
					
					StaticPropertyAddition.AddGenericProperties(rr, l, null, true);
					// TODO: AddAssocArrayProperties
				}
				// Array element accessors (e.g. myArray[0] )
				else if (expr is PostfixExpression_Index)
				{

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

			public static StaticProperty[] GenericProps=new[]{
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
				new StaticProperty("ptr","Returns pointer to the array",new PointerDecl(){InnerDeclaration=new DTokenDeclaration(DTokens.Void)}),
				new StaticProperty("dup","Create a dynamic array of the same size and copy the contents of the array into it."),
				new StaticProperty("idup","D2.0 only! Creates immutable copy of the array"),
				new StaticProperty("reverse","Reverses in place the order of the elements in the array. Returns the array."),
				new StaticProperty("sort","Sorts in place the order of the elements in the array. Returns the array.")
			};

			// Associative Arrays' properties have to be inserted manually

			static void CreateArtificialProperties(StaticProperty[] Properties, IList<ICompletionData> l, ITypeDeclaration DefaultPropType = null)
			{
				foreach (var prop in Properties)
				{
					var p = new DVariable() {
						Name=prop.Name,
						Description=prop.Description,
						Type=prop.OverrideType!=null?prop.OverrideType:DefaultPropType
					};

					l.Add(new DCompletionData(p));
				}
			}

			/// <summary>
			/// Adds init, sizeof, alignof, mangleof, stringof to the completion list
			/// </summary>
			public static void AddGenericProperties(ResolveResult rr, IList<ICompletionData> l, INode relatedNode = null, bool DontAddInitProperty=false)
			{
				if (!DontAddInitProperty)
				{
					var prop_Init = new DVariable();

					if (relatedNode!=null)
						prop_Init.AssignFrom(relatedNode);

					// Override the initializer variable's name and description
					prop_Init.Name = "init";
					prop_Init.Description = "A type's or variable's static initializer expression";

					l.Add(new DCompletionData(prop_Init));
				}

				CreateArtificialProperties(GenericProps, l);
			}

			/// <summary>
			/// Adds init, max, min to the completion list
			/// </summary>
			public static void AddIntegralTypeProperties(int TypeToken, ResolveResult rr, IList<ICompletionData> l, INode relatedNode = null, bool DontAddInitProperty = false)
			{
				var intType = new DTokenDeclaration(TypeToken);

				if (!DontAddInitProperty)
				{
					var prop_Init = new DVariable() { Type=intType, Initializer=new IdentifierExpression(0,LiteralFormat.Scalar)};

					if (relatedNode != null)
						prop_Init.AssignFrom(relatedNode);

					// Override the initializer variable's name and description
					prop_Init.Name = "init";
					prop_Init.Description = "A type's or variable's static initializer expression";

					l.Add(new DCompletionData(prop_Init));
				}

				CreateArtificialProperties(IntegralProps, l, intType);
			}

			public static void AddFloatingTypeProperties(int TypeToken, ResolveResult rr, IList<ICompletionData> l, INode relatedNode = null, bool DontAddInitProperty = false)
			{
				var intType = new DTokenDeclaration(TypeToken);

				if (!DontAddInitProperty)
				{
					var prop_Init = new DVariable() { Type = intType, Initializer = new PostfixExpression_Access() { PostfixForeExpression=new TokenExpression(TypeToken), TemplateOrIdentifier=new IdentifierDeclaration("nan")} };

					if (relatedNode != null)
						prop_Init.AssignFrom(relatedNode);

					// Override the initializer variable's name and description
					prop_Init.Name = "init";
					prop_Init.Description = "A type's or variable's static initializer expression";

					l.Add(new DCompletionData(prop_Init));
				}

				CreateArtificialProperties(FloatingTypeProps, l, intType);
			}

			public static void AddClassTypeProperties(IList<ICompletionData> l, INode relatedNode = null)
			{
				CreateArtificialProperties(ClassTypeProps, l);
			}

			public static void AddArrayProperties(ResolveResult rr, IList<ICompletionData> l, ITypeDeclaration ArrayDecl=null)
			{
				CreateArtificialProperties(ArrayProps, l,ArrayDecl);
			}
		}

		#endregion

		public static void BuildModuleCompletionData(ModuleResult tr, ItemVisibility visMod, IList<ICompletionData> l,
			List<string> alreadyAddedModuleNames)
		{
			if (!tr.IsOnlyModuleNamePartTyped())
				foreach (var i in tr.ResolvedModule)
				{
					var di = i as DNode;
					if (di == null)
					{
						l.Add(new DCompletionData(i));
						continue;
					}

					if (di.IsPublic && CanItemBeShownGenerally(di))
						l.Add(new DCompletionData(i));
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

					l.Add(new NamespaceCompletionData(modNameParts[tr.AlreadyTypedModuleNameParts], packageDir));
				}
				else 
					l.Add(new NamespaceCompletionData(modNameParts[modNameParts.Length - 1], tr.ResolvedModule));
			}
		}

		public static void BuildTypeCompletionData(TypeResult tr, ItemVisibility visMod, IList<ICompletionData> l)
		{
			var n = tr.ResolvedTypeDefinition;
			if (n is DClassLike) // Add public static members of the class and including all base classes
			{
				var curlevel=tr;
				var tvisMod = visMod;
				while (curlevel != null)
				{
					foreach (var i in curlevel.ResolvedTypeDefinition)
					{
						var dn = i as DNode;

						if (dn == null)
							l.Add( new DCompletionData(i));

						// If "this." and if watching the current inheritance level only , add all items
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

						if (add && CanItemBeShownGenerally(dn))
							l.Add( new DCompletionData(dn));
					}
					curlevel = curlevel.BaseClass!=null?curlevel.BaseClass[0]:null;

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
				{
					var dn = i as DEnumValue;
					if (dn != null)
						l.Add(new DCompletionData(i));
				}
			}
		}

		/// <summary>
		/// Returns C:\fx\a\b when PhysicalFileName was "C:\fx\a\b\c\Module.d" , ModuleName= "a.b.c.Module" and WantedDirectory= "a.b"
		/// 
		/// Used when formatting package names in BuildCompletionData();
		/// </summary>
		public static string GetModulePath(string PhysicalFileName, string ModuleName, string WantedDirectory)
		{
			return GetModulePath(PhysicalFileName,ModuleName.Split('.').Length,WantedDirectory.Split('.').Length);
		}

		public static string GetModulePath(string PhysicalFileName, int ModuleNamePartAmount, int WantedDirectoryNamePartAmount)
		{
			var ret = "";

			var physFileNameParts = PhysicalFileName.Split('\\');
			for (int i = 0; i < physFileNameParts.Length - ModuleNamePartAmount + WantedDirectoryNamePartAmount; i++)
				ret += physFileNameParts[i] + "\\";

			return ret.TrimEnd('\\');
		}

		public void BuildToolTip(DEditorDocument EditorDocument, ToolTipRequestArgs ToolTipRequest)
		{
			int offset = EditorDocument.Editor.Document.GetOffset(ToolTipRequest.Line, ToolTipRequest.Column);

			if (!ToolTipRequest.InDocument||
					DCodeResolver.Commenting.IsInCommentAreaOrString(EditorDocument.Editor.Text,offset)) 
				return;

			try
			{
				var caretLoc = new CodeLocation(ToolTipRequest.Column, ToolTipRequest.Line);
				IStatement curStmt = null;
				var rr = DResolver.ResolveType(EditorDocument.Editor.Text, offset, caretLoc,
					DCodeResolver.SearchBlockAt(EditorDocument.SyntaxTree,caretLoc,out curStmt),DCodeResolver.ResolveImports(EditorDocument.SyntaxTree,EnumAvailableModules(EditorDocument)),true,true);
				
				string tt = "";

				//TODO: Build well-formatted tool tip string / Do a better tool tip layout
				foreach (var res in rr)
				{
					tt += (res is ModuleResult?(res as ModuleResult).ResolvedModule.FileName: res.ToString()) + "\r\n";
				}

				tt = tt.Trim();
				if(!string.IsNullOrEmpty(tt))
					ToolTipRequest.ToolTipContent = tt;
			}catch{}
		}

		public bool IsInsightWindowTrigger(char key)
		{
			return key == '(' || key==',';
		}

		#region Module enumeration helper
		public static IEnumerable<IAbstractSyntaxTree> EnumAvailableModules(DEditorDocument Editor)
		{
			return EnumAvailableModules(Editor.HasProject ? Editor.Project as DProject : null);
		}

		public static IEnumerable<IAbstractSyntaxTree> EnumAvailableModules(DProject Project)
		{
			var ret =new List<IAbstractSyntaxTree>();

			if (Project != null)
			{
				// Add all parsed global modules that belong to the project's compiler configuration
				foreach (var astColl in Project.CompilerConfiguration.ASTCache)
					ret.AddRange(astColl);

				// Add all modules that exist in the current solution.
				if (Project.Solution != null)
				{
					foreach (var prj in Project.Solution)
						if (prj is DProject)
							ret.AddRange((prj as DProject).ParsedModules);
				}
				else // If no solution present, add scanned modules of our current project only
					ret.AddRange(Project.ParsedModules);
			}
			else // If no project present, only add the modules of the default compiler configuration
				foreach (var astColl in DSettings.Instance.DMDConfig().ASTCache)
					ret.AddRange(astColl);

			return ret;
		}
		#endregion

		#region Image helper
		Dictionary<string, ImageSource> images = new Dictionary<string, ImageSource>();

		BitmapSource ConvertWFToWPFBitmap(System.Drawing.Bitmap bmp)
		{
			return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
		}

		void InitImages()
		{
			try
			{
				var bi = new BitmapImage();
				
				images["keyword"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_Keyword);
				images["namespace"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_NameSpace);

				images["class"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_Class);
				images["class_internal"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_InternalClass);
				images["class_private"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_PrivateClass);
				images["class_protected"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_ProtectedClass);

				images["struct"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_Struct);
				images["struct_internal"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_InternalStruct);
				images["struct_private"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_PrivateStruct);
				images["struct_protected"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_ProtectedStruct);
				
				images["interface"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_Interface);
				images["interface_internal"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_InternalInterface);
				images["interface_private"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_PrivateInterface);
				images["interface_protected"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_ProtectedInterface);

				images["enum"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_Enum);
				images["enum_internal"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_InternalEnum);
				images["enum_private"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_PrivateEnum);
				images["enum_protected"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_ProtectedEnum);

				images["method"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_Method);
				images["method_internal"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_InternalMethod);
				images["method_private"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_PrivateMethod);
				images["method_protected"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_ProtectedMethod);

				images["parameter"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_Parameter);
				images["local"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_Local);

				images["field"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_Field);
				images["field_internal"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_InternalField);
				images["field_private"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_PrivateField);
				images["field_protected"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_ProtectedField);

				images["property"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_Property);
				images["property_internal"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_InternalProperty);
				images["property_private"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_PrivateProperty);
				images["property_protected"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_ProtectedProperty);

				images["delegate"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_Delegate);
				images["delegate_internal"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_InternalDelegate);
				images["delegate_private"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_PrivateDelegate);
				images["delegate_protected"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_ProtectedDelegate);

				images["literal"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_Literal);
			}
			catch (Exception ex)
			{
				ErrorLogger.Log(ex);
			}
		}

		public ImageSource GetNodeImage(string key)
		{
			if (images.ContainsKey(key))
				return images[key];
			return null;
		}
		#endregion
	}

	public class TokenCompletionData : ICompletionData
	{
		public int Token { get; set; }

		public TokenCompletionData(int Token)
		{
			try
			{
				this.Token = Token;
				Text = DTokens.GetTokenString(Token);
				Description = DTokens.GetDescription(Token);
				Image = DCodeCompletionSupport.Instance.GetNodeImage("keyword");
			}
			catch (Exception ex)
			{
				ErrorLogger.Log(ex);
			}
		}

		public void Complete(ICSharpCode.AvalonEdit.Editing.TextArea textArea, ICSharpCode.AvalonEdit.Document.ISegment completionSegment, EventArgs insertionRequestEventArgs)
		{
			textArea.Document.Replace(completionSegment, Text);
		}

		public object Content
		{
			get { return Text; }
		}

		public object Description
		{
			get;
			private set;
		}

		public System.Windows.Media.ImageSource Image
		{
			get;
			private set;
		}

		public double Priority
		{
			get { return 1; }
		}

		public string Text
		{
			get;
			private set;
		}
	}

	public class NamespaceCompletionData : ICompletionData
	{
		public string ModuleName{get;set;}
		public IAbstractSyntaxTree AssociatedModule { get; set; }
		public string _desc;

		public NamespaceCompletionData(string ModuleName, IAbstractSyntaxTree AssocModule)
		{
			this.ModuleName=ModuleName;
			AssociatedModule = AssocModule;
		}

		public NamespaceCompletionData(string ModuleName, string Description)
		{
			this.ModuleName = ModuleName;
			_desc = Description;
		}

		public void Complete(ICSharpCode.AvalonEdit.Editing.TextArea textArea, ICSharpCode.AvalonEdit.Document.ISegment completionSegment, EventArgs insertionRequestEventArgs)
		{
			textArea.Document.Replace(completionSegment, Text);
		}

		public object Content
		{
			get { return Text; }
		}

		public object Description
		{
			get { return !string.IsNullOrEmpty(_desc)?_desc: (AssociatedModule!=null?AssociatedModule.FileName:null); }
		}

		public System.Windows.Media.ImageSource Image
		{
			get { return DCodeCompletionSupport.Instance.GetNodeImage("namespace"); }
		}

		public double Priority
		{
			get { return 1; }
		}

		public string Text
		{
			get { return ModuleName; }
		}
	}

	public class DCompletionData : ICompletionData
	{
		public DCompletionData(INode n)
		{
			Node = n;
		}

		public string NodeString
		{
			get
			{
				try
				{
					return Node.ToString();
				}
				catch (Exception ex)
				{
					ErrorLogger.Log(ex, ErrorType.Error, ErrorOrigin.Parser);
					return "";
				}
			}
		}

		/// <summary>
		/// Returns node string without attributes and without node path
		/// </summary>
		public string PureNodeString
		{
			get
			{
				try
				{
					if (Node is DNode)
						return (Node as DNode).ToString(false, false);
					return Node.ToString();
				}
				catch (Exception ex)
				{
					ErrorLogger.Log(ex,ErrorType.Error,ErrorOrigin.Parser);
					return "";
				}
			}
		}

		public INode Node { get; protected set; }

		public void Complete(ICSharpCode.AvalonEdit.Editing.TextArea textArea, ICSharpCode.AvalonEdit.Document.ISegment completionSegment, EventArgs insertionRequestEventArgs)
		{
			textArea.Document.Replace(completionSegment, Text);
		}

		public object Content
		{
			get { return Text; }
		}

		public object Description
		{
			// If an empty description was given, do not show an empty decription tool tip
			get {
				try
				{
					return NodeString;
				}
				catch (Exception ex) { ErrorLogger.Log(ex, ErrorType.Error, ErrorOrigin.Parser); }
				return null;
			}
			//TODO: Make a more smarter tool tip
		}

		public System.Windows.Media.ImageSource Image
		{
			get {
				try
				{
					var n = Node as DNode;

					if (n == null)
						return null;

					if (n is DClassLike)
					{
						switch ((n as DClassLike).ClassType)
						{
							case DTokens.Template:
							case DTokens.Class:
								if (n.ContainsAttribute(DTokens.Package))
									return DCodeCompletionSupport.Instance.GetNodeImage("class_internal");
								else if (n.ContainsAttribute(DTokens.Protected))
									return DCodeCompletionSupport.Instance.GetNodeImage("class_protected");
								else if (n.ContainsAttribute(DTokens.Private))
									return DCodeCompletionSupport.Instance.GetNodeImage("class_private");
								return DCodeCompletionSupport.Instance.GetNodeImage("class");

							case DTokens.Union:
							case DTokens.Struct:
								if (n.ContainsAttribute(DTokens.Package))
									return DCodeCompletionSupport.Instance.GetNodeImage("struct_internal");
								else if (n.ContainsAttribute(DTokens.Protected))
									return DCodeCompletionSupport.Instance.GetNodeImage("struct_protected");
								else if (n.ContainsAttribute(DTokens.Private))
									return DCodeCompletionSupport.Instance.GetNodeImage("struct_private");
								return DCodeCompletionSupport.Instance.GetNodeImage("struct");

							case DTokens.Interface:
								if (n.ContainsAttribute(DTokens.Package))
									return DCodeCompletionSupport.Instance.GetNodeImage("interface_internal");
								else if (n.ContainsAttribute(DTokens.Protected))
									return DCodeCompletionSupport.Instance.GetNodeImage("interface_protected");
								else if (n.ContainsAttribute(DTokens.Private))
									return DCodeCompletionSupport.Instance.GetNodeImage("interface_private");
								return DCodeCompletionSupport.Instance.GetNodeImage("interface");
						}
					}
					else if (n is DEnum)
					{
						if (n.ContainsAttribute(DTokens.Package))
							return DCodeCompletionSupport.Instance.GetNodeImage("enum_internal");
						else if (n.ContainsAttribute(DTokens.Protected))
							return DCodeCompletionSupport.Instance.GetNodeImage("enum_protected");
						else if (n.ContainsAttribute(DTokens.Private))
							return DCodeCompletionSupport.Instance.GetNodeImage("enum_private");
						return DCodeCompletionSupport.Instance.GetNodeImage("enum");
					}
					else if (n is DMethod)
					{
						//TODO: Getter or setter functions should be declared as a >single< property only
						if (n.ContainsAttribute(DTokens.PropertyAttribute))
						{
							if (n.ContainsAttribute(DTokens.Package))
								return DCodeCompletionSupport.Instance.GetNodeImage("property_internal");
							else if (n.ContainsAttribute(DTokens.Protected))
								return DCodeCompletionSupport.Instance.GetNodeImage("property_protected");
							else if (n.ContainsAttribute(DTokens.Private))
								return DCodeCompletionSupport.Instance.GetNodeImage("property_private");
							return DCodeCompletionSupport.Instance.GetNodeImage("property");
						}

						if (n.ContainsAttribute(DTokens.Package))
							return DCodeCompletionSupport.Instance.GetNodeImage("method_internal");
						else if (n.ContainsAttribute(DTokens.Protected))
							return DCodeCompletionSupport.Instance.GetNodeImage("method_protected");
						else if (n.ContainsAttribute(DTokens.Private))
							return DCodeCompletionSupport.Instance.GetNodeImage("method_private");
						return DCodeCompletionSupport.Instance.GetNodeImage("method");
					}
					else if (n is DEnumValue)
						return DCodeCompletionSupport.Instance.GetNodeImage("literal");
					else if (n is DVariable)
					{
						if (n.Type is DelegateDeclaration)
						{
							if (n.ContainsAttribute(DTokens.Package))
								return DCodeCompletionSupport.Instance.GetNodeImage("delegate_internal");
							else if (n.ContainsAttribute(DTokens.Protected))
								return DCodeCompletionSupport.Instance.GetNodeImage("delegate_protected");
							else if (n.ContainsAttribute(DTokens.Private))
								return DCodeCompletionSupport.Instance.GetNodeImage("delegate_private");
							return DCodeCompletionSupport.Instance.GetNodeImage("delegate");
						}

						if (n.ContainsAttribute(DTokens.Const))
							return DCodeCompletionSupport.Instance.GetNodeImage("literal");

						var realParent = n.Parent as DNode;

						if (realParent == null)
							return DCodeCompletionSupport.Instance.GetNodeImage("local");

						if (realParent is DClassLike)
						{
							if (n.ContainsAttribute(DTokens.Package))
								return DCodeCompletionSupport.Instance.GetNodeImage("field_internal");
							else if (n.ContainsAttribute(DTokens.Protected))
								return DCodeCompletionSupport.Instance.GetNodeImage("field_protected");
							else if (n.ContainsAttribute(DTokens.Private))
								return DCodeCompletionSupport.Instance.GetNodeImage("field_private");
							return DCodeCompletionSupport.Instance.GetNodeImage("field");
						}

						if (realParent is DMethod)
						{
							if ((realParent as DMethod).Parameters.Contains(n))
								return DCodeCompletionSupport.Instance.GetNodeImage("parameter");
							return DCodeCompletionSupport.Instance.GetNodeImage("local");
						}

						if (realParent.ContainsTemplateParameter(n.Name))
							return DCodeCompletionSupport.Instance.GetNodeImage("parameter");
					}
				}
				catch (Exception ex) { ErrorLogger.Log(ex,ErrorType.Error,ErrorOrigin.Parser); }
				return null;
			}
		}

		public double Priority
		{
			get { return 1; }
		}

		public string Text
		{
			get { return Node.Name; }
			protected set { }
		}
	}
}
