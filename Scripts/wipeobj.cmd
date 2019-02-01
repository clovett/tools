@echo off
set CLEAN=%TEMP%\clean.cmd

dir /s /b /a:D Debug > %CLEAN%
dir /s /b /a:D Release >> %CLEAN%
dir /s /b /a:D obj >> %CLEAN%
dir /s /b /a:D bin >> %CLEAN%

if "%1"=="-p" (
    dir /s /b /a:D x86 >> %CLEAN%
    dir /s /b /a:D x64 >> %CLEAN%
)

for /f "tokens=1* delims=|" %%i in (%CLEAN%) do (
  echo %%i
  rd /s /q "%%i"
)  