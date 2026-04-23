@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "SOURCE_DIR=%SCRIPT_DIR%src"
set "OUTPUT_PATH=%SCRIPT_DIR%FightOfflineSimulationLauncher.exe"
set "COMPILER_PATH=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if not exist "%COMPILER_PATH%" (
    echo Could not find C# compiler: %COMPILER_PATH%
    exit /b 1
)

"%COMPILER_PATH%" ^
 /nologo ^
 /target:winexe ^
 /out:"%OUTPUT_PATH%" ^
 /reference:System.dll ^
 /reference:System.Core.dll ^
 /reference:System.Drawing.dll ^
 /reference:System.Windows.Forms.dll ^
 /reference:System.Web.Extensions.dll ^
 "%SOURCE_DIR%\Program.cs" ^
 "%SOURCE_DIR%\LauncherPaths.cs" ^
 "%SOURCE_DIR%\LauncherProgressSnapshot.cs" ^
 "%SOURCE_DIR%\OfflineSimulationLauncherForm.cs"

if errorlevel 1 (
    exit /b %errorlevel%
)

echo Built: %OUTPUT_PATH%
exit /b 0
