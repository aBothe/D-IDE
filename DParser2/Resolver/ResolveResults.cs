﻿using D_Parser.Dom;
using System.Collections.Generic;
using D_Parser.Dom.Expressions;
using D_Parser.Misc;

namespace D_Parser.Resolver
{
	public abstract class ResolveResult
	{
		/// <summary>
		/// If the entire resolution took more than one level of type searching, this field represents the resolution base that was used to find the current items.
		/// </summary>
		public ResolveResult ResultBase;

		/// <summary>
		/// The type declaration / expression that has been used as the base for this type resolution.
		/// </summary>
		public object DeclarationOrExpressionBase;

		public abstract string ResultPath {get;}
	}

	public abstract class TemplateInstanceResult : ResolveResult
	{
		public INode Node;

		/// <summary>
		/// T!int t;
		/// 
		/// t. -- will be resolved to:
		///		1) TypeResult T; TemplateParameter[0]= StaticType int
		///		2) MemberResult t; MemberDefinition= 1)
		/// </summary>
		public Dictionary<ITemplateParameter, ResolveResult[]> TemplateParameters;
	}

	public class MemberResult : TemplateInstanceResult
	{
		/// <summary>
		/// Usually there should be only one resolved member type.
		/// If the origin of ResolvedMember seems to be unclear (if there are multiple same-named types), there will be two or more items
		/// </summary>
		public ResolveResult[] MemberBaseTypes;

		public bool IsAlias
		{
			get {
				return Node is DVariable && ((DVariable)Node).IsAlias;
			}
		}

		public override string ToString()
		{
			return Node.ToString();
		}

		public override string ResultPath
		{
			get { return DNode.GetNodePath(Node, true); }
		}
	}

	/// <summary>
	/// Encapsules basic type declarations like int, bool, void[], byte*, immutable(char)[] etc.
	/// </summary>
	public class StaticTypeResult : ResolveResult
	{
		public int BaseTypeToken;

		public override string ToString()
		{
			return DeclarationOrExpressionBase.ToString();
		}

		public override string ResultPath
		{
			get { return ToString(); }
		}
	}

	/// <summary>
	/// Keeps class-like definitions
	/// </summary>
	public class TypeResult : TemplateInstanceResult
	{
		/// <summary>
		/// Only will have two or more items if there are multiple definitions of its base class - 
		/// theoretically, this should be marked as a precompile error then.
		/// </summary>
		public TypeResult[] BaseClass;
		public TypeResult[] ImplementedInterfaces;

		public override string ToString()
		{
			return Node.ToString();
		}

		public override string ResultPath
		{
			get { return DNode.GetNodePath(Node, true); }
		}
	}

	/// <summary>
	/// Will be returned on both
	/// 1) int delegate() dg;
	/// 2) delegate() { ... }
	/// whereas on case 1), IsDelegateDeclaration will be true
	/// </summary>
	public class DelegateResult : ResolveResult
	{
		public bool IsDelegateDeclaration
		{
			get
			{
				return DeclarationOrExpressionBase is DelegateDeclaration;
			}
		}

		/// <summary>
		/// delegate() { return 12; } has a return type of static type 'int'
		/// int delegate() dg; will also have the return type 'int', like it's given already
		/// </summary>
		public ResolveResult[] ReturnType;

		public override string ResultPath
		{
			get { return DeclarationOrExpressionBase==null ? "" : DeclarationOrExpressionBase.ToString(); }
		}

		public override string ToString()
		{
			return ResultPath;
		}
	}

	public class ArrayResult : ResolveResult
	{
		public ArrayDecl ArrayDeclaration
		{
			get { return DeclarationOrExpressionBase as ArrayDecl; }
			set { DeclarationOrExpressionBase = value; }
		}

		public ResolveResult[] KeyType;

		public override string ResultPath
		{
			get { return ArrayDeclaration != null ? ArrayDeclaration.ToString() : ""; }
		}

		public override string ToString()
		{
			return DeclarationOrExpressionBase.ToString();
		}
	}





	/// <summary>
	/// Will be returned if not an entire module name but an existing module package was mentioned in the code
	/// </summary>
	public class ModulePackageResult : ResolveResult
	{
		public ModulePackage Package { get; private set; }

		public ModulePackageResult(ModulePackage pack)
		{
			Package = pack;
		}

		public override string ToString()
		{
			return Package.ToString();
		}

		public override string ResultPath
		{
			get { return Package.ToString(); }
		}
	}

	/// <summary>
	/// Will be returned if a module name was typed
	/// </summary>
	public class ModuleResult : ResolveResult
	{
		public IAbstractSyntaxTree Module { get; private set; }

		public ModuleResult(IAbstractSyntaxTree mod)
		{
			Module = mod;
		}

		public override string ToString()
		{
			return Module == null ? "" : Module.ModuleName;
		}

		public override string ResultPath
		{
			get
			{
				return Module == null ? "" : Module.ModuleName;
			}
		}
	}

}
