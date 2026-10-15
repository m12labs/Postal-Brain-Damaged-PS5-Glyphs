[CmdletBinding()]
param([string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\POSTAL Brain Damaged')
$ErrorActionPreference = 'Stop'
$GameDir = (Resolve-Path -LiteralPath $GameDir).Path
if (Get-Process -Name 'POSTAL Brain Damaged' -ErrorAction SilentlyContinue) { throw 'Close the game first.' }
$target = Join-Path $GameDir 'BepInEx\plugins\PS5Glyphs'
if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Recurse -Force }
Write-Host 'PS5 Glyphs removed. Config (BepInEx\config\pbd.ps5glyphs.cfg) and dumps (BepInEx\PS5Glyphs) retained.'
