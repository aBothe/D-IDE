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

	public class ResolveResult:IEnumerable<KeyValuePair<INode,IBlockNode>>
	{
		/// <summary>
		/// The expression that has been parsed
		/// </summary>
		public ITypeDeclaration ParsedDeclaration { get; set; }

		/// <summary>
		/// The keys are the resolved members or even types.
		/// In the case that those are variables or methods, the value part will be either the variable type or the method's return type.
		/// If the key is a class, the value can optionally be its base class.
		/// 
		/// Usually there should be only one resolved member or type.
		/// Only in the case that there are further members/types that are named equally, two or more items will be held by <see cref="ResolvedMembersAndTypes"/>
		/// </summary>
		public readonly List<KeyValuePair<INode, IBlockNode>> ResolvedMembersAndTypes = new List<KeyValuePair<INode, IBlockNode>>();

		public IEnumerable<INode> Members
		{
			get {
				foreach (var kv in ResolvedMembersAndTypes)
					yield return kv.Key;
			}
		}

		public void Add(INode member, IBlockNode typeDeclaration)
		{
			ResolvedMembersAndTypes.Add(new KeyValuePair<INode,IBlockNode>(member,typeDeclaration));
		}

		public void Add(IBlockNode type, IBlockNode baseClass)
		{
			ResolvedMembersAndTypes.Add(new KeyValuePair<INode, IBlockNode>(type, baseClass));
		}

		public int MemberCount
		{
			get { return ResolvedMembersAndTypes.Count; }
		}

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

		/// <summary>
		/// If the entire resolution took more than one level of type searching, this field represents the resolution base that was used to find the current items.
		/// </summary>
		public ResolveResult ResultBase { get; set; }

		public IEnumerator<KeyValuePair<INode, IBlockNode>> GetEnumerator()
		{
			return ResolvedMembersAndTypes.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
