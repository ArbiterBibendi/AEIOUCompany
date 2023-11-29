@echo off
set "GAME_PATH=E:\SteamLibrary\steamapps\common\Lethal Company"
mkdir "%GAME_PATH%\BepInEx\plugins\aeioucompany"
set "TEST_DIR=%GAME_PATH%\BepInEx\plugins\aeioucompany"
set "RELEASE_DIR=release\aeioucompany"

cd ./SpeakServer
echo Building Speak Server...
start /b /wait "" build || (
    echo Building speak server failed
    exit
)
echo Speak Server Build Successful, Building Plugin...
cd ../Plugin
start /b /wait "" build || (
    echo Building Plugin Failed
    exit
)
cd ..