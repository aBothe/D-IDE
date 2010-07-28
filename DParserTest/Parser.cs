
using System;
using System.Collections.Generic;

namespace D_Parser {



public class DTokens{
	public const int _EOF = 0;
	public const int _Identifier = 1;
	public const int _Integer = 2;
	public const int _FloatLiteral = 3;
	public const int _CharLiteral = 4;
	public const int _StringLiteral = 5;
	public const int _DivAss = 6;
	public const int _Dot = 7;
	public const int _DblDot = 8;
	public const int _TrplDot = 9;
	public const int _BitAnd = 10;
	public const int _BitAndAss = 11;
	public const int _And = 12;
	public const int _BitOr = 13;
	public const int _BitOrAss = 14;
	public const int _Or = 15;
	public const int _Minus = 16;
	public const int _MinusAss = 17;
	public const int _Decr = 18;
	public const int _Plus = 19;
	public const int _PlusAss = 20;
	public const int _Incr = 21;
	public const int _Lt = 22;
	public const int _LtAss = 23;
	public const int _BinLt = 24;
	public const int _BinLtAss = 25;
	public const int _Uneq = 26;
	public const int _UneqAss = 27;
	public const int _Gt = 28;
	public const int _GtAss = 29;
	public const int _BinGtAss = 30;
	public const int _BinGt2Ass = 31;
	public const int _BinGt = 32;
	public const int _BinGt2 = 33;
	public const int _Not = 34;
	public const int _NotAss = 35;
	public const int _NotUneq = 36;
	public const int _NotUneqAss = 37;
	public const int _NotLt = 38;
	public const int _NotLtAss = 39;
	public const int _NotGt = 40;
	public const int _NotGtAss = 41;
	public const int _OpenRound = 42;
	public const int _CloseRound = 43;
	public const int _OpenSq = 44;
	public const int _CloseSq = 45;
	public const int _OpenCurly = 46;
	public const int _CloseCurly = 47;
	public const int _QuestionMark = 48;
	public const int _Comma = 49;
	public const int _Semicolon = 50;
	public const int _Colon = 51;
	public const int _Dollar = 52;
	public const int _Assign = 53;
	public const int _Equals = 54;
	public const int _Times = 55;
	public const int _TimesAss = 56;
	public const int _Mod = 57;
	public const int _ModAss = 58;
	public const int _Pow = 59;
	public const int _PowAss = 60;
	public const int _Tilde = 61;
	public const int _TildeAss = 62;
	public const int _abstract = 63;
	public const int _alias = 64;
	public const int _align = 65;
	public const int _asm = 66;
	public const int _assert = 67;
	public const int _auto = 68;
	public const int _body = 69;
	public const int _bool = 70;
	public const int _break = 71;
	public const int _byte = 72;
	public const int _case = 73;
	public const int _cast = 74;
	public const int _catch = 75;
	public const int _cdouble = 76;
	public const int _cent = 77;
	public const int _cfloat = 78;
	public const int _char = 79;
	public const int _class = 80;
	public const int _const = 81;
	public const int _continue = 82;
	public const int _creal = 83;
	public const int _dchar = 84;
	public const int _debug = 85;
	public const int _default = 86;
	public const int _delegate = 87;
	public const int _delete = 88;
	public const int _deprecated = 89;
	public const int _do = 90;
	public const int _double = 91;
	public const int _else = 92;
	public const int _enum = 93;
	public const int _export = 94;
	public const int _extern = 95;
	public const int _false = 96;
	public const int _final = 97;
	public const int _finally = 98;
	public const int _float = 99;
	public const int _for = 100;
	public const int _foreach = 101;
	public const int _foreach_reverse = 102;
	public const int _function = 103;
	public const int _goto = 104;
	public const int _idouble = 105;
	public const int _if = 106;
	public const int _ifloat = 107;
	public const int _immutable = 108;
	public const int _import = 109;
	public const int _in = 110;
	public const int _inout = 111;
	public const int _int = 112;
	public const int _interface = 113;
	public const int _invariant = 114;
	public const int _ireal = 115;
	public const int _is = 116;
	public const int _lazy = 117;
	public const int _long = 118;
	public const int _macro = 119;
	public const int _mixin = 120;
	public const int _module = 121;
	public const int _new = 122;
	public const int _nothrow = 123;
	public const int _null = 124;
	public const int _out = 125;
	public const int _override = 126;
	public const int _package = 127;
	public const int _pragma = 128;
	public const int _private = 129;
	public const int _protected = 130;
	public const int _public = 131;
	public const int _pure = 132;
	public const int _real = 133;
	public const int _ref = 134;
	public const int _return = 135;
	public const int _scope = 136;
	public const int _shared = 137;
	public const int _short = 138;
	public const int _static = 139;
	public const int _struct = 140;
	public const int _super = 141;
	public const int _switch = 142;
	public const int _synchronized = 143;
	public const int _template = 144;
	public const int _this = 145;
	public const int _throw = 146;
	public const int _true = 147;
	public const int _try = 148;
	public const int _typedef = 149;
	public const int _typeid = 150;
	public const int _typeof = 151;
	public const int _ubyte = 152;
	public const int _ucent = 153;
	public const int _uint = 154;
	public const int _ulong = 155;
	public const int _union = 156;
	public const int _unittest = 157;
	public const int _ushort = 158;
	public const int _version = 159;
	public const int _void = 160;
	public const int _volatile = 161;
	public const int _wchar = 162;
	public const int _while = 163;
	public const int _with = 164;
	public const int maxT = 223;

}

public class DParser {
	const bool T = true;
	const bool x = false;
	const int minErrDist = 2;

	/// <summary>
	/// Encapsules whole document structure
	/// </summary>
	DNode doc;
	public DNode Document
        {
            get { return doc; }
        }
	public List<string> import;

	public DLexer lexer;
	public Errors errors = new Errors();

    DToken t
    {
        [System.Diagnostics.DebuggerStepThrough]
        get
        {
            return (DToken)lexer.CurrentToken;
        }
    }

    /// <summary>
    /// lookAhead token
    /// </summary>
    DToken la
    {
        [System.Diagnostics.DebuggerStepThrough]
        get
        {
            return (DToken)lexer.LookAhead;
        }
    }
	int errDist = minErrDist;

public List<string> Imports=new List<string>();
	public string Mod="";

/*--------------------------------------------------------------------------*/


	public DParser(DLexer lexer) {
		this.lexer = lexer;
	}

	void SynErr (int n) {
		if (errDist >= minErrDist) {errors.SynErr(la.line, la.col, n);

		//ErrorMsgs.Add(la.line.ToString()+";"+la.col.ToString()+" "+Errors.ErrMsg(n));
		}
		errDist = 0;
	}

	public void SemErr (string msg) {
		if (errDist >= minErrDist)
		{errors.SemErr(t.line, t.col, msg);
		//ErrorMsgs.Add(la.line.ToString()+";"+la.col.ToString()+" "+msg);
		}
		errDist = 0;
	}

	void Get () {
		lexer.NextToken();

	}

	void Expect (int n) {
		if (la.kind==n) Get(); else { SynErr(n); }
	}

	bool StartOf (int s) {
		return set[s, la.kind];
	}

	void ExpectWeak (int n, int follow) {
		if (la.kind == n) Get();
		else {
			SynErr(n);
			while (!StartOf(follow)) Get();
		}
	}


	bool WeakSeparator(int n, int syFol, int repFol) {
		int kind = la.kind;
		if (kind == n) {Get(); return true;}
		else if (StartOf(repFol)) {return false;}
		else {
			SynErr(n);
			while (!(set[syFol, kind] || set[repFol, kind] || set[0, kind])) {
				Get();
				kind = la.kind;
			}
			return StartOf(syFol);
		}
	}


	void D2() {
		while (StartOf(1)) {
			if (StartOf(2)) {
				DeclDef();
			} else if (StartOf(3)) {
				Statement();
			} else if (la.kind == 120) {
				TemplateMixinDeclaration();
			} else if (la.kind == 159) {
				VersionSpecification();
			} else {
				DebugSpecification();
			}
		}
	}

	void DeclDef() {
		if (la.kind == 121) {
			ModuleDeclaration();
		} else if (StartOf(4)) {
			AttributeSpecifier();
		} else if (la.kind == 109 || la.kind == 139) {
			ImportDeclaration();
		} else if (la.kind == 93) {
			EnumDeclaration();
		} else if (la.kind == 80) {
			ClassDeclaration();
		} else if (la.kind == 113) {
			InterfaceDeclaration();
		} else if (la.kind == 140 || la.kind == 156) {
			AggregateDeclaration();
		} else if (StartOf(5)) {
			Declaration();
		} else if (la.kind == 145) {
			Constructor();
		} else if (la.kind == 61) {
			Destructor();
		} else if (la.kind == 114) {
			Invariant();
		} else if (la.kind == 157) {
			UnitTest();
		} else if (la.kind == 139) {
			StaticConstructor();
		} else if (la.kind == 139) {
			StaticDestructor();
		} else if (la.kind == 137) {
			SharedStaticConstructor();
		} else if (la.kind == 137) {
			SharedStaticDestructor();
		} else if (la.kind == 85 || la.kind == 139 || la.kind == 159) {
			ConditionalDeclaration();
		} else if (la.kind == 139) {
			StaticAssert();
		} else if (la.kind == 144) {
			TemplateDeclaration();
		} else if (la.kind == 120) {
			TemplateMixin();
		} else if (la.kind == 120) {
			MixinDeclaration();
		} else if (la.kind == 50) {
			Get();
		} else SynErr(224);
	}

	void Statement() {
		if (la.kind == 50) {
			Get();
		} else if (StartOf(6)) {
			NonEmptyStatement();
		} else if (la.kind == 46) {
			ScopeBlockStatement();
		} else SynErr(225);
	}

	void TemplateMixinDeclaration() {
		Expect(120);
		Expect(144);
		TemplateIdentifier();
		Expect(42);
		TemplateParameterList();
		Expect(43);
		if (la.kind == 106) {
			Constraint();
		}
		Expect(46);
		while (StartOf(2)) {
			DeclDef();
		}
		Expect(47);
	}

	void VersionSpecification() {
		Expect(159);
		Expect(53);
		if (la.kind == 1) {
			Get();
		} else if (la.kind == 2) {
			Get();
		} else SynErr(226);
		Expect(50);
	}

	void DebugSpecification() {
		Expect(85);
		Expect(53);
		if (la.kind == 1) {
			Get();
		} else if (la.kind == 2) {
			Get();
		} else SynErr(227);
		Expect(50);
	}

	void IntegerLiteral() {
		Expect(2);
		if (StartOf(7)) {
			switch (la.kind) {
			case 165: {
				Get();
				break;
			}
			case 166: {
				Get();
				break;
			}
			case 167: {
				Get();
				break;
			}
			case 168: {
				Get();
				break;
			}
			case 169: {
				Get();
				break;
			}
			case 170: {
				Get();
				break;
			}
			case 171: {
				Get();
				break;
			}
			case 172: {
				Get();
				break;
			}
			case 173: {
				Get();
				break;
			}
			case 174: {
				Get();
				break;
			}
			case 175: {
				Get();
				break;
			}
			case 176: {
				Get();
				break;
			}
			}
		}
	}

	void Ident(out string name) {
		Expect(1);
		name = t.val; 
	}

