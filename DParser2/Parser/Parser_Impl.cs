﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using D_Parser.Dom;
using D_Parser.Dom.Statements;
using D_Parser.Dom.Expressions;

namespace D_Parser.Parser
{
	/// <summary>
	/// Parser for D Code
	/// </summary>
	public partial class DParser
	{
		#region Modules
		// http://www.digitalmars.com/d/2.0/module.html

		/// <summary>
		/// Module entry point
		/// </summary>
		DModule Root()
		{
			Step();

			var module = new DModule();
			module.StartLocation = la.Location;
			doc = module;

			// Only one module declaration possible!
			if (laKind == (Module))
			{
				module.Description = GetComments();
				module.ModuleName = ModuleDeclaration().ToString();
				module.Description += CheckForPostSemicolonComment();
			}

			// Now only declarations or other statements are allowed!
			while (!IsEOF)
			{
				DeclDef(module);
			}
			module.Imports = imports.ToArray();
			module.EndLocation = la.Location;
			return module;
		}

		#region Comments
		string PreviousComment = "";

		string GetComments()
		{
			string ret = "";

			foreach(var c in Lexer.Comments)
				ret += c.CommentText+ ' ';

			Lexer.Comments.Clear();

			ret = ret.Trim();

			if (String.IsNullOrEmpty(ret)) 
				return ""; 

			// Overwrite only if comment is not 'ditto'
			if (ret.ToLowerInvariant() != "ditto")
				PreviousComment = ret;

			return PreviousComment;
		}

		/// <summary>
		/// Returns the pre- and post-declaration comment
		/// </summary>
		/// <returns></returns>
		string CheckForPostSemicolonComment()
		{
			int ExpectedLine = t.line;

			string ret = "";

			int i=0;
			foreach (var c in Lexer.Comments)
			{
				// Ignore ddoc comments made e.g. in int a /** ignored comment */, b,c; 
				// , whereas this method is called as t is the final semicolon
				if (c.EndPosition <= t.Location)
				{
					i++;
					continue;
				}
				else if(c.StartPosition.Line > ExpectedLine)
					break;

				ret += c.CommentText+' ';
				i++;
			}
			Lexer.Comments.RemoveRange(0, i);

			if (string.IsNullOrEmpty(ret))
				return "";

			ret = ret.Trim();
			
			// Add post-declaration string if comment text is 'ditto'
			if (ret.ToLowerInvariant() == "ditto")
				return PreviousComment;

			// Append post-semicolon comment string to previously read comments
			PreviousComment += " " + ret;
			return ' '+ret;
		}

		void ClearCommentCache()
		{
			Lexer.Comments.Clear();
		}
		#endregion

		void DeclDef(IBlockNode module)
		{
			//AttributeSpecifier
			if (IsAttributeSpecifier())
				AttributeSpecifier();

			//ImportDeclaration
			else if (laKind == Import)
				ImportDeclaration();

			//Constructor
			else if (laKind == (This))
				module.Add(Constructor(module is DClassLike ? (module as DClassLike).ClassType == DTokens.Struct : false));

			//Destructor
			else if (laKind == (Tilde) && Lexer.CurrentPeekToken.Kind == (This))
				module.Add(Destructor());

			//Invariant
			else if (laKind == (Invariant))
				module.Add(_Invariant());

			//UnitTest
			else if (laKind == (Unittest))
			{
				Step();
				var dbs = new DMethod(DMethod.MethodType.Unittest);
				dbs.StartLocation = t.Location;
				FunctionBody(dbs);
				dbs.EndLocation = t.EndLocation;
				module.Add(dbs);
			}

			//ConditionalDeclaration
			else if (laKind == (Version) || laKind == (Debug) || laKind == (If))
			{
				Step();
				var n = t.ToString();

				if (t.Kind == (If))
				{
					if (DAttribute.ContainsAttribute(DeclarationAttributes, Static))
					{
						//HACK: Assume that there's only our 'static' attribute applied to the 'if'-statement
						DeclarationAttributes.Clear();
					}
					else
						SynErr(Static, "Conditional declarations must be static");

					if (Expect(OpenParenthesis))
					{
						var condition = AssignExpression();
						Expect(CloseParenthesis);
					}
				}
				else if (laKind == (Assign))
				{
					Step();
					Step();
					Expect(Semicolon);
				}
				else if (t.Kind == (Version))
				{
					Expect(OpenParenthesis);
					n += "(";
					Step();
					n += t.ToString();
					Expect(CloseParenthesis);
					n += ")";
				}
				else if (t.Kind == (Debug) && laKind == (OpenParenthesis))
				{
					Expect(OpenParenthesis);
					n += "(";
					Step();
					n += t.ToString();
					Expect(CloseParenthesis);
					n += ")";
				}

				if (laKind == (Colon))
					Step();
			}

			//TODO
			else if (laKind == (Else))
			{
				Step();
			}

			//StaticAssert
			else if (laKind == (Assert))
			{
				Step();

				if (DAttribute.ContainsAttribute(DeclarationAttributes, Static))
				{
					//HACK: Assume that there's only our 'static' attribute applied to the 'if'-statement
					DeclarationAttributes.Clear();
				}
				else 
					SynErr(Static, "Static assert statements must be explicitly marked as static");

				if (Expect(OpenParenthesis))
				{
					AssignExpression();
					if (laKind == (Comma))
					{
						Step();
						AssignExpression();
					}
					Expect(CloseParenthesis);
				}
					Expect(Semicolon);
			}

			//TemplateMixin
			else if (laKind == Mixin && Peek(1).Kind == Identifier)
				TemplateMixin();

			//MixinDeclaration
			else if (laKind == (Mixin))
				MixinDeclaration();

			//;
			else if (laKind == (Semicolon))
				Step();

			// {
			else if (laKind == (OpenCurlyBrace))
			{
				// Due to having a new attribute scope, we'll have use a new attribute stack here
				var AttrBackup = BlockAttributes;
				BlockAttributes = new Stack<DAttribute>();

				while (DeclarationAttributes.Count > 0)
					BlockAttributes.Push(DeclarationAttributes.Pop());

				ClassBody(module, true);

				// After the block ended, restore the previous block attributes
				BlockAttributes = AttrBackup;
			}

			// Class Allocators
			// Note: Although occuring in global scope, parse it anyway but declare it as semantic nonsense;)
			else if (laKind == (New))
			{
				Step();

				var dm = new DMethod(DMethod.MethodType.Allocator);
				dm.Name = "new";
				ApplyAttributes(dm);

				dm.Parameters = Parameters(dm);
				FunctionBody(dm);
				module.Add(dm);
			}

			// Class Deallocators
			else if (laKind == Delete)
			{
				Step();

				var dm = new DMethod(DMethod.MethodType.Deallocator);
				dm.Name = "delete";
				ApplyAttributes(dm);

				dm.Parameters = Parameters(dm);
				FunctionBody(dm);
				module.Add(dm);
			}

			// else:
			else
				module.AddRange(Declaration());
		}

		string ModuleDeclaration()
		{
			Expect(Module);
			var ret = ModuleFullyQualifiedName();
			Expect(Semicolon);
			return ret;
		}

		string ModuleFullyQualifiedName()
		{
			Expect(Identifier);
			
			var td=t.Value;

			while (laKind == Dot)
			{
				Step();
				Expect(Identifier);

				td += "."+t.Value;
			}

			return td;
		}

		void ImportDeclaration()
		{
			bool IsPublic = DAttribute.ContainsAttribute(BlockAttributes, Public);

			if (DAttribute.ContainsAttribute(DeclarationAttributes, Public))
			{
				DAttribute.CleanupAccessorAttributes(DeclarationAttributes);
				IsPublic = true;
			}

			bool IsStatic = DAttribute.ContainsAttribute(BlockAttributes, Static);

			if (DAttribute.ContainsAttribute(DeclarationAttributes, Static))
			{
				DAttribute.RemoveFromStack(DeclarationAttributes, Static);
				IsStatic = true;
			}

			DeclarationAttributes.Clear();
			CheckForDocComments();
			Expect(Import);

			var imp = _Import();

			imp.IsPublic = IsPublic;
			imp.IsStatic = IsStatic;

			imports.Add(imp);

			// ImportBindings
			if (laKind == (Colon))
			{
				Step();
				ImportBind(imp);
				while (laKind == (Comma))
				{
					Step();
					ImportBind(imp);
				}
			}
			else
				while (laKind == (Comma))
				{
					Step();
					imp = _Import();

					imp.IsPublic = IsPublic;
					imp.IsStatic = IsStatic;

					imports.Add(imp);

					if (laKind == (Colon))
					{
						Step();
						ImportBind(imp);
						while (laKind == (Comma))
						{
							Step();
							ImportBind(imp);
						}
					}
				}

			Expect(Semicolon);
		}

		ImportStatement _Import()
		{
			var import = new ImportStatement();
			// ModuleAliasIdentifier
			if (Lexer.CurrentPeekToken.Kind == (Assign))
			{
				Expect(Identifier);
				import.ModuleAlias = t.Value;
				Step();
			}

			import.ModuleIdentifier = ModuleFullyQualifiedName();

			return import;
		}

		void ImportBind(ImportStatement imp)
		{
			if(imp.ExclusivelyImportedSymbols==null)
				imp.ExclusivelyImportedSymbols = new Dictionary<string, string>();

			Expect(Identifier);
			string imbBind = t.Value;
			string imbBindDef = "";

			if (laKind == (Assign))
			{
				Step();
				Expect(Identifier);
				imbBindDef = t.Value;
			}

			imp.ExclusivelyImportedSymbols.Add(imbBind, imbBindDef);
		}


		INode MixinDeclaration()
		{
			Expect(Mixin);

			if (LA(OpenParenthesis))
			{
				Step();
				AssignExpression();
				Expect(CloseParenthesis);
			}
			else
			{
				// TemplateMixinDeclaration
				if (LA(Template))
					return TemplateDeclaration();

				// TemplateMixin
				else if (LA(Identifier))
				{
					if (PK(Not))
						TemplateInstance();
					else
						Expect(Identifier);

					// MixinIdentifier
					if (LA(Identifier))
						Step();
				}
			}
			Expect(Semicolon);
			return null;
		}
		#endregion

		#region Declarations
		// http://www.digitalmars.com/d/2.0/declaration.html

		bool IsDeclaration()
		{
			return laKind == (Alias) || IsStorageClass || IsBasicType();
		}

		bool CheckForStorageClasses()
		{
			bool ret = false;
			while (IsStorageClass || laKind==PropertyAttribute)
			{
				if (IsAttributeSpecifier()) // extern, align
					AttributeSpecifier();
				else
				{
					Step();
					// Always allow more than only one property attribute
					if (t.Kind==PropertyAttribute || !DAttribute.ContainsAttribute(DeclarationAttributes.ToArray(), t.Kind))
						PushAttribute(new DAttribute(t.Kind,t.Value), false);
				}
				ret = true;
			}
			return ret;
		}

		bool CheckForModifiers()
		{
			bool ret = false;
			while (Modifiers[laKind] || laKind==PropertyAttribute)
			{
				if (IsAttributeSpecifier()) // extern, align
					AttributeSpecifier();
				else
				{
					Step();
					if (!DAttribute.ContainsAttribute(DeclarationAttributes.ToArray(), t.Kind))
						PushAttribute(new DAttribute(t.Kind), false);
				}
				ret = true;
			}
			return ret;
		}

		INode[] Declaration()
		{
			// Skip ref token
			if (laKind == (Ref))
			{
				PushAttribute(new DAttribute(Ref), false);
				Step();
			}

			// Enum possible storage class attributes
			bool HasStorageClassModifiers = CheckForStorageClasses();

			if (laKind == (Alias) || laKind == Typedef)
			{
				Step();
				// _t is just a synthetic node which holds possible following attributes
				var _t = new DMethod();
				ApplyAttributes(_t);

				// AliasThis
				if (laKind == Identifier && PK(This))
				{
					Step();
					var dv = new DVariable();
					dv.Description = GetComments();
					dv.StartLocation = Lexer.LastToken.Location;
					dv.IsAlias = true;
					dv.Name = "this";
					dv.Type = new IdentifierDeclaration(t.Value);
					dv.EndLocation = t.EndLocation;
					Step();
					Expect(Semicolon);
					dv.Description += CheckForPostSemicolonComment();
					return new[]{dv};
				}

				var decls=Decl(HasStorageClassModifiers);

				if(decls!=null && decls.Length>0)
					foreach (var n in decls)
					{
						if (n is DNode)
							(n as DNode).Attributes.AddRange(_t.Attributes);

						if (n is DVariable)
							(n as DVariable).IsAlias = true;
					}

				return decls;
			}
			else if (laKind == (Struct) || laKind == (Union))
				return new[]{ AggregateDeclaration()};
			else if (laKind == (Enum))
				return EnumDeclaration();
			else if (laKind == (Class))
				return new[]{ ClassDeclaration()};
			else if (laKind == (Template) || (laKind==Mixin && Peek(1).Kind==Template))
				return new[]{ TemplateDeclaration()};
			else if (laKind == (Interface))
				return new[]{ InterfaceDeclaration()};
			else if (IsBasicType() || laKind==Ref)
				return Decl(HasStorageClassModifiers);
			else
			{
				SynErr(laKind,"Declaration expected, not "+GetTokenString(laKind));
				Step();
			}
			return null;
		}

