RD /S /Q "bin"
RD /S /Q "Sandbox2Package\obj\"
pause

dotnet publish -c Release
pause