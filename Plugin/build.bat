@echo off
set "OUTPUT_DIRECTORY=..\release\aeioucompany\"

dotnet build

copy "bin\Debug\netframework4.8\AEIOUCompany.dll" "%OUTPUT_DIRECTORY%"
copy "lib\SharpTalk.dll" "%OUTPUT_DIRECTORY%"
copy "lib\FonixTalk.dll" "%OUTPUT_DIRECTORY%"
copy "lib\ftalk_us.dic" "%OUTPUT_DIRECTORY%"
copy "lib\ftalk_us.dll" "%OUTPUT_DIRECTORY%"
exit