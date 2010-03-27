!include "MUI2.nsh"

;--------------------------------------------------------
; Setting custom variables and constants
;--------------------------------------------------------
!define BINARY_APPLICATION_FILES "..\D-IDE\D-IDE\bin\Release"
!define THIRD_PARTY_FILES "..\externalDeps"

!define DNF35_URL "http://download.microsoft.com/download/6/0/f/60fc5854-3cb8-4892-b6db-bd4f42510f28/dotnetfx35.exe"
!define DMD_URL "http://ftp.digitalmars.com/dinstaller.exe"

!define /date FILEDATE "%Y%m%d"
!define /date DATE "%Y.%m.%d"

Var INSTALLTYPE
Var SERVICEUSERNAME
Var SERVICEPASSWORD
Var DOMAIN_PART
Var USER_PART
 
;--------------------------------------------------------
; Setting up a macro for the IndexOf string function
;--------------------------------------------------------
!macro IndexOf Var Str Char
	Push "${Char}"
	Push "${Str}"
	Call IndexOf
	Pop "${Var}"
!macroend

!define IndexOf "!insertmacro IndexOf"

;--------------------------------------------------------
; Setting various predefined NSIS Variables
;--------------------------------------------------------
Name "D-IDE"
OutFile "${FILEDATE}_d-ide.exe"
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
FunctionEnd

;--------------------------------------------------------
; Custom MUI (Modern UI) Configuration
;--------------------------------------------------------
!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_BITMAP ".\D-IDE_Logo.bmp" ; optional
!define MUI_ABORTWARNING

;--------------------------------------------------------
; Show the license page with License.rtf
;--------------------------------------------------------
!insertmacro MUI_PAGE_LICENSE ".\License.rtf"

;--------------------------------------------------------
; (Installer) Show the program files directory picker page
;--------------------------------------------------------
!insertmacro MUI_PAGE_DIRECTORY

;--------------------------------------------------------
; (Installer) Show the install progress page
;--------------------------------------------------------
!insertmacro MUI_PAGE_INSTFILES

;--------------------------------------------------------
; (Uninstaller) Show the uninstaller confirmation page
;--------------------------------------------------------
!insertmacro MUI_UNPAGE_CONFIRM

;--------------------------------------------------------
; (Uninstaller) Show the uninstall progress page
;--------------------------------------------------------
!insertmacro MUI_UNPAGE_INSTFILES

;--------------------------------------------------------
; Add english language support
;--------------------------------------------------------
!insertmacro MUI_LANGUAGE "English"

;--------------------------------------------------------
; Setup Icons
;--------------------------------------------------------
Icon "..\D-IDE\D-IDE\d-ide.ico"
UninstallIcon "..\D-IDE\D-IDE\d-ide.ico"

;--------------------------------------------------------
; Download and install the .Net Framework 3.5
;--------------------------------------------------------
Section "-.Net Framework 3.5" net35_section_id
	Call DotNet35Exists
	Pop $1
	IntCmp $1 0 SkipDotNet35
	
	StrCpy $1 "dotnetfx35.exe"
	StrCpy $2 "$EXEDIR\$1"
	IfFileExists $2 FileExistsAlready FileMissing
		
	FileMissing:
		DetailPrint ".Net Framework 3.5 not installed... Downloading file."
		StrCpy $2 "$TEMP\$1"
		NSISdl::download "${DNF35_URL}" $2

	FileExistsAlready:
		DetailPrint "Installing the .Net Framework 3.5."
		;ExecWait '"$SYSDIR\msiexec.exe" "$2" /quiet'
		ExecWait '"$2" /quiet'

		Call DotNet35Exists
		Pop $1
		IntCmp $1 0 DotNet35Done DotNet35Failed

	DotNet35Failed:
		DetailPrint ".Net Framework 3.5 install failed... Aborting Install"
		MessageBox MB_OK ".Net Framework 3.5 install failed... Aborting Install"
		Abort
		
	SkipDotNet35:
		DetailPrint ".Net Framework 3.5 found... Continuing."
		
	DotNet35Done:
SectionEnd

