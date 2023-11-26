@echo off
dotnet build
copy "bin\Debug\AEIOUSpeak.exe" "..\Plugin\lib"
exit