	void ModuleDeclaration() {
		string n=""; 
		Expect(121);
		while (la.kind!=_Semicolon) {
			if (StartOf(8)) Get(); else SynErr(228);
			n+=t.val; 
		}
		Mod=n; 
		Expect(50);
	}

	void AttributeSpecifier() {
		Attribute();
		if (la.kind == 51) {
			Get();
		} else if (StartOf(9)) {
			DeclarationBlock();
		} else SynErr(229);
	}

	void ImportDeclaration() {
		if (la.kind == 139) {
			Get();
		}
		Expect(109);
		ImportList();
		Expect(50);
	}

	void EnumDeclaration() {
		Expect(93);
		if (la.kind == 1) {
			Get();
		}
		if (la.kind == 51) {
			Get();
			Type();
		}
		EnumBody();
	}

	void ClassDeclaration() {
		if (la.kind == 80) {
			Get();
			Expect(1);
			if (la.kind == 51) {
				BaseClassList();
			}
			ClassBody();
		} else if (la.kind == 80) {
			ClassTemplateDeclaration();
		} else SynErr(230);
	}

	void InterfaceDeclaration() {
		if (la.kind == 113) {
			Get();
			Expect(1);
			InterfaceBody();
		} else if (la.kind == 113) {
			InterfaceTemplateDeclaration();
		} else SynErr(231);
	}

	void AggregateDeclaration() {
		if (la.kind == 140 || la.kind == 156) {
			if (la.kind == 140) {
				Get();
			} else {
				Get();
			}
			Expect(1);
			if (la.kind == 46) {
				StructBody();
			} else if (la.kind == 50) {
				Get();
			} else SynErr(232);
		} else if (la.kind == 140) {
			StructTemplateDeclaration();
		} else if (la.kind == 156) {
			UnionTemplateDeclaration();
		} else SynErr(233);
	}

	void Declaration() {
		bool hasStorageAttr=false; 
		if (la.kind == 64) {
			Get();
		}
		while (StartOf(10)) {
			StorageClass();
			hasStorageAttr=true; 
		}
		if (hasStorageAttr && la.kind==_Identifier && lexer.Peek().kind==_Assign) {
			AutoDeclaration();
		} else if (StartOf(11)) {
			Decl();
		} else SynErr(234);
	}

	void Constructor() {
		string[] paramnames; 
		Expect(145);
		Parameters(out paramnames);
		FunctionBody();
	}

	void Destructor() {
		Expect(61);
		Expect(145);
		Expect(42);
		Expect(43);
		FunctionBody();
	}

	void Invariant() {
		Expect(114);
		Expect(42);
		Expect(43);
		BlockStatement();
	}

	void UnitTest() {
		Expect(157);
		FunctionBody();
	}

	void StaticConstructor() {
		Expect(139);
		Expect(145);
		Expect(42);
		Expect(43);
		FunctionBody();
	}

	void StaticDestructor() {
		Expect(139);
		Expect(61);
		Expect(145);
		Expect(42);
		Expect(43);
		FunctionBody();
	}

	void SharedStaticConstructor() {
		Expect(137);
		Expect(139);
		Expect(145);
		Expect(42);
		Expect(43);
		FunctionBody();
	}

	void SharedStaticDestructor() {
		Expect(137);
		Expect(139);
		Expect(61);
		Expect(145);
		Expect(42);
		Expect(43);
		FunctionBody();
	}

	void ConditionalDeclaration() {
		Condition();
		if (StartOf(12)) {
			CCDeclarationBlock();
			if (la.kind == 92) {
				Get();
				CCDeclarationBlock();
			}
		} else if (la.kind == 51) {
			Get();
			Declaration();
		} else SynErr(235);
	}

	void StaticAssert() {
		Expect(139);
		Expect(67);
		Expect(42);
		AssignExpression();
		if (la.kind == 49) {
			Get();
			AssignExpression();
		}
		Expect(43);
		Expect(50);
	}

	void TemplateDeclaration() {
		Expect(144);
		TemplateIdentifier();
		Expect(42);
		TemplateParameterList();
		Expect(43);
		if (la.kind == 106) {
			Constraint();
		}
		Expect(46);
		while (StartOf(2)) {
			DeclDef();
		}
		Expect(47);
	}

	void TemplateMixin() {
		Expect(120);
		TemplateIdentifier();
		if (la.kind == 34) {
			Get();
			Expect(42);
			TemplateArgumentList();
			Expect(43);
		}
		MixinIdentifier();
		Expect(50);
	}

	void MixinDeclaration() {
		Expect(120);
		Expect(42);
		AssignExpression();
		Expect(43);
		Expect(50);
	}

	void ModuleFullyQualifiedName(out string n) {
		Expect(1);
		n=t.val; 
		while (la.kind == 7) {
			Get();
			if(la.kind==1) n+="."+la.val; 
			Expect(1);
		}
	}

	void ImportList() {
		Import();
		if (la.kind == 49) {
			Get();
			ImportList();
		}
	}

	void Import() {
		string p; 
		if (la.kind==_Assign) {
			Expect(1);
			Expect(53);
		}
		ModuleFullyQualifiedName(out p);
		Imports.Add(p); 
		if (la.kind == 51) {
			Get();
			ImportBindList();
		}
	}

	void ImportBindList() {
		ImportBind();
		while (la.kind == 49) {
			Get();
			ImportBind();
		}
	}

	void ImportBind() {
		Expect(1);
		if (la.kind == 53) {
			Get();
			Expect(1);
		}
	}

	void AssignExpression() {
		ConditionalExpression();
		if (StartOf(13)) {
			switch (la.kind) {
			case 53: {
				Get();
				break;
			}
			case 20: {
				Get();
				break;
			}
			case 17: {
				Get();
				break;
			}
			case 56: {
				Get();
				break;
			}
			case 6: {
				Get();
				break;
			}
			case 58: {
				Get();
				break;
			}
			case 11: {
				Get();
				break;
			}
			case 14: {
				Get();
				break;
			}
			case 60: {
				Get();
				break;
			}
			case 62: {
				Get();
				break;
			}
			case 25: {
				Get();
				break;
			}
			case 30: {
				Get();
				break;
			}
			case 31: {
				Get();
				break;
			}
			case 190: {
				Get();
				break;
			}
			}
			AssignExpression();
		}
	}

	void StorageClass() {
		switch (la.kind) {
		case 63: {
			Get();
			break;
		}
		case 68: {
			Get();
			break;
		}
		case 81: {
			Get();
			break;
		}
		case 89: {
			Get();
			break;
		}
		case 95: {
			Get();
			break;
		}
		case 97: {
			Get();
			break;
		}
		case 108: {
			Get();
			break;
		}
		case 111: {
			Get();
			break;
		}
		case 137: {
			Get();
			break;
		}
		case 123: {
			Get();
			break;
		}
		case 126: {
			Get();
			break;
		}
		case 132: {
			Get();
			break;
		}
		case 136: {
			Get();
			break;
		}
		case 139: {
			Get();
			break;
		}
		case 143: {
			Get();
			break;
		}
		default: SynErr(236); break;
		}
	}

	void AutoDeclaration() {
		Expect(1);
		Expect(53);
		AssignExpression();
		Expect(50);
	}

	void Decl() {
		List<string> vars=new List<string>(); string tn=""; 
		BasicType();
		Declarator(out tn);
		vars.Add(tn); 
		if (la.kind == 53) {
			Get();
			Initializer();
		}
		if (la.kind == 49 || la.kind == 50) {
			while (la.kind == 49) {
				Get();
				Declarator(out tn);
				vars.Add(tn); 
				if (la.kind == 53) {
					Get();
					Initializer();
				}
			}
			Expect(50);
		} else if (StartOf(14)) {
			FunctionBody();
		} else SynErr(237);
	}

	void BasicType() {
		if (StartOf(15)) {
			BasicTypeX();
		} else if (la.kind == 7) {
			Get();
			IdentifierList();
		} else if (la.kind == 1) {
			IdentifierList();
		} else if (la.kind == 151) {
			Typeof();
		} else if (la.kind == 151) {
			Typeof();
			Expect(7);
			IdentifierList();
		} else if (StartOf(16)) {
			if (la.kind == 81) {
				Get();
			} else if (la.kind == 108) {
				Get();
			} else if (la.kind == 137) {
				Get();
			} else {
				Get();
			}
			Expect(42);
			Type();
			Expect(43);
		} else SynErr(238);
	}

	void Declarator(out string id) {
		if (StartOf(17)) {
			BasicType2();
		}
		Ident(out id);
		if (StartOf(18)) {
			DeclaratorSuffixes();
		}
	}

	void Initializer() {
		if (la.kind == 160) {
			VoidInitializer();
		} else if (StartOf(19)) {
			NonVoidInitializer();
		} else SynErr(239);
	}

	void FunctionBody() {
		if (la.kind == 46) {
			BlockStatement();
		} else if (la.kind == 69) {
			BodyStatement();
		} else if (la.kind == 110) {
			InStatement();
			BodyStatement();
		} else if (la.kind == 125) {
			OutStatement();
			BodyStatement();
		} else if (la.kind == 110) {
			InStatement();
			OutStatement();
			BodyStatement();
		} else if (la.kind == 125) {
			OutStatement();
			InStatement();
			BodyStatement();
		} else SynErr(240);
	}

	void BasicTypeX() {
		switch (la.kind) {
		case 70: {
			Get();
			break;
		}
		case 72: {
			Get();
			break;
		}
		case 152: {
			Get();
			break;
		}
		case 138: {
			Get();
			break;
		}
		case 158: {
			Get();
			break;
		}
		case 112: {
			Get();
			break;
		}
		case 154: {
			Get();
			break;
		}
		case 118: {
			Get();
			break;
		}
		case 155: {
			Get();
			break;
		}
		case 79: {
			Get();
			break;
		}
		case 162: {
			Get();
			break;
		}
		case 84: {
			Get();
			break;
		}
		case 99: {
			Get();
			break;
		}
		case 91: {
			Get();
			break;
		}
		case 133: {
			Get();
			break;
		}
		case 107: {
			Get();
			break;
		}
		case 105: {
			Get();
			break;
		}
		case 115: {
			Get();
			break;
		}
		case 78: {
			Get();
			break;
		}
		case 76: {
			Get();
			break;
		}
		case 83: {
			Get();
			break;
		}
		case 160: {
			Get();
			break;
		}
		default: SynErr(241); break;
		}
	}

	void IdentifierList() {
		if (la.kind == 1) {
			TemplateInstance();
		} else if (la.kind == 1) {
			Get();
		} else SynErr(242);
		if (la.kind == 7) {
			Get();
			IdentifierList();
		}
	}

	void Typeof() {
		Expect(151);
		Expect(42);
		if (StartOf(19)) {
			Expression();
		} else if (la.kind == 135) {
			Get();
		} else SynErr(243);
		Expect(43);
	}

	void Type() {
		BasicType();
		if (StartOf(20)) {
			Declarator2();
		}
	}

	void BasicType2() {
		if (la.kind == 55) {
			Get();
		} else if (la.kind == 44) {
			Get();
			if (StartOf(21)) {
				if (StartOf(19)) {
					AssignExpression();
					if (la.kind == 8) {
						Get();
						AssignExpression();
					}
				}
			} else if (StartOf(11)) {
				Type();
			} else SynErr(244);
			Expect(45);
		} else if (la.kind == 87 || la.kind == 103) {
			string[] paramnames; 
			if (la.kind == 87) {
				Get();
			} else {
				Get();
			}
			Parameters(out paramnames);
			if (la.kind == 123 || la.kind == 132) {
				FunctionAttributes();
			}
		} else SynErr(245);
	}

