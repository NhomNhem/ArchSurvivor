<#
export_gdd.ps1

Usage examples (PowerShell):

# Default (uses workspace paths)
./export_gdd.ps1

# Custom input / output
./export_gdd.ps1 -Input "C:\path\to\GDD.md" -Output "C:\path\to\GDD.docx"

# Open generated file after export
./export_gdd.ps1 -Open

This script checks for pandoc on PATH and runs pandoc to generate a .docx file.
#>

param(
    [string]$InputPath = "I:\\unityVers\\ArchSurvivor\\Assets\\_ArchSurvivor\\Document\\GDD.md",
    [string]$Output = "I:\\unityVers\\ArchSurvivor\\Assets\\_ArchSurvivor\\Document\\GDD.docx",
    [string]$PandocPath = $null,
    [switch]$Open
)

function Write-Info($msg){ Write-Host "[info] $msg" -ForegroundColor Cyan }
function Write-Err($msg){ Write-Host "[error] $msg" -ForegroundColor Red }

Write-Info "Input: $InputPath"
Write-Info "Output: $Output"

# Resolve pandoc executable
$pandocExe = $null
if ($PandocPath) {
    if (Test-Path $PandocPath) { $pandocExe = (Resolve-Path $PandocPath).ProviderPath }
    else { Write-Err "Provided --PandocPath '$PandocPath' does not exist."; exit 4 }
} else {
    $cmd = Get-Command pandoc -ErrorAction SilentlyContinue
    if ($cmd) { $pandocExe = $cmd.Path }
    else {
        # Try where.exe (may fail in some hosts)
        try { $where = (where.exe pandoc 2>$null | Select-Object -First 1) } catch { $where = $null }
        if ($where -and (Test-Path $where)) { $pandocExe = $where }
        else {
            # Common install locations
            $candidates = @(
                "$env:ProgramFiles\\Pandoc\\pandoc.exe",
                "$env:ProgramFiles(x86)\\Pandoc\\pandoc.exe",
                "$env:LOCALAPPDATA\\Programs\\Pandoc\\pandoc.exe",
                "$env:ProgramFiles\\Pandoc\\pandoc.exe"
            )
            foreach ($p in $candidates) { if ($p -and (Test-Path $p)) { $pandocExe = $p; break } }
        }
    }
}

if (-not $pandocExe) {
    Write-Err "Pandoc is not installed or not found in PATH."
    Write-Host "You can:"
    Write-Host "  - Run 'Get-Command pandoc' or 'where.exe pandoc' in the PowerShell session where pandoc works and run this script there; or"
    Write-Host "  - Re-run this script specifying the pandoc executable path: `-PandocPath 'C:\\Program Files\\Pandoc\\pandoc.exe'`"
    Write-Host "Install from https://pandoc.org/installing.html if needed."
    exit 2
}

Write-Info "Using pandoc: $pandocExe"

$arguments = @(
    $InputPath,
    '-s',
    '-o', $Output,
    '--metadata', 'title=ArchSurvivor GDD'
)
Write-Info "Running pandoc..."
Write-Host "`"$pandocExe`" " + ($arguments -join ' ')

try {
    & "$pandocExe" @arguments
    $exitCode = $LASTEXITCODE
    if ($exitCode -eq 0) {
        Write-Info "Export succeeded: $Output"
        if ($Open) { Start-Process -FilePath $Output }
        exit 0
    } else {
        Write-Err "Pandoc exited with code $exitCode"
        exit $exitCode
    }
} catch {
    Write-Err "Exception while running pandoc:`n$($_.Exception.Message)"
    exit 3
}
