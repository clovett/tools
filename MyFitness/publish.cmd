@echo off
cd %~dp0

echo ### Publishing MyFitness
if not EXIST bin\publish goto :nobits
if "%LOVETTSOFTWARE_STORAGE_CONNECTION_STRING%" == "" goto :nokey

AzurePublishClickOnce %~dp0bin\publish downloads/MyFitness "%LOVETTSOFTWARE_STORAGE_CONNECTION_STRING%"
goto :eof

:nobits
echo 'publish' folder not found, please run Solution/Publish first.
exit /b 1

:nokey
echo Please set your LOVETTSOFTWARE_STORAGE_CONNECTION_STRING
exit /b 1