		INode[] Decl(bool HasStorageClassModifiers)
		{
			var startLocation = la.Location;
			var initialComment = GetComments();
			ITypeDeclaration ttd = null;

			CheckForStorageClasses();
			// Skip ref token
			if (laKind == (Ref))
			{
				if (!DAttribute.ContainsAttribute(DeclarationAttributes, Ref))
					PushAttribute(new DAttribute(Ref), false);
				Step();
			}

			// Autodeclaration
			var StorageClass = DTokens.ContainsStorageClass(DeclarationAttributes.ToArray());

			// If there's no explicit type declaration, leave our node's type empty!
			if ((StorageClass.Token != DAttribute.Empty.Token && laKind == (Identifier) && DeclarationAttributes.Count > 0 &&
				(PK(Assign) || PK(OpenParenthesis)))) // public auto var=0; // const foo(...) {} 
			{
			}
			else
				ttd = BasicType();

			// Declarators
			var firstNode = Declarator(false);
			firstNode.Description = initialComment;
			firstNode.StartLocation = startLocation;

			if (firstNode.Type == null)
				firstNode.Type = ttd;
			else
				firstNode.Type.InnerMost = ttd;

			ApplyAttributes(firstNode as DNode);

			// Check for declaration constraints
			if (laKind == (If))
				Constraint();

			// BasicType Declarators ;
			if (laKind==Assign || laKind==Comma || laKind==Semicolon)
			{
				// DeclaratorInitializer
				if (laKind == (Assign))
					(firstNode as DVariable).Initializer = Initializer();
				firstNode.EndLocation = t.EndLocation;
				var ret = new List<INode>();
				ret.Add(firstNode);

				// DeclaratorIdentifierList
				while (laKind == (Comma))
				{
					Step();
					Expect(Identifier);

					var otherNode = new DVariable();

					/// Note: In DDoc, all declarations that are made at once (e.g. int a,b,c;) get the same pre-declaration-description!
					otherNode.Description = initialComment;

					otherNode.AssignFrom(firstNode);
					otherNode.StartLocation = t.Location;
					otherNode.Name = t.Value;

					if (laKind == (Assign))
						otherNode.Initializer = Initializer();

					otherNode.EndLocation = t.EndLocation;
					ret.Add(otherNode);
				}

				Expect(Semicolon);

				// Note: In DDoc, only the really last declaration will get the post semicolon comment appended
				if (ret.Count > 0)
					ret[ret.Count - 1].Description += CheckForPostSemicolonComment();

				return ret.ToArray();
			}

			// BasicType Declarator FunctionBody
			else if (firstNode is DMethod && (laKind == In || laKind == Out || laKind == Body || laKind == OpenCurlyBrace))
			{
				firstNode.Description += CheckForPostSemicolonComment();

				FunctionBody(firstNode as DMethod);

				firstNode.Description += CheckForPostSemicolonComment();

				return new[]{ firstNode};
			}
			else
				SynErr(OpenCurlyBrace, "; or function body expected after declaration stub.");

			return null;
		}

		bool IsBasicType()
		{
			return BasicTypes[laKind] || laKind == (Typeof) || MemberFunctionAttribute[laKind] || (laKind == (Dot) && Lexer.CurrentPeekToken.Kind == (Identifier)) || laKind == (Identifier);
		}

		/// <summary>
		/// Used if the parser is unsure if there's a type or an expression - then, instead of throwing exceptions, the Type()-Methods will simply return null;
		/// </summary>
		public bool AllowWeakTypeParsing = false;

		ITypeDeclaration BasicType()
		{
			ITypeDeclaration td = null;
			if (BasicTypes[laKind])
			{
				Step();
				return new DTokenDeclaration(t.Kind);
			}

			if (MemberFunctionAttribute[laKind])
			{
				Step();
				var md = new MemberFunctionAttributeDecl(t.Kind);
				bool p = false;

				if (laKind == OpenParenthesis)
				{
					Step();
					p = true;
				}

				// e.g. cast(const)
				if (laKind != CloseParenthesis)
					md.InnerType = Type();

				if (p)
					Expect(CloseParenthesis);
				return md;
			}

			//TODO
			if (laKind == Ref)
				Step();

			if (laKind == (Typeof))
			{
				td = TypeOf();
				if (laKind != (Dot)) return td;
			}

			if (laKind == (Dot))
				Step();

			if (AllowWeakTypeParsing&& laKind != Identifier)
				return null;

			if (td == null)
				td = IdentifierList();
			else
				td.InnerMost = IdentifierList();

			return td;
		}

		bool IsBasicType2()
		{
			return laKind == (Times) || laKind == (OpenSquareBracket) || laKind == (Delegate) || laKind == (Function);
		}

		ITypeDeclaration BasicType2()
		{
			// *
			if (laKind == (Times))
			{
				Step();
				return new PointerDecl();
			}

			// [ ... ]
			else if (laKind == (OpenSquareBracket))
			{
				Step();
				// [ ]
				if (laKind == (CloseSquareBracket)) { Step();
				return new ArrayDecl(); 
				}

				ITypeDeclaration cd = null;

				// [ Type ]
				if (!IsAssignExpression())
				{
					var la_backup = la;
					bool weaktype = AllowWeakTypeParsing;
					AllowWeakTypeParsing = true;
					var keyType = Type();
					AllowWeakTypeParsing = weaktype;

					if (keyType != null && laKind == CloseSquareBracket)
						cd = new ArrayDecl() { KeyType = keyType };
					else
						la = la_backup;
				}
				
				if(cd==null)
				{
					var fromExpression = AssignExpression();

					// [ AssignExpression .. AssignExpression ]
					if (laKind == DoubleDot)
					{
						Step();
						cd = new ArrayDecl() {KeyExpression= new PostfixExpression_Slice() { FromExpression=fromExpression, ToExpression=AssignExpression()} };
					}
					else
						cd = new ArrayDecl() { KeyExpression=fromExpression };
				}

				if (AllowWeakTypeParsing && laKind != CloseSquareBracket)
					return null;

				Expect(CloseSquareBracket);
				return cd;
			}

			// delegate | function
			else if (laKind == (Delegate) || laKind == (Function))
			{
				Step();
				ITypeDeclaration td = null;
				var dd = new DelegateDeclaration();
				dd.IsFunction = t.Kind == Function;

				dd.Parameters = Parameters(null);
				td = dd;
				//TODO: add attributes to declaration
				while (FunctionAttribute[laKind])
				{
					Step();
					td = new DTokenDeclaration(t.Kind, td);
				}
				return td;
			}
			else
				SynErr(Identifier);
			return null;
		}

		/// <summary>
		/// Parses a type declarator
		/// </summary>
		/// <returns>A dummy node that contains the return type, the variable name and possible parameters of a function declaration</returns>
		DNode Declarator(bool IsParam)
		{
			DNode ret = new DVariable();
			ITypeDeclaration ttd = null;

			while (IsBasicType2())
			{
				if (ret.Type == null) ret.Type = BasicType2();
				else { ttd = BasicType2(); if(ttd!=null)ttd.InnerDeclaration = ret.Type; ret.Type = ttd; }
			}
			/*
			 * Add some syntax possibilities here
			 * like
			 * int (x);
			 * int(*foo);
			 */
			#region This way of declaring function pointers is deprecated
			if (laKind == (OpenParenthesis))
			{
				Step();
				//SynErr(OpenParenthesis,"C-style function pointers are deprecated. Use the function() syntax instead."); // Only deprecated in D2
				var cd = new DelegateDeclaration() as ITypeDeclaration;
				ret.Type = cd;
				var deleg = cd as DelegateDeclaration;

				/* 
				 * Parse all basictype2's that are following the initial '('
				 */
				while (IsBasicType2())
				{
					ttd = BasicType2();

					if (deleg.ReturnType == null) 
						deleg.ReturnType = ttd;
					else
					{
						if(ttd!=null)
							ttd.InnerDeclaration = deleg.ReturnType;
						deleg.ReturnType = ttd;
					}
				}

				/*
				 * Here can be an identifier with some optional DeclaratorSuffixes
				 */
				if (laKind != (CloseParenthesis))
				{
					if (IsParam && laKind != (Identifier))
					{
						/* If this Declarator is a parameter of a function, don't expect anything here
						 * exept a '*' that means that here's an anonymous function pointer
						 */
						if (t.Kind != (Times))
							SynErr(Times);
					}
					else
					{
						Expect(Identifier);
						ret.Name = t.Value;

						/*
						 * Just here suffixes can follow!
						 */
						if (laKind != (CloseParenthesis))
						{
							ITemplateParameter[] _unused2 = null;
							List<INode> _unused = null;
							List<DAttribute> _unused3 = new List<DAttribute>();
							ttd = DeclaratorSuffixes(out _unused2, out _unused, _unused3);

							if (ttd != null)
							{
								ttd.InnerDeclaration = cd;
								cd = ttd;
							}
						}
					}
				}
				ret.Type = cd;
				Expect(CloseParenthesis);
			}
			#endregion
			else
			{
				if (IsParam && laKind != (Identifier))
					return ret;

				if(Expect(Identifier))
					ret.Name = t.Value;
			}

			if (IsDeclaratorSuffix || MemberFunctionAttribute[laKind])
			{
				// DeclaratorSuffixes
				List<INode> _Parameters;
				List<DAttribute> attrs;
				ttd = DeclaratorSuffixes(out (ret as DNode).TemplateParameters, out _Parameters, ret.Attributes);
				if (ttd != null)
				{
					ttd.InnerDeclaration = ret.Type;
					ret.Type = ttd;
				}

				if (_Parameters != null)
				{
					var dm = new DMethod();
					dm.AssignFrom(ret);
					dm.Parameters = _Parameters;
					foreach (var pp in dm.Parameters)
						pp.Parent = dm;
					ret = dm;
				}
			}

			return ret;
		}

		bool IsDeclaratorSuffix
		{
			get { return laKind == (OpenSquareBracket) || laKind == (OpenParenthesis); }
		}

		/// <summary>
		/// Note:
		/// http://www.digitalmars.com/d/2.0/declaration.html#DeclaratorSuffix
		/// The definition of a sequence of declarator suffixes is buggy here! Theoretically template parameters can be declared without a surrounding ( and )!
		/// Also, more than one parameter sequences are possible!
		/// 
		/// TemplateParameterList[opt] Parameters MemberFunctionAttributes[opt]
		/// </summary>
		ITypeDeclaration DeclaratorSuffixes(out ITemplateParameter[] TemplateParameters, out List<INode> _Parameters, List<DAttribute> _Attributes)
		{
			ITypeDeclaration td = null;
			TemplateParameters = null;
			_Parameters = null;

			while (MemberFunctionAttribute[laKind])
			{
				_Attributes.Add(new DAttribute(laKind, la.Value));
				Step();
			}

			while (laKind == (OpenSquareBracket))
			{
				Step();
				var ad = new ArrayDecl();
				ad.InnerDeclaration = td;
				if (laKind != (CloseSquareBracket))
				{
					ITypeDeclaration keyType=null;
					if (!IsAssignExpression())
					{
						AllowWeakTypeParsing = true;
						keyType= ad.KeyType = Type();
						AllowWeakTypeParsing = false;
					}
					if (keyType==null)
						ad.KeyExpression = AssignExpression();
				}
				Expect(CloseSquareBracket);
				td = ad;
			}

			if (laKind == (OpenParenthesis))
			{
				if (IsTemplateParameterList())
				{
					TemplateParameters = TemplateParameterList();
				}
				_Parameters = Parameters(null);

				//TODO: MemberFunctionAttributes -- add them to the declaration
				while (StorageClass[laKind] || laKind==PropertyAttribute)
				{
					Step();
				}
			}

			while (MemberFunctionAttribute[laKind])
			{
				_Attributes.Add(new DAttribute(laKind,la.Value));
				Step();
			}
			return td;
		}

