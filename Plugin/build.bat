@echo off
set "OUTPUT_DIRECTORY=..\release\aeioucompany\"

dotnet build

copy "bin\Debug\netframework4.8\AEIOUCompany.dll" "%OUTPUT_DIRECTORY%"
exit