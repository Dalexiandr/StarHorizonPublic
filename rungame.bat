@echo off
chcp 65001 > nul
setlocal
:Menu
echo Запустить сервер:
echo 1 - Debug
echo 2 - Release
echo Забилдить сервер:
echo 3 - Debug
echo 4 - Release
:RunBuild
set /p choice="Выберите что сделать: "
if "%choice%"=="1" (
echo Сборка Debug версии...
    dotnet build --property WarningLevel=0
    if errorlevel 1 goto Menu
echo Запуск Debug версии...
    start "SERVER" cmd /c "dotnet run --project Content.Server --no-build && pause"
    start "CLIENT" cmd /c "dotnet run --project Content.Client --no-build && pause"
)else if "%choice%"=="2" (
echo Сборка Release версии...
    dotnet build --configuration Release --property WarningLevel=0
    if errorlevel 1 goto Menu
echo Запуск Release версии...
    start "SERVER" cmd /c "dotnet run --project Content.Server --configuration Release --no-build && pause"
    start "CLIENT" cmd /c "dotnet run --project Content.Client --configuration Release --no-build && pause"
)else if "%choice%"=="3" ( 
echo Билд Debug версии...
     dotnet build  --property WarningLevel=0
     goto Menu
)else if "%choice%"=="4" ( 
echo Билд Release версии...
     dotnet build --configuration Release --property WarningLevel=0
     goto Menu
)else ( 
echo Неверное действие. Пожалуйста, выберите 1, 2, 3 или 4.
goto RunBuild)
endlocal