		public ITypeDeclaration IdentifierList()
		{
			ITypeDeclaration td = null;

			bool init = true;
			while (init|| laKind == Dot)
			{
				if(!init)
					Step();
				init = false;
				
				ITypeDeclaration ttd = null;

				if (IsTemplateInstance)
					ttd = TemplateInstance();
				else if (Expect(Identifier))
					ttd = new IdentifierDeclaration(t.Value);

				if (ttd != null)
					ttd.InnerDeclaration = td;
				td = ttd;
			}
			return td;
		}

		bool IsStorageClass
		{
			get
			{
				return laKind == (Abstract) ||
			laKind == (Auto) ||
			((MemberFunctionAttribute[laKind]) && Lexer.CurrentPeekToken.Kind != (OpenParenthesis)) ||
			laKind == (Deprecated) ||
			laKind == (Extern) ||
			laKind == (Final) ||
			laKind == (Override) ||
			laKind == (Scope) ||
			laKind == (Static) ||
			laKind == (Synchronized) ||
			laKind == __gshared ||
			laKind == __thread;
			}
		}

		public ITypeDeclaration Type()
		{
			var td = BasicType();

			if (IsDeclarator2())
			{
				var ttd = Declarator2();
				if (ttd != null)
					ttd.InnerDeclaration = td;
					td = ttd;
				
			}

			return td;
		}

		bool IsDeclarator2()
		{
			return IsBasicType2() || laKind == (OpenParenthesis);
		}

		/// <summary>
		/// http://www.digitalmars.com/d/2.0/declaration.html#Declarator2
		/// The next bug: Following the definition strictly, this function would end up in an endless loop of requesting another Declarator2
		/// 
		/// So here I think that a Declarator2 only consists of a couple of BasicType2's and some DeclaratorSuffixes
		/// </summary>
		/// <returns></returns>
		ITypeDeclaration Declarator2()
		{
			ITypeDeclaration td = null;
			if (laKind == (OpenParenthesis))
			{
				Step();

				td = Declarator2();
				
				if (AllowWeakTypeParsing && (td == null||(t.Kind==OpenParenthesis && laKind==CloseParenthesis) /* -- means if an argumentless function call has been made, return null because this would be an expression */|| laKind!=CloseParenthesis))
					return null;

				Expect(CloseParenthesis);

				// DeclaratorSuffixes
				if (laKind == (OpenSquareBracket))
				{
					List<INode> _unused = null;
					ITemplateParameter[] _unused2 = null;
					List<DAttribute> _unused3 = new List<DAttribute>();
					DeclaratorSuffixes(out _unused2, out _unused,_unused3);
				}
				return td;
			}

			while (IsBasicType2())
			{
				var ttd = BasicType2();
				if (AllowWeakTypeParsing && ttd == null)
					return null;

				if(ttd!=null)
					ttd.InnerDeclaration = td;
				td = ttd;
			}

			return td;
		}

		/// <summary>
		/// Parse parameters
		/// </summary>
		List<INode> Parameters(DMethod Parent)
		{
			var ret = new List<INode>();
			Expect(OpenParenthesis);

			// Empty parameter list
			if (laKind == (CloseParenthesis))
			{
				Step();
				return ret;
			}

			if (laKind != TripleDot)
				ret.Add(Parameter());

			while (laKind == (Comma))
			{
				Step();
				if (laKind == TripleDot)
					break;
				var p = Parameter();
				p.Parent = Parent;
				ret.Add(p);
			}

			/*
			 * There can be only one '...' in every parameter list
			 */
			if (laKind == TripleDot)
			{
				// If it had not a comma, add a VarArgDecl to the last parameter
				bool HadComma = t.Kind == (Comma);

				Step();

				if (!HadComma && ret.Count > 0 && ret is IBlockNode)
				{
					((ret as IBlockNode)[(ret as IBlockNode).Count - 1] as IBlockNode).Type = new VarArgDecl((ret as IBlockNode)[(ret as IBlockNode).Count - 1].Type);
				}
				else
				{
					var dv = new DVariable();
					dv.Type = new VarArgDecl();
					dv.Parent = Parent;
					ret.Add(dv);
				}
			}

			Expect(CloseParenthesis);
			return ret;
		}

		private INode Parameter()
		{
			var attr = new List<DAttribute>();
			var startLocation = la.Location;

			while (ParamModifiers[laKind] || (MemberFunctionAttribute[laKind] && !PK(OpenParenthesis)))
			{
				Step();
				attr.Add(new DAttribute(t.Kind));
			}

			if (laKind == Auto && Lexer.CurrentPeekToken.Kind == Ref) // functional.d:595 // auto ref F fp
			{
				Step();
				Step();
				attr.Add(new DAttribute(Auto));
				attr.Add(new DAttribute(Ref));
			}

			var td = BasicType();

			var ret = Declarator(true);
			ret.StartLocation = startLocation;
			if (attr.Count > 0) (ret as DNode).Attributes.AddRange(attr);
			if (ret.Type == null)
				ret.Type = td;
			else
				ret.Type.InnerDeclaration = td;

			// DefaultInitializerExpression
			if (laKind == (Assign))
			{
				Step();

				var defInit = AssignExpression();

				if (ret is DVariable)
					(ret as DVariable).Initializer = defInit;
			}
			ret.EndLocation = t.EndLocation;

			return ret;
		}

		private IExpression Initializer()
		{
			Expect(Assign);

			// VoidInitializer
			if (laKind == (Void))
			{
				Step();
				return new VoidInitializer() { Location=t.Location,EndLocation=t.EndLocation};
			}

			return NonVoidInitializer();
		}

		IExpression NonVoidInitializer()
		{
			#region ArrayInitializer
			if (laKind == OpenSquareBracket)
			{
				Step();

				// ArrayMemberInitializations
				var ae = new ArrayInitializer() { Location=t.Location};
				var inits=new List<ArrayMemberInitializer>();

				bool IsInit = true;
				while (IsInit || laKind == (Comma))
				{
					if (!IsInit) Step();
					IsInit = false;

					// Allow empty post-comma expression IF the following token finishes the initializer expression
					// int[] a=[1,2,3,4,];
					if (laKind == CloseSquareBracket)
						break;

					// ArrayMemberInitialization
					var ami = new ArrayMemberInitializer()
					{
						Left = NonVoidInitializer()
					};
					bool HasBeenAssExpr = !(t.Kind == (CloseSquareBracket) || t.Kind == (CloseCurlyBrace));

					// AssignExpression : NonVoidInitializer
					if (HasBeenAssExpr && laKind == (Colon))
					{
						Step();
						ami.Specialization = NonVoidInitializer();
					}
					inits.Add(ami);
				}

				ae.ArrayMemberInitializations = inits.ToArray();

				Expect(CloseSquareBracket);
				ae.EndLocation = t.EndLocation;

				// auto i=[1,2,3].idup; // in this case, this entire thing is meant to be an AssignExpression but not a dedicated initializer..
				if (laKind == Dot)
				{
					Step();

					var ae2 = new PostfixExpression_Access();
					ae2.PostfixForeExpression = ae;
					ae2.TemplateOrIdentifier = Type(); //TODO: Is it really a type!?
					ae2.EndLocation = t.EndLocation;

					return ae2;
				}

				return ae;
			}
			#endregion

			// StructInitializer
			if(laKind==OpenCurlyBrace)
			{
				// StructMemberInitializations
				var ae = new StructInitializer() { Location=la.Location};
				var inits=new List<StructMemberInitializer>();

				bool IsInit = true;
				while (IsInit || laKind == (Comma))
				{
					Step();
					IsInit = false;

					// Allow empty post-comma expression IF the following token finishes the initializer expression
					// int[] a=[1,2,3,4,];
					if (laKind == CloseCurlyBrace)
						break;

					// Identifier : NonVoidInitializer
					var sinit = new StructMemberInitializer();
					if (laKind == Identifier && Lexer.CurrentPeekToken.Kind == Colon)
					{
						Step();
						sinit.MemberName = t.Value;
						Step();
					}
					
					sinit.Specialization = NonVoidInitializer();

					inits.Add(sinit);
				}

				ae.StructMemberInitializers = inits.ToArray();

				Expect(CloseCurlyBrace);
				ae.EndLocation = t.EndLocation;
				return ae;
			}

			else
				return AssignExpression();
		}

		ITypeDeclaration TypeOf()
		{
			Expect(Typeof);
			Expect(OpenParenthesis);
			var md = new MemberFunctionAttributeDecl(Typeof);
			if (laKind == (Return))
			{
				Step();
				md.InnerType = new DTokenDeclaration(Return);
			}
			else
				md.InnerType = new DExpressionDecl(Expression());
			Expect(CloseParenthesis);
			return md;
		}

		#endregion

		#region Attributes

		DMethod _Invariant()
		{
			var inv = new DMethod();
			inv.Attributes.Add(new DAttribute(Invariant));

			Expect(Invariant);
			inv.StartLocation = t.Location;
			if (laKind == OpenParenthesis)
			{
				Step();
				Expect(CloseParenthesis);
			}
			inv.Body=BlockStatement();
			inv.EndLocation = t.EndLocation;
			return inv;
		}

		PragmaStatement _Pragma()
		{
			Expect(Pragma);
			var s = new PragmaStatement() { StartLocation=t.Location};
			Expect(OpenParenthesis);
			Expect(Identifier);
			s.PragmaIdentifier = t.Value;

			var l = new List<IExpression>();
			while (laKind == (Comma))
			{
				Step();
				l.Add(AssignExpression());
			}
			if(l.Count>0)
				s.ArgumentList = l.ToArray();
			Expect(CloseParenthesis);
			s.EndLocation = t.EndLocation;

			return s;
		}

		bool IsAttributeSpecifier()
		{
			return (laKind == (Extern) || laKind == (Export) || laKind == (Align) || laKind == Pragma || laKind == (Deprecated) || IsProtectionAttribute()
				|| laKind == (Static) || laKind == (Final) || laKind == (Override) || laKind == (Abstract) || laKind == (Scope) || laKind == (__gshared)
				|| ((laKind == (Auto) || MemberFunctionAttribute[laKind]) && (Lexer.CurrentPeekToken.Kind != (OpenParenthesis) && Lexer.CurrentPeekToken.Kind != (Identifier)))
				|| laKind==PropertyAttribute);
		}

		bool IsProtectionAttribute()
		{
			return laKind == (Public) || laKind == (Private) || laKind == (Protected) || laKind == (Extern) || laKind == (Package);
		}

		private void AttributeSpecifier()
		{
			var attr = new DAttribute(laKind,la.Value);
			if (laKind == (Extern) && Lexer.CurrentPeekToken.Kind == (OpenParenthesis))
			{
				Step(); // Skip extern
				Step(); // Skip (
				while (!IsEOF && laKind != (CloseParenthesis))
					Step();
				Expect(CloseParenthesis);
			}
			else if (laKind == (Align) && Lexer.CurrentPeekToken.Kind == (OpenParenthesis))
			{
				Step();
				Step();
				Expect(Literal);
				Expect(CloseParenthesis);
			}
			else if (laKind == (Pragma))
				_Pragma();
			else
				Step();

			if (laKind == (Colon))
			{
				PushAttribute(attr, true);
				Step();
			}

			else if (laKind != Semicolon)
				PushAttribute(attr,false);
		}
		#endregion

		#region Expressions
		public IExpression Expression()
		{
			// AssignExpression
			var ass = AssignExpression();
			if (laKind != (Comma))
				return ass;

			/*
			 * The following is a leftover of C syntax and proably cause some errors when parsing arguments etc.
			 */
			// AssignExpression , Expression
			var ae = new Expression();
			ae.Add(ass);
			while (laKind == (Comma))
			{
				Step();
				ae.Add(AssignExpression());
			}
			return ae;
		}

