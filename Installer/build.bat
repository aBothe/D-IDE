del ".\build.log"

if "%DEVENV%"=="" set DEVENV="%PROGRAMFILES%\Microsoft Visual Studio 9.0\Common7\IDE\devenv.exe"  /out ".\application_build.log" /rebuild release
if "%MAKENSIS%"=="" set MAKENSIS=%PROGRAMFILES%\NSIS\makensis.exe

%DEVENV% "..\dide\D-IDE\D-IDE.sln"

"%MAKENSIS%" ".\d-ide.nsi" > ".\installer_build.log"

set DEVENV=
set MAKENSIS=
