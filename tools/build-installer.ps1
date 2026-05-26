param(
    [string] $Version = "dev",
    [switch] $NoZip
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string] $Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$project = Join-Path $repoRoot "src\AutoBingSearch.App\AutoBingSearch.App.csproj"
$publishDir = Join-Path $repoRoot "artifacts\publish\installer-win-x64"
$installerRoot = Join-Path $repoRoot "artifacts\installer\AutoBingSearch-Installer"
$installerAppDir = Join-Path $installerRoot "app"
$zipPath = Join-Path $repoRoot "artifacts\installer\AutoBingSearch-Installer-$Version-win-x64.zip"

Write-Step "Publishing self-contained app"
& dotnet publish $project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Write-Step "Creating installer folder"
Remove-Item -LiteralPath $installerRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $installerAppDir -Force | Out-Null
Copy-Item -Path (Join-Path $publishDir "*") -Destination $installerAppDir -Recurse -Force
Copy-Item -Path (Join-Path $repoRoot "installer\Install-AutoBingSearch.ps1") -Destination $installerRoot -Force
Copy-Item -Path (Join-Path $repoRoot "installer\Install-AutoBingSearch.cmd") -Destination $installerRoot -Force
Copy-Item -Path (Join-Path $repoRoot "installer\Uninstall-AutoBingSearch.ps1") -Destination $installerRoot -Force

$readme = @'
# AutoBingSearch Installer

1. Extract this folder on the target PC.
2. Double-click `Install-AutoBingSearch.cmd`.
3. Pick the browser/profile that is signed into Bing when setup opens.

The installer copies the app to `%LOCALAPPDATA%\Programs\AutoBingSearch`, creates Start Menu and Startup shortcuts, registers scheduled tasks, and starts the tray app.

To remove it, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\Uninstall-AutoBingSearch.ps1
```
'@
Set-Content -LiteralPath (Join-Path $installerRoot "README.txt") -Value $readme -Encoding UTF8

if (-not $NoZip) {
    Write-Step "Creating zip package"
    Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue
    Compress-Archive -Path (Join-Path $installerRoot "*") -DestinationPath $zipPath -Force
}

Write-Host ""
Write-Host "Installer package ready." -ForegroundColor Green
Write-Host "Folder: $installerRoot"
if (-not $NoZip) {
    Write-Host "Zip:    $zipPath"
}
