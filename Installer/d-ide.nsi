;--------------------------------------------------------
; Include ModernUI
;--------------------------------------------------------
!include MUI2.nsh

;--------------------------------------------------------
; Setting custom variables and constants
;--------------------------------------------------------
!define BINARY_APPLICATION_FILES "..\D-IDE\D-IDE\bin\Release"
!define PROJECT_FILES "..\D-IDE\D-IDE"
!define THIRD_PARTY_FILES "..\externalDeps"
!define CLR_INSTALLER_HELPER ".\"

!define DNF4_URL "http://download.microsoft.com/download/1/B/E/1BE39E79-7E39-46A3-96FF-047F95396215/dotNetFx40_Full_setup.exe"
;!define VCPPR2008_URL "http://download.microsoft.com/download/1/1/1/1116b75a-9ec3-481a-a3c8-1777b5381140/vcredist_x86.exe"
!define VCPPR2010_URL "http://download.microsoft.com/download/5/B/C/5BC5DBB3-652D-4DCE-B14A-475AB85EEF6E/vcredist_x86.exe"
!define DMD_URL "http://ftp.digitalmars.com/dinstaller.exe"
!define DMD_FILE_LIST_URL "http://ftp.digitalmars.com/"

!define /date FILEDATE "%Y%m%d"
!define /date DATE "%Y.%m.%d"

!define TEXT_DMD_CONFIG_TITLE "Digital Mars D Compiler"
!define TEXT_DMD_CONFIG_SUBTITLE "Choose your installation type."

!define DMD_WEB_INSTALLER "DMD Web Installer"
!define DMD_UNZIP_AND_COPY "DMD Unzip and Copy"

Var DMD_INSTALL_ACTION
Var DMD1_BIN_PATH
Var DMD2_BIN_PATH
Var DMD1_BIN_VERSION
Var DMD2_BIN_VERSION
Var DMD1_LATEST_VERSION
Var DMD2_LATEST_VERSION
Var D_WEB_INSTALL_PATH
Var PERFORM_CLR_FEATURES
Var IS_CONNECTED

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
	File /oname=$PLUGINSDIR\dmd-config-choice.ini "dmd-config-choice.ini"
	File /oname=$PLUGINSDIR\dmd-config.ini "dmd-config.ini"

	SetOutPath $PLUGINSDIR
	File "DIDE.Installer.dll"

	ReadRegStr $DMD1_BIN_PATH HKLM "SOFTWARE\D-IDE" "Dmd1xBinPath"
	ReadRegStr $DMD2_BIN_PATH HKLM "SOFTWARE\D-IDE" "Dmd2xBinPath"
	ReadRegStr $DMD1_BIN_VERSION HKLM "SOFTWARE\D-IDE" "Dmd1xBinVersion"
	ReadRegStr $DMD2_BIN_VERSION HKLM "SOFTWARE\D-IDE" "Dmd2xBinVersion"
	ReadRegStr $D_WEB_INSTALL_PATH HKLM "SOFTWARE\D" "Install_Dir"

	Call IsConnected
	Pop $IS_CONNECTED

	Call DotNet4Exists
	Pop $PERFORM_CLR_FEATURES
	IntCmp $PERFORM_CLR_FEATURES 1 0 +2 +2
	CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "Initialize" 0

	;NSISdl::download "${DMD_FILE_LIST_URL}" $2
	;pop $3
	;MessageBox MB_OK "$3 $2"

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
!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_BITMAP ".\d-ide-logo.bmp";
!define MUI_ABORTWARNING

;Installer Pages
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "License.rtf"
!insertmacro MUI_PAGE_DIRECTORY
Page custom DmdConfigChoicePage DmdConfigChoicePageValidation
Page custom DmdConfigPage DmdConfigPageValidation
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

;Uninstaller Pages
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

;--------------------------------------------------------
; Custom Experience UI Configuration
;--------------------------------------------------------
;!define XPUI_ABORTWARNING

;${Page} Welcome
;${LicensePage} ".\License.rtf"
;${Page} Directory
;Page custom DmdConfigChoicePage DmdConfigChoicePageValidation
;Page custom DmdConfigPage DmdConfigPageValidation
;${Page} Components
;${Page} InstFiles
;${Page} Finish

;${UnPage} Welcome
;!insertmacro XPUI_PAGEMODE_UNINST
;!insertmacro XPUI_PAGE_UNINSTCONFIRM_NSIS
;!insertmacro XPUI_PAGE_INSTFILES
;!insertmacro XPUI_LANGUAGE "English"

ReserveFile "dmd-config-choice.ini"
ReserveFile "dmd-config.ini"
;!insertmacro XPUI_RESERVEFILE_INSTALLOPTIONS

