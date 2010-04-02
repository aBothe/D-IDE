
if "%PROGRAMFILES(X86)%"=="" goto :x86
goto :x64
:x86
	copy CLR.dll "%PROGRAMFILES%\NSIS\Plugins\"
	goto done
:x64
	xcopy CLR.dll "%PROGRAMFILES(X86)%\NSIS\Plugins\"
:done

rem copy .\libraries\*.nsh  "%PROGRAMFILES%\NSIS\Include\"
pause