	void Parameters(out string[] ParamNames) {
		List<string> n=new List<string>(); 
		Expect(42);
		if (StartOf(22)) {
			ParameterList(ref n);
		}
		Expect(43);
		ParamNames=n.ToArray(); 
	}

	void FunctionAttributes() {
		FunctionAttribute();
		if (la.kind == 123 || la.kind == 132) {
			FunctionAttributes();
		}
	}

	void DeclaratorSuffixes() {
		DeclaratorSuffix();
		if (StartOf(18)) {
			DeclaratorSuffixes();
		}
	}

	void DeclaratorSuffix() {
		if (la.kind == 44) {
			Get();
			if (StartOf(19)) {
				if (StartOf(19)) {
					AssignExpression();
				} else {
					Type();
				}
			}
			Expect(45);
		} else if (StartOf(23)) {
			string[] paramnames; 
			if (StartOf(24)) {
				TemplateParameterList();
			}
			Parameters(out paramnames);
			if (StartOf(25)) {
				MemberFunctionAttributes();
			}
		} else SynErr(246);
	}

	void TemplateParameterList() {
		TemplateParameter();
		if (la.kind == 49) {
			Get();
			TemplateParameterList();
		}
	}

	void MemberFunctionAttributes() {
		MemberFunctionAttribute();
		if (StartOf(25)) {
			MemberFunctionAttributes();
		}
	}

	void TemplateInstance() {
		TemplateIdentifier();
		Expect(34);
		if (la.kind == 42) {
			Get();
			TemplateArgumentList();
			Expect(43);
		} else if (StartOf(26)) {
			TemplateSingleArgument();
		} else SynErr(247);
	}

	void Declarator2() {
		if (StartOf(17)) {
			BasicType2();
			if (StartOf(20)) {
				Declarator2();
			}
		} else if (la.kind == 42) {
			Get();
			Declarator2();
			Expect(43);
			if (StartOf(18)) {
				DeclaratorSuffixes();
			}
		} else SynErr(248);
	}

	void ParameterList(ref List<string> paramnames ) {
		string n; 
		if (la.kind == 9) {
			Get();
		} else if (StartOf(27)) {
			Parameter(out n);
			paramnames.Add(n); 
			if (la.kind == 9) {
				Get();
			} else if (la.kind == 43 || la.kind == 49) {
				if (la.kind == 49) {
					Get();
					ParameterList(ref paramnames);
				}
			} else SynErr(249);
		} else SynErr(250);
	}

	void Parameter(out string name) {
		if (StartOf(28)) {
			InOut();
		}
		Declarator(out name);
		if (la.kind == 53) {
			Get();
			DefaultInitializerExpression();
		}
	}

	void InOut() {
		if (la.kind == 110) {
			Get();
		} else if (la.kind == 125) {
			Get();
		} else if (la.kind == 134) {
			Get();
		} else if (la.kind == 117) {
			Get();
		} else SynErr(251);
	}

	void DefaultInitializerExpression() {
		if (StartOf(19)) {
			AssignExpression();
		} else if (la.kind == 177) {
			Get();
		} else if (la.kind == 178) {
			Get();
		} else SynErr(252);
	}

	void FunctionAttribute() {
		if (la.kind == 123) {
			Get();
		} else if (la.kind == 132) {
			Get();
		} else SynErr(253);
	}

	void MemberFunctionAttribute() {
		if (la.kind == 81) {
			Get();
		} else if (la.kind == 108) {
			Get();
		} else if (la.kind == 111) {
			Get();
		} else if (la.kind == 137) {
			Get();
		} else if (la.kind == 123 || la.kind == 132) {
			FunctionAttribute();
		} else SynErr(254);
	}

	void VoidInitializer() {
		Expect(160);
	}

	void NonVoidInitializer() {
		if (StartOf(19)) {
			AssignExpression();
		} else if (la.kind == 44) {
			ArrayInitializer();
		} else if (la.kind == 46) {
			StructInitializer();
		} else SynErr(255);
	}

	void ArrayInitializer() {
		Expect(44);
		if (StartOf(19)) {
			ArrayMemberInitializations();
		}
		Expect(45);
	}

	void StructInitializer() {
		Expect(46);
		if (StartOf(19)) {
			StructMemberInitializers();
		}
		Expect(47);
	}

	void ArrayMemberInitializations() {
		ArrayMemberInitialization();
		if (la.kind == 49) {
			Get();
			ArrayMemberInitializations();
		}
	}

	void ArrayMemberInitialization() {
		if (StartOf(19)) {
			NonVoidInitializer();
		} else if (StartOf(19)) {
			AssignExpression();
			Expect(51);
			NonVoidInitializer();
		} else SynErr(256);
	}

	void StructMemberInitializers() {
		StructMemberInitializer();
		if (la.kind == 49) {
			Get();
			StructMemberInitializers();
		}
	}

	void StructMemberInitializer() {
		if (StartOf(19)) {
			NonVoidInitializer();
		} else if (la.kind == 1) {
			Get();
			Expect(51);
			NonVoidInitializer();
		} else SynErr(257);
	}

	void Expression() {
		AssignExpression();
		if (la.kind == 49) {
			Get();
			Expression();
		}
	}

	void Attribute() {
		switch (la.kind) {
		case 95: {
			LinkageAttribute();
			break;
		}
		case 65: {
			AlignAttribute();
			break;
		}
		case 128: {
			Pragma();
			break;
		}
		case 89: {
			Get();
			break;
		}
		case 94: case 127: case 129: case 130: case 131: {
			ProtectionAttribute();
			break;
		}
		case 139: {
			Get();
			break;
		}
		case 97: {
			Get();
			break;
		}
		case 126: {
			Get();
			break;
		}
		case 63: {
			Get();
			break;
		}
		case 81: {
			Get();
			break;
		}
		case 68: {
			Get();
			break;
		}
		case 136: {
			Get();
			break;
		}
		case 179: {
			Get();
			break;
		}
		case 137: {
			Get();
			break;
		}
		case 108: {
			Get();
			break;
		}
		case 111: {
			Get();
			break;
		}
		case 180: {
			Get();
			if (la.kind == 181) {
				Get();
			} else if (la.kind == 182) {
				Get();
			} else if (la.kind == 183) {
				Get();
			} else SynErr(258);
			break;
		}
		default: SynErr(259); break;
		}
	}

	void DeclarationBlock() {
		if (StartOf(2)) {
			DeclDef();
		} else if (la.kind == 46) {
			Get();
			while (StartOf(2)) {
				DeclDef();
			}
			Expect(47);
		} else SynErr(260);
	}

	void LinkageAttribute() {
		Expect(95);
		if (la.kind == 42) {
			Get();
			LinkageType();
			Expect(43);
		}
	}

	void AlignAttribute() {
		Expect(65);
		if (la.kind == 42) {
			Get();
			Expect(2);
			Expect(43);
		}
	}

	void Pragma() {
		Expect(128);
		Expect(42);
		Expect(1);
		if (la.kind == 49) {
			Get();
			ArgumentList();
		}
		Expect(43);
	}

	void ProtectionAttribute() {
		if (la.kind == 129) {
			Get();
		} else if (la.kind == 127) {
			Get();
		} else if (la.kind == 130) {
			Get();
		} else if (la.kind == 131) {
			Get();
		} else if (la.kind == 94) {
			Get();
		} else SynErr(261);
	}

	void LinkageType() {
		switch (la.kind) {
		case 184: {
			Get();
			break;
		}
		case 185: {
			Get();
			break;
		}
		case 186: {
			Get();
			break;
		}
		case 187: {
			Get();
			break;
		}
		case 188: {
			Get();
			break;
		}
		case 189: {
			Get();
			break;
		}
		default: SynErr(262); break;
		}
	}

	void ArgumentList() {
		AssignExpression();
		if (la.kind == 49) {
			Get();
			ArgumentList();
		}
	}

	void ConditionalExpression() {
		OrOrExpression();
		if (la.kind == 48) {
			Get();
			Expression();
			Expect(51);
			ConditionalExpression();
		}
	}

	void OrOrExpression() {
		if (StartOf(19)) {
			OrOrExpression();
			Expect(15);
		}
		AndAndExpression();
	}

	void AndAndExpression() {
		if (StartOf(19)) {
			AndAndExpression();
			Expect(12);
		}
		OrExpression();
	}

	void OrExpression() {
		if (StartOf(19)) {
			OrExpression();
			Expect(13);
		}
		XorExpression();
	}

	void XorExpression() {
		if (StartOf(19)) {
			XorExpression();
			Expect(59);
		}
		AndExpression();
	}

	void AndExpression() {
		if (StartOf(19)) {
			AndExpression();
			Expect(10);
		}
		CmpExpression();
	}

	void CmpExpression() {
		if (StartOf(19)) {
			ShiftExpression();
		} else if (StartOf(19)) {
			EqualExpression();
		} else if (StartOf(19)) {
			IdentityExpression();
		} else if (StartOf(19)) {
			RelExpression();
		} else if (StartOf(19)) {
			InExpression();
		} else SynErr(263);
	}

	void ShiftExpression() {
		if (StartOf(19)) {
			AddExpression();
		} else if (StartOf(19)) {
			ShiftExpression();
			if (la.kind == 24) {
				Get();
			} else if (la.kind == 32) {
				Get();
			} else if (la.kind == 33) {
				Get();
			} else SynErr(264);
			AddExpression();
		} else SynErr(265);
	}

	void EqualExpression() {
		ShiftExpression();
		if (la.kind == 35) {
			Get();
		} else if (la.kind == 54) {
			Get();
		} else SynErr(266);
		ShiftExpression();
	}

	void IdentityExpression() {
		ShiftExpression();
		if (la.kind == 34) {
			Get();
		}
		Expect(116);
		ShiftExpression();
	}

	void RelExpression() {
		ShiftExpression();
		switch (la.kind) {
		case 22: {
			Get();
			break;
		}
		case 23: {
			Get();
			break;
		}
		case 28: {
			Get();
			break;
		}
		case 29: {
			Get();
			break;
		}
		case 37: {
			Get();
			break;
		}
		case 36: {
			Get();
			break;
		}
		case 26: {
			Get();
			break;
		}
		case 27: {
			Get();
			break;
		}
		case 40: {
			Get();
			break;
		}
		case 41: {
			Get();
			break;
		}
		case 38: {
			Get();
			break;
		}
		case 39: {
			Get();
			break;
		}
		default: SynErr(267); break;
		}
		ShiftExpression();
	}

	void InExpression() {
		ShiftExpression();
		if (la.kind == 34) {
			Get();
		}
		Expect(110);
		ShiftExpression();
	}

	void AddExpression() {
		if (StartOf(19)) {
			MulExpression();
		} else if (StartOf(19)) {
			AddExpression();
			if (la.kind == 19) {
				Get();
			} else if (la.kind == 16) {
				Get();
			} else SynErr(268);
			MulExpression();
		} else if (StartOf(19)) {
			CatExpression();
		} else SynErr(269);
	}

