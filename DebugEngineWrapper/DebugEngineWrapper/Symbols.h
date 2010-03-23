#pragma once
#include "stdafx.h"

using namespace System;
using namespace System::Collections;
using namespace System::Text;

namespace DebugEngineWrapper
{
	public ref class DebugSymbolGroup
	{
	protected:
		List<DebugScopedSymbol^>^ symbols;
	internal:
		DbgSymbolGroup* sg;
		DebugSymbolGroup(DbgSymbolGroup *symg):sg(symg)
		{
			symbols=gcnew List<DebugScopedSymbol^>;
			
			for(UINT i=0;i<Count;i++)
					{
						DebugScopedSymbol^ s=gcnew DebugScopedSymbol(this,i);
						symbols->Add(s);

						if(!s->TypeName->EndsWith("[]") && s->Name!="object.Object" && s->Offset>0) // An array isn't needed to get expanded
							ExpandChildren(i,true);
					}
		}
	private:
		DebugSymbolGroup()
		{
		
		}
	public:
		~DebugSymbolGroup()
		{
			this->!DebugSymbolGroup();
		}
		
		!DebugSymbolGroup()
		{
			sg->Release();
		}

		bool ExpandChildren(ULONG Index,bool Expand)
		{
			return sg->ExpandSymbol(Index,Expand)==S_OK;
		}

		property ULONG Count
		{
			ULONG get(){
				ULONG ret=0;
				sg->GetNumberSymbols(&ret);
				return ret;
			}
		}

		String^ SymbolName(ULONG Index)
		{
			wchar_t* buf=new wchar_t[512];
			ULONG len=0;
			ULONG64 displacement=0;
			if(sg->GetSymbolNameWide(Index,(PWSTR)buf,512,&len)==E_FAIL) return gcnew String("");
			String^ v=gcnew String(buf);
			delete [] buf;
			return v;
		}

		ULONG64 SymbolOffset(ULONG Index)
		{
			ULONG64 ret=0;
			sg->GetSymbolOffset(Index,&ret);
			return ret;
		}

		ULONG SymbolSize(ULONG Index)
		{
			ULONG ret=0;
			sg->GetSymbolSize(Index,&ret);
			return ret;
		}

		String^ TypeName(ULONG Index)
		{
			wchar_t* buf=new wchar_t[1024];
			ULONG len=0;
			if(sg->GetSymbolTypeNameWide(Index,(PWSTR)buf,1024,&len)==E_FAIL) return gcnew String("");
			String^ v=gcnew String(buf);
			delete [] buf;
			return v;
		}

		String^ ValueText(ULONG Index)
		{
			wchar_t* buf=new wchar_t[1024];
			ULONG len=0;
			if(sg->GetSymbolValueTextWide(Index,(PWSTR)buf,1024,&len)==E_FAIL) return gcnew String("");
			String^ v=gcnew String(buf);
			delete [] buf;
			return v;
		}
		
		property array<DebugScopedSymbol^>^ Symbols
		{
			array<DebugScopedSymbol^>^ get()
			{
				return symbols->ToArray();
			}
		}
		
		DebugScopedSymbol^ operator[](ULONG Index)
		{
			for(UINT i=0;i<Count;i++)
			{
				if(Symbols[i]->Id==Index) return Symbols[i];
			}
			return nullptr;
		}
	};


