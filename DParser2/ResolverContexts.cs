using System;
using System.Collections.Generic;
using System.Text;
using D_Parser.Core;
using System.ComponentModel;

namespace D_Parser.Resolver
{
	/// <summary>
	/// Used on code completion:
	/// e.g. if the resolved declaration is a dynamic array (so the elements themselves were NOT accessed)
	/// like in:
	/// void[] buf=...;
	/// int len=buf.length; // Here we only access the array itself but not its elements.
	/// 
	/// For code completion purposes, the comletion list after "buf." would contain D-specific items like .sizeof, .length, .init, .dup, .idup and so on - what makes this enum so important
	/// </summary>
	public enum SpecialType
	{
		None,
		/// <summary>
		/// Outer type is an array
		/// </summary>
		Array,
		/// <summary>
		/// Outer type is an associative array
		/// </summary>
		AssociativeArray,
		/// <summary>
		/// Outer type is a pointer
		/// </summary>
		Pointer,
		/// <summary>
		/// Outer type is a string - so a special kind of array
		/// </summary>
		String
	}

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

	public class AliasResult : ResolveResult
	{
		public ResolveResult[] AliasDefinition;

		public override string ToString()
		{
			string ret = "";

			foreach (var def in AliasDefinition)
				ret += def.ToString()+"\r\n";

			return ret.Trim();
		}
	}

	public class SpecialTypeResult : ResolveResult
	{
		/// <summary>
		/// In the case our base expression is a string literal, this field represented its char size - useful e.g. when wanting to show the string's element size
		/// </summary>
		[DefaultValue(StringType.None)]
		public StringType StringLiteralType { get; set; }

		/// <summary>
		/// The resolved type might be a special kind of variable. So, a variable type or return type e.g. can be an array, associative array, pointer or 'accessed' expression may even be a string
		/// </summary>
		[DefaultValue(SpecialType.None)]
		public SpecialType SpecialType { get; set; }
	}

	/// <summary>
	/// Encapsules basic types like int, bool, void etc.
	/// </summary>
	public class StaticTypeResult : ResolveResult
	{
		public ITypeDeclaration Type;

		public override string ToString()
		{
			return Type.ToString();
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
