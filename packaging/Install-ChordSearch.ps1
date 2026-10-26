#Requires -Version 5.1
<#
.SYNOPSIS
  Installs Chord Search to %LocalAppData%\Programs\ChordSearch and creates a Start Menu shortcut.
#>
$ErrorActionPreference = 'Stop'

$source = $PSScriptRoot
$exeName = 'ChordSearch.App.exe'
$sourceExe = Join-Path $source $exeName

if (-not (Test-Path -LiteralPath $sourceExe)) {
    Write-Error "Cannot find $exeName next to this script. Unzip the full release package first."
}

$targetRoot = Join-Path $env:LOCALAPPDATA 'Programs\ChordSearch'
Write-Host "Installing Chord Search → $targetRoot"

if (Test-Path -LiteralPath $targetRoot) {
    Remove-Item -LiteralPath $targetRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null

Get-ChildItem -LiteralPath $source -Force |
    Where-Object { $_.Name -notin @('Install-ChordSearch.ps1', 'INSTALL.txt') } |
    ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $targetRoot -Recurse -Force
    }

$targetExe = Join-Path $targetRoot $exeName
if (-not (Test-Path -LiteralPath $targetExe)) {
    Write-Error "Install failed — $exeName missing in target folder."
}

$programs = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'
$shortcutPath = Join-Path $programs 'Chord Search.lnk'
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $targetExe
$shortcut.WorkingDirectory = $targetRoot
$shortcut.Description = 'Chord Search — guitar voicing finder'
$shortcut.Save()

Write-Host "Done."
Write-Host "  App:      $targetExe"
Write-Host "  Shortcut: $shortcutPath"
Write-Host "Launch from the Start Menu, or run the exe above."