;--------------------------------------------------------
; Setup Icons
;--------------------------------------------------------
Icon ".\install.ico"
UninstallIcon ".\uninstall.ico"

;------------------------------------------------------------------------
; In this section, we shall use a custom configuration page.
;------------------------------------------------------------------------
Function DmdConfigChoicePage
	;!insertmacro XPUI_HEADER_TEXT "${TEXT_DMD_CONFIG_TITLE}" "${TEXT_DMD_CONFIG_SUBTITLE}"
	;!insertmacro XPUI_INSTALLOPTIONS_DISPLAY "dmd-config-choice.ini"

	!insertmacro MUI_HEADER_TEXT "${TEXT_DMD_CONFIG_TITLE}" "${TEXT_DMD_CONFIG_SUBTITLE}"
	InstallOptions::dialog "$PLUGINSDIR\dmd-config-choice.ini"
FunctionEnd

Function DmdConfigChoicePageValidation
    ; At this point the user has either pressed Next or one of our custom buttons
    ; We find out which by reading from the INI file
    ReadINIStr $0 "$PLUGINSDIR\dmd-config-choice.ini" "Settings" "State"
    StrCmp $0 1 doNothingOption
    StrCmp $0 2 webInstallerOption
    StrCmp $0 3 unzipOption

    Goto done

    doNothingOption:
		StrCpy $DMD_INSTALL_ACTION ""
        Abort ; Return to the page

    webInstallerOption:
		StrCpy $DMD_INSTALL_ACTION "${DMD_WEB_INSTALLER}"
		Abort ; Return to the page

    unzipOption:
		IntCmp $PERFORM_CLR_FEATURES 1 +2
		MessageBox MB_OK "This feature requires the .Net Framework 2.0."

		StrCpy $DMD_INSTALL_ACTION "${DMD_UNZIP_AND_COPY}"
        Abort ; Return to the page

	done:
FunctionEnd

;------------------------------------------------------------------------
; In this section, we shall use a custom configuration page.
;------------------------------------------------------------------------
Function DmdConfigPage
	StrCmp $DMD_INSTALL_ACTION "${DMD_UNZIP_AND_COPY}" DownloadAndUnzip
	Abort

	DownloadAndUnzip:
		IntCmp $PERFORM_CLR_FEATURES 1 +2
		Abort

		CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "GetLocalDMD1Version" 0
		pop $DMD1_BIN_VERSION
		CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "GetLatestDMD1Version" 0
		pop $DMD1_LATEST_VERSION
		IntCmp $DMD1_BIN_VERSION -1 +3 0
			WriteINIStr "$PLUGINSDIR\dmd-config.ini" "Field 3" "Text" "Version 1.$DMD1_BIN_VERSION Installed (Latest is 1.$DMD1_LATEST_VERSION)"
		Goto +2
			WriteINIStr "$PLUGINSDIR\dmd-config.ini" "Field 3" "Text" "(Latest is 1.$DMD1_LATEST_VERSION)"


		CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "GetLocalDMD1Path" 0
		pop $DMD1_BIN_PATH
		WriteINIStr "$PLUGINSDIR\dmd-config.ini" "Field 4" "State" $DMD1_BIN_PATH

		CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "GetLocalDMD2Version" 0
		pop $DMD2_BIN_VERSION
		CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "GetLatestDMD2Version" 0
		pop $DMD2_LATEST_VERSION
		IntCmp $DMD2_BIN_VERSION -1 +3 0
			WriteINIStr "$PLUGINSDIR\dmd-config.ini" "Field 6" "Text" "Version 2.$DMD2_BIN_VERSION Installed (Latest is 2.$DMD2_LATEST_VERSION)"
		Goto +2
			WriteINIStr "$PLUGINSDIR\dmd-config.ini" "Field 6" "Text" "(Latest is 2.$DMD2_LATEST_VERSION)"

		CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "GetLocalDMD2Path" 0
		pop $DMD2_BIN_PATH
		WriteINIStr "$PLUGINSDIR\dmd-config.ini" "Field 7" "State" $DMD2_BIN_PATH

	;!insertmacro XPUI_HEADER_TEXT "${TEXT_DMD_CONFIG_TITLE}" "${TEXT_DMD_CONFIG_SUBTITLE}"
	;!insertmacro XPUI_INSTALLOPTIONS_DISPLAY "dmd-config.ini"

	!insertmacro MUI_HEADER_TEXT "${TEXT_DMD_CONFIG_TITLE}" "${TEXT_DMD_CONFIG_SUBTITLE}"
	InstallOptions::dialog "$PLUGINSDIR\dmd-config.ini"
FunctionEnd