	void MulExpression() {
		if (StartOf(19)) {
			PowExpression();
		} else if (StartOf(19)) {
			MulExpression();
			if (la.kind == 55) {
				Get();
			} else if (la.kind == 191) {
				Get();
			} else if (la.kind == 57) {
				Get();
			} else SynErr(270);
			PowExpression();
		} else SynErr(271);
	}

	void CatExpression() {
		AddExpression();
		Expect(61);
		MulExpression();
	}

	void PowExpression() {
		UnaryExpression();
		if (la.kind == 192) {
			Get();
			PowExpression();
		}
	}

	void UnaryExpression() {
		if (StartOf(29)) {
			PostfixExpression();
		} else if (StartOf(30)) {
			switch (la.kind) {
			case 10: {
				Get();
				break;
			}
			case 21: {
				Get();
				break;
			}
			case 18: {
				Get();
				break;
			}
			case 55: {
				Get();
				break;
			}
			case 16: {
				Get();
				break;
			}
			case 19: {
				Get();
				break;
			}
			case 34: {
				Get();
				break;
			}
			case 61: {
				Get();
				break;
			}
			}
			UnaryExpression();
		} else if (la.kind == 42) {
			Get();
			Type();
			Expect(43);
			Expect(7);
			Expect(1);
		} else if (la.kind == 122) {
			NewExpression();
		} else if (la.kind == 88) {
			DeleteExpression();
		} else if (la.kind == 74) {
			CastExpression();
		} else if (la.kind == 122) {
			NewAnonClassExpression();
		} else SynErr(272);
	}

	void PostfixExpression() {
		if (StartOf(29)) {
			PrimaryExpression();
		} else if (StartOf(29)) {
			PostfixExpression();
			Expect(7);
			if (la.kind == 1) {
				Get();
			} else if (la.kind == 122) {
				NewExpression();
			} else SynErr(273);
		} else if (StartOf(29)) {
			PostfixExpression();
			if (la.kind == 21) {
				Get();
			} else if (la.kind == 18) {
				Get();
			} else SynErr(274);
		} else if (StartOf(29)) {
			PostfixExpression();
			Expect(42);
			if (StartOf(19)) {
				ArgumentList();
			}
			Expect(43);
		} else if (StartOf(29)) {
			IndexExpression();
		} else if (StartOf(29)) {
			SliceExpression();
		} else SynErr(275);
	}

	void NewExpression() {
		if (la.kind == 122) {
			NewArguments();
			Type();
			if (la.kind == 42 || la.kind == 44) {
				if (la.kind == 44) {
					Get();
					AssignExpression();
					Expect(45);
				} else {
					Get();
					ArgumentList();
					Expect(43);
				}
			}
		} else if (la.kind == 122) {
			NewArguments();
			ClassArguments();
			if (la.kind == 51) {
				BaseClassList();
			}
			Expect(46);
			while (StartOf(2)) {
				DeclDef();
			}
			Expect(47);
		} else SynErr(276);
	}

	void DeleteExpression() {
		Expect(88);
		UnaryExpression();
	}

	void CastExpression() {
		Expect(74);
		Expect(42);
		Type();
		Expect(43);
		UnaryExpression();
	}

	void NewAnonClassExpression() {
		Expect(122);
		if (la.kind == 42) {
			PerenArgumentList();
		}
		Expect(80);
		if (la.kind == 42) {
			PerenArgumentList();
		}
		if (StartOf(31)) {
			SuperClass();
		}
		if (StartOf(31)) {
			InterfaceClasses();
		}
		ClassBody();
	}

	void NewArguments() {
		Expect(122);
		if (la.kind == 42) {
			Get();
			if (StartOf(19)) {
				ArgumentList();
			}
			Expect(43);
		}
	}

	void ClassArguments() {
		Expect(80);
		if (la.kind == 42) {
			Get();
			if (StartOf(19)) {
				ArgumentList();
			}
			Expect(43);
		}
	}

	void BaseClassList() {
		Expect(51);
		if (StartOf(31)) {
			SuperClass();
			if (la.kind == 49) {
				Get();
				InterfaceClasses();
			}
		} else if (StartOf(31)) {
			InterfaceClass();
		} else SynErr(277);
	}

	void PrimaryExpression() {
		if (la.kind == 1 || la.kind == 7) {
			if (la.kind == 7) {
				Get();
			}
			Expect(1);
		} else if (la.kind == 1) {
			TemplateInstance();
		} else if (la.kind == 145) {
			Get();
		} else if (la.kind == 141) {
			Get();
		} else if (la.kind == 124) {
			Get();
		} else if (la.kind == 147) {
			Get();
		} else if (la.kind == 96) {
			Get();
		} else if (la.kind == 52) {
			Get();
		} else if (la.kind == 177) {
			Get();
		} else if (la.kind == 178) {
			Get();
		} else if (la.kind == 2) {
			Get();
		} else if (la.kind == 3) {
			Get();
		} else if (la.kind == 4) {
			Get();
		} else if (la.kind == 5) {
			StringLiterals();
		} else if (la.kind == 44) {
			ArrayLiteral();
		} else if (la.kind == 44) {
			AssocArrayLiteral();
		} else if (StartOf(32)) {
			FunctionLiteral();
		} else if (la.kind == 67) {
			AssertExpression();
		} else if (la.kind == 120) {
			MixinExpression();
		} else if (la.kind == 109) {
			ImportExpression();
		} else if (StartOf(11)) {
			BasicType();
			Expect(7);
			Expect(1);
		} else if (la.kind == 151) {
			Typeof();
		} else if (la.kind == 150) {
			TypeidExpression();
		} else if (la.kind == 116) {
			IsExpression();
		} else if (la.kind == 42) {
			Get();
			Expression();
			Expect(43);
		} else if (la.kind == 196) {
			TraitsExpression();
		} else SynErr(278);
	}

	void IndexExpression() {
		PostfixExpression();
		Expect(44);
		ArgumentList();
		Expect(45);
	}

	void SliceExpression() {
		PostfixExpression();
		Expect(44);
		if (StartOf(19)) {
			AssignExpression();
			Expect(8);
			AssignExpression();
		}
		Expect(45);
	}

	void StringLiterals() {
		if (la.kind == 5) {
			StringLiterals();
		}
		Expect(5);
	}

	void ArrayLiteral() {
		Expect(44);
		ArgumentList();
		Expect(45);
	}

	void AssocArrayLiteral() {
		Expect(44);
		KeyValuePairs();
		Expect(45);
	}

	void FunctionLiteral() {
		if (la.kind == 87 || la.kind == 103) {
			if (la.kind == 103) {
				Get();
			} else {
				Get();
			}
			if (StartOf(11)) {
				Type();
			}
		}
		if (la.kind == 42) {
			ParameterAttributes();
		}
		FunctionBody();
	}

	void AssertExpression() {
		Expect(67);
		Expect(42);
		AssignExpression();
		if (la.kind == 49) {
			Get();
			AssignExpression();
		}
		Expect(43);
	}

	void MixinExpression() {
		Expect(120);
		Expect(42);
		AssignExpression();
		Expect(43);
	}

	void ImportExpression() {
		Expect(109);
		Expect(42);
		AssignExpression();
		Expect(43);
	}

	void TypeidExpression() {
		Expect(150);
		Expect(42);
		if (StartOf(11)) {
			Type();
		} else if (StartOf(19)) {
			Expression();
		} else SynErr(279);
		Expect(43);
	}

	void IsExpression() {
		Expect(116);
		Expect(42);
		Type();
		if (la.kind == 1) {
			Get();
		}
		if (la.kind == 51 || la.kind == 54) {
			if (la.kind == 51) {
				Get();
			} else {
				Get();
			}
			TypeSpecialization();
		}
		if (la.kind == 49) {
			Get();
			TemplateParameterList();
		}
		Expect(43);
	}

	void TraitsExpression() {
		Expect(196);
		Expect(42);
		TraitsKeyword();
		Expect(49);
		TraitsArgument();
		while (la.kind == 49) {
			Get();
			TraitsArgument();
		}
		Expect(43);
	}

	void KeyValuePairs() {
		KeyValuePair();
		if (la.kind == 49) {
			Get();
			KeyValuePairs();
		}
	}

	void KeyValuePair() {
		KeyExpression();
		Expect(51);
		ValueExpression();
	}

	void KeyExpression() {
		ConditionalExpression();
	}

	void ValueExpression() {
		ConditionalExpression();
	}

	void ParameterAttributes() {
		string[] n; 
		Parameters(out n);
		if (la.kind == 123 || la.kind == 132) {
			FunctionAttributes();
		}
	}

	void TypeSpecialization() {
		if (StartOf(11)) {
			Type();
		} else if (la.kind == 140) {
			Get();
		} else if (la.kind == 156) {
			Get();
		} else if (la.kind == 80) {
			Get();
		} else if (la.kind == 113) {
			Get();
		} else if (la.kind == 93) {
			Get();
		} else if (la.kind == 103) {
			Get();
		} else if (la.kind == 87) {
			Get();
		} else if (la.kind == 141) {
			Get();
		} else if (la.kind == 81) {
			Get();
		} else if (la.kind == 108) {
			Get();
		} else if (la.kind == 111) {
			Get();
		} else if (la.kind == 137) {
			Get();
		} else if (la.kind == 135) {
			Get();
		} else SynErr(280);
	}

	void NonEmptyStatement() {
		if (la.kind == 1) {
			LabeledStatement();
		} else if (StartOf(19)) {
			ExpressionStatement();
		} else if (StartOf(5)) {
			DeclarationStatement();
		} else if (la.kind == 106) {
			IfStatement();
		} else if (la.kind == 163) {
			WhileStatement();
		} else if (la.kind == 90) {
			DoStatement();
		} else if (la.kind == 100) {
			ForStatement();
		} else if (la.kind == 101 || la.kind == 102) {
			ForeachStatement();
		} else if (la.kind == 142) {
			SwitchStatement();
		} else if (la.kind == 97) {
			FinalSwitchStatement();
		} else if (la.kind == 73) {
			CaseStatement();
		} else if (la.kind == 73) {
			CaseRangeStatement();
		} else if (la.kind == 86) {
			DefaultStatement();
		} else if (la.kind == 82) {
			ContinueStatement();
		} else if (la.kind == 71) {
			BreakStatement();
		} else if (la.kind == 135) {
			ReturnStatement();
		} else if (la.kind == 104) {
			GotoStatement();
		} else if (la.kind == 164) {
			WithStatement();
		} else if (la.kind == 143) {
			SynchronizedStatement();
		} else if (la.kind == 148) {
			TryStatement();
		} else if (la.kind == 136) {
			ScopeGuardStatement();
		} else if (la.kind == 146) {
			ThrowStatement();
		} else if (la.kind == 66) {
			AsmStatement();
		} else if (la.kind == 128) {
			PragmaStatement();
		} else if (la.kind == 120) {
			MixinStatement();
		} else if (la.kind == 101 || la.kind == 102) {
			ForeachRangeStatement();
		} else if (la.kind == 85 || la.kind == 139 || la.kind == 159) {
			ConditionalStatement();
		} else if (la.kind == 139) {
			StaticAssert();
		} else if (la.kind == 120) {
			TemplateMixin();
		} else SynErr(281);
	}

	void ScopeBlockStatement() {
		BlockStatement();
	}

