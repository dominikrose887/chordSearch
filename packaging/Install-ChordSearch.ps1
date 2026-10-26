#Requires -Version 5.1
<#
.SYNOPSIS
  Installs Chord Search to %LocalAppData%\Programs\ChordSearch and creates a Start Menu shortcut.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

# When launched via explorer "Run with PowerShell", PSScriptRoot can be empty.
$source = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($source)) {
    if ($PSCommandPath) { $source = Split-Path -Parent $PSCommandPath }
    elseif ($MyInvocation.MyCommand.Path) { $source = Split-Path -Parent $MyInvocation.MyCommand.Path }
    else { $source = (Get-Location).Path }
}
$source = [System.IO.Path]::GetFullPath($source)

$exeName = 'ChordSearch.App.exe'
$sourceExe = Join-Path $source $exeName

if (-not (Test-Path -LiteralPath $sourceExe)) {
    # Some zip tools nest a single top-level folder
    $nested = Get-ChildItem -LiteralPath $source -Directory -ErrorAction SilentlyContinue |
        Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName $exeName) } |
        Select-Object -First 1
    if ($nested) {
        $source = $nested.FullName
        $sourceExe = Join-Path $source $exeName
    }
}

if (-not (Test-Path -LiteralPath $sourceExe)) {
    throw "Cannot find $exeName next to this script ('$source'). Unzip the full release package first."
}

$pri = Join-Path $source 'ChordSearch.App.pri'
$xbf = Join-Path $source 'MainWindow.xbf'
if (-not (Test-Path -LiteralPath $pri) -or -not (Test-Path -LiteralPath $xbf)) {
    Write-Warning "Missing ChordSearch.App.pri / MainWindow.xbf - the app will not start. Re-download a fixed release zip (v1.0.1+)."
}

$targetRoot = Join-Path $env:LOCALAPPDATA 'Programs\ChordSearch'
Write-Host "Installing Chord Search"
Write-Host "  From: $source"
Write-Host "  To:   $targetRoot"

$srcFull = [System.IO.Path]::GetFullPath($source).TrimEnd('\')
$dstFull = [System.IO.Path]::GetFullPath($targetRoot).TrimEnd('\')
if ($srcFull -eq $dstFull) {
    throw "Source and install folder are the same ($targetRoot). Run this script from the unzipped release folder."
}

if (Test-Path -LiteralPath $targetRoot) {
    Remove-Item -LiteralPath $targetRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null

& robocopy $source $targetRoot /E /NFL /NDL /NJH /NJS /NC /NS /NP `
    /XF Install-ChordSearch.ps1 INSTALL.txt | Out-Null
$rc = $LASTEXITCODE
# robocopy: 0-7 success, >=8 failure
if ($rc -ge 8) {
    throw "robocopy failed with exit code $rc"
}

$targetExe = Join-Path $targetRoot $exeName
if (-not (Test-Path -LiteralPath $targetExe)) {
    throw "Install failed - $exeName missing in target folder."
}
if (-not (Test-Path -LiteralPath (Join-Path $targetRoot 'ChordSearch.App.pri'))) {
    Write-Warning "Installed, but ChordSearch.App.pri is missing - launch will fail."
}

$programs = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'
if (-not (Test-Path -LiteralPath $programs)) {
    New-Item -ItemType Directory -Path $programs -Force | Out-Null
}
$shortcutPath = Join-Path $programs 'Chord Search.lnk'
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $targetExe
$shortcut.WorkingDirectory = $targetRoot
$shortcut.Description = 'Chord Search - guitar voicing finder'
$icon = Join-Path $targetRoot 'Assets\AppIcon.ico'
if (Test-Path -LiteralPath $icon) {
    $shortcut.IconLocation = $icon
}
$shortcut.Save()

Write-Host "Done."
Write-Host "  App:      $targetExe"
Write-Host "  Shortcut: $shortcutPath"
Write-Host "Launch from the Start Menu, or run the exe above."
