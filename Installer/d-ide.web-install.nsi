!define DIDE_URL "http://d-ide.sourceforge.net/d-ide.php?action=check&timestamp=%lastfileversion%"
!define DIDE_VERSION_URL "http://d-ide.sourceforge.net/d-ide.php?action=fileversion"
!define DIDE_VERSION_FILE "LastModificationTime"

Var VERSION_TEXT

OutFile ".\Builds\D-IDE2.web-install.exe"

;--------------------------------------------------------
; Include D-IDE Installer Library Header
;--------------------------------------------------------
!include d-ide.header.nsh
!include TextFunc.nsh

;--------------------------------------------------------
; Install the D-IDE program files
;--------------------------------------------------------
Section "-Install Program Files" install_section_id
	CreateDirectory "$INSTDIR"
	CreateDirectory "$CONFIG_DIR"	
	SetOutPath "$INSTDIR"
	
	Delete "$INSTDIR\*.exe"
	Delete "$INSTDIR\*.exe.config"
	Delete "$INSTDIR\*.dll"
	Delete "$INSTDIR\*.xshd"
	Delete "$INSTDIR\${DIDE_VERSION_FILE}"
	
	DetailPrint "Checking current D-IDE version."
	NSISdl::download "${DIDE_VERSION_URL}" "$INSTDIR\${DIDE_VERSION_FILE}"
	;inetc::get "${DIDE_VERSION_URL}" "$INSTDIR\${DIDE_VERSION_FILE}"
	;Pop $0 ; will return "OK" if successful.
	StrCmp $0 "OK" ReadInVersion SkipVersionRead
	
	ReadInVersion:
		FileOpen $4 "$INSTDIR\${DIDE_VERSION_FILE}" r ; open version file for reading
		FileRead $4 $2
		FileClose $4 
		
		${TrimNewLines} '$2' $VERSION_TEXT
	
	SkipVersionRead:
	
	DetailPrint "Downloading latest D-IDE program files."
	StrCpy $2 "$TEMP\dide.${FILEDATE}.zip"
	NSISdl::download "${DIDE_URL}" $2
	
	SetOverwrite on
	nsisunz::Unzip "$2" "$INSTDIR"
	File /oname=DIDE.Installer.dll "${CLR_INSTALLER_HELPER}\DIDE.Installer.dll"

	WriteRegStr HKLM "Software\D-IDE" "" $INSTDIR
SectionEnd

;--------------------------------------------------------
; Include D-IDE Installer Library Footer
;--------------------------------------------------------
!include d-ide.footer.nsh

