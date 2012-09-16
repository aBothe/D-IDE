#pragma once

#include "stdafx.h"

using namespace System;
using namespace System::Runtime::InteropServices;
using namespace System::IO;
using namespace System::Collections;


namespace DebugEngineWrapper
{
#pragma region Imported decl.d header from DDbg
	struct SubsectionHeader
	{
		USHORT	HeaderSize,//Length of directory header (cbDirHeader)
			EntrySize;//Length of each directory entry (cbDiEntry)
		UINT	SubsectionCount,//Number of directory entries (cDir)
			lfoNextDir,//Offset from lfaBase of next directory; unused
			flags;//Flags describing directory and subsection tables; unused
	};

	struct SubsectionTableEntry
	{
		USHORT subsection, //Subdirectory index
			iMod; /*Module index. This number is 1 based and zero (0) is never a valid index. The index 0xffff is reserved for tables that are not associated with a specific module. These tables include sstLibraries*/
		UINT	DataOffset,//Offset from the base address lfaBase (lfo)
			SectionSize;//Number of bytes in subsection (cb)
	};
	
	enum
{
    sstModule = 0x120,
    sstTypes,
    sstPublic,
    sstPublicSym,
    sstSymbols,
    sstAlignSym,
    sstSrcLnSeg,
    sstSrcModule,
    sstLibraries,
    sstGlobalSym,
    sstGlobalPub,
    sstGlobalTypes,
    sstMPC,
    sstSegMap,
    sstSegName,
    sstPreComp,
    sstPreCompMap,
    sstOffsetMap16,
    sstOffsetMap32,
    sstFileIndex,
    sstStaticSym
};

public struct SegInfo
{
	USHORT	Seg,
			pad;
	UINT	offset,
			cbSeg;
};

public struct ModuleHeader
{
	USHORT	OverlayNumber,
			LibrarySectionIndex,
			SegmentCount,
			Style;
};

 public ref class CodeViewModule
{public:
	ModuleHeader* modhdr;
	array<SegInfo*>^ SegmentInfo;
	String^ Name;
	
	CodeViewModule(ModuleHeader* hdr):modhdr(hdr)
	{
		SegmentInfo=gcnew array<SegInfo*>(hdr->SegmentCount);
	}
};

enum SymbolIndex
{
	S_COMPILE		= 1,
	S_REGISTER,
	S_CONSTANT,
	S_UDT,
	S_SSEARCH,
	S_END,
	S_SKIP,
	S_CVRESERVE,
	S_OBJNAME,
	S_ENDARG,
	S_COBOLUDT,
	S_MANYREG,
	S_RETURN,
	S_ENTRYTHIS,

	S_BPREL16		= 0x100,
	S_LDATA16,
	S_GDATA16,
	S_PUB16,
	S_LPROC16,
	S_GPROC16,
	S_THUNK16,
	S_BLOCK16,
	S_WITH16,
	S_LABEL16,
	S_CEXMODEL16,
	S_VFTPATH16,
	S_REGREL16,

	S_BPREL32		= 0x200,
	S_LDATA32,
	S_GDATA32,
	S_PUB32,
	S_LPROC32,
	S_GPROC32,
	S_THUNK32,
	S_BLOCK32,
	S_WITH32,
	S_LABEL32,
	S_CEXMODEL32,
	S_VFTPATH32,
	S_REGREL32,
	S_LTHREAD32,
	S_GTHREAD32,

	S_PROCREF		= 0x400,
	S_DATAREF,
	S_ALIGN,
};

 struct CVProcedureSymbol
{
	UINT		pParent,
				pEnd,
				pNext,
				proc_length,
				debug_start,
				debug_end,
				offset;
	USHORT		segment,
				proctype;
	BYTE		flags;
};

 struct CVReturnSymbol
{
	USHORT	flags;
	BYTE	style;
};

 struct CVStackSymbol
{
	UINT		offset;
	USHORT	type;
};

 struct CVDataSymbol
{
	UINT	offset;
	USHORT	segment,
			type;
};