	void NoScopeNonEmptyStatement() {
		if (StartOf(6)) {
			NonEmptyStatement();
		} else if (la.kind == 46) {
			BlockStatement();
		} else SynErr(282);
	}

	void BlockStatement() {
		Expect(46);
		while (StartOf(3)) {
			Statement();
		}
		Expect(47);
	}

	void NoScopeStatement() {
		if (la.kind == 50) {
			Get();
		} else if (StartOf(6)) {
			NonEmptyStatement();
		} else if (la.kind == 46) {
			BlockStatement();
		} else SynErr(283);
	}

	void NonEmptyOrScopeBlockStatement() {
		if (StartOf(6)) {
			NonEmptyStatement();
		} else if (la.kind == 46) {
			ScopeBlockStatement();
		} else SynErr(284);
	}

	void LabeledStatement() {
		Expect(1);
		Expect(51);
		NoScopeStatement();
	}

	void ExpressionStatement() {
		Expression();
		Expect(50);
	}

	void DeclarationStatement() {
		Declaration();
	}

	void IfStatement() {
		Expect(106);
		Expect(42);
		IfCondition();
		Expect(43);
		ScopeStatement();
		if (la.kind == 92) {
			Get();
			ScopeStatement();
		}
	}

	void WhileStatement() {
		Expect(163);
		Expect(42);
		Expression();
		Expect(43);
		ScopeStatement();
	}

	void DoStatement() {
		Expect(90);
		ScopeStatement();
		Expect(163);
		Expect(42);
		Expression();
		Expect(43);
	}

	void ForStatement() {
		Expect(100);
		Expect(42);
		Initialize();
		if (StartOf(19)) {
			Expression();
		}
		Expect(50);
		if (StartOf(19)) {
			Expression();
		}
		Expect(43);
		ScopeStatement();
	}

	void ForeachStatement() {
		Foreach();
		Expect(42);
		ForeachTypeList();
		Expect(50);
		Expression();
		Expect(43);
		NoScopeNonEmptyStatement();
	}

	void SwitchStatement() {
		Expect(142);
		Expect(42);
		Expression();
		Expect(43);
		ScopeStatement();
	}

	void FinalSwitchStatement() {
		Expect(97);
		Expect(142);
		Expect(42);
		Expression();
		Expect(43);
		ScopeStatement();
	}

	void CaseStatement() {
		Expect(73);
		ArgumentList();
		Expect(51);
		Statement();
	}

	void CaseRangeStatement() {
		Expect(73);
		FirstExp();
		Expect(51);
		Expect(8);
		Expect(73);
		LastExp();
		Expect(51);
		Statement();
	}

	void DefaultStatement() {
		Expect(86);
		Expect(51);
		Statement();
	}

	void ContinueStatement() {
		Expect(82);
		if (la.kind == 1) {
			Get();
		}
		Expect(50);
	}

	void BreakStatement() {
		Expect(71);
		if (la.kind == 1) {
			Get();
		}
		Expect(50);
	}

	void ReturnStatement() {
		Expect(135);
		if (StartOf(19)) {
			Expression();
		}
		Expect(50);
	}

	void GotoStatement() {
		Expect(104);
		if (la.kind == 1) {
			Get();
		} else if (la.kind == 86) {
			Get();
		} else if (la.kind == 73) {
			Get();
			if (StartOf(19)) {
				Expression();
			}
		} else SynErr(285);
		Expect(50);
	}

	void WithStatement() {
		Expect(164);
		Expect(42);
		if (StartOf(19)) {
			Expression();
		} else if (la.kind == 1 || la.kind == 7) {
			Symbol();
		} else if (la.kind == 1) {
			TemplateInstance();
		} else SynErr(286);
		Expect(43);
		ScopeStatement();
	}

	void SynchronizedStatement() {
		Expect(143);
		if (la.kind == 42) {
			Get();
			Expression();
			Expect(43);
		}
		ScopeStatement();
	}

	void TryStatement() {
		Expect(148);
		ScopeStatement();
		if (la.kind == 75 || la.kind == 98) {
			Catches();
		}
		if (la.kind == 98) {
			FinallyStatement();
		}
	}

	void ScopeGuardStatement() {
		Expect(136);
		Expect(42);
		if (la.kind == 193) {
			Get();
		} else if (la.kind == 194) {
			Get();
		} else if (la.kind == 195) {
			Get();
		} else SynErr(287);
		Expect(43);
		NonEmptyOrScopeBlockStatement();
	}

	void ThrowStatement() {
		Expect(146);
		Expression();
		Expect(50);
	}

	void AsmStatement() {
		Expect(66);
		Expect(46);
		while (StartOf(33)) {
			switch (la.kind) {
			case 1: {
				Get();
				break;
			}
			case 2: {
				IntegerLiteral();
				break;
			}
			case 3: {
				Get();
				break;
			}
			case 5: {
				Get();
				break;
			}
			case 4: {
				Get();
				break;
			}
			case 49: {
				Get();
				break;
			}
			case 50: {
				Get();
				break;
			}
			}
		}
		Expect(47);
	}

	void PragmaStatement() {
		Pragma();
		NoScopeStatement();
	}

	void MixinStatement() {
		Expect(120);
		Expect(42);
		AssignExpression();
		Expect(43);
		Expect(50);
	}

	void ForeachRangeStatement() {
		Foreach();
		Expect(42);
		ForeachType();
		Expect(50);
		Expression();
		Expect(8);
		Expression();
		Expect(43);
		ScopeStatement();
	}

	void ConditionalStatement() {
		Condition();
		NoScopeNonEmptyStatement();
		if (la.kind == 92) {
			Get();
			NoScopeNonEmptyStatement();
		}
	}

	void ScopeStatement() {
		if (StartOf(6)) {
			NonEmptyStatement();
		} else if (la.kind == 46) {
			BlockStatement();
		} else SynErr(288);
	}

	void IfCondition() {
		string id; 
		if (StartOf(19)) {
			Expression();
		} else if (la.kind == 68) {
			Get();
			Ident(out id);
			Expect(53);
			Expression();
		} else if (StartOf(34)) {
			Declarator(out id);
			Expect(53);
			Expression();
		} else SynErr(289);
	}

	void Initialize() {
		if (la.kind == 50) {
			Get();
		} else if (StartOf(6)) {
			NoScopeNonEmptyStatement();
		} else SynErr(290);
	}

	void Foreach() {
		if (la.kind == 101) {
			Get();
		} else if (la.kind == 102) {
			Get();
		} else SynErr(291);
	}

	void ForeachTypeList() {
		ForeachType();
		if (la.kind == 49) {
			Get();
			ForeachTypeList();
		}
	}

	void ForeachType() {
		if (la.kind == 134) {
			Get();
		}
		if (StartOf(11)) {
			Type();
		}
		Expect(1);
	}

	void FirstExp() {
		AssignExpression();
	}

	void LastExp() {
		AssignExpression();
	}

	void Symbol() {
		if (la.kind == 7) {
			Get();
		}
		SymbolTail();
	}

	void Catches() {
		if (la.kind == 75) {
			LastCatch();
		} else if (StartOf(35)) {
			while (la.kind == 75) {
				Catch();
			}
		} else SynErr(292);
	}

	void FinallyStatement() {
		Expect(98);
		NoScopeNonEmptyStatement();
	}

	void LastCatch() {
		Expect(75);
		NoScopeNonEmptyStatement();
	}

	void Catch() {
		Expect(75);
		Expect(42);
		CatchParameter();
		Expect(43);
		NoScopeNonEmptyStatement();
	}

	void CatchParameter() {
		BasicType();
		Expect(1);
	}

	void StructBody() {
		Expect(46);
		if (StartOf(36)) {
			StructBodyDeclarations();
		}
		Expect(47);
	}

	void StructTemplateDeclaration() {
		Expect(140);
		Expect(1);
		Expect(42);
		TemplateParameterList();
		Expect(43);
		if (la.kind == 106) {
			Constraint();
		}
		StructBody();
	}

	void UnionTemplateDeclaration() {
		Expect(156);
		Expect(1);
		Expect(42);
		TemplateParameterList();
		Expect(43);
		if (la.kind == 106) {
			Constraint();
		}
		StructBody();
	}

	void StructBodyDeclarations() {
		StructBodyDeclaration();
		if (StartOf(36)) {
			StructBodyDeclarations();
		}
	}

	void StructBodyDeclaration() {
		if (StartOf(5)) {
			Declaration();
		} else if (la.kind == 139) {
			StaticConstructor();
		} else if (la.kind == 139) {
			StaticDestructor();
		} else if (la.kind == 114) {
			Invariant();
		} else if (la.kind == 157) {
			UnitTest();
		} else if (la.kind == 122) {
			StructAllocator();
		} else if (la.kind == 88) {
			StructDeallocator();
		} else if (la.kind == 145) {
			StructConstructor();
		} else if (la.kind == 145) {
			StructPostblit();
		} else if (la.kind == 61) {
			StructDestructor();
		} else if (la.kind == 64) {
			AliasThis();
		} else SynErr(293);
	}

	void StructAllocator() {
		ClassAllocator();
	}

	void StructDeallocator() {
		ClassDeallocator();
	}

	void StructConstructor() {
		List<string> paramnames=new List<string>(); 
		Expect(145);
		Expect(42);
		ParameterList(ref paramnames);
		Expect(43);
		FunctionBody();
	}

	void StructPostblit() {
		Expect(145);
		Expect(42);
		Expect(145);
		Expect(43);
		FunctionBody();
	}

	void StructDestructor() {
		Expect(61);
		Expect(145);
		Expect(42);
		Expect(43);
		FunctionBody();
	}

	void AliasThis() {
		Expect(64);
		Expect(1);
		Expect(145);
		Expect(50);
	}

	void ClassAllocator() {
		string[] paramnames; 
		Expect(122);
		Parameters(out paramnames);
		FunctionBody();
	}

	void ClassDeallocator() {
		string[] paramnames; 
		Expect(88);
		Parameters(out paramnames);
		FunctionBody();
	}

	void ClassBody() {
		Expect(46);
		if (StartOf(36)) {
			ClassBodyDeclarations();
		}
		Expect(47);
	}

	void ClassTemplateDeclaration() {
		Expect(80);
		Expect(1);
		Expect(42);
		TemplateParameterList();
		Expect(43);
		BaseClassList();
		ClassBody();
	}

	void SuperClass() {
		if (StartOf(37)) {
			Protection();
		}
		Expect(1);
	}

	void InterfaceClasses() {
		InterfaceClass();
		if (la.kind == 49) {
			Get();
			InterfaceClasses();
		}
	}

	void InterfaceClass() {
		if (StartOf(37)) {
			Protection();
		}
		Expect(1);
	}

	void Protection() {
		if (la.kind == 129) {
			Get();
		} else if (la.kind == 127) {
			Get();
		} else if (la.kind == 131) {
			Get();
		} else if (la.kind == 94) {
			Get();
		} else SynErr(294);
	}

	void ClassBodyDeclarations() {
		ClassBodyDeclaration();
		if (StartOf(36)) {
			ClassBodyDeclarations();
		}
	}

