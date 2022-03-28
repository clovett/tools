@echo off
if "%1" == "" goto :nostore
if "%MYKEYFILE%" == "" goto :nokey
set STORE=%1
sn -i "%MYKEYFILE%" %STORE%

goto :eof
:nostore
echo Please specify the VS store (e.g. VS_KEY_BA9A78C377F77CAA)
goto :eof

:nokey
echo Please specify your MYKEYFILE environment variable
goto :eof