		/// <summary>
		/// This function has a very high importance because here we decide whether it's a declaration or assignExpression!
		/// </summary>
		public bool IsAssignExpression()
		{
			if (IsBasicType())
			{
				bool HadPointerDeclaration = false;

				// uint[]** MyArray;
				if (!BasicTypes[laKind])
				{
					// Skip initial dot
					if (laKind == Dot)
						Step();

					if (Peek(1).Kind != Identifier)
					{
						if (laKind == Identifier)
						{
							// Skip initial identifier list
							bool init = true;
							//bool HadTemplateInst = false;
							while (init || Lexer.CurrentPeekToken.Kind == (Dot))
							{
								//HadTemplateInst = false;
								if (Lexer.CurrentPeekToken.Kind == Dot) Peek();
								init = false;

								if (Lexer.CurrentPeekToken.Kind == Identifier)
									Peek();

								if (Lexer.CurrentPeekToken.Kind == (Not))
								{
									//HadTemplateInst = true;
									Peek();
									if (Lexer.CurrentPeekToken.Kind != (Is) && Lexer.CurrentPeekToken.Kind != (In))
									{
										if (Lexer.CurrentPeekToken.Kind == (OpenParenthesis))
											OverPeekBrackets(OpenParenthesis);
										else Peek();
									}
								}
							}
							//if (!init && !HadTemplateInst) Peek();
						}
						else if (laKind == (Typeof) || MemberFunctionAttribute[laKind])
						{
							if (Lexer.CurrentPeekToken.Kind == (OpenParenthesis))
								OverPeekBrackets(OpenParenthesis);
						}
					}
				}

				if (Lexer.CurrentPeekToken == null)
					Peek();

				// Skip basictype2's
				while (Lexer.CurrentPeekToken.Kind == (Times) || Lexer.CurrentPeekToken.Kind == (OpenSquareBracket))
				{
					if (PK(Times))
						HadPointerDeclaration = true;

					if (Lexer.CurrentPeekToken.Kind == (OpenSquareBracket))
						OverPeekBrackets(OpenSquareBracket);
					else Peek();

					if (HadPointerDeclaration && PK(Literal)) // char[a.member*8] abc; // conv.d:3278
					{
						Peek(1);
						return true;
					}
				}

				// And now, after having skipped the basictype and possible trailing basictype2's,
				// we check for an identifier or delegate declaration to ensure that there's a declaration and not an expression
				// Addition: If a times token ('*') follows an identifier list, we can assume that we have a declaration and NOT an expression!
				// Example: *a=b is an expression; a*=b is not possible - instead something like A* a should be taken...
				if (HadPointerDeclaration || Lexer.CurrentPeekToken.Kind == (Identifier) || Lexer.CurrentPeekToken.Kind == (Delegate) || Lexer.CurrentPeekToken.Kind == (Function))
				{
					Peek(1);
					return false;
				}
			}
			else if (IsStorageClass)
				return false;

			Peek(1);
			return true;
		}

		public IExpression AssignExpression()
		{
			var left = ConditionalExpression();
			if (!AssignOps[laKind])
				return left;

			Step();
			var ate = new AssignExpression(t.Kind);
			ate.LeftOperand = left;
			ate.RightOperand = AssignExpression();
			return ate;
		}

		IExpression ConditionalExpression()
		{
			var trigger = OrOrExpression();
			if (laKind != (Question))
				return trigger;

			Expect(Question);
			var se = new ConditionalExpression() { OrOrExpression = trigger };
			se.TrueCaseExpression = AssignExpression();
			Expect(Colon);
			se.FalseCaseExpression = ConditionalExpression();
			return se;
		}

		IExpression OrOrExpression()
		{
			var left = AndAndExpression();
			if (laKind != LogicalOr)
				return left;

			Step();
			var ae = new OrOrExpression();
			ae.LeftOperand = left;
			ae.RightOperand = OrOrExpression();
			return ae;
		}

		IExpression AndAndExpression()
		{
			// Note: Due to making it easier to parse, we ignore the OrExpression-CmpExpression rule
			// -> So we only assume that there's a OrExpression

			var left = OrExpression();
			if (laKind != LogicalAnd)
				return left;

			Step();
			var ae = new AndAndExpression();
			ae.LeftOperand = left;
			ae.RightOperand = AndAndExpression();
			return ae;
		}

		IExpression OrExpression()
		{
			var left = XorExpression();
			if (laKind != BitwiseOr)
				return left;

			Step();
			var ae = new OrExpression();
			ae.LeftOperand = left;
			ae.RightOperand = OrExpression();
			return ae;
		}

		IExpression XorExpression()
		{
			var left = AndExpression();
			if (laKind != Xor)
				return left;

			Step();
			var ae = new XorExpression();
			ae.LeftOperand = left;
			ae.RightOperand = XorExpression();
			return ae;
		}

		IExpression AndExpression()
		{
			// Note: Since we ignored all kinds of CmpExpressions in AndAndExpression(), we have to take CmpExpression instead of ShiftExpression here!
			var left = CmpExpression();
			if (laKind != BitwiseAnd)
				return left;

			Step();
			var ae = new AndExpression();
			ae.LeftOperand = left;
			ae.RightOperand = AndExpression();
			return ae;
		}

		IExpression CmpExpression()
		{
			var left = ShiftExpression();

			OperatorBasedExpression ae = null;

			// Equality Expressions
			if (laKind == Equal || laKind == NotEqual)
				ae = new EqualExpression(laKind == NotEqual);

			// Relational Expressions
			else if (RelationalOperators[laKind])
				ae = new RelExpression(laKind);

			// Identity Expressions
			else if (laKind == Is || (laKind == Not && Peek(1).Kind == Is))
				ae = new IdendityExpression(laKind == Not);

			// In Expressions
			else if (laKind == In || (laKind == Not && Peek(1).Kind == In))
				ae = new InExpression(laKind == Not);

			else return left;

			// Skip possible !-Token
			if (laKind == Not)
				Step();

			// Skip operator
			Step();

			ae.LeftOperand = left;
			ae.RightOperand = ShiftExpression();
			return ae;
		}

		IExpression ShiftExpression()
		{
			var left = AddExpression();
			if (!(laKind == ShiftLeft || laKind == ShiftRight || laKind == ShiftRightUnsigned))
				return left;

			Step();
			var ae = new ShiftExpression(t.Kind);
			ae.LeftOperand = left;
			ae.RightOperand = ShiftExpression();
			return ae;
		}

		/// <summary>
		/// Note: Add, Multiply as well as Cat Expressions are parsed in this method.
		/// </summary>
		IExpression AddExpression()
		{
			var left = UnaryExpression();

			OperatorBasedExpression ae = null;

			switch (laKind)
			{
				case Plus:
				case Minus:
					ae = new AddExpression(laKind == Minus);
					break;
				case Tilde:
					ae = new CatExpression();
					break;
				case Times:
				case Div:
				case Mod:
					ae = new MulExpression(laKind);
					break;
				default:
					return left;
			}

			Step();

			ae.LeftOperand = left;
			ae.RightOperand = AddExpression();
			return ae;
		}

		IExpression UnaryExpression()
		{
			// Note: PowExpressions are handled in PowExpression()

			if (laKind == (BitwiseAnd) || laKind == (Increment) ||
				laKind == (Decrement) || laKind == (Times) ||
				laKind == (Minus) || laKind == (Plus) ||
				laKind == (Not) || laKind == (Tilde))
			{
				Step();

				SimpleUnaryExpression ae = null;

				switch (t.Kind)
				{
					case BitwiseAnd:
						ae = new UnaryExpression_And();
						break;
					case Increment:
						ae = new UnaryExpression_Increment();
						break;
					case Decrement:
						ae = new UnaryExpression_Decrement();
						break;
					case Times:
						ae = new UnaryExpression_Mul();
						break;
					case Minus:
						ae = new UnaryExpression_Sub();
						break;
					case Plus:
						ae = new UnaryExpression_Add();
						break;
					case Tilde:
						ae = new UnaryExpression_Cat();
						break;
					case Not:
						ae = new UnaryExpression_Not();
						break;
				}

				ae.Location = t.Location;

				ae.UnaryExpression = UnaryExpression();

				return ae;
			}

			// ( Type ) . Identifier
			if (laKind == OpenParenthesis)
			{
				AllowWeakTypeParsing = true;
				var curLA = la;
				Step();
				var td = Type();

				AllowWeakTypeParsing = false;

				if (td!=null && ((t.Kind!=OpenParenthesis && laKind == CloseParenthesis && Peek(1).Kind == Dot && Peek(2).Kind == Identifier) || 
					(IsEOF || Peek(1).Kind==EOF || Peek(2).Kind==EOF))) // Also take it as a type declaration if there's nothing following (see Expression Resolving)
				{
					Step();  // Skip to )
					Step();  // Skip to .
					Step();  // Skip to identifier

					var accExpr = new UnaryExpression_Type() { Type=td, AccessIdentifier=t.Value };

					accExpr.Location = curLA.Location;
					accExpr.EndLocation = t.EndLocation;

					return accExpr;
				}
				else
				{
					// Reset the current token with the earlier one to enable Expression parsing
					la = curLA;
				}

			}

			// CastExpression
			if (laKind == (Cast))
			{
				Step();
				var startLoc = t.Location;
				Expect(OpenParenthesis);
				ITypeDeclaration castType = null;
				if (laKind != CloseParenthesis) // Yes, it is possible that a cast() can contain an empty type!
					castType = Type();
				Expect(CloseParenthesis);

				var ae = new CastExpression();
				ae.Type = castType;
				ae.UnaryExpression = UnaryExpression();

				ae.Location = startLoc;
				ae.EndLocation = t.EndLocation;

				return ae;
			}

			// NewExpression
			if (laKind == (New))
				return NewExpression();

			// DeleteExpression
			if (laKind == (Delete))
			{
				Step();
				return new DeleteExpression() { UnaryExpression = UnaryExpression() };
			}


			// PowExpression
			var left = PostfixExpression();

			if (laKind != Pow)
				return left;

			Step();
			var pe = new PowExpression();
			pe.LeftOperand = left;
			pe.RightOperand = UnaryExpression();
			return pe;
		}

		IExpression NewExpression()
		{
			Expect(New);
			var startLoc = t.Location;

			IExpression[] newArgs = null;
			// NewArguments
			if (laKind == (OpenParenthesis))
			{
				Step();
				if (laKind != (CloseParenthesis))
					newArgs = ArgumentList().ToArray();
				Expect(CloseParenthesis);
			}

			/*
			 * If there occurs a class keyword here, interpretate it as an anonymous class definition
			 * http://digitalmars.com/d/2.0/expression.html#NewExpression
			 * 
			 * NewArguments ClassArguments BaseClasslist_opt { DeclDefs } 
			 * 
			 * http://digitalmars.com/d/2.0/class.html#anonymous
			 * 
				NewAnonClassExpression:
					new PerenArgumentListopt class PerenArgumentList_opt SuperClass_opt InterfaceClasses_opt ClassBody

				PerenArgumentList:
					(ArgumentList)
			 * 
			 */
			if (laKind == (Class))
			{
				Step();
				var ac = new AnonymousClassExpression();
				ac.NewArguments = newArgs;

				// ClassArguments
				if (laKind == (OpenParenthesis))
				{
					Step();
					if (laKind == (CloseParenthesis))
						Step();
					else
						ac.ClassArguments = ArgumentList().ToArray();
				}

				var anclass = new DClassLike(Class);

				anclass.Name = "(Anonymous Class)";

				// BaseClasslist_opt
				if (laKind == (Colon))
					//TODO : Add base classes to expression
					anclass.BaseClasses = BaseClassList();
				// SuperClass_opt InterfaceClasses_opt
				else if (laKind != OpenCurlyBrace)
					anclass.BaseClasses = BaseClassList(false);

				//TODO: Add the parsed results to node tree somehow
				ClassBody(anclass);

				ac.AnonymousClass = anclass;

				ac.Location = startLoc;
				ac.EndLocation = t.EndLocation;

				return ac;
			}

			// NewArguments Type
			else
			{
				var initExpr = new NewExpression()
				{
					NewArguments = newArgs,
					Type = BasicType(),
					IsArrayArgument = laKind == OpenSquareBracket,
					Location=startLoc
				};

				var args = new List<IExpression>();
				while (laKind == OpenSquareBracket)
				{
					Step();
					if(laKind!=CloseSquareBracket)
						args.Add(AssignExpression());
					Expect(CloseSquareBracket);
				}

				if (laKind == (OpenParenthesis))
				{
					Step();
					if (laKind != CloseParenthesis)
						args = ArgumentList();
					Expect(CloseParenthesis);
				}

				initExpr.Arguments = args.ToArray();

				initExpr.EndLocation = t.EndLocation;
				return initExpr;
			}
		}

		List<IExpression> ArgumentList()
		{
			var ret = new List<IExpression>();

			ret.Add(AssignExpression());

			while (laKind == (Comma))
			{
				Step();
				ret.Add(AssignExpression());
			}

			return ret;
		}

