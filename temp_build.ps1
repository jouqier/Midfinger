Write-Host "Building for x64..."
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o bin\Release\win-x64

if ($LASTEXITCODE -eq 0) {
    Copy-Item "bin\Release\win-x64\FingerScreensaver.exe" "bin\Release\FingerScreensaver-x64.scr" -Force
    Write-Host "`nBuild completed successfully!"
    Write-Host "Screensaver file: bin\Release\FingerScreensaver-x64.scr"
} else {
    Write-Host "Build failed!"
    exit 1
}



