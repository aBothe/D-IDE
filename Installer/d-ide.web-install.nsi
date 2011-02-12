!define DIDE_URL "http://d-ide.sourceforge.net/d-ide.php?action=check&timestamp=%lastfileversion%"

OutFile ".\Builds\D-IDE2.web-install.exe"

;--------------------------------------------------------
; Include D-IDE Installer Library Header
;--------------------------------------------------------
!include d-ide.header.nsh

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
	
	DetailPrint "Downloading latest D-IDE program files."
	StrCpy $2 "$TEMP\dide.${FILEDATE}.zip"
	NSISdl::download "${DIDE_URL}" $2
	
	SetOverwrite on
	nsisunz::Unzip "$2" "$INSTDIR"

	WriteRegStr HKLM "Software\D-IDE" "" $INSTDIR
SectionEnd

;--------------------------------------------------------
; Include D-IDE Installer Library Footer
;--------------------------------------------------------
!include d-ide.footer.nsh

