@echo off

echo Running sign.cmd
set Arg1=%1
set Arg2=%2
set Arg3=%3

set ProgramName=%Arg1:"=%
set TargetPath=%Arg2:"=%
set WinSdkDir=%Arg3:"=%

if "%WinSdkDir%"=="" goto :nosdk

if not exist "%WinSdkDir%\bin\x86\signtool.exe" goto :notool

if "%MyKeyFile%"=="" goto :nokeyfile

set PATH=%PATH%;%WinSdkDir%\bin\x86

signtool sign /p "Banana_518z!" /f "%MyKeyFile%" /d "%ProgramName%"  /t "http://timestamp.comodoca.com/authenticode" "%targetPath%"

goto :eof

:nosdk
echo Please set your WindowsSDK80Path variable
exit 1

:notool
echo Could not find "%WinSdkDir%\bin\x86\signtool.exe", please make sure your WindowsSDK80Path variable is correct.
exit 1

:nokeyfile
echo Please set your MyKeyFile variable
exit 1
