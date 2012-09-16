
#pragma once
#include "stdafx.h"

using namespace System;

namespace DebugEngineWrapper
{
	///<summary></summary>
	public ref class BreakPoint
	{
	private:
		BreakPoint(){}
	internal:
		DbgBreakPoint *bp;

		BreakPoint(DbgBreakPoint *Bp)
		{
			this->bp=Bp;
		}
	public:

		///<summary>Returns the number of times this breakpoint has been already passed</summary>
		property ULONG HitCount
		{
			ULONG get()
			{
				ULONG ret;
				if(bp==0) return 0;
				bp->GetCurrentPassCount(&ret);
				return ret;
			}
		}

		///<summary>After these number of times the breakpoint gets triggered</summary>
		property ULONG MinimumHits
		{
			ULONG get()
			{
				ULONG v;
				if(bp==0) return 0;
				bp->GetPassCount(&v);
				return v;
			}

			void set(ULONG v)
			{
				if(bp!=0)
				bp->SetPassCount(v);
			}
		}

		property bool IsPassed
		{
			bool get()
			{
				return HitCount>0;
			}
		}

		// TODO: Make these functions work properly
		/*property String^ SourceFile
		{
			String^ get()
			{
				DbgClient* cl;
				DbgAdvanced* adv;

				bp->GetAdder((PDEBUG_CLIENT*)&cl);
				cl->QueryInterface(__uuidof(DbgAdvanced), (void**)&adv);

				wchar_t* cmdbuf=new wchar_t[512];// 
				ULONG Line=0;

				adv->GetSymbolInformationWide(
					DEBUG_SYMINFO_BREAKPOINT_SOURCE_LINE,0,	this->Id,
					&Line,	sizeof(ULONG),	NULL,
					(PWSTR)cmdbuf,	512,	NULL
					);

				String^ v=gcnew String(cmdbuf);
				delete [] cmdbuf;
				return v;
			}
		}

		property ULONG Line
		{
			ULONG get()
			{
				DbgClient* cl;
				DbgAdvanced* adv;

				bp->GetAdder((PDEBUG_CLIENT*)&cl);
				cl->QueryInterface(__uuidof(DbgAdvanced), (void**)&adv);

				ULONG Line=0;

				adv->GetSymbolInformationWide(
					DEBUG_SYMINFO_BREAKPOINT_SOURCE_LINE,0,	this->Id,
					&Line,	sizeof(ULONG),	NULL,
					NULL,	0,	NULL
					);
				return Line;
			}
		}*/

		///<summary></summary>
		property ULONG Id
		{
			ULONG get()
			{
				ULONG ret;
				if(bp!=0)
				bp->GetId(&ret);
				return ret;
			}
		}
		
		///<summary></summary>
		property BreakPointOptions Flags
		{
			BreakPointOptions get()
			{
				ULONG p;
				if(bp!=0)
				bp->GetFlags(&p);
				return (BreakPointOptions)p;
			}

			void set(BreakPointOptions v)
			{
				if(bp!=0)
				bp->SetFlags((ULONG)v);
			}
		}

		///<summary></summary>
		property ULONG64 Offset
		{
			ULONG64 get()
			{
				ULONG64 v;
				if(bp!=0)
				bp->GetOffset(&v);
				return v;
			}

			void set(ULONG64 v)
			{
				if(bp!=0)
				bp->SetOffset(v);
			}
		}

		///<summary>Sets or gets the expression on which this breakpoint gets triggered</summary>
		property String^ TriggerExpression
		{
			String^ get()
			{
				wchar_t* cmdbuf=new wchar_t[512];// 
				ULONG len=0;
				if(bp!=0)
				bp->GetOffsetExpressionWide((PWSTR)cmdbuf,512,&len);
				String^ v=gcnew String(cmdbuf);
				delete [] cmdbuf;
				return v;
			}

			void set(String^ v)
			{
				pin_ptr<const wchar_t> p = PtrToStringChars(v);
				if(bp!=0)
				bp->SetOffsetExpressionWide(p);
			}
		}

	};
}