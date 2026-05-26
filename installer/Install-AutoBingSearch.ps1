param(
    [string] $InstallDir = "$env:LOCALAPPDATA\Programs\AutoBingSearch",
    [switch] $Setup,
    [switch] $NoStart
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

function New-Shortcut {
    param(
        [string] $ShortcutPath,
        [string] $TargetPath,
        [string] $Arguments
    )

    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($ShortcutPath)
    $shortcut.TargetPath = $TargetPath
    $shortcut.Arguments = $Arguments
    $shortcut.WorkingDirectory = Split-Path -Parent $TargetPath
    $shortcut.IconLocation = $TargetPath
    $shortcut.Description = "Start AutoBingSearch in the system tray"
    $shortcut.Save()
}

$packageRoot = $PSScriptRoot
$sourceAppDir = Join-Path $packageRoot "app"
$sourceExe = Join-Path $sourceAppDir "AutoBingSearch.exe"
$installPath = Resolve-SafeInstallDir $InstallDir
$installedExe = Join-Path $installPath "AutoBingSearch.exe"
$configPath = Join-Path $env:APPDATA "AutoBingSearch\config.json"
$startMenuShortcut = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\AutoBingSearch.lnk"

if (-not (Test-Path -LiteralPath $sourceExe)) {
    throw "Installer package is incomplete. Missing $sourceExe"
}

Write-Step "Stopping existing AutoBingSearch processes"
Get-Process AutoBingSearch -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Step "Installing app files"
New-Item -ItemType Directory -Path $installPath -Force | Out-Null
Get-ChildItem -LiteralPath $installPath -Force -ErrorAction SilentlyContinue |
    Remove-Item -Recurse -Force
Copy-Item -Path (Join-Path $sourceAppDir "*") -Destination $installPath -Recurse -Force

Write-Step "Creating Start Menu shortcut"
New-Item -ItemType Directory -Path (Split-Path -Parent $startMenuShortcut) -Force | Out-Null
New-Shortcut -ShortcutPath $startMenuShortcut -TargetPath $installedExe -Arguments "--tray"

if ($Setup -or -not (Test-Path -LiteralPath $configPath)) {
    Write-Step "Opening setup"
    Start-Process -FilePath $installedExe -ArgumentList "--setup" -Wait

    if (-not (Test-Path -LiteralPath $configPath)) {
        throw "Setup was not completed, so AutoBingSearch was not enabled."
    }
}

Write-Step "Registering startup and scheduled tasks"
$install = Start-Process -FilePath $installedExe -ArgumentList "--install", "--quiet" -PassThru -Wait
if ($install.ExitCode -ne 0) {
    Write-Warning "Normal task registration failed. Requesting administrator approval."
    $elevated = Start-Process -FilePath $installedExe -ArgumentList "--install", "--quiet" -Verb RunAs -PassThru -Wait
    if ($elevated.ExitCode -ne 0) {
        throw "Elevated task registration failed with exit code $($elevated.ExitCode)."
    }
}

if (-not $NoStart) {
    Write-Step "Starting tray app"
    Start-Process -FilePath $installedExe -ArgumentList "--tray" -WindowStyle Hidden
}

Write-Host ""
Write-Host "AutoBingSearch is installed and ready." -ForegroundColor Green
Write-Host "Installed app: $installedExe"
