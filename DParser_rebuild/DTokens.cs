using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace D_Parser
{

    public class DTokens
    {
        // ----- terminal classes -----
        public const int EOF = 0;
        public const int Identifier = 1;
        public const int Literal = 2;

        // ----- special character -----
        public const int Assign = 3;
        public const int Plus = 4;
        public const int Minus = 5;
        public const int Times = 6;
        public const int Div = 7;
        public const int Mod = 8;
        public const int Colon = 9;
        public const int DoubleColon = 10;
        public const int Semicolon = 11;
        public const int Question = 12;
        public const int Dollar = 13;
        public const int Comma = 14;
        public const int Dot = 15;
        public const int OpenCurlyBrace = 16;
        public const int CloseCurlyBrace = 17;
        public const int OpenSquareBracket = 18;
        public const int CloseSquareBracket = 19;
        public const int OpenParenthesis = 20;
        public const int CloseParenthesis = 21;
        public const int GreaterThan = 22;
        public const int LessThan = 23;
        public const int Not = 24;
        public const int LogicalAnd = 25;
        public const int LogicalOr = 26;
        public const int Tilde = 27;
        public const int BitwiseAnd = 28;
        public const int BitwiseOr = 29;
        public const int Xor = 30;
        public const int Increment = 31;
        public const int Decrement = 32;
        public const int Equal = 33;
        public const int NotEqual = 34;
        public const int GreaterEqual = 35;
        public const int LessEqual = 36;
        public const int ShiftLeft = 37;
        public const int PlusAssign = 38;
        public const int MinusAssign = 39;
        public const int TimesAssign = 40;
        public const int DivAssign = 41;
        public const int ModAssign = 42;
        public const int BitwiseAndAssign = 43;
        public const int BitwiseOrAssign = 44;
        public const int XorAssign = 45;
        public const int ShiftLeftAssign = 46;
        public const int TildeAssign = 47;
        public const int ShiftRightAssign = 48;
        public const int TripleRightAssign = 49;

        // ----- keywords -----
        public const int Align = 50;
        public const int Asm = 51;
        public const int Assert = 52;
        public const int Auto = 53;
        public const int Body = 54;
        public const int Bool = 55;
        public const int Break = 56;
        public const int Byte = 57;
        public const int Case = 58;
        public const int Cast = 59;
        public const int Catch = 60;
        public const int Cdouble = 61;
        public const int Cent = 62;
        public const int Cfloat = 63;
        public const int Char = 64;
        public const int Class = 65;
        public const int Const = 66;
        public const int Continue = 67;
        public const int Creal = 68;
        public const int Dchar = 69;
        public const int Debug = 70;
        public const int Default = 71;
        public const int Delegate = 72;
        public const int Delete = 73;
        public const int Deprecated = 74;
        public const int Do = 75;
        public const int Double = 76;
        public const int Else = 77;
        public const int Enum = 78;
        public const int Export = 79;
        public const int Extern = 80;
        public const int False = 81;
        public const int Final = 82;
        public const int Finally = 83;
        public const int Float = 84;
        public const int For = 85;
        public const int Foreach = 86;
        public const int Foreach_Reverse = 87;
        public const int Function = 88;
        public const int Goto = 89;
        public const int Idouble = 90;
        public const int If = 91;
        public const int Ifloat = 92;
        public const int Import = 93;
        public const int Immutable = 94;
        public const int In = 95;
        public const int InOut = 96;
        public const int Int = 97;
        public const int Interface = 98;
        public const int Invariant = 99;
        public const int Ireal = 100;
        public const int Is = 101;
        public const int Lazy = 102;
        public const int Long = 103;
        public const int Macro = 104;
        public const int Mixin = 105;
        public const int Module = 106;
        public const int New = 107;
        public const int Nothrow = 108;
        public const int Null = 109;
        public const int Out = 110;
        public const int Override = 111;
        public const int Package = 112;
        public const int Pragma = 113;
        public const int Private = 114;
        public const int Protected = 115;
        public const int Public = 116;
        public const int Pure = 117;
        public const int Real = 118;
        public const int Ref = 119;
        public const int Return = 120;
        public const int Scope = 121;
        public const int Shared = 122;
        public const int Short = 123;
        public const int Static = 124;
        public const int Struct = 125;
        public const int Super = 126;
        public const int Switch = 127;
        public const int Synchronized = 128;
        public const int Template = 129;
        public const int This = 130;
        public const int Throw = 131;
        public const int True = 132;
        public const int Try = 133;
        public const int Typedef = 134;
        public const int Typeid = 135;
        public const int Typeof = 136;
        public const int Ubyte = 137;
        public const int Ucent = 138;
        public const int Uint = 139;
        public const int Ulong = 140;
        public const int Union = 141;
        public const int Unittest = 142;
        public const int Ushort = 143;
        public const int Version = 144;
        public const int Void = 145;
        public const int Volatile = 146;
        public const int Wchar = 147;
        public const int While = 148;
        public const int With = 149;
        public const int __gshared = 150;
        public const int __thread = 151;
        public const int __traits = 152;
        public const int Abstract = 153;
        public const int Alias = 154;
        public const int PropertyAttribute = 155;
        public const int DisabledAttribute = 156;
        public const int SafeAttribute = 157;
        public const int SystemAttribute = 158;

        // Additional operators
        public const int PowAssign = 159; // ^^=
        public const int NotUnequalAssign = 160; // !<>=
        public const int NotUnequal = 161; // !<>
        public const int Unequal = 162; // <>
        public const int UnequalAssign = 163; // <>=
        public const int NotGreaterThan = 164; // !>
        public const int NotGreaterThanAssign = 165; // !>=
        public const int NotLessThan = 166; // !<
        public const int NotLessThanAssign = 167; // !<=
        public const int ShiftRight = 168; // >>
        public const int ShiftRightUnsigned = 169; // >>>
        public const int Pow = 170; // ^^

        public const int MaxToken = 171;
        static BitArray NewSet(params int[] values)
        {
            BitArray bitArray = new BitArray(MaxToken);
            foreach (int val in values)
            {
                bitArray[val] = true;
            }
            return bitArray;
        }

        public static BitArray FunctionAttribute = NewSet(Pure, Nothrow);
        public static BitArray MemberFunctionAttribute = NewSet(Const, Immutable, Shared, InOut, Pure, Nothrow);
        public static BitArray ParamModifiers = NewSet(In, Out, InOut, Ref, Lazy);
        public static BitArray ClassLike = NewSet(Class, Template, Interface, Struct, Union);
        public static BitArray BasicTypes = NewSet(Bool, Byte, Ubyte, Short, Ushort, Int, Uint, Long, Ulong, Char, Wchar, Dchar, Float, Double, Real, Ifloat, Idouble, Ireal, Cfloat, Cdouble, Creal, Void);
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
            TripleRightAssign,// >>>=
            PowAssign,
            LessEqual,
            GreaterEqual,
            NotUnequalAssign,
            UnequalAssign,
            NotGreaterThanAssign,
            NotLessThanAssign
            );
        public static BitArray TypeDeclarationKW = NewSet(Class, Interface, Struct, Template, Enum, Delegate, Function);
        public static BitArray RelationalOperators = NewSet(
            LessThan,
            LessEqual,
            GreaterThan,
            GreaterEqual,
            NotUnequalAssign, // !<>=
            NotUnequal, // !<>
            Unequal,
            UnequalAssign,
            NotGreaterThan,
            NotGreaterThanAssign,
            NotLessThan,
            NotLessThanAssign
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
            while (mods.Contains(Public))
                mods.Remove(Public);
            while (mods.Contains(Private))
                mods.Remove(Private);
            while (mods.Contains(Protected))
                mods.Remove(Protected);
            while (mods.Contains(Package))
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
    "@system",

    // Additional operators
        "^^=",
        "!<>=",
        "!<>",
        "<>",
        "<>=",
        "!>",
        "!>=",
        "!<",
        "!<=",
        ">>",
        ">>>",
        "^^"
		};
        public static string GetTokenString(int token)
        {
            if (token >= 0 && token < tokenList.Length)
            {
                return tokenList[token];
            }
            throw new System.NotSupportedException("Unknown token:" + token);
        }

        public static int GetTokenID(string token)
        {
            if (token == null || token.Length < 1) return -1;

            for (int i = 0; i < tokenList.Length; i++)
            {
                if (tokenList[i] == token) return i;
            }

            return -1;
        }

        public static string GetDescription(string token)
        {
            return GetDescription(GetTokenID(token));
        }

        public static string GetDescription(int token)
        {
            switch (token)
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