;--------------------------------------------------------
; Install the D-IDE program files
;--------------------------------------------------------
Section "-Install Program Files" install_section_id
	CreateDirectory "$INSTDIR"
	SetOutPath "$INSTDIR"

	SetOverwrite on
	File /nonfatal /x .svn "${BINARY_APPLICATION_FILES}\*.exe"
	File /nonfatal /x .svn "${BINARY_APPLICATION_FILES}\*.dll"

	;Config files that should be merged through a seperate process - we need to copy them to a backup folder
	;IfFileExists "$INSTDIR\D-IDEIndexer.exe.config" 0 +4
	;	CreateDirectory "$INSTDIR\ConfigBackup"
	;	Rename "$INSTDIR\D-IDEIndexer.exe.config" "$INSTDIR\ConfigBackup\D-IDEIndexer.exe.config.${FILEDATE}"
	;	Delete "$INSTDIR\D-IDEIndexer.exe.config"

	;IfFileExists "$INSTDIR\D-IDEService.exe.config" 0 +4
	;	CreateDirectory "$INSTDIR\ConfigBackup"
	;	Rename "$INSTDIR\D-IDEService.exe.config" "$INSTDIR\ConfigBackup\D-IDEService.exe.config.${FILEDATE}"
	;	Delete "$INSTDIR\D-IDEService.exe.config"

	;IfFileExists "$INSTDIR\Quartz.xml" 0 +4
	;	CreateDirectory "$INSTDIR\ConfigBackup"
	;	Rename "$INSTDIR\Quartz.xml" "$INSTDIR\ConfigBackup\Quartz.xml.${FILEDATE}"
	;	Delete "$INSTDIR\Quartz.xml"

	;File /oname=D-IDEIndexer.exe.config "${BINARY_APPLICATION_FILES}\D-IDEIndexer.exe.config"
	;File /oname=D-IDEService.exe.config "${BINARY_APPLICATION_FILES}\D-IDEService.exe.config"
	;File /oname=Quartz.xml "${BINARY_APPLICATION_FILES}\Quartz.xml"

	WriteRegStr HKLM "Software\D-IDE" "" $INSTDIR
SectionEnd

;--------------------------------------------------------
; Write the unistaller
;--------------------------------------------------------
Section "-Write Uninstaller" write_uninstaller_id
	WriteUninstaller "$INSTDIR\Uninstall.exe"	
	
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\D-IDE" "DisplayName" "D-IDE"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\D-IDE" "UninstallString" "$\"$INSTDIR\uninstall.exe$\""
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\D-IDE" "QuietUninstallString" "$\"$INSTDIR\Uninstall.exe$\" /S"
SectionEnd

;--------------------------------------------------------
; Create start menu and desktop shortcuts
;--------------------------------------------------------
Section "-Start Menu Shortcuts" start_menu_section_id

	CreateDirectory "$SMPROGRAMS\D-IDE"
	CreateShortCut "$SMPROGRAMS\D-IDE\D-IDE.lnk" "$INSTDIR\D-IDE.exe" "" "$INSTDIR\D-IDE.exe" 0
	CreateShortCut "$SMPROGRAMS\D-IDE\Uninstall.lnk" "$INSTDIR\Uninstall.exe" "" "$INSTDIR\Uninstall.exe" 0
	
	CreateShortCut "$DESKTOP\D-IDE.lnk" "$INSTDIR\D-IDE.exe" "" "$INSTDIR\D-IDE.exe" 0
SectionEnd

;--------------------------------------------------------
; Launch Program After Install with popup dialog 
;--------------------------------------------------------
Section
	MessageBox MB_YESNO|MB_ICONQUESTION "Open D-IDE now?" IDNO DontLaunchThingy
		ExecShell open "$INSTDIR\D-IDE.exe" SW_SHOWNORMAL
		Quit
	DontLaunchThingy:
SectionEnd


;--------------------------------------------------------
; Uninstaller Section
;--------------------------------------------------------
Section "Uninstall"
	DetailPrint "Remove D-IDE Files"

	Delete "$SMPROGRAMS\D-IDE\*.*"
	RMDir "$SMPROGRAMS\D-IDE"

	Delete $INSTDIR\*.exe
	Delete $INSTDIR\*.dll
	Delete "$INSTDIR\Uninstall.exe"

	RMDir "$INSTDIR"

	DeleteRegKey /ifempty HKLM "Software\D-IDE"
	DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\D-IDE"
SectionEnd

;--------------------------------------------------------
; A custom string function (IndexOf)
;--------------------------------------------------------
Function IndexOf
	Exch $R0
	Exch
	Exch $R1
	Push $R2
	Push $R3

	StrCpy $R3 $R0
	StrCpy $R0 -1
	IntOp $R0 $R0 + 1
	StrCpy $R2 $R3 1 $R0
	StrCmp $R2 "" +2
	StrCmp $R2 $R1 +2 -3

	StrCpy $R0 -1

	Pop $R3
	Pop $R2
	Pop $R1
	Exch $R0
FunctionEnd

;--------------------------------------------------------
; Detects Microsoft .Net Framework 3.5
;--------------------------------------------------------
Function DotNet35Exists
	ClearErrors
	ReadRegStr $1 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.5" "Version"
	IfErrors MDNFNotFound MDNFFound

	MDNFFound:
		Push 0
		Goto ExitFunction

	MDNFNotFound:
		Push 1
		Goto ExitFunction

	ExitFunction:
FunctionEnd

