#!/usr/bin/env pwsh
# To run: open PowerShell, navigate to your project directory and execute: .\install-editable.ps1

# Stop the script on any unhandled error
$ErrorActionPreference = 'Stop'

function Write-Ok { param($msg) Write-Host $msg -ForegroundColor Green }
function Write-Err { param($msg) Write-Host "[ERROR] $msg" -ForegroundColor Red }
function Write-Warn { param($msg) Write-Host "[WARNING] $msg" -ForegroundColor Yellow }
function Stop-Operation {
    Write-Err "Operation cancelled by user."
    exit 2
}

# --- 1) User confirmation ---
$choices = @(
    [System.Management.Automation.Host.ChoiceDescription]::new("&Yes", "Proceed with the installation."),
    [System.Management.Automation.Host.ChoiceDescription]::new("&No", "Cancel the operation.")
)
$caption = "Confirmation"
$message = "This script will install the package in editable mode and WILL NOT CHECK for existing environments. Do you want to proceed?"
$result = $host.ui.PromptForChoice($caption, $message, $choices, 1)
if ($result -ne 0) {
    Stop-Operation
}

# --- 2) Locate Python executable ---
# Try 'python' first, then fallback to 'py'
$pythonExe = $null
foreach ($cand in @("python","py")) {
    $cmd = Get-Command $cand -ErrorAction SilentlyContinue
    if ($cmd) { $pythonExe = $cmd.Source; break }
}
if (-not $pythonExe) {
    Write-Err "Python executable not found in path. Please install Python 3.6 or later."
    exit 1
}
$pythonVersion = & $pythonExe --version 2>&1
if ($pythonVersion -notmatch 'Python 3\.\d+') {
    Write-Err "Python version is not 3.x. Please install Python 3.6 or later."
    exit 1
}
Write-Ok "Found $pythonVersion at: $pythonExe"

# --- 3) Determine PIP command ---
$pipCmd = Get-Command pip -ErrorAction SilentlyContinue
if ($pipCmd) {
    # Use pip directly
    $pipExec = $pipCmd.Path
    $pipArgs = @()
} else {
    # Fallback to python -m pip
    $pipExec = $pythonExe
    $pipArgs = '-m','pip'
}
Write-Host "Using PIP: $pipExec $($pipArgs -join ' ')"

# --- Find python base directory ---
$pythonDir = & $pythonExe -c "import sys; sys.stdout.write(sys.base_prefix)"
if (-not (Test-Path $pythonDir)) {
    Write-Err "Failed to determine Python base directory."
    exit 1
}

# --- 4) Find Python's Scripts folder ---
$scriptsDir  = Join-Path $pythonDir 'Scripts'
Write-Host "Checking for Scripts folder at: $scriptsDir"

if (-not (Test-Path $scriptsDir)) {
    Write-Err "Scripts folder not found: $scriptsDir"
    Write-Warn "Please verify that Python is installed correctly."
    $answer = Read-Host "Do you want to proceed anyway? [Y/N]"
    if ($answer -notmatch '^[Yy]$')  {
        Stop-Operation
    }
} else {
    # Check if Scripts folder is in the PATH
    $inPath = $env:PATH.Split(';') | Where-Object { $_ -eq $scriptsDir }
    if ($inPath) {
        $pathOk = $true
    } else {
        Write-Warn "The Scripts folder ($scriptsDir) is NOT in your PATH. This may cause execution issues."
        Write-Warn "Consider adding it to the PATH or running scripts directly from this folder."
    }
}

# Helper to invoke pip
function Invoke-Pip([string[]]$Arguments) {
    & $pipExec @($pipArgs + $Arguments)
}

# --- 5) Install the package in editable mode ---
try {
    Write-Host "==========| Installing package in editable mode |=========="
    Invoke-Pip -Arguments 'install','-e','.','--force-reinstall'
    Write-Host ("="*40)
    Write-Ok "Installation attempt finished."
} catch {
    Write-Err "Failed to install the package: $_"
    exit 1
}

# --- 6) Verify installation via 'pip show' ---
try {
    Invoke-Pip -Arguments 'show','heat-downloader' | Out-Null
} catch {
    Write-Err "Package 'heat-downloader' not found in the Python environment."
    exit 1
}

# --- 7) Check for generated executables ---
$executables = @('heat-downloader.exe','heat-downloader-script.py')
$scriptFullPath = $null
foreach ($exe in $executables) {
    $candidate = Join-Path $scriptsDir $exe
    if (Test-Path $candidate) {
        $scriptFullPath = $candidate
        break
    }
}
if (-not $scriptFullPath) {
    Write-Err "No executable found in $scriptsDir."
    exit 1
}

# --- 8) Test the installed script ---
try {
    & $scriptFullPath '--help' | Out-Null
} catch {
    Write-Err "Error running '$scriptFullPath'. Installation may be incorrect."
    exit 1
}

# --- 9) Success message ---
Write-Ok "`nPackage installed successfully! You can now use 'heat-downloader'."
if ($pathOk) {
    Write-Ok "Just run: heat-downloader --help"
} else {
    Write-Warn ("Since Scripts is not in PATH, run directly: `"$scriptFullPath`"
            Or as a module: `"$pythonExe -m heat_downloader --help")
}