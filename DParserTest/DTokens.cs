using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace D_Parser
{

	public partial class DTokens
	{
		static BitArray NewSet(params int[] values)
		{
			BitArray bitArray = new BitArray(MaxToken);
			foreach(int val in values)
			{
				bitArray[val] = true;
			}
			return bitArray;
		}

        public static BitArray MemberFunctionAttributes = NewSet(Const, Immutable, Shared, InOut);
		public static BitArray ParamModifiers = NewSet(In, Out, InOut, Ref);
		public static BitArray ClassLike = NewSet(Class, Template, Interface, Struct, Union);
		public static BitArray BasicTypes = NewSet(Auto, Bool, Byte        ,Ubyte        ,Short        ,Ushort        ,Int        ,Uint        ,Long        ,Ulong        ,Char        ,Wchar        ,Dchar        ,Float        ,Double        ,Real        ,Ifloat        ,Idouble        ,Ireal        ,Cfloat        ,Cdouble        ,Creal        ,Void);
		public static BitArray AssnStartOp = NewSet(Plus, Minus, Not, Tilde, Times);
		public static BitArray AssignOps = NewSet(
			Assign, // =
			PlusAssign, // +=
			MinusAssign, // -=
			TimesAssign, // *=
			DivAssign, // /=
			ModAssign, // %=
			BitwiseAndAssign, // &=
			BitwiseOrAssign, // |=
			XorAssign, // ^=
			TildeAssign, // ~=
			ShiftLeftAssign, // <<=
			ShiftRightAssign, // >>=
			TripleRightAssign// >>>=
			);
		public static BitArray TypeDeclarationKW = NewSet(Class, Interface, Struct, Template, Enum, Delegate, Function);
		public static BitArray Conditions = NewSet(
			Question, // bool?true:false
			LogicalOr, // ||
			LogicalAnd, // &&
			BitwiseOr, // |
			BitwiseAnd, // &
			Xor, // ^
			Equal, // ==
			NotEqual, // !=
			Is, // is
			//Not, // !
			LessThan, // <
			LessEqual, // <=
			GreaterThan, // >
			GreaterEqual // >=
			);
		public static BitArray VisModifiers = NewSet(Public, Protected, Private, Package);
		public static BitArray Modifiers = NewSet(
			In,
			Out,
			InOut,
			Ref,
			Static,
			Override,
			Const,
			Public,
			Private,
			Protected,
			Package,
			Export,
			Shared,
			Final,
			Invariant,
			Immutable,
			Pure,
			Deprecated,
			Scope,
			__gshared,
			Lazy, 
			Nothrow,
			PropertyAttribute,
			DisabledAttribute,
            SafeAttribute,
            SystemAttribute
            );
        public static BitArray Attributes = NewSet(
            PropertyAttribute,
            DisabledAttribute,
            SafeAttribute,
            SystemAttribute
            );


		public static bool ContainsVisMod(List<int> mods)
		{
			return
			mods.Contains(Public) ||
			mods.Contains(Private) ||
			mods.Contains(Package) ||
			mods.Contains(Protected);
		}

		public static void RemoveVisMod(List<int> mods)
		{
			while(mods.Contains(Public))
				mods.Remove(Public);
			while(mods.Contains(Private))
				mods.Remove(Private);
			while(mods.Contains(Protected))
				mods.Remove(Protected);
			while(mods.Contains(Package))
				mods.Remove(Package);
		}

		static string[] tokenList = new string[] {
			// ----- terminal classes -----
			"<EOF>",
			"<Identifier>",
			"<Literal>",
			// ----- special character -----
			"=",
			"+",
			"-",
			"*",
			"/",
			"%",
			":",
			"::",
			";",
			"?",
			"$",
			",",
			".",
			"{",
			"}",
			"[",
			"]",
			"(",
			")",
			">",
			"<",
			"!",
			"&&",
			"||",
			"~",
			"&",
			"|",
			"^",
			"++",
			"--",
			"==",
			"!=",
			">=",
			"<=",
			"<<",
			"+=",
			"-=",
			"*=",
			"/=",
			"%=",
			"&=",
			"|=",
			"^=",
			"<<=",
			"~=",
			">>=",
			">>>=",
			// ----- keywords -----
	"align",
	"asm",
	"assert",
	"auto",

	"body",
	"bool",
	"break",
	"byte",

	"case",
	"cast",
	"catch",
	"cdouble",
	"cent",
	"cfloat",
	"char",
	"class",
	"const",
	"continue",
	"creal",

	"dchar",
	"debug",
	"default",
	"delegate",
	"delete",
	"deprecated",
	"do",
	"double",

	"else",
	"enum",
	"export",
	"extern",

	"false",
	"final",
	"finally",
	"float",
	"for",
	"foreach",
	"foreach_reverse",
	"function",

	"goto",

	"idouble",
	"if",
	"ifloat",
	"import",
	"immutable",
	"in",
	"inout",
	"int",
	"interface",
	"invariant",
	"ireal",
	"is",

	"lazy",
	"long",

	"macro",
	"mixin",
	"module",

	"new",
	"nothrow",
	"null",

	"out",
	"override",

	"package",
	"pragma",
	"private",
	"protected",
	"public",
	"pure",

	"real",
	"ref",
	"return",

	"scope",
	"shared",
	"short",
	"static",
	"struct",
	"super",
	"switch",
	"synchronized",

	"template",
	"this",
	"throw",
	"true",
	"try",
	"typedef",
	"typeid",
	"typeof",

	"ubyte",
	"ucent",
	"uint",
	"ulong",
	"union",
	"unittest",
	"ushort",

	"version",
	"void",
	"volatile",

	"wchar",
	"while",
	"with",

	"__gshared",
	"__thread",
	"__traits",

	"abstract",
	"alias",

	"@property",
	"@disabled",
    "@safe",
    "@system"
		};
		public static string GetTokenString(int token)
		{
			if(token >= 0 && token < tokenList.Length)
			{
				return tokenList[token];
			}
			throw new System.NotSupportedException("Unknown token:" + token);
		}

		public static int GetTokenID(string token)
		{
			if(token == null || token.Length < 1) return -1;

			for(int i = 0; i < tokenList.Length; i++)
			{
				if(tokenList[i] == token) return i;
			}

			return -1;
		}

		public static string GetDescription(string token)
		{
			return GetDescription( GetTokenID(token));
		}

		public static string GetDescription(int token)
		{
			switch(token)
			{
				case Else:
				case If:
					return "if(a == b)\n{\n   foo();\n}\nelse if(a < b)\n{\n   ...\n}\nelse\n{\n   bar();\n}";
				case For:
					return "for(int i; i<500; i++)\n{\n   foo();\n}";
				case Foreach_Reverse:
				case Foreach: return
					"foreach(element; array)\n{\n   foo(element);\n}\n\nOr:\nforeach(element, index; array)\n{\n   foo(element);\n}";
				case While:
					return "while(a < b)\n{\n   foo();\n   a++;\n}";
				case Do:
					return "do\n{\n   foo();\na++;\n}\nwhile(a < b);";
				case Switch:
					return "switch(a)\n{\n   case 1:\n      foo();\n      break;\n   case 2:\n      bar();\n      break;\n   default:\n      break;\n}";
				default: return "D Keyword";
			}
		}
	}
}