	public ref class DebugSymbols
	{
	internal:
		DbgSymbols *sym;
		DbgDataSpaces* ds;
		DebugSymbols(DbgSymbols *symb,DbgDataSpaces* datas):sym(symb) {
			ds=datas;
		}
	private:
		DebugSymbols(){}
	public:
		~DebugSymbols()
		{
			this->!DebugSymbols();
		}

		!DebugSymbols()
		{
			sym->Release();
		}

		ULONG64 GetOffsetbyName(String^ name)
		{
			pin_ptr<const wchar_t> s = PtrToStringChars(name);
			ULONG64 ret=0;
			if(sym->GetOffsetByNameWide(s,&ret)!=0)ret=0;
			return ret;
		}

		String^ GetNameByOffset(ULONG64 Offset)
		{
			wchar_t* cmdbuf=new wchar_t[512];// 
			ULONG len=0;
			ULONG64 displacement=0;
			if(sym->GetNameByOffsetWide(Offset,(PWSTR)cmdbuf,512,&len,&displacement)==E_FAIL) return gcnew String("");
			String^ v=gcnew String(cmdbuf);
			delete [] cmdbuf;
			return v;
		}

		array<DebugSymbolData^>^ GetSymbols(String^ pattern)
		{
			pin_ptr<const wchar_t> s = PtrToStringChars(pattern);
			ULONG64 searchHandle;
			sym->StartSymbolMatchWide(s,&searchHandle);

			ArrayList^ ret=gcnew ArrayList();
			wchar_t* buf=new wchar_t[512];
			ULONG64 Offset=0;

			while(sym->GetNextSymbolMatchWide(searchHandle,buf,512,NULL,&Offset)==S_OK)
			{
				ret->Add(gcnew DebugSymbolData(gcnew String(buf),Offset));
			}

			sym->EndSymbolMatch(searchHandle);

			array<DebugSymbolData^>^ ret2=gcnew array<DebugSymbolData^>(ret->Count);
			for(int i=0; i<ret->Count;i++)
			{
				ret2[i]=(DebugSymbolData^)ret[i];
			}
			delete ret;

			return ret2;
		}

		property array<DebugSymbolData^>^ Symbols
		{
			array<DebugSymbolData^>^ get()
			{
				return this->GetSymbols("*");
			}
		}
#pragma region Evaluation of local symbol contents
		property array<DebugScopedSymbol^>^ ScopeLocalSymbols
		{
			array<DebugScopedSymbol^>^ get(){
				DebugSymbolGroup^ dsg;

				DbgSymbolGroup* sg;
				if(sym->GetScopeSymbolGroup(DEBUG_SCOPE_GROUP_LOCALS,0,(PDEBUG_SYMBOL_GROUP*)&sg)==S_OK)
				{
					dsg=gcnew DebugSymbolGroup(sg);
					return dsg->Symbols;
				}
				return nullptr;
			}
		}

		DClassInfo^ RetrieveClassInfo(ULONG64 Offset)
		{
			DClassInfo^ di=gcnew DClassInfo(this);

			UINT vtbl=0;
			UINT classinfo=0;
			if(ds->ReadVirtual(Offset,&vtbl,4,nullptr)!=S_OK) return di;
			if(ds->ReadVirtual(vtbl,&classinfo,4,nullptr)!=S_OK) return di;

			di->Base=classinfo;
			return di->BaseClass;
		}

		void ReadExceptionString(ULONG64 Offset,[Runtime::InteropServices::Out] String^ %Message,[Runtime::InteropServices::Out] String^ %SrcFile,[Runtime::InteropServices::Out]ULONG %Line)
		{
			// See E:\dmd2\src\druntime\src\compiler\dmd\mars.h for internal structures of objects
			UINT vtbl=0;
			UINT classinfo=0;
			if(ds->ReadVirtual(Offset,&vtbl,4,nullptr)!=S_OK) return;
			if(ds->ReadVirtual(vtbl,&classinfo,4,nullptr)!=S_OK) return;

			ULONG64 memberoffsets=Offset+8; // The first 8 bytes are filled with 'vtbl' and 'monitor' pointers

			array<Object^>^ str=ReadArray(memberoffsets,BYTE::typeid,1);
			Message="";
			if(str!=nullptr)
			{
				for(int i=0;i<str->Length;i++)	Message+=Convert::ToChar((BYTE)str[i]);
			}
			
			array<Object^>^ str2=ReadArray(memberoffsets+8,BYTE::typeid,1);
			String^ src="";
			if(str2!=nullptr)
			{
				for(int i=0;i<str2->Length;i++)
					src+=Convert::ToChar((BYTE)str2[i]);
				SrcFile=src;
			}
			
			ULONG line=0;
			if(ds->ReadVirtual(memberoffsets+16,&line,4,nullptr)==S_OK)
				Line=line;
		}


		array<Object^>^ ReadArray(ULONG64 Offset,Type^ type,ULONG ElementSize)
		{
			DArray str;
			if(ds->ReadVirtual(Offset,&str,sizeof(str),nullptr)!=S_OK) return nullptr;
			if(str.Length<1) return nullptr;

			UINT elsz=ElementSize;

			UINT sz=elsz*(str.Length>1000?1000:str.Length);
			if(sz<1) return nullptr;
			BYTE* ptr=new BYTE[sz];
			ULONG readb;
			if(ds->ReadVirtual((ULONG64)str.Ptr,ptr,sz,&readb)!=S_OK) return nullptr;
			ULONG Count=readb/elsz;

			array<Object^>^ ret= gcnew array<Object^>(Count);
			for(UINT i=0;i<Count;i++)
			{
				ret[i]=Marshal::PtrToStructure(IntPtr(ptr),type);
				ptr+=ElementSize;
			}
			return ret;
		}

		array<Object^>^ ReadArrayArray(ULONG64 Offset,Type^ type,ULONG ElementSize)
		{
			DArray arr;
			if(ds->ReadVirtual(Offset,&arr,sizeof(arr),nullptr)!=S_OK) return nullptr;
			if(arr.Length<1) return nullptr;

			UINT c=arr.Length>10000?10000:arr.Length;

			array<Object^>^ ret= gcnew array<Object^>(c);
			ULONG p=arr.Ptr;
			for(UINT i=0;i<c;i++)
			{
				ret[i]=ReadArray(p,type,ElementSize);
				p+=8;
			}
			return ret;
		}



#pragma endregion

		property String^ SymbolPath
		{
			void set(String^ value)
			{
				pin_ptr<const wchar_t> s = PtrToStringChars(value);
				sym->SetSymbolPathWide(s);
			}

			String^ get()
			{
				wchar_t* cmdbuf=new wchar_t[512];
				ULONG64 displacement=0;
				sym->GetSymbolPathWide((PWSTR)cmdbuf,512,NULL);
				String^ v=gcnew String(cmdbuf);
				delete [] cmdbuf;
				return v;
			}
		}

		property String^ SourcePath
		{
			void set(String^ value)
			{
				pin_ptr<const wchar_t> s = PtrToStringChars(value);
				sym->SetSourcePathWide(s);
			}

			String^ get()
			{
				wchar_t* cmdbuf=new wchar_t[512];
				sym->GetSourcePathWide((PWSTR)cmdbuf,512,NULL);
				String^ v=gcnew String(cmdbuf);
				delete [] cmdbuf;
				return v;
			}
		}

		bool GetLineByOffset(ULONG64 Offset,[Runtime::InteropServices::Out] String^ %File,[Runtime::InteropServices::Out] ULONG %Line)
		{
			ULONG64 disp;
			ULONG ln;
			ULONG n;
			PWSTR buf=new wchar_t[512];

			if(sym->GetLineByOffsetWide(Offset, &ln, buf,512,&n,&disp)!=0)
				return false;

			Line=n>0?ln:0;
			File=gcnew String((n>0)?buf:((wchar_t*)""));
			delete [] buf;
			return true;
		}

		bool GetOffsetByLine(String^ File,ULONG Line,[Runtime::InteropServices::Out] ULONG64 %Offset)
		{
			pin_ptr<const wchar_t> s = PtrToStringChars(File);
			ULONG64 ret;

			if(sym->GetOffsetByLineWide(Line,(PWSTR)s,&ret)!=0) return false;

			Offset=ret;
			return true;
		}
	};

