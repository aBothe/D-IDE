
#pragma once
#include "DebugEngineWrapper.h"

using namespace System;
using namespace DebugEngineWrapper;

namespace DebugEngineWrapper
{
	ref class DBGEngine;





	class OutputClass : public  IDebugOutputCallbacksWide
	{
	private:
		LONG  m_refCount;
		gcroot<DBGEngine^> dbg;

	public:
		OutputClass(DBGEngine^ eng)
		{
			this->dbg=eng;
		};

		HRESULT __stdcall Output(
			IN ULONG  Mask,
			IN PCWSTR  Text
			)
		{
			dbg->RaiseOutput(static_cast<OutputFlags>(Mask),gcnew String(Text));
			return 0;
		}

		STDMETHODIMP_(ULONG) AddRef(THIS) 
		{
			InterlockedIncrement(&m_refCount);
			return m_refCount;
		}

		STDMETHODIMP_(ULONG) Release(THIS) 
		{
			LONG retVal;
			InterlockedDecrement(&m_refCount);
			retVal = m_refCount;
			if (retVal == 0) 
			{
				delete this;
			}
			return retVal;
		}

		STDMETHODIMP QueryInterface(THIS_
			IN REFIID interfaceId,
			OUT PVOID* ppInterface) 
		{
			*ppInterface = 0;
			HRESULT res = E_NOINTERFACE;
			if (TRUE == IsEqualIID(interfaceId, __uuidof(IUnknown)) || TRUE == IsEqualIID(interfaceId, __uuidof(IDebugOutputCallbacks))) 
			{
				*ppInterface = (IDebugOutputCallbacks*) this;
				AddRef();
				res = S_OK;
			}
			return res;
		}
	};





	class EventsClass : public  IDebugEventCallbacksWide
	{
	private:
		LONG  m_refCount;
		gcroot<DBGEngine^> dbg;

	public:
		EventsClass(DBGEngine^ eng)
		{
			this->dbg=eng;
		};

#pragma region Here the events get received

		HRESULT __stdcall ChangeDebuggeeState(    IN ULONG  Flags,    IN ULONG64  Argument    )
		{
			return 0;
		}

		HRESULT  __stdcall ChangeEngineState(    IN ULONG  Flags,    IN ULONG64  Argument    )
		{
			return 0;
		}

		HRESULT __stdcall Exception(    IN PEXCEPTION_RECORD64  ex,    IN ULONG  FirstChance    )
		{
			CodeException^ mex=gcnew CodeException();
			mex->IsFirstChance=FirstChance!=0;
			mex->Type=/*(ExceptionType)*/ex->ExceptionCode;
			mex->IsContinuable=ex->ExceptionFlags==0;
			mex->Address=ex->ExceptionAddress;

			mex->argc=ex->NumberParameters;
			mex->argv=(ULONG64**)ex->ExceptionInformation;
			return (HRESULT)dbg->RaiseEx(mex);
		}

		HRESULT __stdcall GetInterestMask(    OUT PULONG  Mask    )
		{
			// Notice: Never forget this ***king notation for pointer assignments :-)
			*Mask=DEBUG_EVENT_EXCEPTION
				+DEBUG_EVENT_BREAKPOINT
				+DEBUG_EVENT_CREATE_THREAD
				+DEBUG_EVENT_EXIT_THREAD
				+DEBUG_EVENT_CREATE_PROCESS
				+DEBUG_EVENT_LOAD_MODULE
				+DEBUG_EVENT_UNLOAD_MODULE
				+DEBUG_EVENT_EXIT_PROCESS
				+DEBUG_EVENT_SYSTEM_ERROR
				+DEBUG_EVENT_SESSION_STATUS
				+DEBUG_EVENT_CHANGE_SYMBOL_STATE
				;
			return 0;
		}

				HRESULT __stdcall Breakpoint(    IN IDebugBreakpoint2 *  Bp    )
		{
			ULONG id=0;
			Bp->GetId(&id);

			wchar_t* cmdbuf=new wchar_t[512];// 
			ULONG len=0;
			Bp->GetCommandWide((PWSTR)cmdbuf,512,&len);
			String^ cmd=gcnew String(cmdbuf);
			delete [] cmdbuf;

			ULONG64 off=0;
			Bp->GetOffset(&off);

			wchar_t* expbuf=new wchar_t[512];
			len=0;
			Bp->GetOffsetExpressionWide((PWSTR)expbuf,512,&len);
			String^ exp=gcnew String(expbuf);
			delete [] expbuf;

			return (HRESULT)dbg->RaiseBP(id,cmd,off,exp);
		}

