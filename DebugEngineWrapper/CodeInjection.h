#pragma once
#include "Stdafx.h"

using namespace System;
using namespace System::Diagnostics;
using namespace System::Runtime::InteropServices;
using namespace System::ComponentModel;

namespace DebugEngineWrapper {
public ref class CodeInjection
{
public:

	static ULONG BuildToStringFunctionAssembler(byte* instructions, const ULONG maxSize, const ULONG variableAddress)
	{
		ULONG i=0;
		byte* c = new byte[64];

		// 1) Move the object's address into eax.
		// mov eax, DWORD PTR [var]
		c[i++] = 0xA1;
		memcpy(c+i, &variableAddress, i+=sizeof(variableAddress));

		// 2) Make a pointer out of eax
		// mov ecx,dword ptr ds:[eax]
		c[i++] = 0x8B;
		c[i++] = 0x08;

		// 3) Call the object's virtual toString function (or highest re-implementation)
		// call dword ptr ds:[ecx+4]
		c[i++] = 0xFF;
		c[i++] = 0x51;
		c[i++] = 0x04;

		/*
		 * eax contains the string length
		 * edx contains the address of the first char
		 */

		// 4) Store the string + its length back into the variable (the pointer to the first char with 4 bytes offset)
		// mov dword ptr [var], eax
		c[i++] = 0xA3;
		memcpy(c+i, &variableAddress, i+=sizeof(variableAddress));

		// Write the edx register into the eax one to be able to save it
		// mov eax,edx
		c[i++] = 0x89;
		c[i++] = 0xD0;

		// Store the pointer to the first char
		// mov dword ptr [var], eax
		c[i++] = 0xA3;
		ULONG firstCharAddress = variableAddress+4;
		memcpy(c+i, &firstCharAddress, i+=(sizeof(firstCharAddress)));

		// Return
		// ret
		c[i++] = 0xc3;

		// Some final spacers
		c[i++] = 0xcc; // int3
		c[i++] = 0xcc; // int3
		c[i++] = 0xcc; // int3

		if(i > maxSize)
			return -1;

		memcpy(instructions, c, i);

		delete c;

		return i;
	}

	static BOOL InjectToStringCode(
		const HANDLE hProcess, 
		LPVOID* toStringFuncAddress,
		LPVOID* variableAddress)
	{
		// Allocate the variable (4 bytes for object address + string length / 4 bytes for pointer to the first char)
		*variableAddress = VirtualAllocEx(hProcess, 0, 8, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
		
		if(*variableAddress == 0)
			return 1;

		// Build the toString function
		byte* toStringFunction = new byte[128];
		ULONG codeLength = CodeInjection::BuildToStringFunctionAssembler(toStringFunction, 128, (ULONG)static_cast<ULONG*>(*variableAddress));

		// Allocate the bytes required for the function
		*toStringFuncAddress = VirtualAllocEx(hProcess, 0, codeLength, MEM_COMMIT|MEM_RESERVE, PAGE_EXECUTE_READWRITE);

		if(*toStringFuncAddress == 0)
			return 1;

		// Write into the process memory
		ULONG bytesWritten = 0;
		if(!WriteProcessMemory(hProcess, *toStringFuncAddress, toStringFunction, codeLength, &bytesWritten))
			return 1;

		// Flush instruction cache
		return FlushInstructionCache(hProcess, *toStringFuncAddress, codeLength);
	}

	static void InjectToStringCode(IntPtr^ process,[Out] IntPtr^% toStringFuncAddress, [Out] IntPtr^% variableAddress)
	{
		LPVOID func = NULL;
		LPVOID var = NULL;

		if(!InjectToStringCode(process->ToPointer(), &func, &var))
			throw gcnew Win32Exception(GetLastError());

		toStringFuncAddress = gcnew IntPtr(func);
		variableAddress = gcnew IntPtr(var);
	}
	/*
	static void InjectToStringCode(Process^ process, [Out] IntPtr^% toStringFuncAddress, [Out] IntPtr^% variableAddress)
	{
		ULONG func = 0;
		ULONG var = 0;

		if(!InjectToStringCode(process->Handle.ToPointer(), &func, &var))
			throw gcnew Win32Exception(GetLastError());

		toStringFuncAddress = gcnew IntPtr((int)func);
		variableAddress = gcnew IntPtr((int)var);
	}*/







	// Returns the last error code if an error occurred, otherwise 0
	static int ExecuteMethod(const HANDLE hProcess, const ULONG funcAddress)
	{
		HANDLE thread = CreateRemoteThread(hProcess, 0, 0, (LPTHREAD_START_ROUTINE)&funcAddress, 0, 0, 0);
		if(thread == 0)
			return GetLastError();

		int tt = ResumeThread(thread);

		DWORD singleObject = WaitForSingleObject(thread, INFINITE);
		if(!(singleObject == 0 || singleObject == INFINITE))
			return GetLastError();

		return 0;
	}

	static void ExecuteMethod(IntPtr^ process, IntPtr^ funcAddress)
	{
		int r=0;
		if((r=ExecuteMethod(process->ToPointer(), funcAddress->ToInt32())) != 0)
			throw gcnew Win32Exception(r);
	}


	static String^ EvaluateObjectString(IntPtr^ process, IntPtr^ toStringFunctionAddress, IntPtr^ variableAddress, const ULONG virtualTargetObjectAddress)
	{
		HANDLE hProcess = process->ToPointer();
		byte* varAddr = static_cast<byte*>(variableAddress->ToPointer());
		ULONG bytesWritten = 0;

		ULONG stringLength = 0;

		// Write the object's virtual address into the variable
		if(WriteProcessMemory(hProcess, varAddr, &virtualTargetObjectAddress, sizeof(virtualTargetObjectAddress), &bytesWritten)==0)
			throw gcnew Win32Exception(GetLastError());

		// Execute the injected function
		ExecuteMethod(process, toStringFunctionAddress);

		// Read out the string's length
		if(ReadProcessMemory(hProcess, varAddr, &stringLength, 4, &bytesWritten)==0)
			throw gcnew Win32Exception(GetLastError());

		if(stringLength != 0)
		{
			const ULONG maxLen = 1024;
			char* rawStr = new char[maxLen];

			varAddr += 4;
			if(ReadProcessMemory(hProcess, varAddr, &rawStr, maxLen, &bytesWritten)==0)
				throw gcnew Win32Exception(GetLastError());

			String^ returnedString = gcnew String(rawStr, 0, bytesWritten);

			delete rawStr;

			return returnedString;
		}

		return String::Empty;
	}
};
}
