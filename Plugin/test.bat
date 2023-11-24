@echo off
start /min "Host" "E:\SteamLibrary\steamapps\common\Lethal Company\Lethal Company.exe" -screen-height 600 -screen-width 800 -screen-fullscreen 0
if "%1" == "both" (
    REM The game gets a little angry if you launch it twice quickly
    timeout /t 10
    start /min "Client" "E:\SteamLibrary\steamapps\common\Lethal Company\Lethal Company.exe" -screen-height 600 -screen-width 800 -screen-fullscreen 0
)
