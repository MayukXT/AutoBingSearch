param(
    [string] $InstallDir = "$env:LOCALAPPDATA\Programs\AutoBingSearch",
    [switch] $KeepConfig
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string] $Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Resolve-SafeInstallDir {
    param([string] $Path)

    $programRoot = [System.IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA "Programs"))
    $target = [System.IO.Path]::GetFullPath($Path)

    if (-not $target.StartsWith($programRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "InstallDir must stay inside $programRoot. Refusing to modify: $target"
    }

    return $target
}

$installPath = Resolve-SafeInstallDir $InstallDir
$installedExe = Join-Path $installPath "AutoBingSearch.exe"
$startMenuShortcut = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\AutoBingSearch.lnk"
$startupShortcut = Join-Path ([Environment]::GetFolderPath("Startup")) "AutoBingSearch.lnk"
$configRoot = Join-Path $env:APPDATA "AutoBingSearch"
$localRoot = Join-Path $env:LOCALAPPDATA "AutoBingSearch"

Write-Step "Stopping AutoBingSearch"
Get-Process AutoBingSearch -ErrorAction SilentlyContinue | Stop-Process -Force

if (Test-Path -LiteralPath $installedExe) {
    Write-Step "Removing startup and scheduled tasks"
    $uninstall = Start-Process -FilePath $installedExe -ArgumentList "--uninstall" -PassThru -Wait
    if ($uninstall.ExitCode -ne 0) {
        Write-Warning "The app uninstall command returned exit code $($uninstall.ExitCode). Continuing cleanup."
    }
}

Write-Step "Removing shortcuts"
Remove-Item -LiteralPath $startMenuShortcut -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $startupShortcut -Force -ErrorAction SilentlyContinue

Write-Step "Removing installed app files"
Remove-Item -LiteralPath $installPath -Recurse -Force -ErrorAction SilentlyContinue

if (-not $KeepConfig) {
    Write-Step "Removing app data"
    Remove-Item -LiteralPath $configRoot -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $localRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "AutoBingSearch was removed." -ForegroundColor Green
