@echo off
set SCRIPTDIR=%~dp0
SET PATH=%PATH%;d:\tools;
set PATH=%PATH%;d:\Program Files\Git\cmd\
set PATH=%PATH%;d:\Program Files\git\usr\bin\
set PATH=%PATH%;C:\debuggers
set path=%PATH%;D:\Program Files (x86)\Graphviz2.38\bin
set path=%PATH%;d:\Continuum\Anaconda3\Scripts

REM CUDA 10.0
REM if "%CUDA_PATH_V10_0%" == "" goto :nocuda10
REM set CUDA_PATH=%CUDA_PATH_V10_0%
REM set PATH=%PATH%;%CUDA_PATH_V10_0%\bin
REM set PATH=%PATH%;%CUDA_PATH_V10_0%\libnvvp

REM Select CUDA 9.0
if "%CUDA_PATH_V9_0%" == "" goto :nocuda9
set CUDA_PATH_V9_0=C:\NVidia\CUDA\9.0\Toolkit
set PATH=%PATH%;%CUDA_PATH_V9_0%\bin;
set PATH=%PATH%;%CUDA_PATH_V9_0%\libnvvp;
set PATH=%PATH%;%CUDA_PATH_V9_0%\extras\CUPTI\libx64
set CUDA_PATH=%CUDA_PATH_V9_0%

set KERAS_BACKEND=tensorflow
call "c:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\Tools\VsDevCmd.bat" 
set WindowsSdkDir=
d:\tools\alias -f %SCRIPTDIR%\alias.txt
set WAV_DIRECTORY=d:\datasets\Audio\Kaggle\train
pushd d:\git\ELL\ELL-training\Tensorflow\Audio
activate.bat tensorflow

goto :eof
:nocuda9
echo Cannot find CUDA_PATH_V9_0
goto :eof

:nocuda10
echo Cannot find CUDA_PATH_V10_0
goto :eof