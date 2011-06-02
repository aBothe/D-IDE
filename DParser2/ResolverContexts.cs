using System;
using System.Collections.Generic;
using System.Text;
using D_Parser.Core;
using System.ComponentModel;

namespace D_Parser.Resolver
{
	public enum SpecialExpressionType
	{
		None,
		Array,
		AssociativeArray,
		Pointer,
		String
	}

	public enum StringType
	{
		None,
		utf8,
		utf16,
		utf32
	}

	public class ResolveResult
	{
		/// <summary>
		/// The expression that has been parsed
		/// </summary>
		public IExpression ParsedExpression { get; set; }

		/// <summary>
		/// The currently accessed member or, if the accessed node isn't a variable or method, the respective type definition (class, interface, struct etc.)
		/// </summary>
		public INode TargetMember { get; set; }

		/// <summary>
		/// If the member is a variable, this represents the origin of its type.
		/// If the member is a method, this represents the origin of the method's return type.
		/// </summary>
		public INode MemberTypeDefinition { get; set; }

		/// <summary>
		/// In the case our base expression is a string literal, this field represented its char size - useful e.g. when wanting to show the string's element size
		/// </summary>
		[DefaultValue(StringType.None)]
		public StringType StringLiteralType { get; set; }

		/// <summary>
		/// The resolved type might be a special kind of variable. So, a variable type or return type e.g. can be an array, associative array, pointer or 'accessed' expression may even be a string
		/// </summary>
		[DefaultValue(SpecialExpressionType.None)]
		public SpecialExpressionType SpecialType { get; set; }

		/// <summary>
		/// If the entire resolution took more than one level of type searching, this field represents that inner level.
		/// </summary>
		public ResolveResult ResultBase { get; set; }
	}
}
