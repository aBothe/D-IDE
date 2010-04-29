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

!define TEXT_DMD_CONFIG_TITLE "Digital Mars D Compiler"
!define TEXT_DMD_CONFIG_SUBTITLE "Choose your installation type."

!define DMD_WEB_INSTALLER "DMD Web Installer"
!define DMD_UNZIP_AND_COPY "DMD Unzip and Copy"

Var DMD_INSTALL_ACTION
Var DMD1_BIN_PATH
Var DMD2_BIN_PATH
Var DMD1_BIN_VERSION
Var DMD2_BIN_VERSION

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
	SetOutPath $PLUGINSDIR
	File "DIDE.Installer.dll"
	
	ReadRegStr $DMD1_BIN_PATH HKLM "SOFTWARE\D-IDE" "Dmd1xBinPath"
	ReadRegStr $DMD2_BIN_PATH HKLM "SOFTWARE\D-IDE" "Dmd2xBinPath"
	ReadRegStr $DMD1_BIN_VERSION HKLM "SOFTWARE\D-IDE" "Dmd1xBinVersion"
	ReadRegStr $DMD2_BIN_VERSION HKLM "SOFTWARE\D-IDE" "Dmd2xBinVersion"
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
; Custom Experience UI Configuration
;--------------------------------------------------------
!define XPUI_ABORTWARNING

${Page} Welcome
${LicensePage} ".\License.rtf"
${Page} Directory
Page custom DmdConfigPage DmdConfigPageValidation 
;${Page} Components
${Page} InstFiles
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
	StrCmp $DMD1_BIN_VERSION "" 0 +3
		CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "GetLocalDMD1Version" 0
		pop $DMD1_BIN_VERSION
		WriteINIStr "$PLUGINSDIR\dmd-config.ini" "Field 6" "Text" "(Version 1.$DMD1_BIN_VERSION Installed)"
	
    StrCmp $DMD1_BIN_PATH "" 0 +3
		CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "GetLocalDMD1Path" 0
		pop $DMD1_BIN_PATH
		WriteINIStr "$PLUGINSDIR\dmd-config.ini" "Field 7" "State" $DMD1_BIN_PATH

    StrCmp $DMD2_BIN_VERSION "" 0 +3
		CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "GetLocalDMD2Version" 0
		pop $DMD2_BIN_VERSION 
		WriteINIStr "$PLUGINSDIR\dmd-config.ini" "Field 9" "Text" "(Version 2.$DMD2_BIN_VERSION Installed)"
		
    StrCmp $DMD2_BIN_PATH "" 0 +3
		CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "GetLocalDMD2Path" 0
		pop $DMD2_BIN_PATH 
		WriteINIStr "$PLUGINSDIR\dmd-config.ini" "Field 10" "State" $DMD2_BIN_PATH

	!insertmacro XPUI_HEADER_TEXT "${TEXT_DMD_CONFIG_TITLE}" "${TEXT_DMD_CONFIG_SUBTITLE}"
	!insertmacro XPUI_INSTALLOPTIONS_DISPLAY "dmd-config.ini"
FunctionEnd