		IExpression PostfixExpression()
		{
			// PostfixExpression
			IExpression leftExpr = PrimaryExpression();

			while (!IsEOF)
			{
				if (laKind == (Dot))
				{
					Step();

					var e = new PostfixExpression_Access();
					e.PostfixForeExpression = leftExpr;
					leftExpr = e;
					if (laKind == New)
						e.NewExpression = NewExpression();
					else if (IsTemplateInstance)
						e.TemplateOrIdentifier = TemplateInstance();
					else {
						if (Expect(Identifier))
							e.TemplateOrIdentifier = new IdentifierDeclaration(t.Value);
					}

					e.EndLocation = t.EndLocation;
				}
				else if (laKind == (Increment) || laKind == (Decrement))
				{
					Step();
					var e = t.Kind == Increment ? (PostfixExpression)new PostfixExpression_Increment() : new PostfixExpression_Decrement();

					e.EndLocation = t.EndLocation;					
					e.PostfixForeExpression = leftExpr;
					leftExpr = e;
				}

				// Function call
				else if (laKind == (OpenParenthesis))
				{
					Step();
					var ae = new PostfixExpression_MethodCall();
					ae.PostfixForeExpression = leftExpr;
					leftExpr = ae;

					if (laKind != (CloseParenthesis))
						ae.Arguments = ArgumentList().ToArray();
					Step();
					ae.EndLocation = t.EndLocation;
				}

				// IndexExpression | SliceExpression
				else if (laKind == (OpenSquareBracket))
				{
					Step();

					if (laKind != (CloseSquareBracket))
					{
						var firstEx = AssignExpression();
						// [ AssignExpression .. AssignExpression ]
						if (laKind == DoubleDot)
						{
							Step();

							leftExpr = new PostfixExpression_Slice()
							{
								FromExpression = firstEx,
								PostfixForeExpression = leftExpr,
								ToExpression = AssignExpression()
							};
						}
						// [ ArgumentList ]
						else if (laKind == CloseSquareBracket || laKind == (Comma))
						{
							var args = new List<IExpression>();
							args.Add(firstEx);
							if (laKind == Comma)
							{
								Step();
								args.AddRange(ArgumentList());
							}

							leftExpr = new PostfixExpression_Index()
							{
								PostfixForeExpression = leftExpr,
								Arguments = args.ToArray()
							};
						}
					}
					else // Empty array literal = SliceExpression
					{
						leftExpr = new PostfixExpression_Slice()
						{
							PostfixForeExpression=leftExpr
						};
					}

					Expect(CloseSquareBracket);
					if(leftExpr is PostfixExpression)
						(leftExpr as PostfixExpression).EndLocation = t.EndLocation;
				}
				else break;
			}

			return leftExpr;
		}

		IExpression PrimaryExpression()
		{
			bool isModuleScoped = false;
			// For minimizing possible overhead, skip 'useless' tokens like an initial dot <<< TODO
			if (isModuleScoped= laKind == Dot)
				Step();

			if (laKind == __FILE__ || laKind == __LINE__)
			{
				Step();
				return new IdentifierExpression(t.Kind == __FILE__ ? doc.FileName : (object)t.line)
				{
					Location=t.Location,
					EndLocation=t.EndLocation
				};
			}

			// Dollar (== Array length expression)
			if (laKind == Dollar)
			{
				Step();
				return new TokenExpression(laKind)
				{
					Location = t.Location,
					EndLocation = t.EndLocation
				};
			}

			// TemplateInstance
			if (laKind == (Identifier) && Lexer.CurrentPeekToken.Kind == (Not) 
				&& (Peek().Kind != Is && Lexer.CurrentPeekToken.Kind != In) 
				/* Very important: The 'template' could be a '!is'/'!in' expression - With two tokens each! */)
				return TemplateInstance();

			// Identifier
			if (laKind == (Identifier))
			{
				Step();
				return new IdentifierExpression(t.Value)
				{
					Location = t.Location,
					EndLocation = t.EndLocation
				};
			}

			// SpecialTokens (this,super,null,true,false,$) // $ has been handled before
			if (laKind == (This) || laKind == (Super) || laKind == (Null) || laKind == (True) || laKind == (False))
			{
				Step();
				return new TokenExpression(t.Kind)
				{
					Location = t.Location,
					EndLocation = t.EndLocation
				};
			}

			#region Literal
			if (laKind == Literal)
			{
				Step();
				var startLoc = t.Location;

				// Concatenate multiple string literals here
				if (t.LiteralFormat == LiteralFormat.StringLiteral || t.LiteralFormat == LiteralFormat.VerbatimStringLiteral)
				{
					var a = t.LiteralValue as string;
					while (la.LiteralFormat == LiteralFormat.StringLiteral || la.LiteralFormat == LiteralFormat.VerbatimStringLiteral)
					{
						Step();
						a += t.LiteralValue as string;
					}
					return new IdentifierExpression(a, t.LiteralFormat) { Location = startLoc, EndLocation = t.EndLocation };
				}
				//else if (t.LiteralFormat == LiteralFormat.CharLiteral)return new IdentifierExpression(t.LiteralValue) { LiteralFormat=t.LiteralFormat,Location = startLoc, EndLocation = t.EndLocation };
				return new IdentifierExpression(t.LiteralValue, t.LiteralFormat) { Location = startLoc, EndLocation = t.EndLocation };
			}
			#endregion

			#region ArrayLiteral | AssocArrayLiteral
			if (laKind == (OpenSquareBracket))
			{
				Step();
				var startLoc = t.Location;

				// Empty array literal
				if (laKind == CloseSquareBracket)
				{
					Step();
					return new ArrayLiteralExpression() {Location=startLoc, EndLocation = t.EndLocation };
				}

				var firstExpression = AssignExpression();

				// Associtative array
				if (laKind == Colon)
				{
					Step();

					var ae = new AssocArrayExpression() { Location=startLoc};

					var firstValueExpression = AssignExpression();

					ae.KeyValuePairs.Add(firstExpression, firstValueExpression);

					while (laKind == Comma)
					{
						Step();
						var keyExpr = AssignExpression();
						Expect(Colon);
						var valueExpr = AssignExpression();

						ae.KeyValuePairs.Add(keyExpr, valueExpr);
					}

					Expect(CloseSquareBracket);
					ae.EndLocation = t.EndLocation;
					return ae;
				}
				else // Normal array literal
				{
					var ae = new ArrayLiteralExpression() { Location=startLoc};
					var expressions = new List<IExpression>();
					expressions.Add(firstExpression);

					while (laKind == Comma)
					{
						Step();
						if (laKind == CloseSquareBracket) // And again, empty expressions are allowed
							break;
						expressions.Add(AssignExpression());
					}

					ae.Expressions = expressions;

					Expect(CloseSquareBracket);
					ae.EndLocation = t.EndLocation;
					return ae;
				}
			}
			#endregion

			#region FunctionLiteral
			if (laKind == Delegate || laKind == Function || laKind == OpenCurlyBrace || (laKind == OpenParenthesis && IsFunctionLiteral()))
			{
				var fl = new FunctionLiteral() { Location=la.Location};

				if (laKind == Delegate || laKind == Function)
				{
					Step();
					fl.LiteralToken = t.Kind;
				}

				// file.d:1248
				/*
					listdir (".", delegate bool (DirEntry * de)
					{
						auto s = std.string.format("%s : c %s, w %s, a %s", de.name,
								toUTCString (de.creationTime),
								toUTCString (de.lastWriteTime),
								toUTCString (de.lastAccessTime));
						return true;
					}
					);
				*/
				if (laKind != OpenCurlyBrace) // foo( 1, {bar();} ); -> is a legal delegate
				{
					if (!MemberFunctionAttribute[laKind] && Lexer.CurrentPeekToken.Kind == OpenParenthesis)
						fl.AnonymousMethod.Type = BasicType();
					else if (laKind != OpenParenthesis && laKind != OpenCurlyBrace)
						fl.AnonymousMethod.Type = Type();

					if (laKind == OpenParenthesis)
						fl.AnonymousMethod.Parameters = Parameters(fl.AnonymousMethod);
				}

				FunctionBody(fl.AnonymousMethod);

				fl.EndLocation = t.EndLocation;
				return fl;
			}
			#endregion

			#region AssertExpression
			if (laKind == (Assert))
			{
				Step();
				var startLoc = t.Location;
				Expect(OpenParenthesis);
				var ce = new AssertExpression() { Location=startLoc};

				var exprs = new List<IExpression>();
				exprs.Add(AssignExpression());

				if (laKind == (Comma))
				{
					Step();
					exprs.Add(AssignExpression());
				}
				ce.AssignExpressions = exprs.ToArray();
				Expect(CloseParenthesis);
				ce.EndLocation = t.EndLocation;
				return ce;
			}
			#endregion

			#region MixinExpression | ImportExpression
			if (laKind == Mixin)
			{
				Step();
				var e = new MixinExpression() { Location=t.Location};
				Expect(OpenParenthesis);

				e.AssignExpression = AssignExpression();

				Expect(CloseParenthesis);
				e.EndLocation = t.EndLocation;
				return e;
			}

			if (laKind == Import)
			{
				Step();
				var e = new ImportExpression() { Location=t.Location};
				Expect(OpenParenthesis);

				
				e.AssignExpression = AssignExpression();

				Expect(CloseParenthesis);
				e.EndLocation = t.EndLocation;
				return e;
			}
			#endregion

			if (laKind == (Typeof))
			{
				var startLoc = la.Location;
				return new TypeDeclarationExpression(TypeOf()) {Location=startLoc,EndLocation=t.EndLocation};
			}

			// TypeidExpression
			if (laKind == (Typeid))
			{
				Step();
				var ce = new TypeidExpression() { Location=t.Location};
				Expect(OpenParenthesis);
				

				AllowWeakTypeParsing = true;
				ce.Type = Type();
				AllowWeakTypeParsing = false;

				if (ce.Type==null)
					ce.Expression = AssignExpression();

				Expect(CloseParenthesis);
				ce.EndLocation = t.EndLocation;
				return ce;
			}

			#region IsExpression
			if (laKind == Is)
			{
				Step();
				var ce = new IsExpression() { Location=t.Location};
				Expect(OpenParenthesis);

				var LookAheadBackup = la;

				AllowWeakTypeParsing = true;
				ce.TestedType = Type();
				AllowWeakTypeParsing = false;

				if (ce.TestedType!=null && laKind == Identifier && (Lexer.CurrentPeekToken.Kind == CloseParenthesis || Lexer.CurrentPeekToken.Kind == Equal
					|| Lexer.CurrentPeekToken.Kind == Colon))
				{
					Step();
					ce.TypeAliasIdentifier = strVal;
				}
				else 
				// D Language specs mistake: In an IsExpression there also can be expressions!
				if(ce.TestedType==null || !(laKind==CloseParenthesis || laKind==Equal||laKind==Colon))
				{
					// Reset lookahead token to prior position
					la = LookAheadBackup;
					// Reset wrongly parsed type declaration
					ce.TestedType = null;
					ce.TestedExpression = ConditionalExpression();
				}

				if(ce.TestedExpression==null && ce.TestedType==null)
					SynErr(laKind,"In an IsExpression, either a type or an expression is required!");
				
				if (laKind == CloseParenthesis)
				{
					Step();
					ce.EndLocation = t.EndLocation;
					return ce;
				}

				if (laKind == Colon || laKind == Equal)
				{
					Step();
					ce.EqualityTest = t.Kind == Equal;
				}
				else if (laKind == CloseParenthesis)
				{
					Step();
					ce.EndLocation = t.EndLocation;
					return ce;
				}

				/*
				TypeSpecialization:
					Type
						struct
						union
						class
						interface
						enum
						function
						delegate
						super
					const
					immutable
					inout
					shared
						return
				*/

				if (ClassLike[laKind] || laKind==Typedef || // typedef is possible although it's not yet documented in the syntax docs
					laKind==Enum || laKind==Delegate || laKind==Function || laKind==Super || laKind==Return)
				{
					Step();
					ce.TypeSpecializationToken = t.Kind;
				}
				else
					ce.TypeSpecialization = Type();

				if (laKind == Comma)
				{
					Step();
					ce.TemplateParameterList =
						TemplateParameterList(false);
				}

				Expect(CloseParenthesis);
				ce.EndLocation = t.EndLocation;
				return ce;
			}
			#endregion

			// ( Expression )
			if (laKind == OpenParenthesis)
			{
				Step();
				var ret = new SurroundingParenthesesExpression() {Location=t.Location, Expression = Expression() };
				Expect(CloseParenthesis);
				ret.EndLocation = t.EndLocation;
				return ret;
			}

			// TraitsExpression
			if (laKind == (__traits))
				return TraitsExpression();

			#region BasicType . Identifier
			if (laKind == (Const) || laKind == (Immutable) || laKind == (Shared) || laKind == (InOut) || BasicTypes[laKind])
			{
				Step();
				var startLoc = t.Location;
				IExpression left = null;
				if (!BasicTypes[t.Kind])
				{
					int tk = t.Kind;
					// Put an artificial parenthesis around the following type declaration
					if (laKind != OpenParenthesis)
					{
						var mttd = new MemberFunctionAttributeDecl(tk);
						mttd.InnerType = Type();
						left = new TypeDeclarationExpression(mttd) { Location = startLoc, EndLocation = t.EndLocation };
					}
					else
					{
						Expect(OpenParenthesis);
						var mttd = new MemberFunctionAttributeDecl(tk);
						mttd.InnerType = Type();
						Expect(CloseParenthesis);
						left = new TypeDeclarationExpression(mttd) { Location = startLoc, EndLocation = t.EndLocation };
					}
				}
				else
					left = new TokenExpression(t.Kind) {Location=startLoc,EndLocation=t.EndLocation };

				if (laKind == (Dot) && Peek(1).Kind==Identifier)
				{
					Step();
					Step();

					var meaex = new PostfixExpression_Access() { PostfixForeExpression=left, 
						TemplateOrIdentifier=new IdentifierDeclaration(t.Value),EndLocation=t.EndLocation };

					return meaex;
				}
				return left;
			}
			#endregion

			// TODO? Expressions can of course be empty...
			//return null;

			
			SynErr(Identifier);
			Step();
			return new TokenExpression(t.Kind) { Location = t.Location, EndLocation = t.EndLocation };
		}

