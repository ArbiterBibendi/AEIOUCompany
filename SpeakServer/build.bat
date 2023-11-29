@echo off
set "OUTPUT_DIRECTORY=..\release\aeioucompany"
mkdir "%OUTPUT_DIRECTORY%""
dotnet build
copy "bin\Debug\AEIOUSpeak.exe" "%OUTPUT_DIRECTORY%\AEIOUSpeak.exe"
exit