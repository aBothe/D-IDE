
!include "FileAssociation.nsh"

;--------------------------------------------------------
; Setup DMD Configuration
;--------------------------------------------------------
Section "-Setup DMD Configuration" configuredmd_section_id

	
	IfFileExists "$CONFIG_DIR\D.config.xml" CheckConfigFile Configure

	CheckConfigFile:
		; copy file to temp area to be checked
		StrCpy $0 "$CONFIG_DIR\D.config.xml"
		StrCpy $1 "$TEMP\D.config.xml"
		StrCpy $2 0 ; only 0 or 1, set 0 to overwrite file if it already exists
		System::Call 'kernel32::CopyFile(t r0, t r1, b r2) ?e'

		CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "IsConfigurationValid" 1 "$TEMP\D.config.xml"
		pop $1
		StrCmp $1 "True" AlreadyConfigured Configure
	
	Configure:
		CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "CreateConfigurationFile" 1 "$TEMP\D.config.xml"
		pop $1
		StrCmp $1 "" +2 0
		DetailPrint $1
		
		MessageBox MB_OK "3"
		StrCpy $0 "$TEMP\D.config.xml"
		StrCpy $1 "$CONFIG_DIR\D.config.xml"
		StrCpy $2 0 ; only 0 or 1, set 0 to overwrite file if it already exists
		System::Call 'kernel32::CopyFile(t r0, t r1, b r2) ?e'
		Delete "$TEMP\D.config.xml"
    
	AlreadyConfigured:
	
SectionEnd

;--------------------------------------------------------
; Install the D-IDE program files
;--------------------------------------------------------
Section "-Install Automatic Updater" updater_section_id

	SetOutPath "$INSTDIR"
	SetOverwrite on
	File /nonfatal /x .svn /x *.vshost* "${BINARY_UPDATER_FILES}\*.exe"
	File /nonfatal /x .svn /x *.vshost* "${BINARY_UPDATER_FILES}\*.dll"
SectionEnd

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
	CreateShortCut "$SMPROGRAMS\D-IDE\Check for Updates.lnk" "$INSTDIR\D-IDE.Updater.exe" "" "$INSTDIR\D-IDE.Updater.exe" 0
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
; Install the Digital Mars D Compiler
;--------------------------------------------------------
Section "Digital Mars D Compiler" dmd_section_id
	
	DetailPrint "Downloading the DMD Web Installer from Digital Mars."
	StrCpy $2 "$TEMP\dmd-installer.${FILEDATE}.exe"
	NSISdl::download "${DMD_URL}" $2
	ExecWait '"$2"'
	
SectionEnd

;--------------------------------------------------------
; Launch Program After Install with popup dialog
;--------------------------------------------------------
;Section /o "Launch D-IDE"
Section  "Launch D-IDE"
	StrCmp $IS_DOT_NET_FRESHLY_INSTALLED "Y" SkipLaunch LaunchApp 
			
	LaunchApp:
		ExecShell open "$INSTDIR\D-IDE.exe"
	
	SkipLaunch:
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
