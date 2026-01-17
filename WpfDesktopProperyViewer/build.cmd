@echo off

cd %~dp0
set PublishBits=.\bin\publish
set OutputDir=.\WpfDesktopProperyViewer\bin\publish

msbuild /target:restore /p:Configuration=Release "/p:Platform=Any CPU" WpfDesktopProperyViewer.sln
if ERRORLEVEL 1 exit /b 1
msbuild /target:rebuild /p:Configuration=Release "/p:Platform=Any CPU" WpfDesktopProperyViewer.sln
if ERRORLEVEL 1 exit /b 1
msbuild /target:publish /p:PublishProfile=.\Properties\PublishProfiles\FolderProfile.pubxml /p:Configuration=Release "/p:Platform=Any CPU" /p:PublishDir=%PublishBits% WpfDesktopProperyViewer\WpfDesktopProperyViewer.csproj
if ERRORLEVEL 1 exit /b 1

echo start %OutputDir%\WpfDesktopProperyViewer.exe
goto :eof
