@echo off
set ZipName=WpfDesktopProperyViewer.zip
set PublishZip=.\WpfDesktopProperyViewer\bin\%ZipName%


call build.cmd
if ERRORLEVEL 1 goto :eof

if EXIST %PublishZip% del %PublishZip%

pwsh -command "Compress-Archive -Path %OutputDir%\* -DestinationPath %PublishZip%"
if ERRORLEVEL 1 goto :eof

echo "Publishing to %PublishZip% to srexperiments/setup/tools/%ZipName%
azcopy cp %PublishZip% "https://srexperiments.blob.core.windows.net/setup/tools/"
