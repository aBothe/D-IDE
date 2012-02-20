using D_Parser.Dom;
using D_Parser.Dom.Expressions;

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

	public class MemberResult : ResolveResult
	{
		public INode ResolvedMember;

		/// <summary>
		/// Usually there should be only one resolved member type.
		/// If the origin of ResolvedMember seems to be unclear (if there are multiple same-named types), there will be two or more items
		/// </summary>
		public ResolveResult[] MemberBaseTypes;

		public override string ToString()
		{
			return ResolvedMember.ToString();
		}

		public override string ResultPath
		{
			get { return DNode.GetNodePath(ResolvedMember, true); }
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
			get { return ArrayDeclaration!=null? ArrayDeclaration.ToString():""; }
		}
	}

	/// <summary>
	/// Keeps raw expressions like (1+2)
	/// </summary>
	public class ExpressionResult : ResolveResult
	{
		public IExpression Expression;

		public override string ToString()
		{
			return Expression.ToString();
		}

		public override string ResultPath
		{
			get {
				if (Expression == null)
					return "";

				return Expression.ExpressionTypeRepresentation.ToString();
			}
		}
	}

	public class ModuleResult : ResolveResult
	{
		public IAbstractSyntaxTree ResolvedModule;
		public bool IsOnlyModuleNamePartTyped()
		{
			var modNameParts = ResolvedModule.ModuleName.Split('.');
			return AlreadyTypedModuleNameParts != modNameParts.Length;
		}

		public int AlreadyTypedModuleNameParts = 0;

		public override string ToString()
		{
			return ResolvedModule.ToString();
		}

		public override string ResultPath
		{
			get {
				if (ResolvedModule == null || ResolvedModule.ModuleName == null)
					return "";

				var parts = ResolvedModule.ModuleName.Split('.');
				var ret = "";
				for (int i = 0; i < AlreadyTypedModuleNameParts; i++)
					ret += parts[i] + '.';

				return ret.TrimEnd('.');
			}
		}
	}

	/// <summary>
	/// Keeps class-like definitions
	/// </summary>
	public class TypeResult : ResolveResult
	{
		public IBlockNode ResolvedTypeDefinition;

		/// <summary>
		/// Only will have two or more items if there are multiple definitions of its base class - theoretically, this should be marked as a precompile error then.
		/// </summary>
		public TypeResult[] BaseClass;
		public TypeResult[] ImplementedInterfaces;

		/// <summary>
		/// T!int t;
		/// 
		/// t. -- will be resolved to:
		///		1) TypeResult T; TemplateParameter[0]= StaticType int
		///		2) MemberResult t; MemberDefinition= 1)
		/// </summary>
		public ResolveResult[] TemplateParameters;

		public override string ToString()
		{
			return ResolvedTypeDefinition.ToString();
		}

		public override string ResultPath
		{
			get { return DNode.GetNodePath(ResolvedTypeDefinition, true); }
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
	}
}
