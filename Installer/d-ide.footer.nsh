
;--------------------------------------------------------
; Write the unistaller
;--------------------------------------------------------
Section "-Write Uninstaller" write_uninstaller_id
	WriteUninstaller "$INSTDIR\Uninstall.exe"

	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\D-IDE" "DisplayName" "D-IDE"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\D-IDE" "UninstallString" "$\"$INSTDIR\Uninstall.exe$\""
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\D-IDE" "QuietUninstallString" "$\"$INSTDIR\Uninstall.exe$\" /S"
SectionEnd

;--------------------------------------------------------
; Create start menu and desktop shortcuts
;--------------------------------------------------------
Section "Sample Projects" samples_section_id
	;sample project copied here
	CreateDirectory "$INSTDIR\Samples"
SectionEnd

;--------------------------------------------------------
; Create start menu shortcuts
;--------------------------------------------------------
Section "Start Menu Shortcuts" start_menu_section_id

	CreateDirectory "$SMPROGRAMS\D-IDE"
	CreateShortCut "$SMPROGRAMS\D-IDE\D-IDE.lnk" "$INSTDIR\D-IDE.exe" "" "$INSTDIR\D-IDE.exe" 0
	CreateShortCut "$SMPROGRAMS\D-IDE\Sample Programs.lnk" "$INSTDIR\Samples"
	CreateShortCut "$SMPROGRAMS\D-IDE\Uninstall.lnk" "$INSTDIR\Uninstall.exe" "" "$INSTDIR\Uninstall.exe" 0
SectionEnd

;--------------------------------------------------------
; Create desktop shortcut
;--------------------------------------------------------
Section "Desktop Shortcut" desktop_section_id
	CreateShortCut "$DESKTOP\D-IDE.lnk" "$INSTDIR\D-IDE.exe" "" "$INSTDIR\D-IDE.exe" 0
SectionEnd

;--------------------------------------------------------
; Launch Program After Install with popup dialog
;--------------------------------------------------------
;Section /o "Launch D-IDE"
Section  "Launch D-IDE"
	ExecShell open "$INSTDIR\D-IDE.exe" SW_SHOWNORMAL
SectionEnd

;--------------------------------------------------------
; Uninstaller Section
;--------------------------------------------------------
Section "Uninstall"
	DetailPrint "Remove D-IDE Files"

	Delete "$SMPROGRAMS\D-IDE\*.*"
	RMDir "$SMPROGRAMS\D-IDE"
	
	Delete "$DESKTOP\D-IDE.lnk"

	Delete "$INSTDIR\*.*"
	RMDir "$INSTDIR"

	DeleteRegKey /ifempty HKLM "Software\D-IDE"
	DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\D-IDE"
SectionEnd

;--------------------------------------------------------
; Detects Microsoft .Net Framework 4
;--------------------------------------------------------
Function DotNet4Exists
	ClearErrors
	ReadRegStr $1 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" "Version"
	IfErrors MDNFFullNotFound MDNFFound

	MDNFFullNotFound:
		ReadRegStr $1 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client" "Version"
		IfErrors MDNFNotFound MDNFFound

	MDNFFound:
		Push 1
		Goto ExitFunction

	MDNFNotFound:
		Push 0
		Goto ExitFunction

	ExitFunction:
FunctionEnd

;--------------------------------------------------------
; Detects Visual C++ 2010 Runtime
;--------------------------------------------------------
Function VisualCPP2010RuntimeExists
	ClearErrors
	ReadRegDWORD $1 HKLM "SOFTWARE\Microsoft\VisualStudio\10.0\VC\VCRedist\x86" "Installed"
	IfErrors VCPP2010TryAgain VCPP2010Found
	VCPP2010TryAgain:
		ReadRegDWORD $1 HKLM "SOFTWARE\Microsoft\VisualStudio\10.0\VC\VCRedist\x64" "Installed"
		IfErrors VCPP2010TryYetAgain VCPP2010Found

	VCPP2010TryYetAgain:
		ReadRegDWORD $1 HKLM "SOFTWARE\Microsoft\VisualStudio\10.0\VC\VCRedist\ia64" "Installed"
		IfErrors VCPP2010NotFound VCPP2010Found

	VCPP2010Found:
		Push 0
		Goto ExitFunction

	VCPP2010NotFound:
		Push 1
		Goto ExitFunction

	ExitFunction:
FunctionEnd

;--------------------------------------------------------
; Detects tbe internet
;--------------------------------------------------------
Function IsConnected
	Push $R0
	ClearErrors
	Dialer::AttemptConnect
	IfErrors noie3
	Pop $R0

	StrCmp $R0 "online" maybeConnected
		Push 0
		Goto exitFunction

	noie3:  ; IE3 not installed
		Push 0
		Goto exitFunction

	maybeConnected:
		Dialer::GetConnectedState
		Pop $R0
		StrCmp $R0 "online" connected
		Push 0
		Goto exitFunction

	connected:
		Push 1

	exitFunction:
FunctionEnd
