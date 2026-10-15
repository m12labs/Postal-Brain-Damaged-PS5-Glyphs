[CmdletBinding()]
param([string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\POSTAL Brain Damaged', [switch]$SkipBuild)
# Builds the plugin and packages it as artifacts\PS5Glyphs-<version>.zip, ready to extract into the game folder.
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (!$SkipBuild) { & (Join-Path $PSScriptRoot 'Build.ps1') -GameDir $GameDir }
$source = Join-Path $repo 'artifacts\PS5Glyphs\BepInEx\plugins\PS5Glyphs'
$dll = Join-Path $source 'PS5Glyphs.dll'
if (!(Test-Path -LiteralPath $dll)) { throw 'Run scripts/Build.ps1 first.' }
# Without the map the plugin loads but changes nothing, so never ship that.
if (!(Test-Path -LiteralPath (Join-Path $source 'glyph-map.json'))) { throw 'assets/glyph-map.json is missing; a release without it would not replace any glyphs.' }
$pngs = @(Get-ChildItem -LiteralPath (Join-Path $source 'glyphs') -Filter *.png)
if ($pngs.Count -eq 0) { throw 'No glyph overlays staged; run python -I scripts/make_symbols.py and rebuild.' }
$version = ((Get-Item -LiteralPath $dll).VersionInfo.ProductVersion -split '\+')[0]

$stagingParent = Join-Path $repo '.local'
$staging = Join-Path $stagingParent 'release'
if ([IO.Path]::GetFullPath($staging) -ne (Join-Path $repo '.local\release')) { throw 'Release staging path is outside the repository.' }
foreach ($path in $stagingParent, $staging) {
    if ((Test-Path -LiteralPath $path) -and (Get-Item -LiteralPath $path).Attributes.HasFlag([IO.FileAttributes]::ReparsePoint)) { throw "$path cannot be a junction or symbolic link." }
}
if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
$plugin = Join-Path $staging 'BepInEx\plugins\PS5Glyphs'
New-Item -ItemType Directory -Force -Path (Split-Path $plugin) | Out-Null
Copy-Item -LiteralPath $source -Destination $plugin -Recurse
Copy-Item -LiteralPath (Join-Path $repo 'README.md'), (Join-Path $repo 'LICENSE') -Destination $staging

$dllHash = (Get-FileHash -LiteralPath $dll).Hash
@"
PS5 Glyphs $version - PlayStation 5 button prompts for POSTAL: Brain Damaged

Replaces every Xbox controller prompt (menus, tutorials, hints) with PS5 glyphs.
Keyboard and mouse icons are unchanged. Works alongside the PBDMP multiplayer mod.

1. Install BepInEx 6.0.0-be.788+5b766a3, Unity IL2CPP win-x64, into the game folder
   (Steam: right-click the game > Manage > Browse local files):
   https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788%2B5b766a3.zip
2. Start the game once and let BepInEx finish its first-run setup, then quit.
3. Copy the BepInEx folder from this zip into the game folder, merging with the existing one.
4. Start the game. BepInEx\LogOutput.log should show: PS5 Glyphs $version loaded

Settings: BepInEx\config\pbd.ps5glyphs.cfg (created on first run).
  Enabled = false turns the mod off.
Prompts follow the Xbox layout the game expects, so play with Steam Input on
(Steam > game Properties > Controller), which is Steam's default for a DualSense.

Uninstall: delete BepInEx\plugins\PS5Glyphs.

The PS5 symbols are stamped into the game's own hand-drawn buttons at runtime;
no game art is included in this package.

PS5Glyphs.dll  SHA256 $dllHash
"@ | Set-Content -LiteralPath (Join-Path $staging 'INSTALL.txt') -Encoding utf8

$zip = Join-Path $repo "artifacts\PS5Glyphs-$version.zip"
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
# Built by hand: Windows PowerShell's Compress-Archive writes '\' separators, which other unzip tools misread.
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::Open($zip, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in Get-ChildItem -LiteralPath $staging -Recurse -File) {
        $entry = $file.FullName.Substring($staging.Length + 1).Replace('\', '/')
        [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $entry, [IO.Compression.CompressionLevel]::Optimal)
    }
} finally { $archive.Dispose() }
Write-Host "Release: $zip"
Write-Host "SHA256:  $((Get-FileHash -LiteralPath $zip).Hash)"
