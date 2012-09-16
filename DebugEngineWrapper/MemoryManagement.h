#pragma once
#include "stdafx.h"
#include <cliext/vector>

using namespace System;
using namespace cliext;

namespace DebugEngineWrapper
{
	public ref class DebugDataSpaces
	{
	internal:
		DbgDataSpaces* ds;
		DebugDataSpaces(DbgDataSpaces* dspaces):ds(dspaces){}
	public:
		array<BYTE>^ ReadVirtual(ULONG64 Offset,ULONG Size)
		{
			ULONG readb=0;
			BYTE *ret=new BYTE[Size];
			ds->ReadVirtual(Offset,&ret,Size,&readb);
			
			array<BYTE>^ ret2=gcnew array<BYTE>(readb);
			for(ULONG i=0;i<readb;i++)
			{
				ret2[i]=ret[i];
			}
			delete [] ret;
			return ret2;
		}
		
		BYTE* ReadArray(ULONG64 Offset, ULONG Size)
		{
			ULONG readb=0;
			BYTE *ret=new BYTE[Size];
			ds->ReadVirtual(Offset,&ret,Size,&readb);
			return ret;
		}

		USHORT ReadVirtualByte(ULONG64 Offset)
		{
			USHORT *ret=new USHORT[1];
			ds->ReadVirtual(Offset,&ret,1,0);
			USHORT ret2=ret[0];
			delete [] ret;
			return ret2;
		}

		void Write(){}
	};
}