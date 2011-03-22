;--------------------------------------------------------
; Include UltraModernUI
;--------------------------------------------------------
!include UMUI.nsh

;--------------------------------------------------------
; Setting custom variables and constants
;--------------------------------------------------------
!define BINARY_APPLICATION_FILES "..\D-IDE2\bin\Release"
!define BINARY_UPDATER_FILES "..\D-IDE.Updater\bin\Release"
!define THIRD_PARTY_FILES "..\Misc"
!define CLR_INSTALLER_HELPER ".\"

!define DNF4_URL "http://download.microsoft.com/download/1/B/E/1BE39E79-7E39-46A3-96FF-047F95396215/dotNetFx40_Full_setup.exe"
!define VCPPR2010_URL "http://download.microsoft.com/download/5/B/C/5BC5DBB3-652D-4DCE-B14A-475AB85EEF6E/vcredist_x86.exe"
!define DMD_URL "http://ftp.digitalmars.com/dinstaller.exe"

!define /date FILEDATE "%Y%m%d"
!define /date DATE "%Y.%m.%d"

Var IS_CONNECTED
Var CONFIG_DIR
Var IS_DOT_NET_FRESHLY_INSTALLED

;--------------------------------------------------------
; Setting various predefined NSIS Variables
;--------------------------------------------------------
Name "D-IDE 2"
BrandingText "Alexander Bothe"
InstallDir $PROGRAMFILES\D-IDE
InstallDirRegKey HKLM "Software\D-IDE" "Install_Dir"
RequestExecutionLevel highest
;ShowInstDetails show

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
	StrCpy $IS_DOT_NET_FRESHLY_INSTALLED "N"
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

;--------------------------------------------------------
; The .onInit function is a predifined function that
; runs before the installer displays the first form.
;--------------------------------------------------------
Function .onInstSuccess
	CLR::Destroy
	StrCmp $IS_DOT_NET_FRESHLY_INSTALLED "Y" PromptReboot NoReboot 

	PromptReboot:
		MessageBox MB_YESNO "The .net framework was installed with D-IDE. Would you like to reboot now?" IDNO NoReboot
		Reboot
		
    NoReboot:
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
		StrCpy $IS_DOT_NET_FRESHLY_INSTALLED "Y"
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