Function DmdConfigPageValidation
    ; At this point the user has either pressed Next or one of our custom buttons
    ; We find out which by reading from the INI file
    ReadINIStr $0 "$PLUGINSDIR\dmd-config.ini" "Settings" "State"
    StrCmp $0 4 dmd1PathChanged
    StrCmp $0 7 dmd2PathChanged done

    dmd1PathChanged:
		ReadINIStr $DMD1_BIN_PATH "$PLUGINSDIR\dmd-config.ini" "Field 4" "State"
        Abort ; Return to the page

    dmd2PathChanged:
		ReadINIStr $DMD2_BIN_PATH "$PLUGINSDIR\dmd-config.ini" "Field 7" "State"
        Abort ; Return to the page

    done:
		ReadINIStr $DMD1_BIN_PATH "$PLUGINSDIR\dmd-config.ini" "Field 4" "State"
		ReadINIStr $DMD2_BIN_PATH "$PLUGINSDIR\dmd-config.ini" "Field 7" "State"
FunctionEnd

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
		;ExecWait '"$SYSDIR\msiexec.exe" "$2" /quiet'
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
		;ExecWait "$2 /q"
		ExecWait "$2"

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
	CreateDirectory "$INSTDIR\D-IDE.config"
	SetOutPath "$INSTDIR"

	SetOverwrite on
	File /nonfatal /x .svn /x *.vshost* "${BINARY_APPLICATION_FILES}\*.exe"
	File /nonfatal /x .svn "${BINARY_APPLICATION_FILES}\*.dll"
	File /nonfatal /x .svn "${PROJECT_FILES}\*.xshd"

	File /nonfatal /x .svn "${THIRD_PARTY_FILES}\*.dll"
	File /oname=DIDE.Installer.dll "${CLR_INSTALLER_HELPER}\DIDE.Installer.dll"

	WriteRegStr HKLM "Software\D-IDE" "" $INSTDIR
	WriteRegStr HKLM "SOFTWARE\D-IDE" "Dmd1xBinPath" $DMD1_BIN_PATH
	WriteRegStr HKLM "SOFTWARE\D-IDE" "Dmd2xBinPath" $DMD2_BIN_PATH
	WriteRegStr HKLM "SOFTWARE\D-IDE" "Dmd1xBinVersion" $DMD1_BIN_VERSION
	WriteRegStr HKLM "SOFTWARE\D-IDE" "Dmd2xBinVersion" $DMD2_BIN_VERSION
SectionEnd



