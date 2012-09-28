// DebugEngineWrapper.h

#pragma once
#include "CodeInjection.h"
#include "stdafx.h"

using namespace System;
using namespace System::Diagnostics;



namespace DebugEngineWrapper {

	public ref class DBGEngine : System::IDisposable
	{
	internal:
		DBGEngine(DbgClient* cl)
		{
			DbgControl * ctrl;
			DbgSymbols * sym;
			DbgDataSpaces* datas;
			//DbgSystemObjects * so;
			
			cl->QueryInterface(__uuidof(DbgControl), (void**)&ctrl);
			cl->QueryInterface(__uuidof(DbgSymbols), (void**)&sym);
			cl->QueryInterface(__uuidof(DbgDataSpaces), (void**)&datas);
			//cl->QueryInterface(__uuidof(DbgSystemObjects), (void**)&so);

			this->client=cl;
			this->control=ctrl;
			//this->systemObjects = so;
			this->Symbols=gcnew DebugSymbols(sym,datas);

			AssignCallbacks();
		}
	public:
		property Process^ MainProcess
		{
			Process^ get()
			{
				return Process::GetProcessById(GetTargetProcessId());
			}
		}
		
		property IntPtr^ ProcessHandle
		{
			IntPtr^ get()
			{
				DbgSystemObjects * so=0;
				ULONG64 p=0;

				client->QueryInterface(__uuidof(DbgSystemObjects), (void**)&so);
				
				so->GetCurrentProcessHandle(&p);
				so->Release();

				return gcnew IntPtr((long long)p);
			}
		}

		//HANDLE hProcess;

		ULONG GetTargetProcessId()
		{
			DbgSystemObjects * so=0;
			ULONG  pid=0;

			client->QueryInterface(__uuidof(DbgSystemObjects), (void**)&so);

			so->GetCurrentProcessSystemId(&pid);

			return pid;
		}

		DebugSymbols^ Symbols;
		DebugDataSpaces^ Memory;

		DBGEngine(DBGEngine^ eng)
		{
			this->client=eng->client;
			this->control=eng->control;
			this->Symbols=eng->Symbols;

			AssignCallbacks();
		}

		DBGEngine ()
		{
			HRESULT hr = E_FAIL;

			client=nullptr;
			control=nullptr;

			DbgClient * cl;
			DbgControl * ctrl;
			DbgSymbols * sym;
			DbgDataSpaces* ds;

			// Create the base IDebugClient object
			hr = DebugCreate(__uuidof(DbgClient), (void**)&cl);

			// from the base, create the Control and Symbols objects
			hr = cl->QueryInterface(__uuidof(DbgControl), (void**)&ctrl);

			hr = cl->QueryInterface(__uuidof(DbgSymbols), (void**)&sym);

			hr = cl->QueryInterface(__uuidof(DbgDataSpaces), (void**)&ds);

			this->client=cl;
			this->control=ctrl;
			this->Symbols=gcnew DebugSymbols(sym,ds);
			this->Memory=gcnew DebugDataSpaces(ds);

			this->AssignCallbacks();

			IsSourceCodeOrientedStepping=true;
		}

	private:
		void AssignCallbacks();
	public:

		~DBGEngine()
		{
			try{
			MainProcess->Kill();
			}catch(...){}
			Terminate();
			
			delete Symbols;
			
			this->!DBGEngine();
		}

		!DBGEngine()
		{
			if(NULL != client) client->Release();
			if(NULL != control) control->Release();
			//if(NULL!= systemObjects) systemObjects->Release();
		}

		bool CreateProcessAndAttach(ULONG64 server, String ^commandLine, DebugCreateProcessOptions options, String ^initialDirectory, String ^environment, ULONG processId, AttachFlags flags)
		{
			pin_ptr<const wchar_t> pCommandLine = PtrToStringChars(commandLine);
			pin_ptr<const wchar_t> pInitialDirectory = PtrToStringChars(initialDirectory);
			pin_ptr<const wchar_t> pEnvironment = PtrToStringChars(environment);

			HRESULT res = client->CreateProcessAndAttach2Wide(server, 
				(PWSTR)pCommandLine, 
				&options.ToLegacy(), 
				sizeof(DEBUG_CREATE_PROCESS_OPTIONS), 
				pInitialDirectory, 
				pEnvironment, 
				processId, 
				(ULONG)flags);

			if(res!=S_OK)
				throw gcnew Win32Exception(GetLastError());
			return res;
		}

		bool CreateProcessAndAttach(String ^commandLine, String ^initialDirectory)
		{
			DebugCreateProcessOptions options;
			options.CreateFlags = CreateFlags::DebugOnlyThisProcess;

			return this->CreateProcessAndAttach(0, commandLine, options, initialDirectory, nullptr, 0, AttachFlags::InvasiveNoInitialBreak);
		}

		property bool IsSourceCodeOrientedStepping
		{
			void set(bool v)
			{
				control->SetCodeLevel(v?DEBUG_LEVEL_SOURCE:DEBUG_LEVEL_ASSEMBLY);
			}
		}

		property StackFrame^ CurrentFrame
		{
			StackFrame^ get()
			{
				ULONG num=0;
				DEBUG_STACK_FRAME buf;
				control->GetStackTrace(0,0,0,&buf,1,&num);
				
				StackFrame^ sf=gcnew StackFrame(buf);
				//delete [] buf;

				return sf;
			}
		}

		property ULONG64 CurrentInstructionOffset
		{
			ULONG64 get()
			{
				DbgRegisters* reg=nullptr;
				client->QueryInterface(__uuidof(DbgRegisters), (void**)&reg);
				
				ULONG64 ret=0;
				reg->GetInstructionOffset2(DEBUG_REGSRC_FRAME,&ret);
				reg->Release();
				return ret;
			}
		}

		property ULONG64 CurrentFrameOffset
		{
			ULONG64 get()
			{
				DbgRegisters* reg=nullptr;
				client->QueryInterface(__uuidof(DbgRegisters), (void**)&reg);

				ULONG64 ret=0;
				reg->GetFrameOffset2(DEBUG_REGSRC_FRAME,&ret);
				reg->Release();
				return ret;
			}
		}

		property array<StackFrame^>^ CallStack
		{
			array<StackFrame^>^ get()
			{
				System::Collections::ArrayList^ al=gcnew System::Collections::ArrayList();
				
				ULONG num=0;
				PDEBUG_STACK_FRAME buf=new DEBUG_STACK_FRAME[60];
				control->GetStackTrace(0,0,0,buf,60,&num);

				for(ULONG i=0;i<num;i++)
				{
					al->Add(gcnew StackFrame(buf[i]));
				}

				delete [] buf;

				return reinterpret_cast<array<StackFrame^>^>(al->ToArray());
			}
		};

		property ULONG ExitCode
		{
			ULONG get()
			{
				ULONG c=0;
				try{
				client->GetExitCode(&c);
				}catch(...){return 0;}
				return c;
			}
		}

		property bool IsRunning
		{
			bool get()
			{
				return ExitCode==STILL_ACTIVE;
			}
		}

		void Terminate()
		{
			client->TerminateProcesses();
			client->DetachProcesses();
		}

		void EndPendingWaits()
		{
			control->SetInterrupt(DEBUG_INTERRUPT_EXIT);
		}
		
		void Interrupt(IntPtr^ process)
		{
			if(!DebugBreakProcess(process->ToPointer()))
				throw gcnew Win32Exception(GetLastError());
		}

		property ULONG InterruptTimeOut
		{
			ULONG get()
			{
				ULONG ret=0;
				control->GetInterruptTimeout(&ret);
				return ret;
			}

			void set(ULONG v)
			{
				control->SetInterruptTimeout(v);
			}
		}

#pragma region Breakpoints
		BreakPoint^ AddBreakPoint(BreakPointOptions flags)
		{
			DbgBreakPoint *bp;
			control->AddBreakpoint2(DEBUG_BREAKPOINT_CODE,DEBUG_ANY_ID,&bp);
			BreakPoint^ ret=gcnew BreakPoint(bp);
			ret->Flags=flags;
			return ret;
		}

		void RemoveBreakPoint(BreakPoint^ Bp)
		{
			control->RemoveBreakpoint2(Bp->bp);
			delete Bp;
		}

		BreakPoint^ GetBreakPointById(ULONG Id)
		{
			DbgBreakPoint *bp;
			control->GetBreakpointById(Id,(PDEBUG_BREAKPOINT*)&bp);
			return gcnew BreakPoint(bp);
		}

		BreakPoint^ GetBreakPointByIndex(ULONG i)
		{
			DbgBreakPoint *bp;
			control->GetBreakpointByIndex(i,(PDEBUG_BREAKPOINT*)&bp);
			return gcnew BreakPoint(bp);
		}

		property ULONG BreakpointCount
		{
			ULONG get()
			{
				ULONG ret;
				control->GetNumberBreakpoints(&ret);
				return ret;
			}
		}

		property array<BreakPoint^>^ Breakpoints
		{
			array<BreakPoint^>^ get()
			{
				ULONG c=BreakpointCount;
				List<BreakPoint^>^ ret=gcnew List<BreakPoint^>(c);

				for(ULONG i=0;i<c;i++)
					ret->Add(GetBreakPointByIndex(i));

				return ret->ToArray();
			}
		}
#pragma endregion

		property DebugStatus ExecutionStatus
		{
			void set(DebugStatus v)
			{
				control->SetExecutionStatus((ULONG)v);
			}
			DebugStatus get()
			{
				ULONG ret=0;
				control->GetExecutionStatus((ULONG*)ret);
				return (DebugStatus)ret;
			}
		}

		WaitResult WaitForEvent(ULONG timeout)
		{
			return (WaitResult)this->control->WaitForEvent(0,timeout);
		}

		WaitResult WaitForEvent()
		{
			return WaitForEvent(INFINITE);
		}

		void Execute(DebugOutputControl control, String ^command, DebugExecuteFlags flags)
		{
			//HRESULT
			//  IDebugControl4::ExecuteWide(
			//    IN ULONG  OutputControl,
			//    IN PCWSTR  Command,
			//    IN ULONG  Flags
			//    );
			pin_ptr<const wchar_t> pCommand = PtrToStringChars(command);
			this->control->ExecuteWide((ULONG)control, pCommand, (ULONG)flags);
		}

		void Execute(String^ command)
		{
			Execute(DebugOutputControl::ThisClient, command, DebugExecuteFlags::Default);
		}

#pragma region Inputs, Events, Outputs

		delegate DebugStatus BreakPointHandler(ULONG Id, String^ Command, ULONG64 Offset, String^ SymbolName);
		delegate DebugStatus ExcHandler(CodeException^ Ex);
		delegate DebugStatus LoadModuleHandler( ULONG64 BaseOffset, ULONG ModuleSize, String^ File, ULONG Checksum, ULONG Timestamp);
		delegate DebugStatus UnloadModuleHandler( ULONG64 BaseOffset, String^ File);
		delegate DebugStatus CreateProcHandler( ULONG64 BaseOffset, ULONG ModuleSize, String^ ModuleName, ULONG Checksum, ULONG Timestamp);
		delegate String^ InputHandler( ULONG RequestedLength);
		delegate void OutputHandler( OutputFlags OutputType,String^ Message);
		delegate DebugStatus ExitProcHandler( ULONG ExitCode);
		delegate DebugStatus SessionStatusHandler( SessionStatus Status);

		event BreakPointHandler^ OnBreakPoint;
		event ExcHandler^ OnException;
		event LoadModuleHandler^ OnLoadModule;
		event UnloadModuleHandler^ OnUnloadModule;
		event CreateProcHandler^ OnCreateProcess;
		event InputHandler^ InputRequest;
		event OutputHandler^ Output;
		event ExitProcHandler^ OnExitProcess;
		event SessionStatusHandler^ OnSessionStatusChanged;
	internal:
		
		DebugStatus RaiseBP(ULONG Id, String^ Command, ULONG64 Offset, String^ SymbolName)
		{	return OnBreakPoint(Id,Command,Offset,SymbolName);	}
		DebugStatus RaiseEx(CodeException^ Ex)
		{	return OnException(Ex);	}
		DebugStatus RaiseLoadModuleHandler( ULONG64 BaseOffset, ULONG ModuleSize, String^ ModuleName, ULONG Checksum, ULONG Timestamp)
		{	return OnLoadModule(BaseOffset,ModuleSize,ModuleName,Checksum,Timestamp);	}
		DebugStatus RaiseUnloadModuleHandler( ULONG64 BaseOffset, String^ ModuleName)
		{	return OnUnloadModule(BaseOffset,ModuleName);	}
		DebugStatus RaiseCrProc( ULONG64 BaseOffset, ULONG ModuleSize, String^ ModuleName, ULONG Checksum, ULONG Timestamp)
		{	return OnCreateProcess(BaseOffset,ModuleSize,ModuleName,Checksum,Timestamp);	}
		DebugStatus RaiseExitPr(ULONG c)
		{	return OnExitProcess(c);	}
		DebugStatus RaiseSessSt(SessionStatus s)
		{	return OnSessionStatusChanged(s);	}

		void ReqInput( ULONG ReqLength)
		{	String^ ret=InputRequest(ReqLength);
		pin_ptr<const wchar_t> ptr = PtrToStringChars(ret);
			control->ReturnInputWide(ptr);
			}
		void RaiseOutput( OutputFlags of,String^ s)
		{	Output(of,s);	}
#pragma endregion

		//DbgSystemObjects * systemObjects;
		DbgClient * client;
		DbgControl * control;
	};
}