		HRESULT __stdcall LoadModule(
			IN ULONG64  ImageFileHandle,
			IN ULONG64  BaseOffset,
			IN ULONG  ModuleSize,
			IN PCWSTR  ModuleName,
			IN PCWSTR  ImageName,
			IN ULONG  CheckSum,
			IN ULONG  Timestamp
			)
		{
			return (HRESULT)dbg->RaiseLoadModuleHandler(BaseOffset,ModuleSize,gcnew String(ImageName),CheckSum,Timestamp);
		}

		HRESULT __stdcall UnloadModule(
			IN PCWSTR  ImageBaseName,
			IN ULONG64  BaseOffset
			)
		{
			return (HRESULT)dbg->RaiseUnloadModuleHandler(BaseOffset,gcnew String(ImageBaseName));
		}

		HRESULT __stdcall CreateProcess(
			IN ULONG64  ImageFileHandle,
			IN ULONG64  Handle,
			IN ULONG64  BaseOffset,
			IN ULONG  ModuleSize,
			IN PCWSTR  ModuleName,
			IN PCWSTR  ImageName,
			IN ULONG  CheckSum,
			IN ULONG  TimeDateStamp,
			IN ULONG64  InitialThreadHandle,
			IN ULONG64  ThreadDataOffset,
			IN ULONG64  StartOffset
			)
		{
			return (HRESULT)dbg->RaiseCrProc(BaseOffset,ModuleSize,gcnew String(ModuleName),CheckSum,TimeDateStamp);
		}

		HRESULT __stdcall ExitProcess(    IN ULONG  ExitCode    )
		{
			return (HRESULT)dbg->RaiseExitPr(ExitCode);
		}

		HRESULT __stdcall SessionStatus(    IN ULONG  Status    )
		{
			return (HRESULT)dbg->RaiseSessSt((DebugEngineWrapper::SessionStatus)Status);
		}

		HRESULT __stdcall ChangeSymbolState(    IN ULONG  Flags,    IN ULONG64  Argument    )
		{
			return 0;
		}

		HRESULT __stdcall SystemError(    IN ULONG  Error,    IN ULONG  Level    )
		{
			return 0;
		}

		HRESULT __stdcall CreateThread(    IN ULONG64  Handle,    IN ULONG64  DataOffset,    IN ULONG64  StartOffset    )
		{
			return 0;
		}

		HRESULT __stdcall ExitThread(    IN ULONG  ExitCode    )
		{
			return 0;
		}

#pragma endregion

		STDMETHODIMP_(ULONG) AddRef(THIS) 
		{
			InterlockedIncrement(&m_refCount);
			return m_refCount;
		}

		STDMETHODIMP_(ULONG) Release(THIS) 
		{
			LONG retVal;
			InterlockedDecrement(&m_refCount);
			retVal = m_refCount;
			if (retVal == 0) 
			{
				delete this;
			}
			return retVal;
		}

		STDMETHODIMP QueryInterface(THIS_
			IN REFIID interfaceId,
			OUT PVOID* ppInterface) 
		{
			*ppInterface = 0;
			HRESULT res = E_NOINTERFACE;
			if (TRUE == IsEqualIID(interfaceId, __uuidof(IUnknown)) || TRUE == IsEqualIID(interfaceId, __uuidof(IDebugEventCallbacks))) 
			{
				*ppInterface = (IDebugEventCallbacks*) this;
				AddRef();
				res = S_OK;
			}
			return res;
		}
	};






















	// Debug engine ctor must be placed here because c++ wants OutputClass to be defined -.-
	void DBGEngine::AssignCallbacks()
	{
		client->SetEventCallbacksWide((PDEBUG_EVENT_CALLBACKS_WIDE) new EventsClass(this));
		client->SetOutputCallbacksWide((PDEBUG_OUTPUT_CALLBACKS_WIDE) new OutputClass(this));
	}

}