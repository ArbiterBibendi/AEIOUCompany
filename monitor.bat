@echo off
color 02
set filter=:
if not "%1"=="all" (
    set "filter=LCMOD"
)
powershell -Command "& {Get-Content E:\SteamLibrary\steamapps\common\Lethal` Company\BepInEx\LogOutput.log -Wait -Tail 0} ^| Select-String -Pattern %filter%"