	void ClassBodyDeclaration() {
		if (StartOf(5)) {
			Declaration();
		} else if (la.kind == 145) {
			Constructor();
		} else if (la.kind == 61) {
			Destructor();
		} else if (la.kind == 139) {
			StaticConstructor();
		} else if (la.kind == 139) {
			StaticDestructor();
		} else if (la.kind == 114) {
			Invariant();
		} else if (la.kind == 157) {
			UnitTest();
		} else if (la.kind == 122) {
			ClassAllocator();
		} else if (la.kind == 88) {
			ClassDeallocator();
		} else SynErr(295);
	}

	void PerenArgumentList() {
		Expect(42);
		ArgumentList();
		Expect(43);
	}

	void InterfaceBody() {
		Expect(46);
		while (StartOf(2)) {
			DeclDef();
		}
		Expect(47);
	}

	void InterfaceTemplateDeclaration() {
		Expect(113);
		Expect(1);
		Expect(42);
		TemplateParameterList();
		Expect(43);
		if (la.kind == 106) {
			Constraint();
		}
		if (la.kind == 51) {
			BaseInterfaceList();
		}
		InterfaceBody();
	}

	void BaseInterfaceList() {
		Expect(51);
		InterfaceClasses();
	}

	void EnumBody() {
		if (la.kind == 50) {
			Get();
		} else if (la.kind == 46) {
			Get();
			EnumMember();
			while (la.kind == 49) {
				Get();
				if (StartOf(11)) {
					EnumMember();
				}
			}
			Expect(47);
		} else SynErr(296);
	}

	void EnumMember() {
		if (StartOf(11)) {
			Type();
		}
		Expect(1);
		if (la.kind == 53) {
			Get();
			AssignExpression();
		}
	}

	void BodyStatement() {
		Expect(69);
		BlockStatement();
	}

	void InStatement() {
		Expect(110);
		BlockStatement();
	}

	void OutStatement() {
		Expect(125);
		if (la.kind == 42) {
			Get();
			Expect(1);
			Expect(43);
		}
		BlockStatement();
	}

	void TemplateIdentifier() {
		Expect(1);
	}

	void Constraint() {
		Expect(106);
		Expect(42);
		Expression();
		Expect(43);
	}

	void TemplateParameter() {
		if (la.kind == 1) {
			TemplateTypeParameter();
		} else if (StartOf(5)) {
			TemplateValueParameter();
		} else if (la.kind == 64) {
			TemplateAliasParameter();
		} else if (la.kind == 1) {
			TemplateTupleParameter();
		} else if (la.kind == 145) {
			TemplateThisParameter();
		} else SynErr(297);
	}

	void TemplateTypeParameter() {
		Expect(1);
		if (la.kind == 51) {
			TemplateTypeParameterSpecialization();
		}
		if (la.kind == 53) {
			TemplateTypeParameterDefault();
		}
	}

	void TemplateValueParameter() {
		Declaration();
		if (la.kind == 51) {
			TemplateValueParameterSpecialization();
		}
		if (la.kind == 53) {
			TemplateValueParameterDefault();
		}
	}

	void TemplateAliasParameter() {
		Expect(64);
		Expect(1);
		if (la.kind == 51) {
			TemplateAliasParameterSpecialization();
		}
		if (la.kind == 53) {
			TemplateAliasParameterDefault();
		}
	}

	void TemplateTupleParameter() {
		Expect(1);
		Expect(9);
	}

	void TemplateThisParameter() {
		Expect(145);
		TemplateTypeParameter();
	}

	void TemplateArgumentList() {
		TemplateArgument();
		if (la.kind == 49) {
			Get();
			TemplateArgumentList();
		}
	}

	void TemplateSingleArgument() {
		switch (la.kind) {
		case 1: {
			Get();
			break;
		}
		case 70: case 72: case 76: case 78: case 79: case 83: case 84: case 91: case 99: case 105: case 107: case 112: case 115: case 118: case 133: case 138: case 152: case 154: case 155: case 158: case 160: case 162: {
			BasicTypeX();
			break;
		}
		case 4: {
			Get();
			break;
		}
		case 5: {
			Get();
			break;
		}
		case 2: {
			IntegerLiteral();
			break;
		}
		case 3: {
			Get();
			break;
		}
		case 147: {
			Get();
			break;
		}
		case 96: {
			Get();
			break;
		}
		case 124: {
			Get();
			break;
		}
		case 177: {
			Get();
			break;
		}
		case 178: {
			Get();
			break;
		}
		default: SynErr(298); break;
		}
	}

	void TemplateArgument() {
		if (StartOf(11)) {
			Type();
		} else if (StartOf(19)) {
			AssignExpression();
		} else if (la.kind == 1 || la.kind == 7) {
			Symbol();
		} else SynErr(299);
	}

	void SymbolTail() {
		if (la.kind == 1) {
			Get();
		} else if (la.kind == 1) {
			TemplateInstance();
		} else SynErr(300);
		if (la.kind == 7) {
			Get();
			SymbolTail();
		}
	}

	void TemplateTypeParameterSpecialization() {
		Expect(51);
		Type();
	}

	void TemplateTypeParameterDefault() {
		Expect(53);
		Type();
	}

	void TemplateValueParameterSpecialization() {
		Expect(51);
		ConditionalExpression();
	}

	void TemplateValueParameterDefault() {
		Expect(53);
		if (la.kind == 177) {
			Get();
		} else if (la.kind == 178) {
			Get();
		} else if (StartOf(19)) {
			ConditionalExpression();
		} else SynErr(301);
	}

	void TemplateAliasParameterSpecialization() {
		Expect(51);
		Type();
	}

	void TemplateAliasParameterDefault() {
		Expect(53);
		Type();
	}

	void MixinIdentifier() {
		Expect(1);
	}

	void Condition() {
		if (la.kind == 159) {
			VersionCondition();
		} else if (la.kind == 85) {
			DebugCondition();
		} else if (la.kind == 139) {
			StaticIfCondition();
		} else SynErr(302);
	}

	void CCDeclarationBlock() {
		if (StartOf(5)) {
			Declaration();
		} else if (la.kind == 46) {
			Get();
			while (StartOf(5)) {
				Declaration();
			}
			Expect(47);
		} else SynErr(303);
	}

	void VersionCondition() {
		Expect(159);
		Expect(42);
		if (la.kind == 2) {
			Get();
		} else if (la.kind == 1) {
			Get();
		} else if (la.kind == 157) {
			Get();
		} else SynErr(304);
		Expect(43);
	}

	void DebugCondition() {
		Expect(85);
		if (la.kind == 42) {
			Get();
			if (la.kind == 2) {
				Get();
			} else if (la.kind == 1) {
				Get();
			} else SynErr(305);
			Expect(43);
		}
	}

	void StaticIfCondition() {
		Expect(139);
		Expect(106);
		Expect(42);
		AssignExpression();
		Expect(43);
	}

	void TraitsKeyword() {
		switch (la.kind) {
		case 197: {
			Get();
			break;
		}
		case 198: {
			Get();
			break;
		}
		case 199: {
			Get();
			break;
		}
		case 200: {
			Get();
			break;
		}
		case 201: {
			Get();
			break;
		}
		case 202: {
			Get();
			break;
		}
		case 203: {
			Get();
			break;
		}
		case 204: {
			Get();
			break;
		}
		case 205: {
			Get();
			break;
		}
		case 206: {
			Get();
			break;
		}
		case 207: {
			Get();
			break;
		}
		case 208: {
			Get();
			break;
		}
		case 209: {
			Get();
			break;
		}
		case 210: {
			Get();
			break;
		}
		case 211: {
			Get();
			break;
		}
		case 212: {
			Get();
			break;
		}
		case 213: {
			Get();
			break;
		}
		case 214: {
			Get();
			break;
		}
		case 215: {
			Get();
			break;
		}
		case 216: {
			Get();
			break;
		}
		case 217: {
			Get();
			break;
		}
		case 218: {
			Get();
			break;
		}
		case 219: {
			Get();
			break;
		}
		case 220: {
			Get();
			break;
		}
		case 221: {
			Get();
			break;
		}
		case 222: {
			Get();
			break;
		}
		default: SynErr(306); break;
		}
	}

	void TraitsArgument() {
		if (StartOf(19)) {
			AssignExpression();
		} else if (StartOf(11)) {
			Type();
		} else SynErr(307);
	}



	public void Parse() {
		la = new Token();
		la.val = "";
		Get();
		D2();

    Expect(0);
	}

