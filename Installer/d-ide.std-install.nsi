
!define /date BUILD_DATE "%Y%m%d"
OutFile ".\Builds\D-IDE2.${BUILD_DATE}.exe"

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
; Include D-IDE Installer Library Footer
;--------------------------------------------------------
!include d-ide.footer.nsh