		bool IsFunctionLiteral()
		{
			if (laKind != OpenParenthesis)
				return false;

			OverPeekBrackets(OpenParenthesis, true);

			return Lexer.CurrentPeekToken.Kind == OpenCurlyBrace;
		}
		#endregion

		#region Statements
		void IfCondition(IfStatement par)
		{
			if (Lexer.CurrentPeekToken.Kind == Times || IsAssignExpression())
				par.IfCondition = Expression();
			else
			{
				var sl = la.Location;

				ITypeDeclaration tp = null;
				if (laKind == Auto)
				{
					tp = new DTokenDeclaration(laKind);
					Step();
				}
				else
					tp = BasicType();

				DNode n = null;
			repeated_decl:
				n = Declarator(false);

				n.StartLocation = sl;
				if (n.Type == null)
					n.Type = tp;
				else
					n.Type.InnerMost = tp;

				// Initializer is optional
				if (laKind == Assign)
				{
					Expect(Assign);
					(n as DVariable).Initializer = Expression();
				}
				n.EndLocation = t.EndLocation;
				par.IfVariable=n as DVariable;
				if (laKind == Comma)
				{
					Step();
					goto repeated_decl;
				}
			}
		}

		public bool IsStatement
		{
			get {
				return laKind == OpenCurlyBrace ||
					(laKind == Identifier && Peek(1).Kind == Colon) ||
					laKind == If || (laKind == Static && 
						(Lexer.CurrentPeekToken.Kind == If || 
						Lexer.CurrentPeekToken.Kind==Assert)) ||
					laKind == While || laKind == Do ||
					laKind == For ||
					laKind == Foreach || laKind == Foreach_Reverse ||
					(laKind == Final && Lexer.CurrentPeekToken.Kind == Switch) || laKind == Switch ||
					laKind == Case || laKind == Default ||
					laKind == Continue || laKind == Break||
					laKind==Return ||
					laKind==Goto ||
					laKind==With||
					laKind==Synchronized||
					laKind==Try||
					laKind==Throw||
					laKind==Scope||
					laKind==Asm||
					laKind==Pragma||
					laKind==Mixin||
					laKind==Version||
					laKind==Debug||
					laKind==Assert||
					laKind==Volatile
					;
			}
		}

		IStatement Statement(bool BlocksAllowed=true,bool EmptyAllowed=true)
		{
			IStatement ret = null;

			if (EmptyAllowed && laKind == Semicolon)
			{
				Step();
				return null;
			}

			if (BlocksAllowed && laKind == OpenCurlyBrace)
				return BlockStatement();

			#region LabeledStatement (loc:... goto loc;)
			if (laKind == Identifier && Lexer.CurrentPeekToken.Kind == Colon)
			{
				Step();

				ret = (new LabeledStatement() { StartLocation = t.Location, Identifier = t.Value });

				Step();
				ret.EndLocation = t.EndLocation;

				return ret;
			}
			#endregion

			#region IfStatement
			else if (laKind == (If) || (laKind == Static && Lexer.CurrentPeekToken.Kind == If))
			{
				bool isStatic = laKind==Static;
				if (isStatic)
					Step();

				Step();

				var dbs = new IfStatement() { StartLocation = t.Location,IsStatic=isStatic };
				
				Expect(OpenParenthesis);

				// IfCondition
				IfCondition(dbs);

				Expect(CloseParenthesis);
				// ThenStatement

				dbs.ThenStatement = Statement();

				// ElseStatement
				if (laKind == (Else))
				{
					Step();
					dbs.ElseStatement = Statement();
				}

				dbs.EndLocation = t.EndLocation;

				return dbs;
			}
			#endregion

			#region WhileStatement
			else if (laKind == While)
			{
				Step();

				var dbs = new WhileStatement() { StartLocation = t.Location };

				Expect(OpenParenthesis);
				dbs.Condition = Expression();
				Expect(CloseParenthesis);

				dbs.ScopedStatement = Statement();
				dbs.EndLocation = t.EndLocation;

				return dbs;
			}
			#endregion

			#region DoStatement
			else if (laKind == (Do))
			{
				Step();

				var dbs = new WhileStatement() { StartLocation=t.Location };

				dbs.ScopedStatement = Statement();

				Expect(While);
				Expect(OpenParenthesis);
				dbs.Condition = Expression();
				Expect(CloseParenthesis);

				dbs.EndLocation = t.EndLocation;

				return dbs;
			}
			#endregion

			#region ForStatement
			else if (laKind == (For))
			{
				Step();

				var dbs = new ForStatement { StartLocation = t.Location };

				Expect(OpenParenthesis);

				// Initialize
				if (laKind != Semicolon)
					dbs.Initialize = Statement(false); // Against the D language theory, blocks aren't allowed here!
				else 
					Step();
				// Enforce a trailing semi-colon only if there hasn't been an expression (the ; gets already skipped in there)
				//	Expect(Semicolon);

				// Test
				if (laKind != (Semicolon))
					dbs.Test = Expression();

				Expect(Semicolon);

				// Increment
				if (laKind != (CloseParenthesis))
					dbs.Increment= Expression();

				Expect(CloseParenthesis);

				dbs.ScopedStatement = Statement();
				dbs.EndLocation = t.EndLocation;

				return dbs;
			}
			#endregion

			#region ForeachStatement
			else if (laKind == (Foreach) || laKind == (Foreach_Reverse))
			{
				Step();

				var dbs = new ForeachStatement() { StartLocation = t.Location, IsReverse = t.Kind == Foreach_Reverse };

				Expect(OpenParenthesis);

				var tl = new List<DVariable>();

				bool init = true;
				while (init || laKind == (Comma))
				{
					if (!init) Step();
					init = false;

					var forEachVar = new DVariable();
					forEachVar.StartLocation = la.Location;

					if (laKind == (Ref))
					{
						Step();
						forEachVar.Attributes.Add(new DAttribute(Ref));
					}
					if (laKind == (Identifier) && (Lexer.CurrentPeekToken.Kind == (Semicolon) || Lexer.CurrentPeekToken.Kind == Comma))
					{
						Step();
						forEachVar.Name = t.Value;
					}
					else
					{
						forEachVar.Type = Type();
						if (laKind == Identifier)
						{
							Expect(Identifier);
							forEachVar.Name = t.Value;
						}
					}
					forEachVar.EndLocation = t.EndLocation;

					tl.Add(forEachVar);
				}
				dbs.ForeachTypeList = tl.ToArray();

				Expect(Semicolon);
				dbs.Aggregate = Expression();

				// ForeachRangeStatement
				if (laKind == DoubleDot)
				{
					Step();
					//TODO: Put this in the expression variable
					Expression();
				}

				Expect(CloseParenthesis);

				dbs.ScopedStatement = Statement();
				dbs.EndLocation = t.EndLocation;

				return dbs;
			}
			#endregion

			#region [Final] SwitchStatement
			else if ((laKind == (Final) && Lexer.CurrentPeekToken.Kind == (Switch)) || laKind == (Switch))
			{
				var dbs = new SwitchStatement { StartLocation = la.Location };

				if (laKind == (Final))
				{
					dbs.IsFinal = true;
					Step();
				}
				Step();
				Expect(OpenParenthesis);
				dbs.SwitchExpression = Expression();
				Expect(CloseParenthesis);

				dbs.ScopedStatement = Statement();
				dbs.EndLocation = t.EndLocation;

				return dbs;
			}
			#endregion

			#region CaseStatement
			else if (laKind == (Case))
			{
				Step();

				var dbs = new SwitchStatement.CaseStatement() { StartLocation = la.Location };

				dbs.ArgumentList = Expression();

				Expect(Colon);

				// CaseRangeStatement
				if (laKind == DoubleDot)
				{
					Step();
					Expect(Case);
					dbs.LastExpression = AssignExpression();
					Expect(Colon);
				}

				var sl = new List<IStatement>();

				while (laKind != Case && laKind != Default && laKind != CloseCurlyBrace && !IsEOF)
				{
					var stmt = Statement();

					if (stmt != null)
						sl.Add(stmt);
				}

				dbs.ScopeStatementList = sl.ToArray();
				dbs.EndLocation = t.EndLocation;

				return dbs;
			}
			#endregion

			#region Default
			else if (laKind == (Default))
			{
				Step();

				var dbs = new SwitchStatement.DefaultStatement()
				{
					StartLocation = la.Location
				};

				Expect(Colon);

				var sl = new List<IStatement>();

				while (laKind != Case && laKind != Default && laKind != CloseCurlyBrace && !IsEOF)
				{
					var stmt = Statement();

					if (stmt != null)
						sl.Add(stmt);
				}

				dbs.ScopeStatementList = sl.ToArray();
				dbs.EndLocation = t.EndLocation;

				return dbs;
			}
			#endregion

			#region Continue | Break
			else if (laKind == (Continue))
			{
				Step();
				var s = new ContinueStatement() { StartLocation = t.Location };
				if (laKind == (Identifier))
				{
					Step();
					s.Identifier = t.Value;
				}
				Expect(Semicolon);
				s.EndLocation = t.EndLocation;

				return s;
			}

			else if (laKind == (Break))
			{
				Step();
				var s = new BreakStatement() { StartLocation = t.Location };
				if (laKind == (Identifier))
				{
					Step();
					s.Identifier = t.Value;
				}
				Expect(Semicolon);
				s.EndLocation = t.EndLocation;

				return s;
			}
			#endregion

			#region Return
			else if (laKind == (Return))
			{
				Step();
				var s = new ReturnStatement() { StartLocation = t.Location };
				if (laKind != (Semicolon))
					s.ReturnExpression = Expression();

				Expect(Semicolon);
				s.EndLocation = t.EndLocation;

				return s;
			}
			#endregion

			#region Goto
			else if (laKind == (Goto))
			{
				Step();
				var s = new GotoStatement() { StartLocation = t.Location };

				if (laKind == (Identifier))
				{
					Step();
					s.StmtType = GotoStatement.GotoStmtType.Identifier;
					s.LabelIdentifier = t.Value;
				}
				else if (laKind == Default)
				{
					Step();
					s.StmtType = GotoStatement.GotoStmtType.Default;
				}
				else if (laKind == (Case))
				{
					Step();
					s.StmtType = GotoStatement.GotoStmtType.Case;

					if (laKind != (Semicolon))
						s.CaseExpression = Expression();
				}

				Expect(Semicolon);
				s.EndLocation = t.EndLocation;

				return s;
			}
			#endregion

			#region WithStatement
			else if (laKind == (With))
			{
				Step();

				var dbs = new WithStatement() { StartLocation = t.Location };

				Expect(OpenParenthesis);

				// Symbol
				dbs.WithExpression = Expression();

				Expect(CloseParenthesis);

				dbs.ScopedStatement = Statement();

				dbs.EndLocation = t.EndLocation;
				return dbs;
			}
			#endregion

			#region SynchronizedStatement
			else if (laKind == (Synchronized))
			{
				Step();
				var dbs = new SynchronizedStatement() { StartLocation = t.Location };

				if (laKind == (OpenParenthesis))
				{
					Step();
					dbs.SyncExpression = Expression();
					Expect(CloseParenthesis);
				}

				dbs.ScopedStatement = Statement();

				dbs.EndLocation = t.EndLocation;
				return dbs;
			}
			#endregion

			#region TryStatement
			else if (laKind == (Try))
			{
				Step();

				var s=new TryStatement(){StartLocation=t.Location};

				s.ScopedStatement=Statement();

				if (!(laKind == (Catch) || laKind == (Finally)))
					SemErr(Catch, "At least one catch or a finally block expected!");

				var catches = new List<TryStatement.CatchStatement>();
				// Catches
				while (laKind == (Catch))
				{
					Step();

					var c = new TryStatement.CatchStatement() { StartLocation=t.Location};

					// CatchParameter
					if (laKind == (OpenParenthesis))
					{
						Step();
						var catchVar = new DVariable();
						var tt = la; //TODO?
						catchVar.Type = BasicType();
						if (laKind != Identifier)
						{
							la = tt;
							catchVar.Type = new IdentifierDeclaration("Exception");
						}
						Expect(Identifier);
						catchVar.Name = t.Value;
						Expect(CloseParenthesis);

						c.CatchParameter = catchVar;
					}

					c.ScopedStatement = Statement();
					c.EndLocation = t.EndLocation;

					catches.Add(c);
				}

				if(catches.Count>0)
					s.Catches = catches.ToArray();

				if (laKind == (Finally))
				{
					Step();

					var f = new TryStatement.FinallyStatement() { StartLocation=t.Location};

					f.ScopedStatement = Statement();
					f.EndLocation = t.EndLocation;

					s.FinallyStmt = f;
				}

				s.EndLocation = t.EndLocation;
				return s;
			}
			#endregion

			#region ThrowStatement
			else if (laKind == (Throw))
			{
				Step();
				var s = new ThrowStatement() { StartLocation = t.Location };
				s.ThrowExpression = Expression();
				Expect(Semicolon);
				s.EndLocation = t.EndLocation;

				return s;
			}
			#endregion

			#region ScopeGuardStatement
			else if (laKind == (Scope))
			{
				Step();
				var s = new ScopeGuardStatement() { StartLocation=t.Location };
				if (laKind == OpenParenthesis)
				{
					Expect(OpenParenthesis);
					Expect(Identifier); // exit, failure, success
					s.GuardedScope = t.Value.ToLower();
					Expect(CloseParenthesis);
				}

				s.ScopedStatement = Statement();

				s.EndLocation = t.EndLocation;
				return s;
			}
			#endregion

			#region AsmStmt
			else if (laKind == (Asm))
			{
				Step();
				var s = new AsmStatement() { StartLocation=t.Location};

				Expect(OpenCurlyBrace);

				var l=new List<string>();
				var curInstr = "";
				while (!IsEOF && laKind != (CloseCurlyBrace))
				{
					if (laKind == Semicolon)
					{
						l.Add(curInstr.Trim());
						curInstr = "";
					}
					else
						curInstr += laKind==Identifier? la.Value: DTokens.GetTokenString(laKind);

					Step();
				}

				Expect(CloseCurlyBrace);
				s.EndLocation = t.EndLocation;
				return s;
			}
			#endregion

			#region PragmaStatement
			else if (laKind == (Pragma))
			{
				var s=_Pragma();

				s.ScopedStatement = Statement();
				s.EndLocation = t.EndLocation;
				return s;
			}
			#endregion

			#region MixinStatement
			//TODO: Handle this one in terms of adding it to the node structure
			else if (laKind == (Mixin))
			{
				// TemplateMixin
				if (Peek(1).Kind != OpenParenthesis)
					return TemplateMixin();
				else
				{
					Step();
					var s = new MixinStatement() { StartLocation = t.Location };
					Expect(OpenParenthesis);

					s.MixinExpression = AssignExpression();

					Expect(CloseParenthesis);
					Expect(Semicolon);

					s.EndLocation = t.EndLocation;
					return s;
				}
			}
			#endregion

			#region Conditions
			if (laKind == Debug)
			{
				Step();
				var s = new ConditionStatement.DebugStatement() { StartLocation = t.Location };

				if (laKind == OpenParenthesis)
				{
					Step();
					if (laKind == Identifier || laKind == Literal)
					{
						Step();
						if (laKind == Literal)
							s.DebugIdentifierOrLiteral = t.LiteralValue;
						else 
							s.DebugIdentifierOrLiteral = t.Value;
					}
					else 
						SynErr(t.Kind, "Identifier or Literal expected, "+DTokens.GetTokenString(t.Kind)+" found");

					Expect(CloseParenthesis);
				}

				s.ScopedStatement = Statement();

				if (laKind == Else)
				{
					Step();
					s.ElseStatement = Statement();
				}

				s.EndLocation = t.EndLocation;
				return s;
			}

			if (laKind == Version)
			{
				Step();
				var s = new ConditionStatement.VersionStatement() { StartLocation = t.Location };

				if (laKind == OpenParenthesis)
				{
					Step();
					if (laKind == Identifier || laKind == Literal || laKind==Unittest)
					{
						Step();
						if(laKind==Unittest)
							s.VersionIdentifierOrLiteral = "unittest";
						else if(laKind==Literal)
							s.VersionIdentifierOrLiteral=t.LiteralValue;
						else 
							s.VersionIdentifierOrLiteral=t.Value;
					}
					else
						SynErr(t.Kind, "Identifier or Literal expected, " + DTokens.GetTokenString(t.Kind) + " found");

					Expect(CloseParenthesis);
				}

				s.ScopedStatement = Statement();

				if (laKind == Else)
				{
					Step();
					s.ElseStatement = Statement();
				}

				s.EndLocation = t.EndLocation;
				return s;
			}
			#endregion

			#region (Static) AssertExpression
			else if (laKind == Assert || (laKind == Static && PK(Assert)))
			{
				var s = new AssertStatement() { StartLocation = la.Location, IsStatic = laKind == Static };

				if (s.IsStatic)
					Step();

				s.AssertExpression = AssignExpression();
				Expect(Semicolon);
				s.EndLocation = t.EndLocation;

				return s;
			}
			#endregion

			#region D1: VolatileStatement
			else if (laKind == Volatile)
			{
				Step();
				var s = new VolatileStatement() { StartLocation = t.Location };
				s.ScopedStatement = Statement();
				s.EndLocation = t.EndLocation;

				return s;
			}
			#endregion

			// ImportDeclaration
			else if (laKind == Import)
				ImportDeclaration();

			else if (!(ClassLike[laKind] || laKind == Enum || Modifiers[laKind] || laKind==PropertyAttribute || laKind == Alias || laKind == Typedef) && IsAssignExpression())
			{
				var s = new ExpressionStatement() { StartLocation = la.Location };
				// a==b, a=9; is possible -> Expressions can be there, not only single AssignExpressions!
				s.Expression = Expression();
				Expect(Semicolon);
				s.EndLocation = t.EndLocation;
				return s;
			}
			else
			{
				var s = new DeclarationStatement() { StartLocation = la.Location };
				s.Declarations = Declaration();
				s.EndLocation = t.EndLocation;
				return s;
			}

			return null;
		}