 struct PackedSymbolsHeader
{
	USHORT	symhash,
			addrhash;
	UINT	cbSymbol,
			cbSymHash,
			cbAddrHash;
};

#pragma region const defs
//=================================================================================================
// D recOEM's

const UINT OEM_DIGITALMARS  = 0x0042;
const UINT D_DYN_ARRAY 	    = 0x0001;
const UINT D_ASSOC_ARRAY    = 0x0002;
const UINT D_DELEGATE   	= 0x0003;

//=================================================================================================
#pragma region leaf indeces

const UINT LF_MODIFIER_16t	= 0x0001;
const UINT LF_POINTER_16t	= 0x0002;
const UINT LF_ARRAY_16t		= 0x0003;
const UINT LF_CLASS_16t		= 0x0004;
const UINT LF_STRUCTURE_16t	= 0x0005;
const UINT LF_UNION_16t		= 0x0006;
const UINT LF_ENUM_16t		= 0x0007;
const UINT LF_PROCEDURE_16t	= 0x0008;
const UINT LF_MFUNCTION_16t	= 0x0009;
const UINT LF_VTSHAPE		= 0x000a;
const UINT LF_COBOL0_16t	= 0x000b;
const UINT LF_COBOL1		= 0x000c;
const UINT LF_BARRAY_16t	= 0x000d;
const UINT LF_LABEL			= 0x000e;
const UINT LF_NULL			= 0x000f;
const UINT LF_NOTTRAN		= 0x0010;
const UINT LF_DIMARRAY_16t	= 0x0011;
const UINT LF_VFTPATH_16t	= 0x0012;
const UINT LF_PRECOMP_16t	= 0x0013;
const UINT LF_ENDPRECOMP	= 0x0014;
const UINT LF_OEM_16t		= 0x0015;
const UINT LF_TYPESERVER	= 0x0016;

const UINT LF_MODIFIER		= 0x1001;
const UINT LF_POINTER		= 0x1002;
const UINT LF_ARRAY			= 0x1003;
const UINT LF_CLASS			= 0x1004;
const UINT LF_STRUCTURE		= 0x1005;
const UINT LF_UNION			= 0x1006;
const UINT LF_ENUM			= 0x1007;
const UINT LF_PROCEDURE		= 0x1008;
const UINT LF_MFUNCTION		= 0x1009;
const UINT LF_COBOL0		= 0x100a;
const UINT LF_BARRAY		= 0x100b;
const UINT LF_DIMARRAY		= 0x100c;
const UINT LF_VFTPATH		= 0x100d;
const UINT LF_PRECOMP		= 0x100e;
const UINT LF_OEM			= 0x100f;

const UINT LF_SKIP_16t		= 0x0200;
const UINT LF_ARGLIST_16t	= 0x0201;
const UINT LF_DEFARG_16t	= 0x0202;
const UINT LF_LIST			= 0x0203;
const UINT LF_FIELDLIST_16t	= 0x0204;
const UINT LF_DERIVED_16t	= 0x0205;
const UINT LF_BITFIELD_16t	= 0x0206;
const UINT LF_METHODLIST_16t= 0x0207;
const UINT LF_DIMCONU_16t	= 0x0208;
const UINT LF_DIMCONLU_16t	= 0x0209;
const UINT LF_DIMVARU_16t	= 0x020a;
const UINT LF_DIMVARLU_16t	= 0x020b;
const UINT LF_REFSYM		= 0x020c;

const UINT LF_SKIP			= 0x1200;
const UINT LF_ARGLIST		= 0x1201;
const UINT LF_DEFARG		= 0x1202;
const UINT LF_FIELDLIST		= 0x1203;
const UINT LF_DERIVED		= 0x1204;
const UINT LF_BITFIELD		= 0x1205;
const UINT LF_METHODLIST	= 0x1206;
const UINT LF_DIMCONU		= 0x1207;
const UINT LF_DIMCONLU		= 0x1208;
const UINT LF_DIMVARU		= 0x1209;
const UINT LF_DIMVARLU		= 0x120a;

const UINT LF_BCLASS_16t	= 0x0400;
const UINT LF_VBCLASS_16t	= 0x0401;
const UINT LF_IVBCLASS_16t	= 0x0402;
const UINT LF_ENUMERATE		= 0x0403;
const UINT LF_FRIENDFCN_16t	= 0x0404;
const UINT LF_INDEX_16t		= 0x0405;
const UINT LF_MEMBER_16t	= 0x0406;
const UINT LF_STMEMBER_16t	= 0x0407;
const UINT LF_METHOD_16t	= 0x0408;
const UINT LF_NESTTYPE_16t	= 0x0409;
const UINT LF_VFUNCTAB_16t	= 0x040a;
const UINT LF_FRIENDCLS_16t	= 0x040b;
const UINT LF_ONEMETHOD_16t	= 0x040c;
const UINT LF_VFUNCOFF_16t	= 0x040d;
const UINT LF_NESTTYPEEX_16t= 0x040e;
const UINT LF_MEMBERMODIFY_16t	= 0x040f;

const UINT LF_BCLASS		= 0x1400;
const UINT LF_VBCLASS		= 0x1401;
const UINT LF_IVBCLASS		= 0x1402;
const UINT LF_FRIENDFCN		= 0x1403;
const UINT LF_INDEX			= 0x1404;
const UINT LF_MEMBER		= 0x1405;
const UINT LF_STMEMBER		= 0x1406;
const UINT LF_METHOD		= 0x1407;
const UINT LF_NESTTYPE		= 0x1408;
const UINT LF_VFUNCTAB		= 0x1409;
const UINT LF_FRIENDCLS		= 0x140a;
const UINT LF_ONEMETHOD		= 0x140b;
const UINT LF_VFUNCOFF		= 0x140c;
const UINT LF_NESTTYPEEX	= 0x140d;
const UINT LF_MEMBERMODIFY	= 0x140e;

const UINT LF_NUMERIC		= 0x8000;
const UINT LF_CHAR			= 0x8000;
const UINT LF_SHORT			= 0x8001;
const UINT LF_USHORT		= 0x8002;
const UINT LF_LONG			= 0x8003;
const UINT LF_ULONG			= 0x8004;
const UINT LF_REAL32		= 0x8005;
const UINT LF_REAL64		= 0x8006;
const UINT LF_REAL80		= 0x8007;
const UINT LF_REAL128		= 0x8008;
const UINT LF_QUADWORD		= 0x8009;
const UINT LF_UQUADWORD		= 0x800a;
const UINT LF_REAL48		= 0x800b;
const UINT LF_COMPLEX32		= 0x800c;
const UINT LF_COMPLEX64		= 0x800d;
const UINT LF_COMPLEX80		= 0x800e;
const UINT LF_COMPLEX128	= 0x800f;
const UINT LF_VARSTRING		= 0x8010;
const UINT LF_UCHAR			= 0x8011;
#pragma endregion
//=================================================================================================
// Primitive Types

// Special Types
const UINT T_NOTYPE 	= 0x0000;
const UINT T_ABS 		= 0x0001;
const UINT T_SEGMENT 	= 0x0002;
const UINT T_VOID 		= 0x0003;
const UINT T_PVOID 		= 0x0103;
const UINT T_PFVOID 	= 0x0203;
const UINT T_PHVOID 	= 0x0303;
const UINT T_32PVOID 	= 0x0403;
const UINT T_32PFVOID 	= 0x0503;
const UINT T_CURRENCY 	= 0x0004;
const UINT T_NBASICSTR 	= 0x0005;
const UINT T_FBASICSTR 	= 0x0006;
const UINT T_NOTTRANS 	= 0x0007;
const UINT T_BIT 		= 0x0060;
const UINT T_PASCHAR 	= 0x0061;

// Character Types
const UINT T_CHAR 		= 0x0010;
const UINT T_UCHAR 		= 0x0020;
const UINT T_PCHAR 		= 0x0110;
const UINT T_PUCHAR 	= 0x0120;
const UINT T_PFCHAR 	= 0x0210;
const UINT T_PFUCHAR 	= 0x0220;
const UINT T_PHCHAR 	= 0x0310;
const UINT T_PHUCHAR 	= 0x0320;
const UINT T_32PCHAR 	= 0x0410;
const UINT T_32PUCHAR 	= 0x0420;
const UINT T_32PFCHAR 	= 0x0510;
const UINT T_32PFUCHAR 	= 0x0520;

// Real Character Types
const UINT T_RCHAR 		= 0x0070;
const UINT T_PRCHAR 	= 0x0170;
const UINT T_PFRCHAR 	= 0x0270;
const UINT T_PHRCHAR 	= 0x0370;
const UINT T_32PRCHAR 	= 0x0470;
const UINT T_32PFRCHAR 	= 0x0570;

// Wide Character Types
const UINT T_WCHAR 		= 0x0071;
const UINT T_PWCHAR 	= 0x0171;
const UINT T_PFWCHAR 	= 0x0271;
const UINT T_PHWCHAR 	= 0x0371;
const UINT T_32PWCHAR 	= 0x0471;
const UINT T_32PFWCHAR 	= 0x0571;

// Double Wide Character Types - D enhancement
const UINT T_DCHAR 		= 0x0078;
const UINT T_32PDCHAR   = 0x0478;
const UINT T_32PFDCHAR  = 0x0578;

// Real 16-bit Integer Types
const UINT T_INT2 		= 0x0072;
const UINT T_UINT2 		= 0x0073;
const UINT T_PINT2 		= 0x0172;
const UINT T_PUINT2 	= 0x0173;
const UINT T_PFINT2 	= 0x0272;
const UINT T_PFUINT2 	= 0x0273;
const UINT T_PHINT2 	= 0x0372;
const UINT T_PHUINT2 	= 0x0373;
const UINT T_32PINT2 	= 0x0472;
const UINT T_32PUINT2 	= 0x0473;
const UINT T_32PFINT2 	= 0x0572;
const UINT T_32PFUINT2 	= 0x0573;

// 16-bit Short Types
const UINT T_SHORT 		= 0x0011;
const UINT T_USHORT 	= 0x0021;
const UINT T_PSHORT 	= 0x0111;
const UINT T_PUSHORT 	= 0x0121;
const UINT T_PFSHORT 	= 0x0211;
const UINT T_PFUSHORT 	= 0x0221;
const UINT T_PHSHORT 	= 0x0311;
const UINT T_PHUSHORT 	= 0x0321;
const UINT T_32PSHORT 	= 0x0411;
const UINT T_32PUSHORT 	= 0x0421;
const UINT T_32PFSHORT 	= 0x0511;
const UINT T_32PFUSHORT = 0x0521;

// Real 32-bit Integer Types
const UINT T_INT4 		= 0x0074;
const UINT T_UINT4 		= 0x0075;
const UINT T_PINT4 		= 0x0174;
const UINT T_PUINT4 	= 0x0175;
const UINT T_PFINT4 	= 0x0274;
const UINT T_PFUINT4 	= 0x0275;
const UINT T_PHINT4 	= 0x0374;
const UINT T_PHUINT4 	= 0x0375;
const UINT T_32PINT4 	= 0x0474;
const UINT T_32PUINT4 	= 0x0475;
const UINT T_32PFINT4 	= 0x0574;
const UINT T_32PFUINT4 	= 0x0575;

// 32-bit Long Types
const UINT T_LONG 		= 0x0012;
const UINT T_ULONG 		= 0x0022;
const UINT T_PLONG 		= 0x0112;
const UINT T_PULONG 	= 0x0122;
const UINT T_PFLONG 	= 0x0212;
const UINT T_PFULONG 	= 0x0222;
const UINT T_PHLONG 	= 0x0312;
const UINT T_PHULONG 	= 0x0322;
const UINT T_32PLONG 	= 0x0412;
const UINT T_32PULONG 	= 0x0422;
const UINT T_32PFLONG 	= 0x0512;
const UINT T_32PFULONG 	= 0x0522;

// Real 64-bit int Types
const UINT T_INT8 		= 0x0076;
const UINT T_UINT8 		= 0x0077;
const UINT T_PINT8 		= 0x0176;
const UINT T_PUINT8 	= 0x0177;
const UINT T_PFINT8 	= 0x0276;
const UINT T_PFUINT8 	= 0x0277;
const UINT T_PHINT8 	= 0x0376;
const UINT T_PHUINT8 	= 0x0377;
const UINT T_32PINT8 	= 0x0476;
const UINT T_32PUINT8 	= 0x0477;
const UINT T_32PFINT8 	= 0x0576;
const UINT T_32PFUINT8 	= 0x0577;

// 64-bit Integral Types
const UINT T_QUAD 		= 0x0013;
const UINT T_UQUAD 		= 0x0023;
const UINT T_PQUAD 		= 0x0113;
const UINT T_PUQUAD 	= 0x0123;
const UINT T_PFQUAD 	= 0x0213;
const UINT T_PFUQUAD 	= 0x0223;
const UINT T_PHQUAD 	= 0x0313;
const UINT T_PHUQUAD 	= 0x0323;
const UINT T_32PQUAD 	= 0x0413;
const UINT T_32PUQUAD 	= 0x0423;
const UINT T_32PFQUAD 	= 0x0513;
const UINT T_32PFUQUAD 	= 0x0523;

// 32-bit Real Types
const UINT T_REAL32 	= 0x0040;
const UINT T_PREAL32 	= 0x0140;
const UINT T_PFREAL32 	= 0x0240;
const UINT T_PHREAL32 	= 0x0340;
const UINT T_32PREAL32 	= 0x0440;
const UINT T_32PFREAL32 = 0x0540;

// 48-bit Real Types
const UINT T_REAL48 	= 0x0044;
const UINT T_PREAL48 	= 0x0144;
const UINT T_PFREAL48 	= 0x0244;
const UINT T_PHREAL48 	= 0x0344;
const UINT T_32PREAL48 	= 0x0444;
const UINT T_32PFREAL48 = 0x0544;

// 64-bit Real Types
const UINT T_REAL64 	= 0x0041;
const UINT T_PREAL64 	= 0x0141;
const UINT T_PFREAL64 	= 0x0241;
const UINT T_PHREAL64 	= 0x0341;
const UINT T_32PREAL64 	= 0x0441;
const UINT T_32PFREAL64 = 0x0541;

// 80-bit Real Types
const UINT T_REAL80 	= 0x0042;
const UINT T_PREAL80 	= 0x0142;
const UINT T_PFREAL80 	= 0x0242;
const UINT T_PHREAL80 	= 0x0342;
const UINT T_32PREAL80 	= 0x0442;
const UINT T_32PFREAL80 = 0x0542;

// 128-bit Real Types
const UINT T_REAL128 	= 0x0043;
const UINT T_PREAL128 	= 0x0143;
const UINT T_PFREAL128 	= 0x0243;
const UINT T_PHREAL128 	= 0x0343;
const UINT T_32PREAL128 = 0x0443;
const UINT T_32PFREAL128 = 0x0543;

// 32-bit Complex Types
const UINT T_CPLX32 	= 0x0050;
const UINT T_PCPLX32 	= 0x0150;
const UINT T_PFCPLX32 	= 0x0250;
const UINT T_PHCPLX32 	= 0x0350;
const UINT T_32PCPLX32 	= 0x0450;
const UINT T_32PFCPLX32 = 0x0550;

// 64-bit Complex Types
const UINT T_CPLX64 	= 0x0051;
const UINT T_PCPLX64 	= 0x0151;
const UINT T_PFCPLX64	= 0x0251;
const UINT T_PHCPLX64 	= 0x0351;
const UINT T_32PCPLX64 	= 0x0451;
const UINT T_32PFCPLX64 = 0x0551;

// 80-bit Complex Types
const UINT T_CPLX80 	= 0x0052;
const UINT T_PCPLX80 	= 0x0152;
const UINT T_PFCPLX80 	= 0x0252;
const UINT T_PHCPLX80 	= 0x0352;
const UINT T_32PCPLX80 	= 0x0452;
const UINT T_32PFCPLX80 = 0x0552;

// 128-bit Complex Types
const UINT T_CPLX128 	= 0x0053;
const UINT T_PCPLX128 	= 0x0153;
const UINT T_PFCPLX128 	= 0x0253;
const UINT T_PHCPLX128 	= 0x0353;
const UINT T_32PCPLX128 = 0x0453;
const UINT T_32PFCPLX128 = 0x0553;

// Boolean Types
const UINT T_BOOL08 	= 0x0030;
const UINT T_BOOL16 	= 0x0031;
const UINT T_BOOL32 	= 0x0032;
const UINT T_BOOL64 	= 0x0033;
const UINT T_PBOOL08 	= 0x0130;
const UINT T_PBOOL16 	= 0x0131;
const UINT T_PBOOL32 	= 0x0132;
const UINT T_PBOOL64 	= 0x0133;
const UINT T_PFBOOL08 	= 0x0230;
const UINT T_PFBOOL16 	= 0x0231;
const UINT T_PFBOOL32 	= 0x0232;
const UINT T_PFBOOL64 	= 0x0233;
const UINT T_PHBOOL08 	= 0x0330;
const UINT T_PHBOOL16 	= 0x0331;
const UINT T_PHBOOL32 	= 0x0332;
const UINT T_PHBOOL64 	= 0x0333;
const UINT T_32PBOOL08 	= 0x0430;
const UINT T_32PBOOL16 	= 0x0431;
const UINT T_32PBOOL32 	= 0x0432;
const UINT T_32PBOOL64 	= 0x0433;
const UINT T_32PFBOOL08 = 0x0530;
const UINT T_32PFBOOL16 = 0x0531;
const UINT T_32PFBOOL32 = 0x0532;
const UINT T_32PFBOOL64 = 0x0533;
#pragma endregion
//=================================================================================================
// classes for representing types

ref class Leaf
{
	public:USHORT	leaf_index;
};

ref class LeafModifer : Leaf
{public:
	USHORT	attribute, index;
};

ref class LeafPointer : Leaf
{public:
	USHORT	attribute,
			type;
};

ref class LeafArgList : Leaf
{public:
	USHORT		argcount;
	array<USHORT>^	indeces;
};

ref class LeafProcedure : Leaf
{public:
	USHORT	rvtype;
	BYTE	call,
			reserved;
	USHORT	cParms,
			arglist;
};

ref class LeafMFunction : Leaf
{public:
	USHORT	rvtype,
			_class,
			_this;
	BYTE	call,
			reserved;
	USHORT	cParms,
			arglist;
	UINT	thisadjust;
};

ref class LeafNumeric : Leaf
{public:
		char	c;
		BYTE	uc;
		short	s;
		USHORT	us;
		int		i;
		UINT	ui;
		long	l;
		ULONG64	ul;
		float	f;
		double	d;/*
		real	r;
		cfloat	cf;
		cdouble	cd;
		creal	cr;
		string	str;*/

	UINT getUint()
	{
		switch ( leaf_index )
		{
			case LF_USHORT:	return us;
			case LF_ULONG:	return (UINT)ul;
			case LF_UCHAR:	return uc;
			default:
				break;
		}
		return 0;
	}
};

ref class LeafClassStruc : Leaf
{public:
	USHORT	count,
			field,
			property,
			dList,
			vshape,
			type;
	LeafNumeric^	length;
	String^	name;
};

ref class LeafVTShape : Leaf
{public:
	array<BYTE>^	descriptor;
};

ref class LeafFieldList : Leaf
{public:
	array<Leaf^>^	fields;
};

ref class LeafMethodList : Leaf
{public:
};

ref class LeafDerived : Leaf
{public:
	array<USHORT>^	types;
};

ref class LeafBaseClass : Leaf
{public:
	USHORT	type,
			attribute;
	LeafNumeric^	offset;
};

ref class LeafMember : Leaf
{public:
	USHORT	type,
			attribute;
	LeafNumeric^	offset;
	String^	name;
};

ref class LeafMethod : Leaf
{public:
	USHORT	count,
			mList;
	String^	name;
};

ref class LeafArray : Leaf
{public:
	USHORT	elemtype,
			idxtype;
	LeafNumeric^	length;
	String^	name;
};

ref class LeafEnum : Leaf
{public:
	USHORT	count,
			type,
			field,
			property;
	String^	name;
};

ref class LeafEnumNameValue : Leaf
{public:
	USHORT	attribute;
	LeafNumeric^	value;
	String^	name;
};

ref class LeafNestedType : Leaf
{public:
	USHORT	index;
	String^	name;
};

ref class LeafStaticDataMember : Leaf
{public:
	USHORT	type,
			attribute;
	String^	name;
};

ref class LeafUnion : Leaf
{public:
	USHORT	count,
			field,
			property;
	LeafNumeric^	length;
	String^	name;
};

ref class LeafDynArray : Leaf
{public:
	USHORT	index_type,
			elem_type;
};

ref class LeafAssocArray : Leaf
{public:
	USHORT	key_type,
			elem_type;
};

ref class LeafDelegate : Leaf
{public:
	USHORT	this_type,
			func_type;
};
#pragma endregion
/*
#pragma region codeview.d
//=================================================================================================
// classes for accessing CodeView data

ref class Symbol
{public:
	SymbolIndex	symbol_index;

	Symbol(SymbolIndex si) { symbol_index = si; }
};

ref class ReturnSymbol : Symbol
{public:
	CVReturnSymbol*	cvdata;
	array<BYTE>^			registers;

	ReturnSymbol(CVReturnSymbol* cv):Symbol(SymbolIndex::S_RETURN)
	 { cvdata = cv; }
};

ref class NamedSymbol : Symbol
{public:
	string				mangled_name,
						name_type,
						name_notype;

    this(SymbolIndex si)    { super(si); }

	int opCmp(Object o)
	{
	    string str;
	    NamedSymbol ns = cast(NamedSymbol)o;
	    if ( ns is null )
	    {
            StringWrap sw = cast(StringWrap)o;
            if ( sw is null )
                return -1;
            str = sw.str;
        }
        else
            str = ns.name_notype;
        if ( name_notype == str )
            return 0;
	    if ( name_notype < str )
            return -1;
        return 1;
	}
}

class StackSymbol : NamedSymbol
{
	CVStackSymbol*	cvdata;
	int				size;

	this(CVStackSymbol* cv)
	{
		super(SymbolIndex.S_BPREL32);
		cvdata=cv;
	}

	uint offset() { return cvdata.offset; }
	uint cvtype() { return cvdata.type; }
}

class DataSymbol : NamedSymbol
{
	CVDataSymbol*	cvdata;
	uint			size;

	this(SymbolIndex si, CVDataSymbol* cv)
	{
		super(si);
		cvdata=cv;
	}

	uint offset() { return cvdata.offset; }
	uint cvtype() { return cvdata.type; }
}

abstract class ScopeSymbol : NamedSymbol
{
	ScopeSymbol		parent_scope;
	SymbolSet		symbols;
	uint			lfo;

	this(SymbolIndex si, uint _lfo) { super(si); lfo = _lfo; symbols = new SymbolSet; }
}

class ProcedureSymbol : ScopeSymbol
{
	CVProcedureSymbol*	cvdata;
	SymbolSet			arguments;
	ReturnSymbol		return_sym;

	this(SymbolIndex si, uint lfo, CVProcedureSymbol* cvd)
	{
		super(si,lfo);
		cvdata = cvd;
		arguments = new SymbolSet;
	}
}

class SymbolSet
{
	ProcedureSymbol[]	proc_symbols;
	StackSymbol[]		stack_symbols;
	DataSymbol[]		data_symbols;
	NamedSymbol[]		named_symbols;
	Symbol[]			symbols;

    void opCatAssign(SymbolSet s)
    {
        symbols ~= s.symbols;
        named_symbols ~= s.named_symbols;
        data_symbols ~= s.data_symbols;
        stack_symbols ~= s.stack_symbols;
        proc_symbols ~= s.proc_symbols;
    }

	void add(Symbol s)
	{
		NamedSymbol ns = cast(NamedSymbol)s;
		if ( ns is null ) {
			symbols ~= s;
			return;
		}
		named_symbols ~= ns;
		ClassInfo ci = s.classinfo;
		if ( ci == ProcedureSymbol.classinfo )
			proc_symbols ~= cast(ProcedureSymbol)s;
		else if ( ci == StackSymbol.classinfo )
			stack_symbols ~= cast(StackSymbol)s;
		else if ( ci == DataSymbol.classinfo )
			data_symbols ~= cast(DataSymbol)s;
	}

	/**********************************************************************************************
        Find procedure symbol covering the given address.
    ********************************************************************************************** /
	ProcedureSymbol findProcedureSymbol(uint address)
	{
		foreach ( ps; proc_symbols )
			if ( address >= ps.cvdata.offset && address < ps.cvdata.offset+ps.cvdata.proc_length )
				return ps;
		return null;
	}

	/**********************************************************************************************
        Find data symbol covering the given address.
    ********************************************************************************************** /
	DataSymbol findDataSymbol(uint address, uint segment)
	{
		foreach ( ds; data_symbols )
		{
			if ( segment > 0 && ds.cvdata.segment != segment )
				continue;
			if ( address == ds.cvdata.offset )
				return ds;
		}
		return null;
	}

	/**********************************************************************************************
        Find data symbol by name.
    ********************************************************************************************** /
	DataSymbol findDataSymbol(string name)
	{
		foreach ( ds; data_symbols )
		{
			if ( ds.name_notype == name )
				return ds;
		}
		return null;
	}

	/**********************************************************************************************
        Find nearest data symbol to the given address.
    ********************************************************************************************** /
	DataSymbol findNearestDataSymbol(uint address, inout uint min_dist, uint segment)
	{
		DataSymbol min_ds;
		foreach ( ds; data_symbols )
		{
			if ( address < ds.cvdata.offset || ds.cvdata.segment != segment )
				continue;
			uint dist = abs(cast(int)address-cast(int)ds.cvdata.offset);
			if ( dist < min_dist ) {
				min_dist = dist;
				min_ds = ds;
			}
		}
		return min_ds;
	}
}

class Module
{
	ModuleHeader*	header;
	SegInfo[]		seginfos;
	string			name;
	ushort			pe_section;

	SymbolSet		symbols;

	SourceModule	source_module;

    CodeView        codeview;
    
	this(CodeView cv)
    {
        symbols = new SymbolSet;
        codeview = cv;
    }
}

class Location
{
    string      path;

	ScopeSymbol	scope_sym;
	DataSymbol	data_sym;
	Module		mod;
	CodeBlock   codeblock;
    CodeView    codeview;
	uint		address;

	this(uint addr, CodeView cv)
	{
        codeview = cv;
		address = addr;
	}

	this(uint addr)
	{
		address = addr;
	}

	this(string p)
	{
		path = p;
	}

    uint line()
    {
        if ( codeblock is null )
            return 0;
        return codeblock.line;
    }

	string file()
	{
	    if ( codeblock is null || codeblock.segment is null )
            return null;
		return codeblock.segment.file.name;
	}
    
    size_t getCodeBase()
    {
        return mod.codeview.image.getCodeBase;
    }

    bool bind(ImageSet images, string[] source_search_paths)
    {
        if ( path is null )
            return false;

		if ( find(path, '"') >= 0 )
			path = replace(path, "\"", "");

		string[] file_line = split(path, ":");
		if ( file_line is null || file_line.length < 2 || !isNumeric(file_line[$-1]) ) {
		    DbgIO.println("Invalid location format. Use <part of filename>:<linenumber>");
			return false;
		}

		string	file = join(file_line[0..$-1], ":"),
				line = file_line[$-1];

		if ( find(file, '/') >= 0 )
			file = replace(file, "/", "\\");
        
		SourceFile[] sfs = images.findSrcFiles(file);
		if ( sfs.length == 0 )
			sfs = images.findSrcFiles(file, source_search_paths);
		if ( sfs.length == 0 ) {
		    DbgIO.println("Source file \"%s\" not found", file);
			return false;
		}

        uint linenum = cast(uint)atoi(line);
        Location loc;
        foreach ( sf; sfs )
        {
            debug DbgIO.println("searching sf %s", sf.name);
            auto loc2 = images.findSrcLine(sf, linenum);
            if ( loc is null )
                loc = loc2;
            else if ( loc2 !is null && loc2.line < loc.line )
                loc = loc2;
        }
        if ( loc is null )
            DbgIO.println("Line %d in \"%s\" not found", linenum, sfs[0].name);

        scope_sym = loc.scope_sym;
        data_sym = loc.data_sym;
        mod = loc.mod;
        codeblock = loc.codeblock;
        address = loc.address;
        path = null;
        return true;
    }
}

ref class UserDefinedType
{
	USHORT	type_index;
	String^	name;
};


#pragma endregion
*/
}