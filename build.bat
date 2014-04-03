@echo off
setlocal EnableExtensions

if (%FrameworkDir%)==() set FrameworkDir=%WINDIR%\Microsoft.NET\Framework64\
if (%FrameworkVersion%)==() set FrameworkVersion=v4.0.30319

set MSBUILD=%FrameworkDir%%FrameworkVersion%\msbuild.exe

if not exist %MSBUILD% goto Error_NoMsBuild

set TARGET=%1
if (%TARGET%)==() set TARGET=Default

set VERBOSITY=%2
if (%VERBOSITY%)==() set VERBOSITY=minimal

FOR /F "tokens=1,2* delims= " %%a in ("%*") do set BUILDARGS=%%c

echo %BUILDARGS%

%MSBUILD% /verbosity:%VERBOSITY% /nologo /m /t:%TARGET% %BUILDARGS% %~dp0build/build.proj

if errorlevel 1 goto Error_BuildFailed

echo.
echo *** BUILD SUCCESSFUL ***
echo.
goto :EOF

:Error_BuildFailed
echo.
echo *** BUILD FAILED ***
echo.
exit /b 1

:Error_NoMsBuild
echo.
echo. ERROR: Unable to locate MSBuild.exe (expected location: %MSBUILD%)
echo.
exit /b 1