		BlockStatement BlockStatement(INode ParentNode=null)
		{
			var OldPreviousCommentString = PreviousComment;
			PreviousComment = "";

			var bs = new BlockStatement() { StartLocation=la.Location, ParentNode=ParentNode};
			if (Expect(OpenCurlyBrace))
			{
				if (ParseStructureOnly && laKind != CloseCurlyBrace)
				{
					Lexer.SkipCurrentBlock();
					laKind = la.Kind;
				}
				else
					while (!IsEOF && laKind != (CloseCurlyBrace))
					{
						var s = Statement();
						bs.Add(s);
					}
				Expect(CloseCurlyBrace);
			}
			if(t!=null)
				bs.EndLocation = t.EndLocation;

			PreviousComment = OldPreviousCommentString;
			return bs;
		}
		#endregion

		#region Structs & Unions
		private INode AggregateDeclaration()
		{
			if (!(laKind == (Union) || laKind == (Struct)))
				SynErr(t.Kind, "union or struct required");
			Step();

			var ret = new DClassLike(t.Kind) { StartLocation = t.Location, Description = GetComments() };
			ApplyAttributes(ret);

			// Allow anonymous structs&unions
			if (laKind == Identifier)
			{
				Expect(Identifier);
				ret.Name = t.Value;
			}

			if (laKind == (Semicolon))
			{
				Step();
				return ret;
			}

			// StructTemplateDeclaration
			if (laKind == (OpenParenthesis))
			{
				ret.TemplateParameters = TemplateParameterList();

				// Constraint[opt]
				if (laKind == (If))
					Constraint();
			}

			ClassBody(ret);

			return ret;
		}
		#endregion

		#region Classes
		private INode ClassDeclaration()
		{
			Expect(Class);

			var dc = new DClassLike(Class) { StartLocation = t.Location, Description=GetComments() };
			ApplyAttributes(dc);

			Expect(Identifier);
			dc.Name = t.Value;

			if (laKind == (OpenParenthesis))
			{
				dc.TemplateParameters = TemplateParameterList(true);

				// Constraints
				if (laKind == If)
				{
					Step();
					Expect(OpenParenthesis);

					dc.Constraint = Expression();

					Expect(CloseParenthesis);
				}
			}

			if (laKind == (Colon))
				dc.BaseClasses = BaseClassList();

			ClassBody(dc);

			dc.EndLocation = t.EndLocation;
			return dc;
		}

		private List<ITypeDeclaration> BaseClassList()
		{
			return BaseClassList(true);
		}

		private List<ITypeDeclaration> BaseClassList(bool ExpectColon)
		{
			if (ExpectColon) Expect(Colon);

			var ret = new List<ITypeDeclaration>();

			bool init = true;
			while (init || laKind == (Comma))
			{
				if (!init) Step();
				init = false;
				if (IsProtectionAttribute() && laKind != (Protected))
					Step();

				var ids=IdentifierList();
				if (ids != null)
					ret.Add(ids);
			}
			return ret;
		}

		private void ClassBody(IBlockNode ret,bool KeepBlockAttributes=false)
		{
			if (String.IsNullOrEmpty(ret.Description))
				ret.Description = GetComments();
			var OldPreviousCommentString = PreviousComment;
			PreviousComment = "";

			if (Expect(OpenCurlyBrace))
			{
				var stk_backup = BlockAttributes;

				if(!KeepBlockAttributes)
					BlockAttributes = new Stack<DAttribute>();

				ret.BlockStartLocation = t.Location;
				while (!IsEOF && laKind != (CloseCurlyBrace))
				{
					DeclDef(ret);
				}
				Expect(CloseCurlyBrace);
				ret.EndLocation = t.EndLocation;

				if(!KeepBlockAttributes)
					BlockAttributes = stk_backup;
			}

			PreviousComment = OldPreviousCommentString;

			ret.Description += CheckForPostSemicolonComment();
		}

		INode Constructor(bool IsStruct)
		{
			Expect(This);
			var dm = new DMethod(){
				SpecialType = DMethod.MethodType.Constructor,
				StartLocation = t.Location,
				Name = "this"
			};

			if (IsStruct && Lexer.CurrentPeekToken.Kind == (This) && laKind == (OpenParenthesis))
			{
				var dv = new DVariable();
				dv.Name = "this";
				dm.Parameters.Add(dv);
				Step();
				Step();
				Expect(CloseParenthesis);
			}
			else
			{
				if (IsTemplateParameterList())
					dm.TemplateParameters = TemplateParameterList();

				dm.Parameters = Parameters(dm);
			}

			// handle post argument attributes
			while (IsAttributeSpecifier())
			{
				AttributeSpecifier();
			}

			if (laKind == (If))
				Constraint();

			// handle post argument attributes
			while (IsAttributeSpecifier())
			{
				AttributeSpecifier();
			}

			FunctionBody(dm);
			return dm;
		}

		INode Destructor()
		{
			Expect(Tilde);
			Expect(This);
			var dm = new DMethod();
			dm.SpecialType = DMethod.MethodType.Destructor;
			dm.StartLocation = Lexer.LastToken.Location;
			dm.Name = "~this";

			if (IsTemplateParameterList())
				dm.TemplateParameters = TemplateParameterList();

			dm.Parameters = Parameters(dm);

			if (laKind == (If))
				Constraint();

			FunctionBody(dm);
			return dm;
		}
		#endregion

		#region Interfaces
		private IBlockNode InterfaceDeclaration()
		{
			Expect(Interface);
			var dc = new DClassLike() { StartLocation = t.Location, Description = GetComments() };

			ApplyAttributes(dc);

			Expect(Identifier);
			dc.Name = t.Value;

			if (laKind == (OpenParenthesis))
				dc.TemplateParameters = TemplateParameterList();

			if (laKind == (If))
				Constraint();

			if (laKind == (Colon))
				dc.BaseClasses = BaseClassList();

			// Empty interfaces are allowed
			if (laKind == Semicolon)
				Step();
			else
				ClassBody(dc);

			dc.EndLocation = t.EndLocation;
			return dc;
		}

