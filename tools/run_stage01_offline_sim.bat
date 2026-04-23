@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "REPO_ROOT=%%~fI"
set "PROJECT_PATH=%REPO_ROOT%\game"
set "UNITY_LOG=%REPO_ROOT%\Temp\stage01_offline_sim_unity.log"

if not exist "%REPO_ROOT%\Temp" mkdir "%REPO_ROOT%\Temp"

if not defined UNITY_EXE set "UNITY_EXE=C:\Program Files\Unity 6000.3.13f1\Editor\Unity.exe"

if not exist "%UNITY_EXE%" (
    echo [offline-sim] Unity executable not found: %UNITY_EXE%
    echo [offline-sim] Set UNITY_EXE to your Unity editor path and retry.
    exit /b 1
)

"%UNITY_EXE%" ^
 -batchmode ^
 -nographics ^
 -quit ^
 -projectPath "%PROJECT_PATH%" ^
 -logFile "%UNITY_LOG%" ^
 -executeMethod Fight.Editor.Stage01OfflineSimulationBatch.RunFromCommandLine ^
 %*

set "EXIT_CODE=%ERRORLEVEL%"

if not "%EXIT_CODE%"=="0" (
    echo [offline-sim] Failed. Unity log: %UNITY_LOG%
    exit /b %EXIT_CODE%
)

echo [offline-sim] Completed. Unity log: %UNITY_LOG%
exit /b 0
