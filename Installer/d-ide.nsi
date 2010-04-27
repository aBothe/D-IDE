;--------------------------------------------------------
; Include ExperienceUI
;--------------------------------------------------------
!ifdef XPUI_SYSDIR
	!include "${XPUI_SYSDIR}\XPUI.nsh"
!else
	!include "XPUI.nsh"
!endif

;--------------------------------------------------------
; Setting custom variables and constants
;--------------------------------------------------------
!define BINARY_APPLICATION_FILES "..\D-IDE\D-IDE\bin\Release"
!define PROJECT_FILES "..\D-IDE\D-IDE"
!define THIRD_PARTY_FILES "..\externalDeps"
!define CLR_INSTALLER_HELPER ".\"

!define DNF35_URL "http://download.microsoft.com/download/6/0/f/60fc5854-3cb8-4892-b6db-bd4f42510f28/dotnetfx35.exe"
!define DMD_URL "http://ftp.digitalmars.com/dinstaller.exe"

!define /date FILEDATE "%Y%m%d"
!define /date DATE "%Y.%m.%d"

!define TEXT_DMD_CONFIG_TITLE "Digital Marse D Compiler"
!define TEXT_DMD_CONFIG_SUBTITLE "Choose your installation type."

Var DMD1_BIN_PATH
Var DMD2_BIN_PATH

;--------------------------------------------------------
; Setting various predefined NSIS Variables
;--------------------------------------------------------
Name "D-IDE"
OutFile ".\Builds\D-IDE.${FILEDATE}.exe"
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
	!insertmacro XPUI_INSTALLOPTIONS_EXTRACT "dmd-config.ini"
	
	ReadRegStr $DMD1_BIN_PATH HKLM "SOFTWARE\D-IDE" "Dmd1xBinPath"
	ReadRegStr $DMD2_BIN_PATH HKLM "SOFTWARE\D-IDE" "Dmd2xBinPath"
FunctionEnd

;--------------------------------------------------------
; Custom Experience UI Configuration
;--------------------------------------------------------
!define XPUI_ABORTWARNING

${Page} Welcome
${LicensePage} ".\License.rtf"
${Page} Components
${Page} Directory
Page custom DmdConfigPage DmdConfigPageValidation

${Page} Finish

${UnPage} Welcome
!insertmacro XPUI_PAGEMODE_UNINST
!insertmacro XPUI_PAGE_UNINSTCONFIRM_NSIS
!insertmacro XPUI_PAGE_INSTFILES
!insertmacro XPUI_LANGUAGE "English"

ReserveFile "dmd-config.ini"
!insertmacro XPUI_RESERVEFILE_INSTALLOPTIONS

;--------------------------------------------------------
; Setup Icons
;--------------------------------------------------------
Icon ".\install.ico"
UninstallIcon ".\uninstall.ico"

;------------------------------------------------------------------------
; In this section, we shall use a custom configuration page.
;------------------------------------------------------------------------
Function DmdConfigPage
	!insertmacro XPUI_HEADER_TEXT "$(TEXT_DMD_CONFIG_TITLE)" "$(TEXT_DMD_CONFIG_SUBTITLE)"
	!insertmacro XPUI_INSTALLOPTIONS_DISPLAY "dmd-config.ini"
FunctionEnd

Function DmdConfigPageValidation
  CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "GetLatestDMD2Url" 0 ; 5 "mystring1" "x" 10 15.8 false
  pop $0  
  MessageBox MB_OK "Latest DMD 2 URL is: $0"
  CLR::Destroy
FunctionEnd

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
; Download and install Digital Mars DMD
;--------------------------------------------------------
Section /o "Digital Mars DMD" dmd_section_id
	StrCpy $1 "dinstaller.exe"
	StrCpy $2 "$EXEDIR\$1"
	IfFileExists $2 FileExistsAlready FileMissing
		
	FileMissing:
		DetailPrint "Digital Mars DMD not installed... Downloading file."
		StrCpy $2 "$TEMP\$1"
		NSISdl::download "${DMD_URL}" $2

	FileExistsAlready:
		DetailPrint "Installing Digital Mars DMD."
		ExecWait '"$2"'
		
	;DMDDone:
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
	File /nonfatal /x .svn "${PROJECT_FILES}\*.xshd"
	
	File /nonfatal /x .svn "${THIRD_PARTY_FILES}\*.dll"
	File /oname=DIDE.Installer.dll "${CLR_INSTALLER_HELPER}\DIDE.Installer.dll"

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

	WriteRegStr HKLM "Software\D-IDE" "" $INSTDIR
SectionEnd

;--------------------------------------------------------
; Configure DMD
;--------------------------------------------------------
Section "-Configure DMD" configure_dmd_section_id
  CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "GetLatestDMD2Url" 0 ; 5 "mystring1" "x" 10 15.8 false
  pop $0  
  MessageBox MB_OK "Latest DMD 2 URL is: $0"
  CLR::Destroy
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
; Detects Microsoft .Net Framework 3.5
;--------------------------------------------------------
Function DotNet35Exists
	ClearErrors
	ReadRegStr $1 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.5" "Vecrsion"
	IfErrors MDNFNotFound MDNFFound

	MDNFFound:
		Push 0
		Goto ExitFunction

	MDNFNotFound:
		Push 1
		Goto ExitFunction

	ExitFunction:
FunctionEnd

