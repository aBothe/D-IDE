#pragma once
#include "stdafx.h"

using namespace System;

namespace DebugEngineWrapper
{
	/*public ref class DebugSymbolGroup
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
	};*/


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

		/*property DebugSymbolGroup^ ScopeLocalSymbols
		{
			DebugSymbolGroup^ get(){
				DbgSymbolGroup* sg;
				if(sym->GetScopeSymbolGroup(DEBUG_SCOPE_GROUP_LOCALS,0,(PDEBUG_SYMBOL_GROUP*)&sg)==S_OK)
				{
					return gcnew DebugSymbolGroup(sg);
				}
				return nullptr;
			}
		}

		property DebugSymbolGroup^ ScopeArgumentSymbols
		{
			DebugSymbolGroup^ get(){
				DbgSymbolGroup* sg;
				if(sym->GetScopeSymbolGroup(DEBUG_SCOPE_GROUP_ARGUMENTS,0,(PDEBUG_SYMBOL_GROUP*)&sg)==S_OK)
				{
					return gcnew DebugSymbolGroup(sg);
				}
				return nullptr;
			}
		}*/

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