@echo off
dotnet build
copy "bin\Debug\netstandard2.1\LCMod.dll" "E:\SteamLibrary\steamapps\common\Lethal Company\BepInEx\plugins"