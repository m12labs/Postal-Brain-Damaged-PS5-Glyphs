[CmdletBinding()]
param([string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\POSTAL Brain Damaged')
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$env:DOTNET_CLI_HOME = Join-Path $repo '.local\dotnet-home'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:NUGET_PACKAGES = Join-Path $repo '.local\nuget'
$GameDir = (Resolve-Path -LiteralPath $GameDir).Path
Push-Location $repo
try {
    dotnet build src/PS5Glyphs/PS5Glyphs.csproj -c Release "-p:GameDir=$GameDir" --nologo
    if ($LASTEXITCODE) { throw 'Plugin build failed.' }
    $bin = 'src/PS5Glyphs/bin/Release/net6.0'
    $package = Join-Path $repo 'artifacts\PS5Glyphs\BepInEx\plugins\PS5Glyphs'
    if (Test-Path -LiteralPath $package) { Remove-Item -LiteralPath $package -Recurse -Force }
    New-Item -ItemType Directory -Force -Path (Join-Path $package 'glyphs') | Out-Null
    Copy-Item -LiteralPath "$bin/PS5Glyphs.dll" -Destination $package
    if (Test-Path -LiteralPath "$bin/glyph-map.json") { Copy-Item -LiteralPath "$bin/glyph-map.json" -Destination $package }
    Get-ChildItem -LiteralPath "$bin/glyphs" -Filter *.png -ErrorAction SilentlyContinue | Copy-Item -Destination (Join-Path $package 'glyphs')
    Write-Host "Build output: $package"
} finally { Pop-Location }