Function DmdConfigPageValidation
    ; At this point the user has either pressed Next or one of our custom buttons
    ; We find out which by reading from the INI file
    ReadINIStr $0 "$PLUGINSDIR\dmd-config.ini" "Settings" "State"
    StrCmp $0 1 doNothingOption
    StrCmp $0 2 webInstallerOption
    StrCmp $0 3 unzipOption 
    StrCmp $0 7 dmd1PathChanged
    StrCmp $0 10 dmd2PathChanged
    StrCmp $0 11 queryVersions
    
    Goto done 
    ;Abort ; Return to the page
    
    doNothingOption:
		StrCpy $DMD_INSTALL_ACTION ""
        Abort ; Return to the page
    
    webInstallerOption:
		StrCpy $DMD_INSTALL_ACTION "${DMD_WEB_INSTALLER}"
		Abort
		
    unzipOption:
		StrCpy $DMD_INSTALL_ACTION "${DMD_UNZIP_AND_COPY}"
        Abort ; Return to the page
    
    dmd1PathChanged:
		;ReadINIStr $0 "$PLUGINSDIR\dmd-config.ini" "Field 7" "State"
		;CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "IsValidDMDInstallForVersion" 2 1 $0 
		;Pop $1
        Abort ; Return to the page
    
    dmd2PathChanged:
		;ReadINIStr $0 "$PLUGINSDIR\dmd-config.ini" "Field 10" "State"
		;CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "IsValidDMDInstallForVersion" 2 1 $0 
		;Pop $1
        Abort ; Return to the page
    
    queryVersions:
		;ReadINIStr $0 "$PLUGINSDIR\dmd-config.ini" "Field 10" "State"
		CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "GetLatestDMD1Version" 0
		Pop $0
		MessageBox MB_OK "Version 1.$0"
		CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "GetLatestDMD2Version" 0
		Pop $0
        MessageBox MB_OK "Version 2.$0"
		Abort ; Return to the page
    
    done:
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
; Download and install Digital-Mars DMD
;--------------------------------------------------------
Section "-Digital-Mars DMD Install/Update" dmd_section_id
	StrCmp $DMD_INSTALL_ACTION "${DMD_WEB_INSTALLER}" WebInstall 0
	StrCmp $DMD_INSTALL_ACTION "${DMD_UNZIP_AND_COPY}" DownloadAndUnzip ConfigureDMD
	
	WebInstall:
		DetailPrint "Installing DMD with the official DMD Web Installer."
		StrCpy $1 "dinstaller.exe"
		StrCpy $2 "$EXEDIR\$1"
		IfFileExists $2 FileExists FileMissing
			
		FileMissing:
			DetailPrint "Digital Mars DMD not installed... Downloading file."
			StrCpy $2 "$TEMP\$1"
			NSISdl::download "${DMD_URL}" $2

		FileExists:
			DetailPrint "Installing Digital Mars DMD."
			ExecWait '"$2"'
			
		Goto ConfigureDMD
		
	DownloadAndUnzip:
		DetailPrint "Downloading and instaling DMD to target location."
			
		Goto ConfigureDMD
	
	ConfigureDMD:
	
		DetailPrint "Configuring DMD and D-IDE."
		
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
	WriteRegStr HKLM "SOFTWARE\D-IDE" "Dmd1xBinPath" $DMD1_BIN_PATH
	WriteRegStr HKLM "SOFTWARE\D-IDE" "Dmd2xBinPath" $DMD2_BIN_PATH
	WriteRegStr HKLM "SOFTWARE\D-IDE" "Dmd1xBinVersion" $DMD1_BIN_VERSION
	WriteRegStr HKLM "SOFTWARE\D-IDE" "Dmd2xBinVersion" $DMD2_BIN_VERSION
SectionEnd


            ;WriteLine("Latest (online) DMD 1 Url       --> " + InstallerHelper.GetLatestDMD1Url());
            ;WriteLine("Latest (online) DMD 1 Version   --> " + InstallerHelper.GetLatestDMD1Version());
            ;WriteLine("Local (installed) DMD 1 Path    --> " + InstallerHelper.GetLocalDMD1Path());
            ;WriteLine("Local (installed) DMD 1 Version --> " + InstallerHelper.GetLocalDMD1Version());
            ;WriteLine("Latest (online) DMD 2 Url       --> " + InstallerHelper.GetLatestDMD2Url());
            ;WriteLine("Latest (online) DMD 2 Version   --> " + InstallerHelper.GetLatestDMD2Version());
            ;WriteLine("Local (installed) DMD 2 Path    --> " + InstallerHelper.GetLocalDMD2Path());
            ;WriteLine("Local (installed) DMD 2 Version --> " + InstallerHelper.GetLocalDMD2Version());

;--------------------------------------------------------
; Configure DMD
;--------------------------------------------------------
Section "-Configure DMD" configure_dmd_section_id
  CLR::Call /NOUNLOAD "DIDE.Installer.dll" "DIDE.Installer.InstallerHelper" "GetLatestDMD2Url" 0 ; 5 "mystring1" "x" 10 15.8 false
  pop $0  
  MessageBox MB_OK "Latest DMD 2 URL is: $0"
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

