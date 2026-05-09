@echo off
echo ========================================
echo  Installing Tools/ss14_ru/requirements.txt
echo ========================================

python --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Python not found. Install Python and add it to PATH.
    pause
    exit /b 1
)

if not exist "Tools/ss14_ru/requirements.txt" (
    echo [ERROR] File Tools/ss14_ru/requirements.txt not found.
    pause
    exit /b 1
)

echo.
echo [1/2] Upgrading pip...
python -m pip install --upgrade pip

echo.
echo [2/2] Installing dependencies...
python -m pip install -r "Tools/ss14_ru/requirements.txt"

if errorlevel 1 (
    echo.
    echo [ERROR] Something went wrong during installation.
) else (
    echo.
    echo [OK] All dependencies installed successfully!
)

echo.
pause
