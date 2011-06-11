using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using D_Parser.Dom;

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
		IAbstractSyntaxTree Root()
		{
			Step();

			var module = new DModule();
			module.StartLocation = la.Location;
			doc = module;
			// Only one module declaration possible possible!
			if (la.Kind == (Module))
			{
				module.Description = GetComments();
				module.ModuleName = ModuleDeclaration().ToString();
				module.Description += CheckForPostSemicolonComment();
			}
			var _block = module as IBlockNode;
			// Now only declarations or other statements are allowed!
			while (!IsEOF)
			{
				DeclDef(_block);
			}
			module.EndLocation = la.Location;
			return module;
		}

		#region Comments
		string PreviousComment = "";

		string GetComments()
		{
			string ret = "";

			while (Lexer.Comments.Count > 0)
			{
				var c = Lexer.Comments.Pop();

				foreach (var line in c.CommentText.Split('\n'))
					ret += line.Trim().TrimStart('*') + "\r\n";
				ret += "\r\n";
			}

			ret = ret.Trim().Trim('*', '+');

			if (String.IsNullOrEmpty(ret)) return "";

			// Overwrite only if comment is not 'ditto'
			if (ret.ToLower() != "ditto")
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

			while (Lexer.Comments.Count > 0 && Lexer.Comments.Peek().StartPosition.Line == ExpectedLine)
			{
				var c = Lexer.Comments.Pop();

				foreach (var line in c.CommentText.Split('\n'))
					ret += line.Trim().TrimStart('*') + "\n";
				ret += "\n";
			}

			ret = ret.Trim().Trim('*', '+');

			// Add post-declaration string only if comment is not 'ditto'
			if (ret.ToLower() != "ditto")
			{
				if (!String.IsNullOrEmpty(ret))
				{
					PreviousComment += "\n" + ret;
					PreviousComment = PreviousComment.Trim();
					return ret;
				}
			}
			else
				return PreviousComment;

			return "";
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
			else if (la.Kind == (Import))
				ImportDeclaration();

			//Constructor
			else if (la.Kind == (This))
				module.Add(Constructor(module is DClassLike ? (module as DClassLike).ClassType == DTokens.Struct : false));

			//Destructor
			else if (la.Kind == (Tilde) && Lexer.CurrentPeekToken.Kind == (This))
				module.Add(Destructor());

			//Invariant
			else if (la.Kind == (Invariant))
				module.Add(_Invariant());

			//UnitTest
			else if (la.Kind == (Unittest))
			{
				Step();
				var dbs = new DMethod(DMethod.MethodType.Unittest);
				dbs.StartLocation = t.Location;
				FunctionBody(dbs);
				dbs.EndLocation = t.EndLocation;
				module.Add(dbs);
			}

			//ConditionalDeclaration
			else if (la.Kind == (Version) || la.Kind == (Debug) || la.Kind == (If))
			{
				Step();
				var n = t.ToString();

				if (t.Kind == (If))
				{
					Expect(OpenParenthesis);
					AssignExpression();
					Expect(CloseParenthesis);
				}
				else if (la.Kind == (Assign))
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
				else if (t.Kind == (Debug) && la.Kind == (OpenParenthesis))
				{
					Expect(OpenParenthesis);
					n += "(";
					Step();
					n += t.ToString();
					Expect(CloseParenthesis);
					n += ")";
				}

				if (la.Kind == (Colon))
					Step();
			}

			//TODO
			else if (la.Kind == (Else))
			{
				Step();
			}

			//StaticAssert
			else if (la.Kind == (Assert))
			{
				Step();
				Expect(OpenParenthesis);
				AssignExpression();
				if (la.Kind == (Comma))
				{
					Step();
					AssignExpression();
				}
				Expect(CloseParenthesis);
				Expect(Semicolon);
			}
			//TemplateMixin

			//MixinDeclaration
			else if (la.Kind == (Mixin))
				MixinDeclaration();

			//;
			else if (la.Kind == (Semicolon))
				Step();

			// {
			else if (la.Kind == (OpenCurlyBrace))
			{
				// Due to having a new attribute scope, we'll have use a new attribute stack here
				var AttrBackup = BlockAttributes;
				BlockAttributes = new Stack<DAttribute>();

				while (DeclarationAttributes.Count > 0)
					BlockAttributes.Push(DeclarationAttributes.Pop());

				ClassBody(module);

				// After the block ended, restore the previous block attributes
				BlockAttributes = AttrBackup;
			}

			// Class Allocators
			// Note: Although occuring in global scope, parse it anyway but declare it as semantic nonsense;)
			else if (la.Kind == (New))
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
			else if (la.Kind == Delete)
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
			else Declaration(module);
		}

		ITypeDeclaration ModuleDeclaration()
		{
			Expect(Module);
			var ret = ModuleFullyQualifiedName();
			Expect(Semicolon);
			return ret;
		}

		ITypeDeclaration ModuleFullyQualifiedName()
		{
			Expect(Identifier);


			var td=new IdentifierDeclaration(t.Value);

			while (la.Kind == Dot)
			{
				Step();
				Expect(Identifier);
				var ttd = new IdentifierDeclaration(t.Value);

				ttd.InnerDeclaration = td;
				td = ttd;
			}

			return td;
		}

		void ImportDeclaration()
		{
			bool IsPublic = DAttribute.ContainsAttribute(BlockAttributes, Public) || DAttribute.ContainsAttribute(DeclarationAttributes, Public);
			DeclarationAttributes.Clear();
			CheckForDocComments();
			Expect(Import);

			var imp = _Import();
			if (!doc.ContainsImport(imp)) // Check if import is already done
				doc.Imports.Add(imp, IsPublic);

			// ImportBindings
			if (la.Kind == (Colon))
			{
				Step();
				ImportBind();
				while (la.Kind == (Comma))
				{
					Step();
					ImportBind();
				}
			}
			else
				while (la.Kind == (Comma))
				{
					Step();
					imp = _Import();
					if (!doc.ContainsImport(imp)) // Check if import is already done
						doc.Imports.Add(imp, IsPublic);

					if (la.Kind == (Colon))
					{
						Step();
						ImportBind();
						while (la.Kind == (Comma))
						{
							Step();
							ImportBind();
						}
					}
				}

			Expect(Semicolon);
		}

		ITypeDeclaration _Import()
		{
			// ModuleAliasIdentifier
			if (Lexer.CurrentPeekToken.Kind == (Assign))
			{
				Expect(Identifier);
				string ModuleAliasIdentifier = t.Value;
				Step();
			}

			return ModuleFullyQualifiedName();
		}

		void ImportBind()
		{
			Expect(Identifier);
			string imbBind = t.Value;
			string imbBindDef = null;

			if (la.Kind == (Assign))
			{
				Step();
				Expect(Identifier);
				imbBindDef = t.Value;
			}
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
			return la.Kind == (Alias) || IsStorageClass || IsBasicType();
		}

		bool CheckForStorageClasses()
		{
			bool ret = false;
			while (IsStorageClass || Attributes[la.Kind])
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

		bool CheckForModifiers()
		{
			bool ret = false;
			while (Modifiers[la.Kind] || Attributes[la.Kind])
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

		void Declaration(IBlockNode par)
		{
			// Skip ref token
			if (la.Kind == (Ref))
			{
				PushAttribute(new DAttribute(Ref), false);
				Step();
			}

			// Enum possible storage class attributes
			bool HasStorageClassModifiers = CheckForStorageClasses();

			if (la.Kind == (Alias) || la.Kind == Typedef)
			{
				Step();
				// _t is just a synthetic node
				var _t = new DStatementBlock();
				ApplyAttributes(_t);

				// AliasThis
				if (la.Kind == Identifier && PK(This))
				{
					Step();
					var dv = new DVariable();
					dv.Description = GetComments();
					dv.StartLocation = Lexer.LastToken.Location;
					dv.IsAlias = true;
					dv.Name = "this";
					dv.Type = new IdentifierDeclaration(t.Value);
					dv.EndLocation = t.EndLocation;
					par.Add(dv);
					Step();
					Expect(Semicolon);
					dv.Description += CheckForPostSemicolonComment();
					return;
				}

				Decl(_t, HasStorageClassModifiers);
				foreach (var n in _t)
				{
					if (n is DVariable)
						(n as DVariable).IsAlias = true;
				}

				par.AddRange(_t);
			}
			else if (la.Kind == (Struct) || la.Kind == (Union))
				par.Add(AggregateDeclaration());
			else if (la.Kind == (Enum))
				EnumDeclaration(ref par);
			else if (la.Kind == (Class))
				par.Add(ClassDeclaration());
			else if (la.Kind == (Template))
				par.Add(TemplateDeclaration());
			else if (la.Kind == (Interface))
				par.Add(InterfaceDeclaration());
			else if (IsBasicType() || la.Kind==Ref)
				Decl(par, HasStorageClassModifiers);
			else
			{
				Step();
				SynErr(la.Kind,"Declaration expected, not "+GetTokenString(la.Kind));
			}
		}

		void Decl(IBlockNode par, bool HasStorageClassModifiers)
		{
			var startLocation = la.Location;
			ITypeDeclaration ttd = null;

			CheckForStorageClasses();
			// Skip ref token
			if (la.Kind == (Ref))
			{
				if (!DAttribute.ContainsAttribute(DeclarationAttributes, Ref))
					PushAttribute(new DAttribute(Ref), false);
				Step();
			}

			// Autodeclaration
			var StorageClass = DTokens.ContainsStorageClass(DeclarationAttributes.ToArray());

			// If there's no explicit type declaration, leave our node's type empty!
			if ((StorageClass.Token != DAttribute.Empty.Token && la.Kind == (Identifier) && DeclarationAttributes.Count > 0 &&
				(PK(Assign) || PK(OpenParenthesis)))) // public auto var=0; // const foo(...) {} 
			{
			}
			else
				ttd = BasicType();

			// Declarators
			var firstNode = Declarator(false);
			firstNode.Description = GetComments();
			firstNode.StartLocation = startLocation;

			if (firstNode.Type == null)
				firstNode.Type = ttd;
			else
				firstNode.Type.InnerMost = ttd;

			ApplyAttributes(firstNode as DNode);

			// Check for declaration constraints
			if (la.Kind == (If))
				Constraint();

			// BasicType Declarators ;
			if (la.Kind==Assign || la.Kind==Comma || la.Kind==Semicolon)
			{
				// DeclaratorInitializer
				if (la.Kind == (Assign))
					(firstNode as DVariable).Initializer = Initializer();
				firstNode.EndLocation = t.EndLocation;
				par.Add(firstNode);

				// DeclaratorIdentifierList
				while (la.Kind == (Comma))
				{
					Step();
					Expect(Identifier);

					var otherNode = new DVariable();
					otherNode.Assign(firstNode);
					otherNode.StartLocation = t.Location;
					otherNode.Name = t.Value;

					if (la.Kind == (Assign))
						otherNode.Initializer = Initializer();
					otherNode.EndLocation = t.EndLocation;
					par.Add(otherNode);
				}

				Expect(Semicolon);
				var pb = (par as IBlockNode);
				if (pb.Count > 0)
					pb[pb.Count - 1].Description += CheckForPostSemicolonComment();
			}

			// BasicType Declarator FunctionBody
			else if (firstNode is IBlockNode && (la.Kind == In || la.Kind == Out || la.Kind == Body || la.Kind == OpenCurlyBrace))
			{
				FunctionBody(firstNode as IBlockNode);

				par.Add(firstNode);
			}
			else
			{
				SynErr(OpenCurlyBrace, "; or function body expected after declaration stub.");
			}
		}

		bool IsBasicType()
		{
			return BasicTypes[la.Kind] || la.Kind == (Typeof) || MemberFunctionAttribute[la.Kind] || (la.Kind == (Dot) && Lexer.CurrentPeekToken.Kind == (Identifier)) || la.Kind == (Identifier);
		}

		/// <summary>
		/// Used if the parser is unsure if there's a type or an expression - then, instead of throwing exceptions, the Type()-Methods will simply return null;
		/// </summary>
		public bool AllowWeakTypeParsing = false;

		ITypeDeclaration BasicType()
		{
			ITypeDeclaration td = null;
			if (BasicTypes[la.Kind])
			{
				Step();
				return new DTokenDeclaration(t.Kind);
			}

			if (MemberFunctionAttribute[la.Kind])
			{
				Step();
				var md = new MemberFunctionAttributeDecl(t.Kind);
				bool p = false;

				if (la.Kind == OpenParenthesis)
				{
					Step();
					p = true;
				}

				// e.g. cast(const)
				if (la.Kind != CloseParenthesis)
					md.InnerType = Type();

				if (p)
					Expect(CloseParenthesis);
				return md;
			}

			//TODO
			if (la.Kind == Ref)
				Step();

			if (la.Kind == (Typeof))
			{
				td = TypeOf();
				if (la.Kind != (Dot)) return td;
			}

			if (la.Kind == (Dot))
				Step();

			if (AllowWeakTypeParsing&& la.Kind != Identifier)
				return null;

			if (td == null)
				td = IdentifierList();
			else
				td.InnerMost = IdentifierList();

			return td;
		}

		bool IsBasicType2()
		{
			return la.Kind == (Times) || la.Kind == (OpenSquareBracket) || la.Kind == (Delegate) || la.Kind == (Function);
		}

		ITypeDeclaration BasicType2()
		{
			// *
			if (la.Kind == (Times))
			{
				Step();
				return new PointerDecl();
			}

			// [ ... ]
			else if (la.Kind == (OpenSquareBracket))
			{
				Step();
				// [ ]
				if (la.Kind == (CloseSquareBracket)) { Step();
				return new ArrayDecl(); 
				}

				ITypeDeclaration cd = null;

				// [ Type ]
				if (!IsAssignExpression())
				{
					cd = new ArrayDecl() { KeyType=Type()};
				}
				else
				{
					var fromExpression = AssignExpression();

					// [ AssignExpression .. AssignExpression ]
					if (la.Kind == DoubleDot)
					{
						Step();
						cd = new DExpressionDecl(new PostfixExpression_Slice() { FromExpression=fromExpression, ToExpression=AssignExpression()});
					}
					else
						cd = new DExpressionDecl(new PostfixExpression_Index() { Arguments=new[]{fromExpression}});
				}

				if (AllowWeakTypeParsing && la.Kind != CloseSquareBracket)
					return null;

				Expect(CloseSquareBracket);
				return cd;
			}

			// delegate | function
			else if (la.Kind == (Delegate) || la.Kind == (Function))
			{
				Step();
				ITypeDeclaration td = null;
				var dd = new DelegateDeclaration();
				dd.IsFunction = t.Kind == Function;

				dd.Parameters = Parameters(null);
				td = dd;
				//TODO: add attributes to declaration
				while (FunctionAttribute[la.Kind])
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
			if (la.Kind == (OpenParenthesis))
			{
				Step();
				SynErr(OpenParenthesis,"C-style function pointers are deprecated. Use the function() syntax instead.");
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
				if (la.Kind != (CloseParenthesis))
				{
					if (IsParam && la.Kind != (Identifier))
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
						if (la.Kind != (CloseParenthesis))
						{
							ITemplateParameter[] _unused2 = null;
							List<INode> _unused = null;
							ttd = DeclaratorSuffixes(out _unused2, out _unused);

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
				if (IsParam && la.Kind != (Identifier))
					return ret;

				Expect(Identifier);
				ret.Name = t.Value;
			}

			if (IsDeclaratorSuffix)
			{
				// DeclaratorSuffixes
				List<INode> _Parameters;
				ttd = DeclaratorSuffixes(out (ret as DNode).TemplateParameters, out _Parameters);
				if (ttd != null)
				{
					ttd.InnerDeclaration = ret.Type;
					ret.Type = ttd;
				}

				if (_Parameters != null)
				{
					var dm = new DMethod();
					dm.Assign(ret);
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
			get { return la.Kind == (OpenSquareBracket) || la.Kind == (OpenParenthesis); }
		}

		/// <summary>
		/// Note:
		/// http://www.digitalmars.com/d/2.0/declaration.html#DeclaratorSuffix
		/// The definition of a sequence of declarator suffixes is buggy here! Theoretically template parameters can be declared without a surrounding ( and )!
		/// Also, more than one parameter sequences are possible!
		/// 
		/// TemplateParameterList[opt] Parameters MemberFunctionAttributes[opt]
		/// </summary>
		ITypeDeclaration DeclaratorSuffixes(out ITemplateParameter[] TemplateParameters, out List<INode> _Parameters)
		{
			ITypeDeclaration td = null;
			TemplateParameters = null;
			_Parameters = null;

			while (la.Kind == (OpenSquareBracket))
			{
				Step();
				var ad = new ArrayDecl();
				ad.InnerDeclaration = td;
				if (la.Kind != (CloseSquareBracket))
				{
					if (!IsAssignExpression())
					{
						AllowWeakTypeParsing = true;
						ad.KeyType = Type();
						AllowWeakTypeParsing = false;
					}
					if (ad.KeyType==null)
						ad.KeyType = new DExpressionDecl(AssignExpression());
				}
				Expect(CloseSquareBracket);
				td = ad;
			}

			if (la.Kind == (OpenParenthesis))
			{
				if (IsTemplateParameterList())
				{
					TemplateParameters = TemplateParameterList();
				}
				_Parameters = Parameters(null);

				//TODO: MemberFunctionAttributes -- add them to the declaration
				while (StorageClass[la.Kind] || Attributes[la.Kind])
				{
					Step();
				}
			}
			return td;
		}

		public ITypeDeclaration IdentifierList()
		{
			ITypeDeclaration td = null;

			if (la.Kind != (Identifier))
				SynErr(Identifier);

			// Template instancing or Identifier
			td = TemplateInstance();

			while (la.Kind == Dot)
			{
				Step();
				var ttd = TemplateInstance();

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
				return la.Kind == (Abstract) ||
			la.Kind == (Auto) ||
			((MemberFunctionAttribute[la.Kind]) && Lexer.CurrentPeekToken.Kind != (OpenParenthesis)) ||
			la.Kind == (Deprecated) ||
			la.Kind == (Extern) ||
			la.Kind == (Final) ||
			la.Kind == (Override) ||
			la.Kind == (Scope) ||
			la.Kind == (Static) ||
			la.Kind == (Synchronized) ||
			la.Kind == __gshared ||
			la.Kind == __thread;
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
			return IsBasicType2() || la.Kind == (OpenParenthesis);
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
			if (la.Kind == (OpenParenthesis))
			{
				Step();

				td = Declarator2();
				
				if (AllowWeakTypeParsing && (td == null||(t.Kind==OpenParenthesis && la.Kind==CloseParenthesis) /* -- means if an argumentless function call has been made, return null because this would be an expression */|| la.Kind!=CloseParenthesis))
					return null;

				Expect(CloseParenthesis);

				// DeclaratorSuffixes
				if (la.Kind == (OpenSquareBracket))
				{
					List<INode> _unused = null;
					ITemplateParameter[] _unused2 = null;
					DeclaratorSuffixes(out _unused2, out _unused);
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
		List<INode> Parameters(IBlockNode Parent)
		{
			var ret = new List<INode>();
			Expect(OpenParenthesis);

			// Empty parameter list
			if (la.Kind == (CloseParenthesis))
			{
				Step();
				return ret;
			}

			if (la.Kind != TripleDot)
				ret.Add(Parameter());

			while (la.Kind == (Comma))
			{
				Step();
				if (la.Kind == TripleDot)
					break;
				var p = Parameter();
				p.Parent = p;
				ret.Add(p);
			}

			/*
			 * There can be only one '...' in every parameter list
			 */
			if (la.Kind == TripleDot)
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
					dv.Parent = Parent;
					dv.Type = new VarArgDecl();
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

			while (ParamModifiers[la.Kind] || (MemberFunctionAttribute[la.Kind] && !PK(OpenParenthesis)))
			{
				Step();
				attr.Add(new DAttribute(t.Kind));
			}

			if (la.Kind == Auto && Lexer.CurrentPeekToken.Kind == Ref) // functional.d:595 // auto ref F fp
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
			if (la.Kind == (Assign))
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
			if (la.Kind == (Void))
			{
				Step();
				return new VoidInitializer() { Location=t.Location,EndLocation=t.EndLocation};
			}

			return NonVoidInitializer();
		}

		IExpression NonVoidInitializer()
		{
			#region ArrayInitializer
			if (la.Kind == OpenSquareBracket)
			{
				Step();

				// ArrayMemberInitializations
				var ae = new ArrayInitializer() { Location=t.Location};
				var inits=new List<ArrayMemberInitializer>();

				bool IsInit = true;
				while (IsInit || la.Kind == (Comma))
				{
					if (!IsInit) Step();
					IsInit = false;

					// Allow empty post-comma expression IF the following token finishes the initializer expression
					// int[] a=[1,2,3,4,];
					if (la.Kind == CloseSquareBracket)
						break;

					// ArrayMemberInitialization
					var ami = new ArrayMemberInitializer()
					{
						Left = NonVoidInitializer()
					};
					bool HasBeenAssExpr = !(t.Kind == (CloseSquareBracket) || t.Kind == (CloseCurlyBrace));

					// AssignExpression : NonVoidInitializer
					if (HasBeenAssExpr && la.Kind == (Colon))
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
				if (la.Kind == Dot)
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
			if(la.Kind==OpenCurlyBrace)
			{
				// StructMemberInitializations
				var ae = new StructInitializer() { Location=la.Location};
				var inits=new List<StructMemberInitializer>();

				bool IsInit = true;
				while (IsInit || la.Kind == (Comma))
				{
					Step();
					IsInit = false;

					// Allow empty post-comma expression IF the following token finishes the initializer expression
					// int[] a=[1,2,3,4,];
					if (la.Kind == CloseCurlyBrace)
						break;

					// Identifier : NonVoidInitializer
					var sinit = new StructMemberInitializer();
					if (la.Kind == Identifier && Lexer.CurrentPeekToken.Kind == Colon)
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
			if (la.Kind == (Return))
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

		IBlockNode _Invariant()
		{
			IBlockNode inv = new DMethod();
			inv.Name = "invariant";

			Expect(Invariant);
			Expect(OpenParenthesis);
			Expect(CloseParenthesis);
			BlockStatement(ref inv);
			return inv;
		}

		void _Pragma()
		{
			Expect(Pragma);
			Expect(OpenParenthesis);
			Expect(Identifier);

			if (la.Kind == (Comma))
			{
				Step();
				ArgumentList();
			}
			Expect(CloseParenthesis);
		}

		bool IsAttributeSpecifier()
		{
			return (la.Kind == (Extern) || la.Kind == (Export) || la.Kind == (Align) || la.Kind == Pragma || la.Kind == (Deprecated) || IsProtectionAttribute()
				|| la.Kind == (Static) || la.Kind == (Final) || la.Kind == (Override) || la.Kind == (Abstract) || la.Kind == (Scope) || la.Kind == (__gshared)
				|| ((la.Kind == (Auto) || MemberFunctionAttribute[la.Kind]) && (Lexer.CurrentPeekToken.Kind != (OpenParenthesis) && Lexer.CurrentPeekToken.Kind != (Identifier)))
				|| Attributes[la.Kind]);
		}

		bool IsProtectionAttribute()
		{
			return la.Kind == (Public) || la.Kind == (Private) || la.Kind == (Protected) || la.Kind == (Extern) || la.Kind == (Package);
		}

		private void AttributeSpecifier()
		{
			var attr = new DAttribute(la.Kind);
			if (la.Kind == (Extern) && Lexer.CurrentPeekToken.Kind == (OpenParenthesis))
			{
				Step(); // Skip extern
				Step(); // Skip (
				while (!IsEOF && la.Kind != (CloseParenthesis))
					Step();
				Expect(CloseParenthesis);
			}
			else if (la.Kind == (Align) && Lexer.CurrentPeekToken.Kind == (OpenParenthesis))
			{
				Step();
				Step();
				Expect(Literal);
				Expect(CloseParenthesis);
			}
			else if (la.Kind == (Pragma))
				_Pragma();
			else
				Step();

			if (la.Kind == (Colon))
			{
				PushAttribute(attr, true);
				Step();
			}

			else if (la.Kind != Semicolon)
				PushAttribute(attr, false);
		}
		#endregion

		#region Expressions
		public IExpression Expression()
		{
			// AssignExpression
			var ass = AssignExpression();
			if (la.Kind != (Comma))
				return ass;

			/*
			 * The following is a leftover of C syntax and proably cause some errors when parsing arguments etc.
			 */
			// AssignExpression , Expression
			var ae = new Expression();
			ae.Add(ass);
			while (la.Kind == (Comma))
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
				if (!BasicTypes[la.Kind])
				{
					// Skip initial dot
					if (la.Kind == Dot)
						Step();

					if (Peek(1).Kind != Identifier)
					{
						if (la.Kind == Identifier)
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
						else if (la.Kind == (Typeof) || MemberFunctionAttribute[la.Kind])
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
			if (!AssignOps[la.Kind])
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
			if (la.Kind != (Question))
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
			if (la.Kind != LogicalOr)
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
			if (la.Kind != LogicalAnd)
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
			if (la.Kind != BitwiseOr)
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
			if (la.Kind != Xor)
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
			if (la.Kind != BitwiseAnd)
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
			if (la.Kind == Equal || la.Kind == NotEqual)
				ae = new EqualExpression(la.Kind == NotEqual);

			// Relational Expressions
			else if (RelationalOperators[la.Kind])
				ae = new RelExpression(la.Kind);

			// Identity Expressions
			else if (la.Kind == Is || (la.Kind == Not && Peek(1).Kind == Is))
				ae = new IdendityExpression(la.Kind == Not);

			// In Expressions
			else if (la.Kind == In || (la.Kind == Not && Peek(1).Kind == In))
				ae = new InExpression(la.Kind == Not);

			else return left;

			// Skip possible !-Token
			if (la.Kind == Not)
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
			if (!(la.Kind == ShiftLeft || la.Kind == ShiftRight || la.Kind == ShiftRightUnsigned))
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

			switch (la.Kind)
			{
				case Plus:
				case Minus:
					ae = new AddExpression(la.Kind == Minus);
					break;
				case Tilde:
					ae = new CatExpression();
					break;
				case Times:
				case Div:
				case Mod:
					ae = new MulExpression(la.Kind);
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

			if (la.Kind == (BitwiseAnd) || la.Kind == (Increment) ||
				la.Kind == (Decrement) || la.Kind == (Times) ||
				la.Kind == (Minus) || la.Kind == (Plus) ||
				la.Kind == (Not) || la.Kind == (Tilde))
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
			if (la.Kind == OpenParenthesis)
			{
				AllowWeakTypeParsing = true;
				var curLA = la;
				Step();
				var td = Type();

				AllowWeakTypeParsing = false;

				if (td!=null && ((t.Kind!=OpenParenthesis && la.Kind == CloseParenthesis && Peek(1).Kind == Dot && Peek(2).Kind == Identifier) || 
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
					Lexer.LookAhead=curLA;
				}

			}

			// CastExpression
			if (la.Kind == (Cast))
			{
				Step();
				var startLoc = t.Location;
				Expect(OpenParenthesis);
				ITypeDeclaration castType = null;
				if (la.Kind != CloseParenthesis) // Yes, it is possible that a cast() can contain an empty type!
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
			if (la.Kind == (New))
				return NewExpression();

			// DeleteExpression
			if (la.Kind == (Delete))
			{
				Step();
				return new DeleteExpression() { UnaryExpression = UnaryExpression() };
			}


			// PowExpression
			var left = PostfixExpression();

			if (la.Kind != Pow)
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
			if (la.Kind == (OpenParenthesis))
			{
				Step();
				if (la.Kind != (CloseParenthesis))
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
			if (la.Kind == (Class))
			{
				Step();
				var ac = new AnonymousClassExpression();
				ac.NewArguments = newArgs;

				// ClassArguments
				if (la.Kind == (OpenParenthesis))
				{
					Step();
					if (la.Kind == (CloseParenthesis))
						Step();
					else
						ac.ClassArguments = ArgumentList().ToArray();
				}

				var anclass = new DClassLike(Class);

				anclass.Name = "(Anonymous Class)";

				// BaseClasslist_opt
				if (la.Kind == (Colon))
					//TODO : Add base classes to expression
					anclass.BaseClasses = BaseClassList();
				// SuperClass_opt InterfaceClasses_opt
				else if (la.Kind != OpenCurlyBrace)
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
					IsArrayArgument = la.Kind == OpenSquareBracket,
					Location=startLoc
				};

				var args = new List<IExpression>();
				while (la.Kind == OpenSquareBracket)
				{
					Step();
					if(la.Kind!=CloseSquareBracket)
						args.Add(AssignExpression());
					Expect(CloseSquareBracket);
				}

				if (la.Kind == (OpenParenthesis))
				{
					Step();
					if (la.Kind != CloseParenthesis)
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

			while (la.Kind == (Comma))
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
				if (la.Kind == (Dot))
				{
					Step();

					var e = new PostfixExpression_Access();
					e.PostfixForeExpression = leftExpr;
					leftExpr = e;
					if (la.Kind == New)
						e.NewExpression = NewExpression();
					else
						e.TemplateOrIdentifier = TemplateInstance();

					e.EndLocation = t.EndLocation;
				}
				else if (la.Kind == (Increment) || la.Kind == (Decrement))
				{
					Step();
					var e = t.Kind == Increment ? (PostfixExpression)new PostfixExpression_Increment() : new PostfixExpression_Decrement();

					e.EndLocation = t.EndLocation;					
					e.PostfixForeExpression = leftExpr;
					leftExpr = e;
				}

				// Function call
				else if (la.Kind == (OpenParenthesis))
				{
					Step();
					var ae = new PostfixExpression_MethodCall();
					ae.PostfixForeExpression = leftExpr;
					leftExpr = ae;

					if (la.Kind != (CloseParenthesis))
						ae.Arguments = ArgumentList().ToArray();
					Step();
					ae.EndLocation = t.EndLocation;
				}

				// IndexExpression | SliceExpression
				else if (la.Kind == (OpenSquareBracket))
				{
					Step();

					if (la.Kind != (CloseSquareBracket))
					{
						var firstEx = AssignExpression();
						// [ AssignExpression .. AssignExpression ]
						if (la.Kind == DoubleDot)
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
						else if (la.Kind == CloseSquareBracket || la.Kind == (Comma))
						{
							var args = new List<IExpression>();
							args.Add(firstEx);
							if (la.Kind == Comma)
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
			if (isModuleScoped= la.Kind == Dot)
				Step();

			if (la.Kind == __FILE__ || la.Kind == __LINE__)
			{
				Step();
				return new IdentifierExpression(t.Kind == __FILE__ ? doc.FileName : (object)t.line)
				{
					Location=t.Location,
					EndLocation=t.EndLocation
				};
			}

			// Dollar (== Array length expression)
			if (la.Kind == Dollar)
			{
				Step();
				return new TokenExpression(la.Kind)
				{
					Location = t.Location,
					EndLocation = t.EndLocation
				};
			}

			// TemplateInstance
			if (la.Kind == (Identifier) && Lexer.CurrentPeekToken.Kind == (Not) && (Peek().Kind != Is && Lexer.CurrentPeekToken.Kind != In) /* Very important: The 'template' could be a '!is' expression - With two tokens! */)
			{
				var startLoc = la.Location;
				return new TypeDeclarationExpression(TemplateInstance())
				{
					Location=startLoc,
					EndLocation=t.EndLocation
				};
			}

			// Identifier
			if (la.Kind == (Identifier))
			{
				Step();
				return new IdentifierExpression(t.Value)
				{
					Location = t.Location,
					EndLocation = t.EndLocation
				};
			}

			// SpecialTokens (this,super,null,true,false,$) // $ has been handled before
			if (la.Kind == (This) || la.Kind == (Super) || la.Kind == (Null) || la.Kind == (True) || la.Kind == (False))
			{
				Step();
				return new TokenExpression(t.Kind)
				{
					Location = t.Location,
					EndLocation = t.EndLocation
				};
			}

			#region Literal
			if (la.Kind == Literal)
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
					return new IdentifierExpression(a) {LiteralFormat=t.LiteralFormat, Location=startLoc,EndLocation=t.EndLocation};
				}
				else if (t.LiteralFormat == LiteralFormat.CharLiteral)
					return new IdentifierExpression(t.LiteralValue) { LiteralFormat=t.LiteralFormat,Location = startLoc, EndLocation = t.EndLocation };
				return new IdentifierExpression(t.LiteralValue) { LiteralFormat=t.LiteralFormat,Location = startLoc, EndLocation = t.EndLocation };
			}
			#endregion

			#region ArrayLiteral | AssocArrayLiteral
			if (la.Kind == (OpenSquareBracket))
			{
				Step();
				var startLoc = t.Location;

				// Empty array literal
				if (la.Kind == CloseSquareBracket)
				{
					Step();
					return new ArrayLiteralExpression() {Location=startLoc, EndLocation = t.EndLocation };
				}

				var firstExpression = AssignExpression();

				// Associtative array
				if (la.Kind == Colon)
				{
					Step();

					var ae = new AssocArrayExpression() { Location=startLoc};

					var firstValueExpression = AssignExpression();

					ae.KeyValuePairs.Add(firstExpression, firstValueExpression);

					while (la.Kind == Comma)
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

					while (la.Kind == Comma)
					{
						Step();
						if (la.Kind == CloseSquareBracket) // And again, empty expressions are allowed
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
			if (la.Kind == Delegate || la.Kind == Function || la.Kind == OpenCurlyBrace || (la.Kind == OpenParenthesis && IsFunctionLiteral()))
			{
				var fl = new FunctionLiteral() { Location=la.Location};

				if (la.Kind == Delegate || la.Kind == Function)
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
				if (la.Kind != OpenCurlyBrace) // foo( 1, {bar();} ); -> is a legal delegate
				{
					if (!MemberFunctionAttribute[la.Kind] && Lexer.CurrentPeekToken.Kind == OpenParenthesis)
						fl.AnonymousMethod.Type = BasicType();
					else if (la.Kind != OpenParenthesis && la.Kind != OpenCurlyBrace)
						fl.AnonymousMethod.Type = Type();

					if (la.Kind == OpenParenthesis)
						fl.AnonymousMethod.Parameters = Parameters(fl.AnonymousMethod);
				}

				FunctionBody(fl.AnonymousMethod);

				fl.EndLocation = t.EndLocation;
				return fl;
			}
			#endregion

			#region AssertExpression
			if (la.Kind == (Assert))
			{
				Step();
				var startLoc = t.Location;
				Expect(OpenParenthesis);
				var ce = new AssertExpression() { Location=startLoc};

				var exprs = new List<IExpression>();
				exprs.Add(AssignExpression());

				if (la.Kind == (Comma))
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
			if (la.Kind == Mixin)
			{
				Step();
				var e = new MixinExpression() { Location=t.Location};
				Expect(OpenParenthesis);

				e.AssignExpression = AssignExpression();

				Expect(CloseParenthesis);
				e.EndLocation = t.EndLocation;
				return e;
			}

			if (la.Kind == Import)
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

			if (la.Kind == (Typeof))
			{
				var startLoc = la.Location;
				return new TypeDeclarationExpression(TypeOf()) {Location=startLoc,EndLocation=t.EndLocation};
			}

			// TypeidExpression
			if (la.Kind == (Typeid))
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
			if (la.Kind == Is)
			{
				Step();
				var ce = new IsExpression() { Location=t.Location};
				Expect(OpenParenthesis);

				AllowWeakTypeParsing = true;
				ce.Type = Type();
				AllowWeakTypeParsing = false;

				// Originally, a Type is required!
				if (ce.Type==null) // Just allow function calls - but even doing this is still a mess :-D
					ce.Type = new DExpressionDecl(PostfixExpression());

				if (la.Kind == CloseParenthesis)
				{
					Step();
					ce.EndLocation = t.EndLocation;
					return ce;
				}

				if (la.Kind == Identifier)
				{
					Step();
					ce.Identifier = t.Value;
				}

				if (la.Kind == Colon || la.Kind == Equal)
				{
					Step();
					ce.EqualityTest = t.Kind == Equal;
				}
				else if (la.Kind == CloseParenthesis)
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

				if (ClassLike[la.Kind] || LA(Typedef) || // typedef is possible although it's not yet documented in the syntax docs
					LA(Enum) || LA(Delegate) || LA(Function) || LA(Super) || LA(Return))
				{
					Step();
					ce.TypeSpecializationToken = t.Kind;
				}
				else
					ce.Type = Type();

				if (la.Kind == Comma)
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
			if (la.Kind == OpenParenthesis)
			{
				Step();
				var ret = new SurroundingParenthesesExpression() {Location=t.Location, Expression = Expression() };
				Expect(CloseParenthesis);
				ret.EndLocation = t.EndLocation;
				return ret;
			}

			// TraitsExpression
			if (la.Kind == (__traits))
				return TraitsExpression();

			#region BasicType . Identifier
			if (la.Kind == (Const) || la.Kind == (Immutable) || la.Kind == (Shared) || la.Kind == (InOut) || BasicTypes[la.Kind])
			{
				Step();
				var startLoc = t.Location;
				IExpression left = null;
				if (!BasicTypes[t.Kind])
				{
					int tk = t.Kind;
					// Put an artificial parenthesis around the following type declaration
					if (la.Kind != OpenParenthesis)
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

				if (la.Kind == (Dot) && Peek(1).Kind==Identifier)
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
			if (la.Kind != OpenParenthesis)
				return false;

			OverPeekBrackets(OpenParenthesis, true);

			return Lexer.CurrentPeekToken.Kind == OpenCurlyBrace;
		}
		#endregion

		#region Statements
		void IfCondition(ref IBlockNode par)
		{
			IfCondition(ref par, false);
		}
		void IfCondition(ref IBlockNode par, bool IsFor)
		{
			var stmtBlock = par as DStatementBlock;

			if ((!IsFor && Lexer.CurrentPeekToken.Kind == Times) || IsAssignExpression())
			{
				if (stmtBlock != null)
					stmtBlock.Expression = Expression();
				else
					Expression();
			}
			else
			{
				var sl = la.Location;

				ITypeDeclaration tp = null;
				if (la.Kind == Auto)
				{
					tp = new DTokenDeclaration(la.Kind);
					Step();
				}
				else
					tp = BasicType();

				INode n = null;
			repeated_decl:
				n = Declarator(false);

				n.StartLocation = sl;
				if (n.Type == null)
					n.Type = tp;
				else
					n.Type.InnerMost = tp;

				// Initializer is optional
				if (la.Kind == Assign)
				{
					Expect(Assign);
					(n as DVariable).Initializer = Expression();
				}
				n.EndLocation = t.EndLocation;
				par.Add(n);
				if (la.Kind == Comma)
				{
					Step();
					goto repeated_decl;
				}
			}
		}

		void Statement(IBlockNode par, bool CanBeEmpty, bool BlocksAllowed)
		{
			if (CanBeEmpty && la.Kind == (Semicolon))
			{
				Step();
				return;
			}

			else if (BlocksAllowed && la.Kind == (OpenCurlyBrace))
			{
				BlockStatement(ref par);
				return;
			}

			#region LabeledStatement (loc:... goto loc;)
			else if (la.Kind == (Identifier) && Lexer.CurrentPeekToken.Kind == (Colon))
			{
				Step();
				Step();
				return;
			}
			#endregion

			#region IfStatement
			else if (la.Kind == (If) || (la.Kind == Static && Lexer.CurrentPeekToken.Kind == If))
			{
				if (la.Kind == Static)
					Step();
				Step();
				var dbs = new DStatementBlock(If);
				var bs = dbs as IBlockNode;
				dbs.StartLocation = t.Location;
				Expect(OpenParenthesis);

				// IfCondition
				IfCondition(ref bs);

				Expect(CloseParenthesis);
				// ThenStatement

				Statement(bs, false, true);
				if ((dbs as IBlockNode).Count > 0) par.Add(dbs);

				// ElseStatement
				if (la.Kind == (Else))
				{
					Step();
					dbs = new DStatementBlock(Else);
					dbs.StartLocation = t.Location;
					bs = dbs as IBlockNode;
					Statement(bs, false, true);
					dbs.EndLocation = t.EndLocation;
					if ((dbs as IBlockNode).Count > 0) par.Add(dbs);
				}
			}
			#endregion

			#region WhileStatement
			else if (la.Kind == (While))
			{
				Step();

				var dbs = new DStatementBlock(While);
				var bs = dbs as IBlockNode;
				dbs.StartLocation = t.Location;

				Expect(OpenParenthesis);
				IfCondition(ref bs);
				Expect(CloseParenthesis);

				Statement(bs, false, true);
				dbs.EndLocation = t.EndLocation;
				if ((dbs as IBlockNode).Count > 0) par.Add(dbs);
			}
			#endregion

			#region DoStatement
			else if (la.Kind == (Do))
			{
				Step();

				var dbs = new DStatementBlock(Do) as IBlockNode;
				dbs.StartLocation = t.Location;
				Statement(dbs, false, true);

				Expect(While);
				Expect(OpenParenthesis);
				IfCondition(ref dbs);
				Expect(CloseParenthesis);

				dbs.EndLocation = t.EndLocation;
				if ((dbs as IBlockNode).Count > 0) par.Add(dbs);
			}
			#endregion

			#region ForStatement
			else if (la.Kind == (For))
			{
				Step();

				var dbs = new DStatementBlock(For) as IBlockNode;
				dbs.StartLocation = t.Location;

				Expect(OpenParenthesis);

				// Initialize
				if (la.Kind != Semicolon)
					IfCondition(ref dbs, true);
				Expect(Semicolon);

				// Test
				if (la.Kind != (Semicolon))
					(dbs as DStatementBlock).Expression = Expression();

				Expect(Semicolon);

				// Increment
				if (la.Kind != (CloseParenthesis))
					Expression();

				Expect(CloseParenthesis);

				Statement(dbs, false, true);
				dbs.EndLocation = t.EndLocation;
				if ((dbs as IBlockNode).Count > 0) par.Add(dbs);
			}
			#endregion

			#region ForeachStatement
			else if (la.Kind == (Foreach) || la.Kind == (Foreach_Reverse))
			{
				Step();

				var dbs = new DStatementBlock(t.Kind) as IBlockNode;
				dbs.StartLocation = t.Location;

				Expect(OpenParenthesis);

				bool init = true;
				while (init || la.Kind == (Comma))
				{
					if (!init) Step();
					init = false;

					var forEachVar = new DVariable();
					forEachVar.StartLocation = la.Location;

					if (la.Kind == (Ref))
					{
						Step();
						forEachVar.Attributes.Add(new DAttribute(Ref));
					}
					if (la.Kind == (Identifier) && (Lexer.CurrentPeekToken.Kind == (Semicolon) || Lexer.CurrentPeekToken.Kind == Comma))
					{
						Step();
						forEachVar.Name = t.Value;
					}
					else
					{
						forEachVar.Type = Type();
						if (la.Kind == Identifier)
						{
							Expect(Identifier);
							forEachVar.Name = t.Value;
						}
					}
					forEachVar.EndLocation = t.EndLocation;
					if (!String.IsNullOrEmpty(forEachVar.Name)) dbs.Add(forEachVar);
				}

				Expect(Semicolon);
				(dbs as DStatementBlock).Expression = Expression();

				// ForeachRangeStatement
				if (la.Kind == DoubleDot)
				{
					Step();
					//TODO: Put this in the expression variable
					Expression();
				}

				Expect(CloseParenthesis);

				Statement(dbs, false, true);

				dbs.EndLocation = t.EndLocation;
				if ((dbs as IBlockNode).Count > 0) par.Add(dbs);
			}
			#endregion

			#region [Final] SwitchStatement
			else if ((la.Kind == (Final) && Lexer.CurrentPeekToken.Kind == (Switch)) || la.Kind == (Switch))
			{
				var dbs = new DStatementBlock(Switch) as IBlockNode;
				dbs.StartLocation = la.Location;

				if (la.Kind == (Final))
				{
					(dbs as DNode).Attributes.Add(new DAttribute(Final));
					Step();
				}
				Step();
				Expect(OpenParenthesis);
				(dbs as DStatementBlock).Expression = Expression();
				Expect(CloseParenthesis);
				Statement(dbs, false, true);
				dbs.EndLocation = t.EndLocation;

				if ((dbs as IBlockNode).Count > 0) par.Add(dbs);
			}
			#endregion

			#region CaseStatement
			else if (la.Kind == (Case))
			{
				Step();

				var dbs = new DStatementBlock(Case) as IBlockNode;
				dbs.StartLocation = la.Location;

				(dbs as DStatementBlock).Expression = AssignExpression();

				if (!(la.Kind == (Colon) && Lexer.CurrentPeekToken.Kind == (Dot) && Peek().Kind == Dot))
					while (la.Kind == (Comma))
					{
						Step();
						AssignExpression();
					}
				Expect(Colon);

				// CaseRangeStatement
				if (la.Kind == DoubleDot)
				{
					Step();
					Expect(Case);
					AssignExpression();
					Expect(Colon);
				}

				if (la.Kind != CloseCurlyBrace) // {case 1:} is allowed
					Statement(dbs, true, true);
				dbs.EndLocation = t.EndLocation;

				if ((dbs as IBlockNode).Count > 0) par.Add(dbs);
			}
			#endregion

			#region Default
			else if (la.Kind == (Default))
			{
				Step();

				IBlockNode dbs = new DStatementBlock(Default);
				dbs.StartLocation = la.Location;

				Expect(Colon);
				if (la.Kind != CloseCurlyBrace) // switch(...) { default: }  is allowed!
					Statement(dbs, true, true);
				dbs.EndLocation = t.EndLocation;

				if ((dbs as IBlockNode).Count > 0) par.Add(dbs);
			}
			#endregion

			#region Continue | Break
			else if (la.Kind == (Continue) || la.Kind == (Break))
			{
				Step();
				if (la.Kind == (Identifier))
					Step();
				Expect(Semicolon);
			}
			#endregion

			#region Return
			else if (la.Kind == (Return))
			{
				Step();
				if (la.Kind != (Semicolon))
					Expression();
				Expect(Semicolon);
			}
			#endregion

			#region Goto
			else if (la.Kind == (Goto))
			{
				Step();
				if (la.Kind == (Identifier) || la.Kind == (Default))
				{
					Step();
				}
				else if (la.Kind == (Case))
				{
					Step();
					if (la.Kind != (Semicolon))
						Expression();
				}

				Expect(Semicolon);
			}
			#endregion

			#region WithStatement
			else if (la.Kind == (With))
			{
				Step();

				IBlockNode dbs = new DStatementBlock(With);
				dbs.StartLocation = t.Location;

				Expect(OpenParenthesis);

				// Symbol
				(dbs as DStatementBlock).Expression = Expression();

				Expect(CloseParenthesis);
				Statement(dbs, false, true);
				dbs.EndLocation = t.EndLocation;

				if ((dbs as IBlockNode).Count > 0) par.Add(dbs);
			}
			#endregion

			#region SynchronizedStatement
			else if (la.Kind == (Synchronized))
			{
				Step();
				IBlockNode dbs = new DStatementBlock(Synchronized);
				dbs.StartLocation = t.Location;

				if (la.Kind == (OpenParenthesis))
				{
					Step();
					(dbs as DStatementBlock).Expression = Expression();
					Expect(CloseParenthesis);
				}
				Statement(dbs, false, true);

				dbs.EndLocation = t.EndLocation;
				if ((dbs as IBlockNode).Count > 0) par.Add(dbs);
			}
			#endregion

			#region TryStatement
			else if (la.Kind == (Try))
			{
				Step();

				IBlockNode dbs = new DStatementBlock(Try);
				dbs.StartLocation = t.Location;
				Statement(dbs, false, true);
				dbs.EndLocation = t.EndLocation;
				if ((dbs as IBlockNode).Count > 0) par.Add(dbs);

				if (!(la.Kind == (Catch) || la.Kind == (Finally)))
					SynErr(Catch, "catch or finally expected");

				// Catches
			do_catch:
				if (la.Kind == (Catch))
				{
					Step();
					dbs = new DStatementBlock(Catch);
					dbs.StartLocation = t.Location;

					// CatchParameter
					if (la.Kind == (OpenParenthesis))
					{
						Step();
						var catchVar = new DVariable();
						var tt = la; //TODO?
						catchVar.Type = BasicType();
						if (la.Kind != Identifier)
						{
							Lexer.LookAhead = tt;
							catchVar.Type = new IdentifierDeclaration("Exception");
						}
						Expect(Identifier);
						catchVar.Name = t.Value;
						Expect(CloseParenthesis);
						dbs.Add(catchVar);
					}

					Statement(dbs, false, true);
					dbs.EndLocation = t.EndLocation;
					if ((dbs as IBlockNode).Count > 0) par.Add(dbs);

					if (la.Kind == (Catch))
						goto do_catch;
				}

				if (la.Kind == (Finally))
				{
					Step();

					dbs = new DStatementBlock(Finally);
					dbs.StartLocation = t.Location;
					Statement(dbs, false, true);
					dbs.EndLocation = t.EndLocation;
					if ((dbs as IBlockNode).Count > 0) par.Add(dbs);
				}
			}
			#endregion

			#region ThrowStatement
			else if (la.Kind == (Throw))
			{
				Step();
				Expression();
				Expect(Semicolon);
			}
			#endregion

			// ScopeGuardStatement
			else if (la.Kind == (Scope))
			{
				Step();
				if (la.Kind == OpenParenthesis)
				{
					Expect(OpenParenthesis);
					Expect(Identifier); // exit, failure, success
					Expect(CloseParenthesis);
				}
				Statement(par, false, true);
			}

			// AsmStatement
			else if (la.Kind == (Asm))
			{
				Step();
				Expect(OpenCurlyBrace);

				while (!IsEOF && la.Kind != (CloseCurlyBrace))
				{
					Step();
				}

				Expect(CloseCurlyBrace);
			}

			// PragmaStatement
			else if (la.Kind == (Pragma))
			{
				_Pragma();
				Statement(par, true, true);
			}

			// MixinStatement
			//TODO: Handle this one in terms of adding it to the node structure
			else if (la.Kind == (Mixin))
			{
				MixinDeclaration();
			}

			// (Static) AssertExpression
			else if (la.Kind == Assert || (la.Kind == Static && PK(Assert)))
			{
				if (LA(Static))
					Step();

				AssignExpression();
				Expect(Semicolon);
			}

			#region VersionStatement | DebugCondition
			else if (la.Kind == Version || la.Kind == Debug)
			{
				Step();

				// a debug attribute doesn't require a '('!
				if (t.Kind == Version || la.Kind == OpenParenthesis)
				{
					Expect(OpenParenthesis);
					while (!IsEOF && !LA(CloseParenthesis))
						Step();
					Expect(CloseParenthesis);
				}

				if (LA(Colon))
					Step();
				else
					Statement(par, false, true);

				if (la.Kind == Else)
				{
					Step();
					Statement(par, false, true);
				}
			}
			#endregion

			// Blockstatement
			else if (la.Kind == (OpenCurlyBrace))
				BlockStatement(ref par);

			else if (!(ClassLike[la.Kind] || la.Kind == Enum || Modifiers[la.Kind] || Attributes[la.Kind] || la.Kind == Alias || la.Kind == Typedef) && IsAssignExpression())
			{
				// a==b, a=9; is possible -> Expressions can be there, not only single AssignExpressions!
				var ex = Expression();
				Expect(Semicolon);
			}
			else
				Declaration(par);
		}

		void BlockStatement(ref IBlockNode par)
		{
			if (String.IsNullOrEmpty(par.Description)) par.Description = GetComments();
			var OldPreviousCommentString = PreviousComment;
			PreviousComment = "";

			if (Expect(OpenCurlyBrace))
			{
				par.BlockStartLocation = t.Location;
				if (la.Kind != CloseCurlyBrace)
				{
					if (ParseStructureOnly)
						Lexer.SkipCurrentBlock();
					else
						while (!IsEOF && la.Kind != (CloseCurlyBrace))
						{
							Statement(par, true, true);
						}
				}
				Expect(CloseCurlyBrace);
				par.EndLocation = t.EndLocation;
			}

			PreviousComment = OldPreviousCommentString;
		}
		#endregion

		#region Structs & Unions
		private INode AggregateDeclaration()
		{
			if (!(la.Kind == (Union) || la.Kind == (Struct)))
				SynErr(t.Kind, "union or struct required");
			Step();

			var ret = new DClassLike(t.Kind);
			ApplyAttributes(ret);

			// Allow anonymous structs&unions
			if (la.Kind == Identifier)
			{
				Expect(Identifier);
				ret.Name = t.Value;
			}

			if (la.Kind == (Semicolon))
			{
				Step();
				return ret;
			}

			// StructTemplateDeclaration
			if (la.Kind == (OpenParenthesis))
			{
				ret.TemplateParameters = TemplateParameterList();

				// Constraint[opt]
				if (la.Kind == (If))
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

			var dc = new DClassLike(Class);
			ApplyAttributes(dc);
			dc.StartLocation = t.Location;

			Expect(Identifier);
			dc.Name = t.Value;

			if (la.Kind == (OpenParenthesis))
			{
				dc.TemplateParameters = TemplateParameterList(true);

				// Constraints
				if (la.Kind == If)
				{
					Step();
					Expect(OpenParenthesis);

					dc.Constraint = Expression();

					Expect(CloseParenthesis);
				}
			}

			if (la.Kind == (Colon))
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
			while (init || la.Kind == (Comma))
			{
				if (!init) Step();
				init = false;
				if (IsProtectionAttribute() && la.Kind != (Protected))
					Step();

				var ids=IdentifierList();
				if (ids != null)
					ret.Add(ids);
			}
			return ret;
		}

		private void ClassBody(IBlockNode ret)
		{
			if (String.IsNullOrEmpty(ret.Description))
				ret.Description = GetComments();
			var OldPreviousCommentString = PreviousComment;
			PreviousComment = "";

			if (Expect(OpenCurlyBrace))
			{
				var stk_backup = BlockAttributes;
				BlockAttributes = new Stack<DAttribute>();

				ret.BlockStartLocation = t.Location;
				while (!IsEOF && la.Kind != (CloseCurlyBrace))
				{
					DeclDef(ret);
				}
				Expect(CloseCurlyBrace);
				ret.EndLocation = t.EndLocation;
				BlockAttributes = stk_backup;
			}

			PreviousComment = OldPreviousCommentString;
		}

		INode Constructor(bool IsStruct)
		{
			Expect(This);
			var dm = new DMethod();
			dm.SpecialType = DMethod.MethodType.Constructor;
			dm.StartLocation = t.Location;
			dm.Name = "this";

			if (IsStruct && Lexer.CurrentPeekToken.Kind == (This) && la.Kind == (OpenParenthesis))
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

			if (la.Kind == (If))
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

			if (la.Kind == (If))
				Constraint();

			FunctionBody(dm);
			return dm;
		}
		#endregion

		#region Interfaces
		private IBlockNode InterfaceDeclaration()
		{
			Expect(Interface);
			var dc = new DClassLike();
			dc.StartLocation = t.Location;
			ApplyAttributes(dc);

			Expect(Identifier);
			dc.Name = t.Value;

			if (la.Kind == (OpenParenthesis))
				dc.TemplateParameters = TemplateParameterList();

			if (la.Kind == (If))
				Constraint();

			if (la.Kind == (Colon))
				dc.BaseClasses = BaseClassList();

			// Empty interfaces are allowed
			if (la.Kind == Semicolon)
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
		private void EnumDeclaration(ref IBlockNode par)
		{
			Expect(Enum);

			DEnum mye = new DEnum();
			mye.StartLocation = t.Location;
			ApplyAttributes(mye);

			if (IsBasicType() && la.Kind != Identifier)
				mye.Type = Type();
			else if (la.Kind == Auto)
			{
				Step();
				mye.Type = new DTokenDeclaration(Auto);
			}

			if (la.Kind == (Identifier))
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

			if (la.Kind == (Colon))
			{
				Step();
				mye.Type = Type();
			}

			if (la.Kind == (Assign) || la.Kind == (Semicolon))
			{
			another_enumvalue:
				DVariable enumVar = new DVariable();
				enumVar.Assign(mye);
				enumVar.Attributes.Add(new DAttribute(Enum));
				if (mye.Type != null)
					enumVar.Type = mye.Type;
				else
					enumVar.Type = new DTokenDeclaration(Enum);

				if (la.Kind == (Comma))
				{
					Step();
					Expect(Identifier);
					enumVar.Name = t.Value;
				}

				if (la.Kind == (Assign))
				{
					Step();
					enumVar.Initializer = AssignExpression();
				}
				enumVar.EndLocation = t.Location;
				par.Add(enumVar);

				if (la.Kind == (Comma))
					goto another_enumvalue;

				Expect(Semicolon);
			}
			else
			{
				Expect(OpenCurlyBrace);
				mye.BlockStartLocation = t.Location;

				bool init = true;
				while ((init && la.Kind != (Comma)) || la.Kind == (Comma))
				{
					if (!init) Step();
					init = false;

					if (la.Kind == (CloseCurlyBrace)) break;

					DEnumValue ev = new DEnumValue();
					ev.StartLocation = t.Location;
					if (la.Kind == (Identifier) && (Lexer.CurrentPeekToken.Kind == (Assign) || Lexer.CurrentPeekToken.Kind == (Comma) || Lexer.CurrentPeekToken.Kind == (CloseCurlyBrace)))
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

					if (la.Kind == (Assign))
					{
						Step();
						ev.Initializer = AssignExpression();
					}

					ev.EndLocation = t.EndLocation;

					if (String.IsNullOrEmpty(mye.Name))
						par.Add(ev);
					else
						mye.Add(ev);
				}
				Expect(CloseCurlyBrace);
				mye.EndLocation = t.EndLocation;
				if (!String.IsNullOrEmpty(mye.Name))
					par.Add(mye);
			}
		}
		#endregion

		#region Functions
		void FunctionBody(IBlockNode par)
		{
			bool HadIn = false, HadOut = false;

		check_again:
			if (!HadIn && la.Kind == (In))
			{
				HadIn = true;
				Step();
				BlockStatement(ref par);

				if (!HadOut && la.Kind == (Out))
					goto check_again;
			}

			if (!HadOut && la.Kind == (Out))
			{
				HadOut = true;
				Step();

				if (la.Kind == (OpenParenthesis))
				{
					Step();
					Expect(Identifier);
					Expect(CloseParenthesis);
				}

				BlockStatement(ref par);

				if (!HadIn && la.Kind == (In))
					goto check_again;
			}

			if (HadIn || HadOut)
				Expect(Body);
			else if (la.Kind == (Body))
				Step();

			if (la.Kind == Semicolon) // A function declaration can be empty, of course. This here represents a simple abstract or virtual function
			{
				Step();
				par.Description += CheckForPostSemicolonComment();
			}
			else
				BlockStatement(ref par);

		}
		#endregion

		#region Templates
		/*
         * American beer is like sex on a boat - Fucking close to water;)
         */

		private INode TemplateDeclaration()
		{
			Expect(Template);
			var dc = new DClassLike(Template);
			ApplyAttributes(dc);
			dc.StartLocation = t.Location;

			Expect(Identifier);
			dc.Name = t.Value;

			dc.TemplateParameters = TemplateParameterList();

			if (la.Kind == (If))
				Constraint();

			if (la.Kind == (Colon))
				dc.BaseClasses = BaseClassList();

			ClassBody(dc);

			dc.EndLocation = t.EndLocation;
			return dc;
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

			if (la.Kind == (CloseParenthesis))
			{
				Step();
				return ret.ToArray();
			}

			bool init = true;
			while (init || la.Kind == (Comma))
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
			if (la.Kind == (This))
			{
				Step();

				return new TemplateThisParameter()
				{
					FollowParameter=TemplateParameter()
				};
			}

			// TemplateTupleParameter
			if (la.Kind == (Identifier) && Lexer.CurrentPeekToken.Kind == TripleDot)
			{
				Step();
				var id = t.Value;
				Step();

				return new TemplateTupleParameter() { Name=id};
			}

			// TemplateAliasParameter
			if (la.Kind == (Alias))
			{
				Step();
				var al = new TemplateAliasParameter();
				Expect(Identifier);

				al.Name = t.Value;

				// TODO?:
				// alias BasicType Declarator TemplateAliasParameterSpecialization_opt TemplateAliasParameterDefault_opt

				// TemplateAliasParameterSpecialization
				if (la.Kind == (Colon))
				{
					Step();

					AllowWeakTypeParsing=true;
					al.SpecializationType = Type();
					AllowWeakTypeParsing=false;

					if (al.SpecializationType==null)
						al.SpecializationExpression = ConditionalExpression();
				}

				// TemplateAliasParameterDefault
				if (la.Kind == (Assign))
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
			if (la.Kind == (Identifier) && (Lexer.CurrentPeekToken.Kind == (Colon) || Lexer.CurrentPeekToken.Kind == (Assign) || Lexer.CurrentPeekToken.Kind == (Comma) || Lexer.CurrentPeekToken.Kind == (CloseParenthesis)))
			{
				var tt = new TemplateTypeParameter();
				Expect(Identifier);

				tt.Name = t.Value;

				if (la.Kind == (Colon))
				{
					Step();
					tt.Specialization = Type();
				}

				if (la.Kind == (Assign))
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

			if (la.Kind == (Colon))
			{
				Step();
				tv.SpecializationExpression = ConditionalExpression();
			}

			if (la.Kind == (Assign))
			{
				Step();
				tv.DefaultExpression = AssignExpression();
			}
			return tv;
		}

		private AbstractTypeDeclaration TemplateInstance()
		{
			if (!Expect(Identifier))
				return null;

			if (la.Kind != Not || (Peek(1).Kind==Is || Lexer.CurrentPeekToken.Kind==In)) // myExpr !is null  --> there, it would parse 'is' as template argument
				return new IdentifierDeclaration(t.Value);

			var td = new TemplateDecl() { TemplateIdentifier=t.Value};
			Expect(Not);
			if (la.Kind == (OpenParenthesis))
			{
				Step();
				if (la.Kind != CloseParenthesis)
				{
					bool init = true;
					while (init || la.Kind == (Comma))
					{
						if (!init) Step();
						init = false;

						if (IsAssignExpression())
							td.Template.Add(new DExpressionDecl(AssignExpression()));
						else
							td.Template.Add(Type());
					}
				}
				Expect(CloseParenthesis);
			}
			else
			{
				Step();
				if (t.Kind == (Identifier) || t.Kind == (Literal))
					td.Template.Add(new IdentifierDeclaration(t.LiteralValue));
				else
					td.Template.Add(new DTokenDeclaration(t.Kind));
			}
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

			while (la.Kind == Comma)
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