	DClassInfo^ DClassInfo::BaseClass::get()
	{
		if(Base==0) return nullptr;
		// See E:\dmd2\src\druntime\src\compiler\dmd\mars.h for internal structures of objects
		DClassInfo^ di=gcnew DClassInfo(ds);

		array<Object^>^ str=ds->ReadArray(Base+16,BYTE::typeid,1);
		if(str!=nullptr)
			for(int i=0;i<str->Length;i++)	di->Name+=Convert::ToChar((BYTE)str[i]);

		UINT base=0;
		ds->ds->ReadVirtual(Base+40,&base,4,nullptr);
		di->Base=base;

		return di;
	};
	
	DebugScopedSymbol::DebugScopedSymbol(DebugSymbolGroup^ Owner,ULONG Index)
	{
		SymGroup=Owner;
		Id=Index;
		
		ULONG i=Index;
		Name=Owner->SymbolName(i)->Replace('@','.');
		Offset=Owner->SymbolOffset(i);
		TextValue=Owner->ValueText(i);
		TypeName=Owner->TypeName(i);
		Size=Owner->SymbolSize(i);

		ULONG c=10;

		DEBUG_SYMBOL_PARAMETERS prm;
		Owner->sg->GetSymbolParameters(i,1,&prm);
		ParentId=prm.ParentSymbol;
		Depth=prm.Flags & DEBUG_SYMBOL_EXPANSION_LEVEL_MASK;
		Flags=(DebugSymbolFlags)(prm.Flags-Depth);
	};

	array<DebugScopedSymbol^>^ DebugScopedSymbol::Children::get()
	{	
		List<DebugScopedSymbol^>^ ret=gcnew List<DebugScopedSymbol^>();
		for(UINT i=0;i<SymGroup->Count;i++)
		{
			DebugScopedSymbol^ t=SymGroup[i];
			if(t->ParentId==this->Id)
				ret->Add(t);
		}
		return ret->ToArray();
	};

	ULONG DebugScopedSymbol::ChildrenCount::get()
	{	
	ULONG ret=0;
		for(UINT i=0;i<SymGroup->Count;i++)
		{
			if(SymGroup[i]->ParentId==this->Id) ret++;
		}
		return ret;
	};

	DebugScopedSymbol^ DebugScopedSymbol::Parent::get()
	{
		if(ParentId>SymGroup->Count) return nullptr;
		return SymGroup->Symbols[ParentId];
	};
}