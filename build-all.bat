@echo off
echo ========================================
echo Building Finger Screensaver with Maximum Optimization for all architectures
echo ========================================
echo.

REM Создаем директорию для выходных файлов
if not exist "bin\Release" mkdir "bin\Release"

echo [1/3] Building for ARM64 with Maximum Optimization...
dotnet publish -c Release -r win-arm64 --self-contained -p:PublishSingleFile=true -p:StripSymbols=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true -p:OptimizationPreference=Speed -o bin\Release\win-arm64
if %errorlevel% neq 0 (
    echo ERROR: ARM64 Maximum Optimization build failed!
    pause
    exit /b 1
)
copy /Y "bin\Release\win-arm64\FingerScreensaver.exe" "bin\Release\FingerScreensaver-arm64.scr"

echo.
echo [2/3] Building for x64 with Maximum Optimization...
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:StripSymbols=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true -p:OptimizationPreference=Speed -o bin\Release\win-x64
if %errorlevel% neq 0 (
    echo ERROR: x64 Maximum Optimization build failed!
    pause
    exit /b 1
)
copy /Y "bin\Release\win-x64\FingerScreensaver.exe" "bin\Release\FingerScreensaver-x64.scr"

echo.
echo [3/3] Building for x86 with Maximum Optimization...
dotnet publish -c Release -r win-x86 --self-contained -p:PublishSingleFile=true -p:StripSymbols=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true -p:OptimizationPreference=Speed -o bin\Release\win-x86
if %errorlevel% neq 0 (
    echo ERROR: x86 Maximum Optimization build failed!
    pause
    exit /b 1
)
copy /Y "bin\Release\win-x86\FingerScreensaver.exe" "bin\Release\FingerScreensaver-x86.scr"

echo.
echo ========================================
echo Maximum Optimization Build completed successfully!
echo ========================================
echo.
echo Screensaver files (.scr) - Maximum Optimization:
echo   - bin\Release\FingerScreensaver-arm64.scr (for ARM64)
echo   - bin\Release\FingerScreensaver-x64.scr (for x64)
echo   - bin\Release\FingerScreensaver-x86.scr (for x86)
echo.
echo Benefits of Maximum Optimization:
echo   - Optimized for speed and size
echo   - Ready-to-run compilation
echo   - Compressed single file
echo   - Fully self-contained
echo   - Compatible with Windows Forms
echo.
echo To install: Right-click on .scr file and select "Install"
echo or copy to C:\Windows\System32
echo.
pause