;--------------------------------------------------------
; Download and install Digital-Mars DMD
;--------------------------------------------------------
Section "-Digital-Mars DMD Install/Update" dmd_section_id
	DetailPrint "Internet? $IS_CONNECTED, .Net 2.0? $PERFORM_CLR_FEATURES"

	StrCmp $DMD_INSTALL_ACTION "${DMD_WEB_INSTALLER}" WebInstall 0
	StrCmp $DMD_INSTALL_ACTION "${DMD_UNZIP_AND_COPY}" DownloadAndUnzip ConfigureDMD

	WebInstall:

		IntCmp $IS_CONNECTED 1 +2
			MessageBox MB_OK "It seems that you are not connected to the internet. Please connect now if you wish to download the Digital Mars compilers."

		DetailPrint "Installing DMD with the official DMD Web Installer."
		StrCpy $1 "dinstaller.exe"
		StrCpy $2 "$EXEDIR\$1"
		IfFileExists $2 FileExists FileMissing

		FileMissing:
			DetailPrint "Digital Mars DMD not installed... Downloading file."
			StrCpy $2 "$EXEDIR\$1"
			NSISdl::download "${DMD_URL}" $2

		FileExists:
			DetailPrint "Installing Digital Mars DMD."
			ExecWait '"$2"'

		;get the latest install location from the registry!
		ReadRegStr $D_WEB_INSTALL_PATH HKLM "SOFTWARE\D" "Install_Dir"
		StrCpy $DMD1_BIN_PATH "$D_WEB_INSTALL_PATH\dmd"
		StrCpy $DMD2_BIN_PATH "$D_WEB_INSTALL_PATH\dmd2"
		WriteRegStr HKLM "SOFTWARE\D-IDE" "Dmd1xBinPath" $DMD1_BIN_PATH
		WriteRegStr HKLM "SOFTWARE\D-IDE" "Dmd2xBinPath" $DMD2_BIN_PATH

		Goto ConfigureDMD

	DownloadAndUnzip:

		IntCmp $PERFORM_CLR_FEATURES 0 Finished

		IntCmp $IS_CONNECTED 1 +2
			MessageBox MB_OK "It seems that you are not connected to the internet. Please connect now if you wish to download the Digital Mars compilers."

		DetailPrint "Downloading and instaling DMD 1.$DMD1_LATEST_VERSION to target location."
		StrCpy $1 "dmd1.$DMD1_LATEST_VERSION.zip"
		StrCpy $2 "$EXEDIR\$1"
		IfFileExists $2 Dmd1FileExists Dmd1FileMissing

		Dmd1FileMissing:
			StrCpy $2 "$EXEDIR\$1"
			CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "GetLatestDMD1Url" 0
			Pop $0
			NSISdl::download "$0" $2
			IfFileExists $2 Dmd1FileExists Dmd2FileMissing

		Dmd1FileExists:
			MessageBox MB_YESNO "Are you sure you want to clean out the folder ($DMD1_BIN_PATH) and unzip the new DMD 1 Compiler into it?" IDYES 0 IDNO Dmd2FileMissing
			RMDir /r "$DMD1_BIN_PATH"
			CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "FixDmdInstallPath" 1 "$DMD1_BIN_PATH"
			Pop $3
			CreateDirectory "$3"
			DetailPrint "Unzipping DMD 1.$DMD1_LATEST_VERSION to $3."
			nsisunz::Unzip "$2" "$3"

			DetailPrint "Downloading and instaling DMD 2.$DMD2_LATEST_VERSION to target location."
			StrCpy $1 "dmd2.$DMD2_LATEST_VERSION.zip"
			StrCpy $2 "$EXEDIR\$1"
			IfFileExists $2 Dmd2FileExists Dmd2FileMissing

		Dmd2FileMissing:
			StrCpy $1 "dmd2.$DMD2_LATEST_VERSION.zip"
			StrCpy $2 "$EXEDIR\$1"
			CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "GetLatestDMD2Url" 0
			Pop $0
			NSISdl::download "$0" $2
			IfFileExists $2 Dmd2FileExists ConfigureDMD

		Dmd2FileExists:
			MessageBox MB_YESNO "Are you sure you want to clean out the folder ($DMD2_BIN_PATH) and unzip the new DMD 2 Compiler into it?" IDYES 0 IDNO ConfigureDMD
			RMDir /r "$DMD2_BIN_PATH"
			CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "FixDmdInstallPath" 1 "$DMD2_BIN_PATH"
			Pop $3
			CreateDirectory "$3"
			DetailPrint "Unzipping DMD 2.$DMD2_LATEST_VERSION to $3."
			nsisunz::Unzip "$2" "$3"

			Goto ConfigureDMD

	ConfigureDMD:
		IntCmp $PERFORM_CLR_FEATURES 0 Finished

		DetailPrint "Configuring DMD and D-IDE."

		IntCmp $DMD1_BIN_VERSION -1 0 +4 +4
		CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "GetLocalDMD1Version" 0
		pop $DMD1_BIN_VERSION
		WriteRegStr HKLM "SOFTWARE\D-IDE" "Dmd1xBinVersion" $DMD1_BIN_VERSION

		IntCmp $DMD2_BIN_VERSION -1 0 +4 +4
		CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "GetLocalDMD2Version" 0
		pop $DMD2_BIN_VERSION
		WriteRegStr HKLM "SOFTWARE\D-IDE" "Dmd2xBinVersion" $DMD2_BIN_VERSION

		CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "Initialize" 0
		CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "CreateConfigurationFile" 1 "$INSTDIR\D-IDE.config\D-IDE.settings.xml"
		pop $1
		StrCmp $1 "" +2 0
		DetailPrint $1

	Finished:

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
; Detects Microsoft .Net Framework 2.0
;--------------------------------------------------------
;Function DotNet20Exists
;	ClearErrors
;	ReadRegStr $1 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v2.0.50727" "Version"
;	IfErrors MDNFNotFound MDNFFound
;
;	MDNFFound:
;		Push 1
;		Goto ExitFunction
;
;	MDNFNotFound:
;		Push 0
;		Goto ExitFunction
;
;	ExitFunction:
;FunctionEnd

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
	ReadRegStr $1 HKLM "SOFTWARE\Microsoft\VisualStudio\10.0\VC\VCRedist\x86" "Installed"
	IfErrors VCPP2010TryAgain VCPP2010Found
	VCPP2010TryAgain:
		ReadRegStr $1 HKLM "SOFTWARE\Microsoft\VisualStudio\10.0\VC\VCRedist\x64" "Installed"
		IfErrors VCPP2010TryYetAgain VCPP2010Found

	VCPP2010TryYetAgain:
		ReadRegStr $1 HKLM "SOFTWARE\Microsoft\VisualStudio\10.0\VC\VCRedist\ia64" "Installed"
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
