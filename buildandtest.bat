@echo off
cd ./SpeakServer
echo "hey1"
start /b /wait "" build
echo "hey2"
cd ../Plugin
start /b /wait "" build
test