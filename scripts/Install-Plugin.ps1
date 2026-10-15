[CmdletBinding()]
param([string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\POSTAL Brain Damaged')
$ErrorActionPreference = 'Stop'
$GameDir = (Resolve-Path -LiteralPath $GameDir).Path
if (Get-Process -Name 'POSTAL Brain Damaged' -ErrorAction SilentlyContinue) { throw 'Close the game before installing the plugin.' }
if (!(Test-Path -LiteralPath (Join-Path $GameDir 'BepInEx\interop\Assembly-CSharp.dll'))) { throw 'Install BepInEx 6 IL2CPP and launch the game once first.' }
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$source = Join-Path $repo 'artifacts\PS5Glyphs\BepInEx\plugins\PS5Glyphs'
if (!(Test-Path -LiteralPath (Join-Path $source 'PS5Glyphs.dll'))) { throw 'Run scripts/Build.ps1 first.' }
# The plugin owns its whole folder, so an update replaces it outright.
$target = Join-Path $GameDir 'BepInEx\plugins\PS5Glyphs'
if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Recurse -Force }
Copy-Item -LiteralPath $source -Destination $target -Recurse
Write-Host "Installed PS5 Glyphs: $target"
