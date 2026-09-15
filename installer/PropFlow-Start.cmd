@echo off
rem Start PropFlow and keep the window open so the sign-in credentials stay readable.
rem Launched by the Start Menu shortcut; the guided agent handles first install and upgrades.
setlocal
cd /d "%~dp0"
echo Starting the PropFlow guided installer. The first run builds the web app and takes a few minutes.
echo.
call "%~dp0PropFlow-Install-Agent.cmd"
set EXITCODE=%ERRORLEVEL%
echo.
if %EXITCODE% neq 0 (
  echo PropFlow did not start. See the messages above; logs are in %%ProgramData%%\PropFlow\logs.
) else (
  echo PropFlow is running. Leave this window closed or open, it makes no difference.
  echo Stop it later with the "Stop PropFlow" shortcut.
)
echo.
pause
exit /b %EXITCODE%
