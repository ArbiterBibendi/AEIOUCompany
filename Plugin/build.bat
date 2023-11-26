@echo off
set "GAME_PATH=E:\SteamLibrary\steamapps\common\Lethal Company"
dotnet build
copy "bin\Debug\netframework4.8\AEIOUCompany.dll" "%GAME_PATH%\BepInEx\plugins"

copy "lib\SharpTalk.dll" "%GAME_PATH%\BepInEx\plugins"
copy "lib\FonixTalk.dll" "%GAME_PATH%\BepInEx\plugins"
copy "lib\AEIOUSpeak.exe" "%GAME_PATH%\BepInEx\plugins"
copy "lib\ftalk_us.dic" "%GAME_PATH%\BepInEx\plugins"