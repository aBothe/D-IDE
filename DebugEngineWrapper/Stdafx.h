// stdafx.h : Includedatei für Standardsystem-Includedateien
// oder häufig verwendete projektspezifische Includedateien,
// die nur in unregelmäßigen Abständen geändert werden.

#pragma once
#include "windows.h"
#include "dbghelp.h"
#include "wdbgexts.h"
WINDBG_EXTENSION_APIS  ExtensionApis;
#include "dbgeng.h"
#include "tools.h"

typedef IDebugAdvanced3 DbgAdvanced;
typedef IDebugClient5 DbgClient;
typedef IDebugControl4 DbgControl;
typedef IDebugDataSpaces4 DbgDataSpaces;
typedef IDebugRegisters2 DbgRegisters;
typedef IDebugSymbols3 DbgSymbols; 
typedef IDebugSystemObjects4 DbgSystemObjects;
typedef IDebugBreakpoint2 DbgBreakPoint;
typedef IDebugSymbolGroup2 DbgSymbolGroup;

#include "enums.h"
#include "vcclr.h"
#include "MemoryManagement.h"
#include "CodeViewAnalyzer.h"
#include "SymbolExtracter.h"
#include "Breakpoint.h"
#include "Symbols.h"
#include "StackFrame.h"
#include "CommunicationInterfaces.h"