		void Constraint()
		{
			Expect(If);
			Expect(OpenParenthesis);
			Expression();
			Expect(CloseParenthesis);
		}
		#endregion

		#region Enums
		private INode[] EnumDeclaration()
		{
			Expect(Enum);
			var ret = new List<INode>();

			var mye = new DEnum() { StartLocation = t.Location, Description = GetComments() };

			ApplyAttributes(mye);

			if (IsBasicType() && laKind != Identifier)
				mye.Type = Type();
			else if (laKind == Auto)
			{
				Step();
				mye.Type = new DTokenDeclaration(Auto);
			}

			if (laKind == (Identifier))
			{
				if (Lexer.CurrentPeekToken.Kind == (Assign) || Lexer.CurrentPeekToken.Kind == (OpenCurlyBrace) || Lexer.CurrentPeekToken.Kind == (Semicolon) || Lexer.CurrentPeekToken.Kind == Colon)
				{
					Step();
					mye.Name = t.Value;
				}
				else
				{
					mye.Type = Type();

					Expect(Identifier);
					mye.Name = t.Value;
				}
			}

			if (laKind == (Colon))
			{
				Step();
				mye.Type = Type();
			}

			if (laKind == (Assign) || laKind == (Semicolon))
			{
			another_enumvalue:
				var enumVar = new DVariable();

				enumVar.AssignFrom(mye);

				enumVar.Attributes.Add(new DAttribute(Enum));
				if (mye.Type != null)
					enumVar.Type = mye.Type;
				else
					enumVar.Type = new DTokenDeclaration(Enum);

				if (laKind == (Comma))
				{
					Step();
					Expect(Identifier);
					enumVar.Name = t.Value;
				}

				if (laKind == (Assign))
				{
					Step();
					enumVar.Initializer = AssignExpression();
				}
				enumVar.EndLocation = t.Location;
				ret.Add(enumVar);

				if (laKind == (Comma))
					goto another_enumvalue;

				Expect(Semicolon);
			}
			else
			{
				Expect(OpenCurlyBrace);

				var OldPreviousComment = PreviousComment;
				PreviousComment = "";
				mye.BlockStartLocation = t.Location;

				bool init = true;
				while ((init && laKind != (Comma)) || laKind == (Comma))
				{
					if (!init) Step();
					init = false;

					if (laKind == (CloseCurlyBrace)) break;

					var ev = new DEnumValue() { StartLocation = t.Location, Description = GetComments() };

					if (laKind == (Identifier) && (Lexer.CurrentPeekToken.Kind == (Assign) || Lexer.CurrentPeekToken.Kind == (Comma) || Lexer.CurrentPeekToken.Kind == (CloseCurlyBrace)))
					{
						Step();
						ev.Name = t.Value;
					}
					else
					{
						ev.Type = Type();
						Expect(Identifier);
						ev.Name = t.Value;
					}

					if (laKind == (Assign))
					{
						Step();
						ev.Initializer = AssignExpression();
					}

					ev.EndLocation = t.EndLocation;
					ev.Description += CheckForPostSemicolonComment();
					/*
					//HACK: If it's an anonymous enum, directly add the enum values to the parent block
					if (String.IsNullOrEmpty(mye.Name))
						yield return ev;
					else*/
						mye.Add(ev);
				}
				Expect(CloseCurlyBrace);
				PreviousComment = OldPreviousComment;

				mye.EndLocation = t.EndLocation;
				if (!String.IsNullOrEmpty(mye.Name))
					ret.Add(mye);
			}

			mye.Description += CheckForPostSemicolonComment();

			return ret.ToArray();
		}
		#endregion

		#region Functions
		void FunctionBody(DMethod par)
		{
			bool HadIn = false, HadOut = false;

		check_again:
			if (!HadIn && laKind == (In))
			{
				HadIn = true;
				Step();
				par.In=BlockStatement(par);

				if (!HadOut && laKind == (Out))
					goto check_again;
			}

			if (!HadOut && laKind == (Out))
			{
				HadOut = true;
				Step();

				if (laKind == (OpenParenthesis))
				{
					Step();
					Expect(Identifier);
					Expect(CloseParenthesis);
				}

				par.Out=BlockStatement(par);

				if (!HadIn && laKind == (In))
					goto check_again;
			}

			if (HadIn || HadOut)
				Expect(Body);
			else if (laKind == (Body))
				Step();

			if (laKind == Semicolon) // A function declaration can be empty, of course. This here represents a simple abstract or virtual function
			{
				Step();
				par.Description += CheckForPostSemicolonComment();
			}
			else
				par.Body=BlockStatement(par);

			par.EndLocation = t.EndLocation;
		}
		#endregion

		#region Templates
		/*
         * American beer is like sex on a boat - Fucking close to water;)
         */

		private INode TemplateDeclaration()
		{
			// TemplateMixinDeclaration
			bool isTemplateMixinDecl = laKind == Mixin;
			if (isTemplateMixinDecl)
				Step();
			Expect(Template);
			var dc = new DClassLike(Template) { Description=GetComments() };
			ApplyAttributes(dc);

			if (isTemplateMixinDecl)
				dc.Attributes.Add(new DAttribute(Mixin));

			dc.StartLocation = t.Location;

			Expect(Identifier);
			dc.Name = t.Value;

			dc.TemplateParameters = TemplateParameterList();

			if (laKind == (If))
				Constraint();

			if (laKind == (Colon))
				dc.BaseClasses = BaseClassList();

			ClassBody(dc);

			dc.EndLocation = t.EndLocation;
			return dc;
		}

		TemplateMixin TemplateMixin()
		{
			// mixin TemplateIdentifier !( TemplateArgumentList ) MixinIdentifier ;
			//							|<--			optional			 -->|
			var r = new TemplateMixin();

			Expect(Mixin);
			r.StartLocation = t.Location;

			if (Expect(Identifier))
			{
				r.TemplateId = t.Value;

				if (laKind==Not)
				{
					Step();
					if(Expect(OpenParenthesis) && laKind!=CloseParenthesis)
					{
						var args = new List<IExpression>();
				
						bool init = true;
						while (init || laKind == (Comma))
						{
							if (!init) Step();
							init = false;

							if (IsAssignExpression())
								args.Add(AssignExpression());
							else
								args.Add(new TypeDeclarationExpression(Type()));

							r.Arguments = args.ToArray();
						}
					}
					Expect(CloseParenthesis);
				}
			}

			// MixinIdentifier
			if (laKind == Identifier)
			{
				Step();
				r.MixinId = t.Value;
			}

			Expect(Semicolon);
			r.EndLocation = t.EndLocation;

			return r;
		}

		/// <summary>
		/// Be a bit lazy here with checking whether there're templates or not
		/// </summary>
		private bool IsTemplateParameterList()
		{
			Lexer.StartPeek();
			int r = 0;
			while (r >= 0 && Lexer.CurrentPeekToken.Kind != EOF)
			{
				if (Lexer.CurrentPeekToken.Kind == OpenParenthesis) r++;
				else if (Lexer.CurrentPeekToken.Kind == CloseParenthesis)
				{
					r--;
					if (r <= 0)
						if (Peek().Kind == OpenParenthesis)
							return true;
						else return false;
				}
				Peek();
			}
			return false;
		}

		private ITemplateParameter[] TemplateParameterList()
		{
			return TemplateParameterList(true);
		}

		private ITemplateParameter[] TemplateParameterList(bool MustHaveSurroundingBrackets)
		{
			if (MustHaveSurroundingBrackets) Expect(OpenParenthesis);

			var ret = new List<ITemplateParameter>();

			if (laKind == (CloseParenthesis))
			{
				Step();
				return ret.ToArray();
			}

			bool init = true;
			while (init || laKind == (Comma))
			{
				if (!init) Step();
				init = false;

				ret.Add(TemplateParameter());
			}

			if (MustHaveSurroundingBrackets) Expect(CloseParenthesis);

			return ret.ToArray();
		}

		ITemplateParameter TemplateParameter()
		{
			// TemplateThisParameter
			if (laKind == (This))
			{
				Step();

				return new TemplateThisParameter()
				{
					FollowParameter=TemplateParameter()
				};
			}

			// TemplateTupleParameter
			if (laKind == (Identifier) && Lexer.CurrentPeekToken.Kind == TripleDot)
			{
				Step();
				var id = t.Value;
				Step();

				return new TemplateTupleParameter() { Name=id};
			}

			// TemplateAliasParameter
			if (laKind == (Alias))
			{
				Step();
				var al = new TemplateAliasParameter();
				Expect(Identifier);

				al.Name = t.Value;

				// TODO?:
				// alias BasicType Declarator TemplateAliasParameterSpecialization_opt TemplateAliasParameterDefault_opt

				// TemplateAliasParameterSpecialization
				if (laKind == (Colon))
				{
					Step();

					AllowWeakTypeParsing=true;
					al.SpecializationType = Type();
					AllowWeakTypeParsing=false;

					if (al.SpecializationType==null)
						al.SpecializationExpression = ConditionalExpression();
				}

				// TemplateAliasParameterDefault
				if (laKind == (Assign))
				{
					Step();

					AllowWeakTypeParsing=true;
					al.DefaultType = Type();
					AllowWeakTypeParsing=false;

					if (al.DefaultType==null)
						al.DefaultExpression = ConditionalExpression();
				}
				return al;
			}

			// TemplateTypeParameter
			if (laKind == (Identifier) && (Lexer.CurrentPeekToken.Kind == (Colon) || Lexer.CurrentPeekToken.Kind == (Assign) || Lexer.CurrentPeekToken.Kind == (Comma) || Lexer.CurrentPeekToken.Kind == (CloseParenthesis)))
			{
				var tt = new TemplateTypeParameter();
				Expect(Identifier);

				tt.Name = t.Value;

				if (laKind == (Colon))
				{
					Step();
					tt.Specialization = Type();
				}

				if (laKind == (Assign))
				{
					Step();
					tt.Default = Type();
				}
				return tt;
			}

			// TemplateValueParameter
			var tv = new TemplateValueParameter();
				
			var bt = BasicType();
			var dv = Declarator(false);

			tv.Type = dv.Type;
			tv.Name = dv.Name;

			if (tv.Type == null)
				tv.Type = bt;
			else
				tv.Type.InnerDeclaration = bt;

			if (laKind == (Colon))
			{
				Step();
				tv.SpecializationExpression = ConditionalExpression();
			}

			if (laKind == (Assign))
			{
				Step();
				tv.DefaultExpression = AssignExpression();
			}
			return tv;
		}

		bool IsTemplateInstance
		{
			get {
				return laKind == Identifier && Peek(1).Kind == Not && !(Peek(2).Kind == Is || Lexer.CurrentPeekToken.Kind == In);
			}
		}

		public TemplateInstanceExpression TemplateInstance()
		{
			if (!Expect(Identifier))
				return null;

			var td = new TemplateInstanceExpression() { TemplateIdentifier=t.Value, Location=t.Location};
			var args = new List<IExpression>();
			if (!Expect(Not))
				return td;
			if (laKind == (OpenParenthesis))
			{
				Step();
				if (laKind != CloseParenthesis)
				{
					bool init = true;
					while (init || laKind == (Comma))
					{
						if (!init) Step();
						init = false;

						if (IsAssignExpression())
							args.Add(AssignExpression());
						else
							args.Add(new TypeDeclarationExpression(Type()));
					}
				}
				Expect(CloseParenthesis);
			}
			else
			{
				Step();
				if (t.Kind == Literal)
					args.Add(new IdentifierExpression(t.LiteralValue, LiteralFormat.Scalar));
				else if (t.Kind == Identifier)
					args.Add(new IdentifierExpression(t.Value));
				else
					args.Add(new TokenExpression(t.Kind));
			}
			td.Arguments = args.ToArray();
			td.EndLocation = t.EndLocation;
			return td;
		}
		#endregion

		#region Traits
		IExpression TraitsExpression()
		{
			Expect(__traits);
			var ce = new TraitsExpression() { Location=t.Location};
			Expect(OpenParenthesis);
			

			Expect(Identifier);
			ce.Keyword = t.Value;

			var al = new List<TraitsArgument>();

			while (laKind == Comma)
			{
				Step();
				if (IsAssignExpression())
					al.Add(new TraitsArgument(){AssignExpression= AssignExpression()});
				else
					al.Add(new TraitsArgument(){Type= Type()});
			}

			Expect(CloseParenthesis);
			ce.EndLocation = t.EndLocation;
			return ce;
		}
		#endregion
	}

}