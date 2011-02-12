;--------------------------------------------------------
; Include UltraModernUI
;--------------------------------------------------------
!include UMUI.nsh

;--------------------------------------------------------
; Setting custom variables and constants
;--------------------------------------------------------
!define BINARY_APPLICATION_FILES "..\D-IDE2\bin\Release"
!define THIRD_PARTY_FILES "..\Misc"
!define CLR_INSTALLER_HELPER ".\"

!define DNF4_URL "http://download.microsoft.com/download/1/B/E/1BE39E79-7E39-46A3-96FF-047F95396215/dotNetFx40_Full_setup.exe"
!define VCPPR2010_URL "http://download.microsoft.com/download/5/B/C/5BC5DBB3-652D-4DCE-B14A-475AB85EEF6E/vcredist_x86.exe"

!define /date FILEDATE "%Y%m%d"
!define /date DATE "%Y.%m.%d"

Var IS_CONNECTED
Var CONFIG_DIR

;--------------------------------------------------------
; Setting various predefined NSIS Variables
;--------------------------------------------------------
Name "D-IDE 2"
OutFile ".\Builds\D-IDE2.${FILEDATE}.exe"
BrandingText "Alexander Bothe"
InstallDir $PROGRAMFILES\D-IDE
InstallDirRegKey HKLM "Software\D-IDE" "Install_Dir"
RequestExecutionLevel highest

;--------------------------------------------------------
; The .onInit function is a predifined function that
; runs before the installer displays the first form.
;--------------------------------------------------------
Function .onInit
	InitPluginsDir

	SetOutPath $PLUGINSDIR
	File "DIDE.Installer.dll"
	
	SetShellVarContext all
	StrCpy $CONFIG_DIR "$APPDATA\D-IDE.config"
	
	Call IsConnected
	Pop $IS_CONNECTED

FunctionEnd

;--------------------------------------------------------
; The .onGUIEnd function is a predifined function that
; runs when the Gui is closed.
;--------------------------------------------------------
Function .onInstFailed
    CLR::Destroy
FunctionEnd
Function .onInstSuccess
    CLR::Destroy
FunctionEnd

;--------------------------------------------------------
; Modern UI Configuration
;--------------------------------------------------------
!define UMUI_SKIN "blue"

!define UMUI_PAGEBGIMAGE
!define UMUI_UNPAGEBGIMAGE

!define MUI_HEADERIMAGE_BITMAP ".\d-ide-logo.bmp";
!define MUI_ABORTWARNING
!define MUI_UNABORTWARNING

;Installer Pages
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "License.rtf"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

;Uninstaller Pages
!insertmacro UMUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

;--------------------------------------------------------
; Setup Icons
;--------------------------------------------------------
Icon ".\install.ico"
UninstallIcon ".\uninstall.ico"

;--------------------------------------------------------
; Download and install the .Net Framework 4
;--------------------------------------------------------
Section "-.Net Framework 4" net4_section_id
	Call DotNet4Exists
	Pop $1
	IntCmp $1 1 SkipDotNet4

	StrCpy $1 "dotNetFx40_Full_setup.exe"
	StrCpy $2 "$EXEDIR\$1"
	IfFileExists $2 FileExistsAlready FileMissing

	FileMissing:
		DetailPrint ".Net Framework 4 not installed... Downloading file."
		StrCpy $2 "$TEMP\$1"
		NSISdl::download "${DNF4_URL}" $2

	FileExistsAlready:
		DetailPrint "Installing the .Net Framework 4."
		ExecWait '"$2" /quiet'

		Call DotNet4Exists
		Pop $1
		IntCmp $1 1 DotNet4Done DotNet4Failed

	DotNet4Failed:
		DetailPrint ".Net Framework 4 install failed... Aborting Install"
		MessageBox MB_OK ".Net Framework 4 install failed... Aborting Install"
		Abort

	SkipDotNet4:
		DetailPrint ".Net Framework 4 found... Continuing."

	DotNet4Done:
SectionEnd

;--------------------------------------------------------
; Download and install the Visual C++ 2010 Runtime
;--------------------------------------------------------
Section "-Visual C++ 2010 Runtime" vcpp2010runtime_section_id
	Call VisualCPP2010RuntimeExists
	Pop $1
	IntCmp $1 0 SkipVCPP2010Runtime

	StrCpy $1 "vcredist_x86.exe"
	StrCpy $2 "$EXEDIR\$1"
	IfFileExists $2 FileExistsAlready FileMissing

	FileMissing:
		DetailPrint "Visual C++ 2010 Runtime not installed... Downloading file."
		StrCpy $2 "$TEMP\$1"
		NSISdl::download "${VCPPR2010_URL}" $2

	FileExistsAlready:
		DetailPrint "Installing the Visual C++ 2010 Runtime."
		ExecWait "$2 /q"

		Call VisualCPP2010RuntimeExists
		Pop $1
		IntCmp $1 0 VCPP2010RuntimeDone VCPP2010RuntimeFailed

	VCPP2010RuntimeFailed:
		DetailPrint "Visual C++ 2010 Runtime install failed... Aborting Install"
		MessageBox MB_OK "Visual C++ 2010 Runtime install failed... Aborting Install"
		Abort

	SkipVCPP2010Runtime:
		DetailPrint "Visual C++ 2010 Runtime found... Continuing."

	VCPP2010RuntimeDone:
SectionEnd

;--------------------------------------------------------
; Install the D-IDE program files
;--------------------------------------------------------
Section "-Install Program Files" install_section_id
	CreateDirectory "$INSTDIR"
	CreateDirectory "$CONFIG_DIR"	
	SetOutPath "$INSTDIR"

	SetOverwrite on
	File /nonfatal /x .svn /x *.vshost* "${BINARY_APPLICATION_FILES}\*.exe"
	File /nonfatal /x .svn /x *.vshost* "${BINARY_APPLICATION_FILES}\*.exe.config"
	File /nonfatal /x .svn "${BINARY_APPLICATION_FILES}\*.dll"
	File /nonfatal /x .svn "${BINARY_APPLICATION_FILES}\*.xshd"

	File /nonfatal /x .svn "${THIRD_PARTY_FILES}\*.dll"
	File /oname=DIDE.Installer.dll "${CLR_INSTALLER_HELPER}\DIDE.Installer.dll"

	WriteRegStr HKLM "Software\D-IDE" "" $INSTDIR
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
	CreateShortCut "$DESKTOP\D-IDE.lnk" "$INSTDIR\D-IDE.exe" "" "$INSTDIR\D-IDE.exe" 0
SectionEnd

;--------------------------------------------------------
; Create start menu shortcuts
;--------------------------------------------------------
Section "Start Menu Shortcuts" start_menu_section_id

	CreateDirectory "$SMPROGRAMS\D-IDE"
	CreateShortCut "$SMPROGRAMS\D-IDE\D-IDE.lnk" "$INSTDIR\D-IDE.exe" "" "$INSTDIR\D-IDE.exe" 0
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
