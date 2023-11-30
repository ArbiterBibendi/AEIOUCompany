@echo off
set "OUTPUT_DIRECTORY=..\release\aeioucompany"
mkdir "%OUTPUT_DIRECTORY%""
dotnet build
copy "bin\Debug\AEIOUSpeak.exe" "%OUTPUT_DIRECTORY%\AEIOUSpeak.exe"
copy "SharpTalk.dll" "%OUTPUT_DIRECTORY%"
copy "FonixTalk.dll" "%OUTPUT_DIRECTORY%"
copy "ftalk_us.dic" "%OUTPUT_DIRECTORY%"
copy "ftalk_us.dll" "%OUTPUT_DIRECTORY%"
exit