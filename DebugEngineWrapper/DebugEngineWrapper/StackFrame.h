#pragma once
#include "stdafx.h"

namespace DebugEngineWrapper
{
	public ref struct StackFrame
	{
	private:
		ULONG64 ins;
		ULONG64 ret;
		ULONG64 fr;
		ULONG64 stk;
		ULONG64 fte;
		System::Collections::ArrayList^ al;
		ULONG fnum;
		bool virt;
	internal:
		StackFrame(DEBUG_STACK_FRAME frm)
		{
			ins=frm.InstructionOffset;
			ret=frm.ReturnOffset;
			fr=frm.FrameOffset;
			stk=frm.StackOffset;
			fte=frm.FuncTableEntry;

			al=gcnew System::Collections::ArrayList();
			if(frm.Params[0])al->Add(frm.Params[0]);
			if(frm.Params[1])al->Add(frm.Params[1]);
			if(frm.Params[2])al->Add(frm.Params[2]);
			if(frm.Params[3])al->Add(frm.Params[3]);

			fnum=frm.FrameNumber;
			virt=frm.Virtual==TRUE;
		}
	public:
		property ULONG64 InstructionOffset
		{
			ULONG64 get()
			{
				return ins;
			}
		};

		property ULONG64 ReturnOffset
		{
			ULONG64 get()
			{
				return ret;
			}
		};

		property ULONG64 FrameOffset
		{
			ULONG64 get()
			{
				return fr;
			}
		};

		property ULONG64 StackOffset
		{
			ULONG64 get()
			{
				return stk;
			}
		};

		property ULONG64 FunctionTableEntry
		{
			ULONG64 get()
			{
				return fte;
			}
		};

		property array<ULONG64>^ ArgumentOffsets
		{
			array<ULONG64>^ get()
			{
				return reinterpret_cast<array<ULONG64>^>(al->ToArray(Int64::typeid));
			}
		};

		property ULONG FrameNumber
		{
			ULONG get()
			{
				return fnum;
			}
		};

		property bool IsVirtual
		{
			bool get()
			{
				return virt;
			}
		};
	};
}