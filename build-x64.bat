@echo off
echo ========================================
echo Building Finger Screensaver with Aggressive Trimming for x64
echo ========================================
echo.

REM Создаем директорию для выходных файлов
if not exist "bin\Release" mkdir "bin\Release"

dotnet publish -c Release -r win-x64 --self-contained -p:PublishTrimmed=true -p:TrimMode=full -p:PublishSingleFile=true -p:StripSymbols=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false -p:PublishReadyToRun=false -p:IncludeNativeLibrariesForSelfExtract=true -o bin\Release\win-x64
if %errorlevel% neq 0 (
    echo ERROR: Aggressive Trimming build failed!
    pause
    exit /b 1
)

copy /Y "bin\Release\win-x64\FingerScreensaver.exe" "bin\Release\FingerScreensaver-x64.scr"

echo.
echo ========================================
echo Aggressive Trimming Build completed successfully!
echo ========================================
echo.
echo Screensaver file: bin\Release\FingerScreensaver-x64.scr
echo.
echo Benefits of Aggressive Trimming:
echo   - Smaller file size (2-3x reduction)
echo   - Removes unused code and libraries
echo   - Fully self-contained
echo   - Compatible with Windows Forms
echo.
echo To install: Right-click on .scr file and select "Install"
echo or copy to C:\Windows\System32
echo.
pause