	static readonly bool[,] set = {
		{T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,T,T, T,T,x,T, x,x,T,x, x,x,x,x, T,x,T,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,T,x, T,x,T,x, x,x,T,x, T,x,x,T, x,x,x,x, x,T,x,T, T,T,T,T, T,T,T,T, T,T,T,x, T,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, x,T,T,T, T,T,x,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,T,x, T,T,T,T, T,T,T,T, T,T,T,T, T,T,x,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,T,T, T,x,T,T, T,T,T,T, T,x,T,T, T,x,x,x, x,x,x,x, x,x,x,x, x,T,T,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,T,x,T, T,T,x,x, T,x,T,x, T,x,x,x, T,x,T,T, T,T,x,T, T,T,x,x, x,T,x,T, x,T,T,T, x,T,x,T, x,x,x,x, x,T,x,T, T,T,x,T, T,T,T,T, x,x,T,x, T,T,x,T, x,x,T,T, T,T,T,T, T,T,x,x, T,T,T,T, T,x,x,T, T,T,x,x, x,x,x,T, T,x,T,T, T,T,T,T, T,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,T,T, T,T,x,T, x,x,T,x, x,x,x,x, T,x,T,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,T,x, T,x,T,x, x,x,T,x, T,x,x,T, x,x,x,x, x,T,x,T, T,x,T,T, T,T,T,T, T,T,T,x, T,x,T,T, x,T,T,T, T,T,T,T, T,T,T,T, x,x,x,T, T,T,x,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x,T, T,x,T,x, T,x,T,T, T,T,T,x, T,x,x,x, T,T,x,T, T,T,T,T, x,T,T,T, x,T,T,T, T,x,T,T, T,x,T,T, x,x,T,T, T,x,T,T, T,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,T,x,x, x,x,T,T, x,T,x,x, x,x,x,x, x,x,x,x, T,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,T,T, x,x,x,x, T,T,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, T,x,T,x, T,x,x,x, T,x,T,T, x,T,x,T, T,x,x,x, x,T,x,T, x,x,x,T, x,T,x,T, x,x,x,x, x,T,x,T, T,x,x,T, T,x,x,T, x,x,T,x, x,x,x,T, x,x,T,x, x,x,x,x, T,T,x,x, T,T,T,T, x,x,x,T, x,x,x,x, x,x,x,T, T,x,T,T, x,x,T,x, T,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,T,T, T,T,x,T, x,x,T,x, x,x,x,x, T,x,T,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,T,x, T,x,T,x, x,x,x,x, T,x,x,T, x,x,x,x, x,T,x,T, T,x,T,T, T,T,T,T, T,T,T,x, T,x,T,T, x,T,T,T, T,T,T,T, T,T,T,T, x,x,x,T, T,T,x,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x,T, T,x,T,x, T,x,T,T, T,T,T,x, T,x,x,x, T,T,x,T, T,T,T,T, x,T,T,T, x,T,T,T, T,x,T,T, T,x,T,T, x,x,T,T, T,x,T,T, T,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,T, T,T,T,T, T,T,T,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,x,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, x},
		{x,T,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,x, x,T,x,T, T,T,x,x, T,x,T,x, T,x,x,x, T,x,T,T, T,T,x,T, T,T,x,x, x,T,x,T, x,T,T,T, x,T,x,T, x,x,x,x, x,T,x,T, T,T,x,T, T,T,T,T, x,x,T,x, T,T,x,T, x,x,T,T, T,T,T,T, T,T,x,x, T,T,T,T, T,x,x,T, T,T,x,x, x,x,x,T, T,x,T,T, T,T,T,T, T,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,T,x,x, x,x,x,T, x,T,x,x, x,x,x,x, x,x,x,x, T,x,x,T, x,x,x,x, x,x,x,x, x,x,x,T, x,x,T,x, x,x,x,x, T,x,x,x, T,T,x,T, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, T,x,x,x, T,x,T,T, x,T,x,T, T,x,x,x, x,x,x,T, x,x,x,x, x,x,x,T, x,x,x,x, x,T,x,T, T,x,x,T, T,x,x,T, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,T,T, x,x,T,x, T,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, T,x,T,x, T,x,x,x, T,x,T,T, x,T,x,T, T,x,x,x, x,T,x,T, x,x,x,T, x,T,x,T, x,x,x,x, x,T,x,T, T,x,x,T, T,x,x,T, x,x,T,x, x,x,x,T, x,x,T,x, x,x,x,x, T,T,x,x, T,T,T,T, x,x,x,T, x,x,x,x, x,x,x,T, T,x,T,T, x,x,T,x, T,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,x,x,x, x,x,T,x, x,x,x,T, x,x,T,x, x,T,x,x, T,x,x,x, x,T,x,x, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, T,x,T,x, T,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, T,x,x,x, T,x,T,T, x,x,x,T, T,x,x,x, x,x,x,T, x,x,x,x, x,x,x,T, x,x,x,x, x,T,x,T, x,x,x,x, T,x,x,T, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,T,T, x,x,T,x, T,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, T,x,T,x, T,x,x,x, T,x,T,T, x,T,x,T, T,x,x,x, x,T,x,T, x,x,x,T, x,T,x,T, x,x,x,x, x,T,x,T, T,x,x,T, T,x,x,T, x,x,T,x, x,x,x,T, x,x,T,x, x,x,x,x, T,T,x,x, T,T,T,T, x,x,x,T, x,T,x,x, x,x,x,T, T,x,T,T, x,x,T,x, T,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,T,T, T,T,x,T, x,x,T,x, x,x,x,x, T,x,T,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,T,x, T,x,T,x, x,x,x,x, T,x,x,T, x,x,x,x, x,T,x,x, x,x,x,T, x,T,T,x, T,x,T,x, T,x,T,T, x,T,x,T, T,x,x,T, T,x,x,T, x,x,x,x, T,x,x,T, x,x,x,T, x,T,x,T, T,T,T,T, T,x,x,T, T,x,T,x, T,x,T,x, T,T,x,x, x,x,x,x, x,T,x,x, x,T,T,x, x,T,x,x, x,T,x,T, x,x,T,T, T,x,T,T, x,x,T,x, T,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, T,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,T,T, T,T,x,T, x,x,T,x, x,x,x,x, T,x,T,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,T,x, T,T,T,x, x,x,x,x, T,x,x,T, x,x,x,x, x,T,x,x, x,x,x,T, x,T,T,x, T,x,T,x, T,x,T,T, x,T,x,T, T,x,x,T, T,x,x,T, x,x,x,x, T,x,x,T, x,x,x,T, x,T,x,T, T,T,T,T, T,x,x,T, T,x,T,x, T,x,T,x, T,T,x,x, x,x,x,x, x,T,x,x, x,T,T,x, x,T,x,x, x,T,x,T, x,x,T,T, T,x,T,T, x,x,T,x, T,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, T,x,T,x, T,x,x,x, T,x,T,T, x,T,x,T, T,x,x,x, x,T,x,T, x,x,x,T, x,T,x,T, x,x,x,x, x,T,x,T, T,x,x,T, T,x,x,T, x,x,T,x, x,x,x,T, x,x,T,x, x,x,x,x, T,T,x,x, T,T,T,T, x,x,x,T, x,T,x,x, x,x,x,T, T,x,T,T, x,x,T,x, T,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, T,x,T,x, T,x,x,x, T,x,T,T, x,T,x,T, T,x,x,x, x,T,x,T, x,x,x,T, x,T,x,T, x,x,x,x, x,T,x,T, T,x,x,T, T,x,x,T, x,x,T,x, x,x,x,T, x,x,T,x, x,x,x,x, T,T,x,x, T,T,T,T, x,x,x,T, x,T,x,x, x,x,x,T, T,x,T,T, x,x,T,x, T,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,T, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, T,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,T,T, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, T,x,x,x, T,x,T,T, x,x,x,T, T,x,x,x, x,x,x,T, x,x,x,x, T,x,x,T, x,x,x,x, x,T,x,T, x,x,x,x, T,x,x,T, x,x,T,x, x,x,x,x, T,x,x,x, x,x,x,x, x,T,x,x, x,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, T,x,T,T, x,x,T,x, T,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,T,T, T,T,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, T,x,T,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,T,x, T,x,x,x, T,x,T,T, x,T,x,T, T,x,x,T, x,x,x,T, x,x,x,x, T,x,x,T, x,x,x,T, x,T,x,T, T,T,T,T, T,x,x,T, T,x,T,x, T,x,x,x, T,T,x,x, x,x,x,x, x,T,x,x, x,T,T,x, x,T,x,x, x,T,x,T, x,x,T,T, T,x,T,T, x,x,T,x, T,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, T,x,T,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,T,T, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{T,T,T,T, T,T,x,T, x,x,T,x, x,x,x,x, T,x,T,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,T,x, T,x,T,T, x,x,T,x, T,x,x,T, x,x,x,x, x,T,x,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,T,x, T,T,T,T, T,T,T,T, T,T,T,T, T,T,x,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,T,T, T,x,T,T, T,T,T,T, T,x,T,T, T,x,x,x, x,x,x,x, x,x,x,x, x,T,T,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, T,x,x,x, T,x,T,x, T,x,x,x, T,x,T,T, x,T,x,T, T,x,x,x, T,T,x,T, x,x,x,T, x,T,x,T, x,x,x,x, x,T,x,T, T,x,x,T, T,x,T,T, x,x,T,x, x,x,T,T, x,x,T,x, x,x,x,x, T,T,x,x, T,T,T,T, x,x,x,T, x,T,x,x, x,x,x,T, T,x,T,T, x,T,T,x, T,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x}

	};
} // end Parser


public class Errors {
	public int count = 0;                                    // number of errors detected
	public System.IO.TextWriter errorStream = Console.Out;   // error messages go to this stream
  public string errMsgFormat = "-- line {0} col {1}: {2}"; // 0=line, 1=column, 2=text

