#Requires -Version 5.1
<#
.SYNOPSIS
  Publishes win-x64, builds Inno Setup installer, and packs a portable zip.
#>
[CmdletBinding()]
param(
    [string] $Version = '1.0.6'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$iscc = Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'
if (-not (Test-Path -LiteralPath $iscc)) {
    $iscc = Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'
}
if (-not (Test-Path -LiteralPath $iscc)) {
    throw 'Inno Setup 6 not found (ISCC.exe). Install from https://jrsoftware.org/isinfo.php'
}

$publish = Join-Path $root 'artifacts\publish-win-x64'
$artifacts = Join-Path $root 'artifacts'
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }

Write-Host "==> Publishing self-contained win-x64 ($Version)"
dotnet publish (Join-Path $root 'ChordSearch.App\ChordSearch.App.csproj') `
    -c Release `
    -p:Platform=x64 `
    -p:RuntimeIdentifier=win-x64 `
    -p:SelfContained=true `
    -p:PublishReadyToRun=true `
    -p:PublishTrimmed=false `
    -p:Version=$Version `
    -p:WindowsPackageType=None `
    -o $publish

# Ensure XAML resources exist (also copied by csproj target into default publish dir)
$built = Join-Path $root 'ChordSearch.App\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64'
foreach ($name in @('ChordSearch.App.pri', 'App.xbf', 'MainWindow.xbf', 'MainPage.xbf')) {
    $src = Join-Path $built $name
    $dst = Join-Path $publish $name
    if ((Test-Path $src) -and -not (Test-Path $dst)) {
        Copy-Item $src $dst -Force
        Write-Host "Copied missing $name from build output"
    }
}

@('ChordSearch.App.exe', 'ChordSearch.App.pri', 'MainWindow.xbf') | ForEach-Object {
    if (-not (Test-Path (Join-Path $publish $_))) { throw "Publish incomplete: missing $_" }
}

Get-ChildItem $publish -Filter '*.pdb' -Recurse | Remove-Item -Force -ErrorAction SilentlyContinue

Write-Host '==> Building installer (Inno Setup)'
& $iscc (Join-Path $root 'packaging\ChordSearch.iss')
if ($LASTEXITCODE -ne 0) { throw "ISCC failed: $LASTEXITCODE" }

Write-Host '==> Portable zip'
$portableStage = Join-Path $artifacts "ChordSearch-$Version-win-x64-portable"
$portableZip = Join-Path $artifacts "ChordSearch-$Version-win-x64-portable.zip"
if (Test-Path $portableStage) { Remove-Item $portableStage -Recurse -Force }
New-Item -ItemType Directory -Path $portableStage -Force | Out-Null
Copy-Item (Join-Path $publish '*') $portableStage -Recurse -Force
Copy-Item (Join-Path $root 'packaging\portable.txt') $portableStage -Force
Copy-Item (Join-Path $root 'packaging\INSTALL.txt') $portableStage -Force
if (Test-Path $portableZip) { Remove-Item $portableZip -Force }
Compress-Archive -Path (Join-Path $portableStage '*') -DestinationPath $portableZip -Force

Write-Host 'Done.'
Get-ChildItem $artifacts -File | Where-Object { $_.Name -like "*$Version*" } | Format-Table Name, Length -AutoSize
