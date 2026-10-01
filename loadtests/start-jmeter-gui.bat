@echo off
setlocal
cd /d "%~dp0jmeter\apache-jmeter-5.6.3\bin"
start "" jmeter.bat -t "%~dp0pulsechat-loadtest.jmx"
endlocal
