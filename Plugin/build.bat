@echo off
set "GAME_PATH=E:\SteamLibrary\steamapps\common\Lethal Company"
mkdir "%GAME_PATH%\BepInEx\plugins\aeioucompany"
dotnet build
copy "bin\Debug\netframework4.8\AEIOUCompany.dll" "%GAME_PATH%\BepInEx\plugins\aeioucompany"

copy "lib\SharpTalk.dll" "%GAME_PATH%\BepInEx\plugins\aeioucompany"
copy "lib\FonixTalk.dll" "%GAME_PATH%\BepInEx\plugins\aeioucompany"
copy "lib\AEIOUSpeak.exe" "%GAME_PATH%\BepInEx\plugins\aeioucompany"
copy "lib\ftalk_us.dic" "%GAME_PATH%\BepInEx\plugins\aeioucompany"
copy "lib\ftalk_us.dll" "%GAME_PATH%\BepInEx\plugins\aeioucompany"
exit