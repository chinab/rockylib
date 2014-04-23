net stop "PrivacyService"
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe" "%cd%\System.Agent.WinService.exe"  -u
taskkill /f /im System.Agent.WinService.exe
pause