	public static string ErrMsg(int n)
	{
	string s;
		switch (n) {
			case 0: s = "EOF expected"; break;
			case 1: s = "Identifier expected"; break;
			case 2: s = "Integer expected"; break;
			case 3: s = "FloatLiteral expected"; break;
			case 4: s = "CharLiteral expected"; break;
			case 5: s = "StringLiteral expected"; break;
			case 6: s = "DivAss expected"; break;
			case 7: s = "Dot expected"; break;
			case 8: s = "DblDot expected"; break;
			case 9: s = "TrplDot expected"; break;
			case 10: s = "BitAnd expected"; break;
			case 11: s = "BitAndAss expected"; break;
			case 12: s = "And expected"; break;
			case 13: s = "BitOr expected"; break;
			case 14: s = "BitOrAss expected"; break;
			case 15: s = "Or expected"; break;
			case 16: s = "Minus expected"; break;
			case 17: s = "MinusAss expected"; break;
			case 18: s = "Decr expected"; break;
			case 19: s = "Plus expected"; break;
			case 20: s = "PlusAss expected"; break;
			case 21: s = "Incr expected"; break;
			case 22: s = "Lt expected"; break;
			case 23: s = "LtAss expected"; break;
			case 24: s = "BinLt expected"; break;
			case 25: s = "BinLtAss expected"; break;
			case 26: s = "Uneq expected"; break;
			case 27: s = "UneqAss expected"; break;
			case 28: s = "Gt expected"; break;
			case 29: s = "GtAss expected"; break;
			case 30: s = "BinGtAss expected"; break;
			case 31: s = "BinGt2Ass expected"; break;
			case 32: s = "BinGt expected"; break;
			case 33: s = "BinGt2 expected"; break;
			case 34: s = "Not expected"; break;
			case 35: s = "NotAss expected"; break;
			case 36: s = "NotUneq expected"; break;
			case 37: s = "NotUneqAss expected"; break;
			case 38: s = "NotLt expected"; break;
			case 39: s = "NotLtAss expected"; break;
			case 40: s = "NotGt expected"; break;
			case 41: s = "NotGtAss expected"; break;
			case 42: s = "OpenRound expected"; break;
			case 43: s = "CloseRound expected"; break;
			case 44: s = "OpenSq expected"; break;
			case 45: s = "CloseSq expected"; break;
			case 46: s = "OpenCurly expected"; break;
			case 47: s = "CloseCurly expected"; break;
			case 48: s = "QuestionMark expected"; break;
			case 49: s = "Comma expected"; break;
			case 50: s = "Semicolon expected"; break;
			case 51: s = "Colon expected"; break;
			case 52: s = "Dollar expected"; break;
			case 53: s = "Assign expected"; break;
			case 54: s = "Equals expected"; break;
			case 55: s = "Times expected"; break;
			case 56: s = "TimesAss expected"; break;
			case 57: s = "Mod expected"; break;
			case 58: s = "ModAss expected"; break;
			case 59: s = "Pow expected"; break;
			case 60: s = "PowAss expected"; break;
			case 61: s = "Tilde expected"; break;
			case 62: s = "TildeAss expected"; break;
			case 63: s = "abstract expected"; break;
			case 64: s = "alias expected"; break;
			case 65: s = "align expected"; break;
			case 66: s = "asm expected"; break;
			case 67: s = "assert expected"; break;
			case 68: s = "auto expected"; break;
			case 69: s = "body expected"; break;
			case 70: s = "bool expected"; break;
			case 71: s = "break expected"; break;
			case 72: s = "byte expected"; break;
			case 73: s = "case expected"; break;
			case 74: s = "cast expected"; break;
			case 75: s = "catch expected"; break;
			case 76: s = "cdouble expected"; break;
			case 77: s = "cent expected"; break;
			case 78: s = "cfloat expected"; break;
			case 79: s = "char expected"; break;
			case 80: s = "class expected"; break;
			case 81: s = "const expected"; break;
			case 82: s = "continue expected"; break;
			case 83: s = "creal expected"; break;
			case 84: s = "dchar expected"; break;
			case 85: s = "debug expected"; break;
			case 86: s = "default expected"; break;
			case 87: s = "delegate expected"; break;
			case 88: s = "delete expected"; break;
			case 89: s = "deprecated expected"; break;
			case 90: s = "do expected"; break;
			case 91: s = "double expected"; break;
			case 92: s = "else expected"; break;
			case 93: s = "enum expected"; break;
			case 94: s = "export expected"; break;
			case 95: s = "extern expected"; break;
			case 96: s = "false expected"; break;
			case 97: s = "final expected"; break;
			case 98: s = "finally expected"; break;
			case 99: s = "float expected"; break;
			case 100: s = "for expected"; break;
			case 101: s = "foreach expected"; break;
			case 102: s = "foreach_reverse expected"; break;
			case 103: s = "function expected"; break;
			case 104: s = "goto expected"; break;
			case 105: s = "idouble expected"; break;
			case 106: s = "if expected"; break;
			case 107: s = "ifloat expected"; break;
			case 108: s = "immutable expected"; break;
			case 109: s = "import expected"; break;
			case 110: s = "in expected"; break;
			case 111: s = "inout expected"; break;
			case 112: s = "int expected"; break;
			case 113: s = "interface expected"; break;
			case 114: s = "invariant expected"; break;
			case 115: s = "ireal expected"; break;
			case 116: s = "is expected"; break;
			case 117: s = "lazy expected"; break;
			case 118: s = "long expected"; break;
			case 119: s = "macro expected"; break;
			case 120: s = "mixin expected"; break;
			case 121: s = "module expected"; break;
			case 122: s = "new expected"; break;
			case 123: s = "nothrow expected"; break;
			case 124: s = "null expected"; break;
			case 125: s = "out expected"; break;
			case 126: s = "override expected"; break;
			case 127: s = "package expected"; break;
			case 128: s = "pragma expected"; break;
			case 129: s = "private expected"; break;
			case 130: s = "protected expected"; break;
			case 131: s = "public expected"; break;
			case 132: s = "pure expected"; break;
			case 133: s = "real expected"; break;
			case 134: s = "ref expected"; break;
			case 135: s = "return expected"; break;
			case 136: s = "scope expected"; break;
			case 137: s = "shared expected"; break;
			case 138: s = "short expected"; break;
			case 139: s = "static expected"; break;
			case 140: s = "struct expected"; break;
			case 141: s = "super expected"; break;
			case 142: s = "switch expected"; break;
			case 143: s = "synchronized expected"; break;
			case 144: s = "template expected"; break;
			case 145: s = "this expected"; break;
			case 146: s = "throw expected"; break;
			case 147: s = "true expected"; break;
			case 148: s = "try expected"; break;
			case 149: s = "typedef expected"; break;
			case 150: s = "typeid expected"; break;
			case 151: s = "typeof expected"; break;
			case 152: s = "ubyte expected"; break;
			case 153: s = "ucent expected"; break;
			case 154: s = "uint expected"; break;
			case 155: s = "ulong expected"; break;
			case 156: s = "union expected"; break;
			case 157: s = "unittest expected"; break;
			case 158: s = "ushort expected"; break;
			case 159: s = "version expected"; break;
			case 160: s = "void expected"; break;
			case 161: s = "volatile expected"; break;
			case 162: s = "wchar expected"; break;
			case 163: s = "while expected"; break;
			case 164: s = "with expected"; break;
			case 165: s = "\"U\" expected"; break;
			case 166: s = "\"u\" expected"; break;
			case 167: s = "\"L\" expected"; break;
			case 168: s = "\"l\" expected"; break;
			case 169: s = "\"UL\" expected"; break;
			case 170: s = "\"Ul\" expected"; break;
			case 171: s = "\"uL\" expected"; break;
			case 172: s = "\"ul\" expected"; break;
			case 173: s = "\"LU\" expected"; break;
			case 174: s = "\"Lu\" expected"; break;
			case 175: s = "\"lU\" expected"; break;
			case 176: s = "\"lu\" expected"; break;
			case 177: s = "\"__FILE__\" expected"; break;
			case 178: s = "\"__LINE__\" expected"; break;
			case 179: s = "\"__gshared\" expected"; break;
			case 180: s = "\"@\" expected"; break;
			case 181: s = "\"disable\" expected"; break;
			case 182: s = "\"property\" expected"; break;
			case 183: s = "\"safe\" expected"; break;
			case 184: s = "\"C\" expected"; break;
			case 185: s = "\"C++\" expected"; break;
			case 186: s = "\"D\" expected"; break;
			case 187: s = "\"Windows\" expected"; break;
			case 188: s = "\"Pascal\" expected"; break;
			case 189: s = "\"System\" expected"; break;
			case 190: s = "\"^^=\" expected"; break;
			case 191: s = "\"/\" expected"; break;
			case 192: s = "\"^^\" expected"; break;
			case 193: s = "\"exit\" expected"; break;
			case 194: s = "\"success\" expected"; break;
			case 195: s = "\"failure\" expected"; break;
			case 196: s = "\"__traits\" expected"; break;
			case 197: s = "\"isAbstractClass\" expected"; break;
			case 198: s = "\"isArithmetic\" expected"; break;
			case 199: s = "\"isAssociativeArray\" expected"; break;
			case 200: s = "\"isFinalClass\" expected"; break;
			case 201: s = "\"isFloating\" expected"; break;
			case 202: s = "\"isIntegral\" expected"; break;
			case 203: s = "\"isScalar\" expected"; break;
			case 204: s = "\"isStaticArray\" expected"; break;
			case 205: s = "\"isUnsigned\" expected"; break;
			case 206: s = "\"isVirtualFunction\" expected"; break;
			case 207: s = "\"isAbstractFunction\" expected"; break;
			case 208: s = "\"isFinalFunction\" expected"; break;
			case 209: s = "\"isStaticFunction\" expected"; break;
			case 210: s = "\"isRef\" expected"; break;
			case 211: s = "\"isOut\" expected"; break;
			case 212: s = "\"isLazy\" expected"; break;
			case 213: s = "\"hasMember\" expected"; break;
			case 214: s = "\"identifier\" expected"; break;
			case 215: s = "\"getMember\" expected"; break;
			case 216: s = "\"getOverloads\" expected"; break;
			case 217: s = "\"getVirtualFunctions\" expected"; break;
			case 218: s = "\"classInstanceSize\" expected"; break;
			case 219: s = "\"allMembers\" expected"; break;
			case 220: s = "\"derivedMembers\" expected"; break;
			case 221: s = "\"isSame\" expected"; break;
			case 222: s = "\"compiles\" expected"; break;
			case 223: s = "??? expected"; break;
			case 224: s = "invalid DeclDef"; break;
			case 225: s = "invalid Statement"; break;
			case 226: s = "invalid VersionSpecification"; break;
			case 227: s = "invalid DebugSpecification"; break;
			case 228: s = "invalid ModuleDeclaration"; break;
			case 229: s = "invalid AttributeSpecifier"; break;
			case 230: s = "invalid ClassDeclaration"; break;
			case 231: s = "invalid InterfaceDeclaration"; break;
			case 232: s = "invalid AggregateDeclaration"; break;
			case 233: s = "invalid AggregateDeclaration"; break;
			case 234: s = "invalid Declaration"; break;
			case 235: s = "invalid ConditionalDeclaration"; break;
			case 236: s = "invalid StorageClass"; break;
			case 237: s = "invalid Decl"; break;
			case 238: s = "invalid BasicType"; break;
			case 239: s = "invalid Initializer"; break;
			case 240: s = "invalid FunctionBody"; break;
			case 241: s = "invalid BasicTypeX"; break;
			case 242: s = "invalid IdentifierList"; break;
			case 243: s = "invalid Typeof"; break;
			case 244: s = "invalid BasicType2"; break;
			case 245: s = "invalid BasicType2"; break;
			case 246: s = "invalid DeclaratorSuffix"; break;
			case 247: s = "invalid TemplateInstance"; break;
			case 248: s = "invalid Declarator2"; break;
			case 249: s = "invalid ParameterList"; break;
			case 250: s = "invalid ParameterList"; break;
			case 251: s = "invalid InOut"; break;
			case 252: s = "invalid DefaultInitializerExpression"; break;
			case 253: s = "invalid FunctionAttribute"; break;
			case 254: s = "invalid MemberFunctionAttribute"; break;
			case 255: s = "invalid NonVoidInitializer"; break;
			case 256: s = "invalid ArrayMemberInitialization"; break;
			case 257: s = "invalid StructMemberInitializer"; break;
			case 258: s = "invalid Attribute"; break;
			case 259: s = "invalid Attribute"; break;
			case 260: s = "invalid DeclarationBlock"; break;
			case 261: s = "invalid ProtectionAttribute"; break;
			case 262: s = "invalid LinkageType"; break;
			case 263: s = "invalid CmpExpression"; break;
			case 264: s = "invalid ShiftExpression"; break;
			case 265: s = "invalid ShiftExpression"; break;
			case 266: s = "invalid EqualExpression"; break;
			case 267: s = "invalid RelExpression"; break;
			case 268: s = "invalid AddExpression"; break;
			case 269: s = "invalid AddExpression"; break;
			case 270: s = "invalid MulExpression"; break;
			case 271: s = "invalid MulExpression"; break;
			case 272: s = "invalid UnaryExpression"; break;
			case 273: s = "invalid PostfixExpression"; break;
			case 274: s = "invalid PostfixExpression"; break;
			case 275: s = "invalid PostfixExpression"; break;
			case 276: s = "invalid NewExpression"; break;
			case 277: s = "invalid BaseClassList"; break;
			case 278: s = "invalid PrimaryExpression"; break;
			case 279: s = "invalid TypeidExpression"; break;
			case 280: s = "invalid TypeSpecialization"; break;
			case 281: s = "invalid NonEmptyStatement"; break;
			case 282: s = "invalid NoScopeNonEmptyStatement"; break;
			case 283: s = "invalid NoScopeStatement"; break;
			case 284: s = "invalid NonEmptyOrScopeBlockStatement"; break;
			case 285: s = "invalid GotoStatement"; break;
			case 286: s = "invalid WithStatement"; break;
			case 287: s = "invalid ScopeGuardStatement"; break;
			case 288: s = "invalid ScopeStatement"; break;
			case 289: s = "invalid IfCondition"; break;
			case 290: s = "invalid Initialize"; break;
			case 291: s = "invalid Foreach"; break;
			case 292: s = "invalid Catches"; break;
			case 293: s = "invalid StructBodyDeclaration"; break;
			case 294: s = "invalid Protection"; break;
			case 295: s = "invalid ClassBodyDeclaration"; break;
			case 296: s = "invalid EnumBody"; break;
			case 297: s = "invalid TemplateParameter"; break;
			case 298: s = "invalid TemplateSingleArgument"; break;
			case 299: s = "invalid TemplateArgument"; break;
			case 300: s = "invalid SymbolTail"; break;
			case 301: s = "invalid TemplateValueParameterDefault"; break;
			case 302: s = "invalid Condition"; break;
			case 303: s = "invalid CCDeclarationBlock"; break;
			case 304: s = "invalid VersionCondition"; break;
			case 305: s = "invalid DebugCondition"; break;
			case 306: s = "invalid TraitsKeyword"; break;
			case 307: s = "invalid TraitsArgument"; break;

			default: s = "error " + n; break;
		}
		return s;
	}

	public void SynErr (int line, int col, int n) {
		errorStream.WriteLine(errMsgFormat, line, col, ErrMsg(n));
		count++;
	}

	public void SemErr (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}

	public void SemErr (string s) {
		errorStream.WriteLine(s);
		count++;
	}

	public void Warning (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
	}

	public void Warning(string s) {
		errorStream.WriteLine(s);
	}
} // Errors


public class FatalError: Exception {
	public FatalError(string m): base(m) {}
}

}