using System;
using System.Collections.Generic;
using System.Text;
using D_Parser.Dom;
using System.ComponentModel;
using D_Parser.Dom.Expressions;

namespace D_Parser.Resolver
{
	/// <summary>
	/// To enable resolving the element sizes parse-time, this enum represents the 3 different string types
	/// </summary>
	public enum StringType
	{
		None,
		/// <summary>
		/// string
		/// </summary>
		utf8,
		/// <summary>
		/// wstring
		/// </summary>
		utf16,
		/// <summary>
		/// dstring
		/// </summary>
		utf32
	}

	public abstract class ResolveResult
	{
		/// <summary>
		/// If the entire resolution took more than one level of type searching, this field represents the resolution base that was used to find the current items.
		/// </summary>
		public ResolveResult ResultBase;
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
	}

	/// <summary>
	/// Encapsules basic types like int, bool, void etc.
	/// </summary>
	public class StaticTypeResult : ResolveResult
	{
		public int BaseTypeToken;
		public ITypeDeclaration Type;

		public override string ToString()
		{
			return Type.ToString();
		}
	}

	/// <summary>
	/// Holds raw expressions like (1+2)
	/// </summary>
	public class ExpressionResult : ResolveResult
	{
		public IExpression Expression;

		public override string ToString()
		{
			return Expression.ToString();
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
	}

	public class TypeResult : ResolveResult
	{
		public IBlockNode ResolvedTypeDefinition;

		/// <summary>
		/// Only will have two or more items if there are multiple definitions of its base class - theoretically, this should be marked as a precompile error then.
		/// </summary>
		public TypeResult[] BaseClass;
		public TypeResult[] ImplementedInterfaces;

		public override string ToString()
		{
			return ResolvedTypeDefinition.ToString();
		}
	}
}
