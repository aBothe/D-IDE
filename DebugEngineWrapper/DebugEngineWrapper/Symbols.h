#pragma once
#include "stdafx.h"

using namespace System;
using namespace System::Collections;

namespace DebugEngineWrapper
{
	public enum class DebugSymbolFlags
	{
		IsExpanded=DEBUG_SYMBOL_EXPANDED,
		IsReadonly=DEBUG_SYMBOL_READ_ONLY,
		IsArray=DEBUG_SYMBOL_IS_ARRAY,
		IsFloat=DEBUG_SYMBOL_IS_FLOAT,
		IsArgument=DEBUG_SYMBOL_IS_ARGUMENT,
		IsLocal=DEBUG_SYMBOL_IS_LOCAL,
	};

	public ref struct DebugScopedSymbol
	{
		internal:
		DebugScopedSymbol()
		{}
		
		public:
		ULONG Depth;
		ULONG ParentId;
		DebugSymbolFlags^ Flags;
		//DEBUG_SYMBOL_ENTRY* SymbolData;
		ULONG Id;
		String^ Name;
		String^ TypeName;
		String^ TextValue;
		ULONG64 Offset;
		ULONG Size;
	};

	public ref class DebugSymbolGroup
	{
	internal:
		DbgSymbolGroup* sg;
		DebugSymbolGroup(DbgSymbolGroup *symg):sg(symg) {}
	private:
		DebugSymbolGroup(){}
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
	};


	public ref class DebugSymbols
	{
	internal:
		DbgSymbols *sym;
		DebugSymbols(DbgSymbols *symb):sym(symb) {}
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

		property array<DebugScopedSymbol^>^ ScopeLocalSymbols
		{
			array<DebugScopedSymbol^>^ get(){
				DebugSymbolGroup^ dsg;
				
				DbgSymbolGroup* sg;
				if(sym->GetScopeSymbolGroup(DEBUG_SCOPE_GROUP_LOCALS,0,(PDEBUG_SYMBOL_GROUP*)&sg)==S_OK)
				{
					dsg=gcnew DebugSymbolGroup(sg);
					
					List<DebugScopedSymbol^>^ ret=gcnew List<DebugScopedSymbol^>();
					
					for(UINT i=0;i<dsg->Count;i++)
					{
					DebugScopedSymbol^ s=gcnew DebugScopedSymbol();
					s->Id=i;
					s->Name=dsg->SymbolName(i);
					s->Offset=dsg->SymbolOffset(i);
					s->TextValue=dsg->ValueText(i);
					s->TypeName=dsg->TypeName(i);
					s->Size=dsg->SymbolSize(i);
					
					ULONG c=10;
					
					//DEBUG_SYMBOL_PARAMETERS* params = (DEBUG_SYMBOL_PARAMETERS*) malloc(c*sizeof(DEBUG_SYMBOL_PARAMETERS));
					DEBUG_SYMBOL_PARAMETERS prm;
					dsg->sg->GetSymbolParameters(i,1,&prm);
					s->ParentId=prm.ParentSymbol;
					s->Depth=prm.Flags & DEBUG_SYMBOL_EXPANSION_LEVEL_MASK;
					s->Flags=(DebugSymbolFlags)(prm.Flags-s->Depth);
					
					ret->Add(s);
					
					dsg->ExpandChildren(i,true);
										
					}
					
					return ret->ToArray();
				}
				return nullptr;
			}
		}

		property array<DebugScopedSymbol^>^ ScopeArgumentSymbols
		{
			array<DebugScopedSymbol^>^ get(){
				DebugSymbolGroup^ dsg;
				
				DbgSymbolGroup* sg;
				if(sym->GetScopeSymbolGroup(DEBUG_SCOPE_GROUP_ARGUMENTS,0,(PDEBUG_SYMBOL_GROUP*)&sg)==S_OK)
				{
					dsg=gcnew DebugSymbolGroup(sg);
					
					array<DebugScopedSymbol^>^ ret=gcnew array<DebugScopedSymbol^>(dsg->Count);
					
					for(UINT i=0;i<dsg->Count;i++)
					{
					ret[i]=gcnew DebugScopedSymbol();
					ret[i]->Id=i;
					ret[i]->Name=dsg->SymbolName(i);
					ret[i]->Offset=dsg->SymbolOffset(i);
					ret[i]->TextValue=dsg->ValueText(i);
					ret[i]->TypeName=dsg->TypeName(i);
					ret[i]->Size=dsg->SymbolSize(i);
					
					//DEBUG_SYMBOL_ENTRY  info;
					//dsg->sg->GetSymbolEntryInformation(i,ret[i]->SymbolData);
					//*ret[i]->SymbolData=info;
					
					}
					
					return ret;
				}
				return nullptr;
			}
		}

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
}