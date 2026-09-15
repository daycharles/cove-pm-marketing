@echo off
setlocal
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0PropFlow-Install-Agent.ps1" %*
set "EXITCODE=%ERRORLEVEL%"
if not "%CI%"=="true" pause
exit /b %EXITCODE%
