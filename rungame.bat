@echo off
chcp 65001 > nul
setlocal 
echo Версия сборки:
echo 1 - Debug
echo 2 - Release
:RunBuild
set /p choice="Выберите версию сборки: "
if "%choice%"=="1" ( 
echo Запуск Debug версии...
    start "SERVER" cmd /c runserver.bat
    start "CLIENT" cmd /c runclient.bat
)else if "%choice%"=="2" ( 
echo Запуск Release версии...
    start "SERVER" cmd /c runserver-Release.bat
    start "CLIENT" cmd /c runclient-Release.bat
)else ( 
echo Неверный номер сборки. Пожалуйста, выберите 1 или 2.
goto RunBuild)
endlocal
