# prepare the release package for thunderstore
param([string]$Configuration = "Release")

$ErrorActionPreference = "Stop"
$repo = Split-Path $PSScriptRoot -Parent
$manifest = Get-Content (Join-Path $repo "package\manifest.json") -Raw | ConvertFrom-Json

dotnet build (Join-Path $repo "src\WheelOfWuck.csproj") -c $Configuration -p:Deploy=false -v:m
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

$stage = Join-Path $repo "dist\stage"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force $stage | Out-Null
Copy-Item (Join-Path $repo "package\*"), (Join-Path $repo "README.md"), (Join-Path $repo "CHANGELOG.md") $stage
Copy-Item (Join-Path $repo "src\bin\$Configuration\WheelOfWuck.dll") $stage

$zip = Join-Path $repo "dist\senkodev-$($manifest.name)-$($manifest.version_number).zip"
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zip
Remove-Item $stage -Recurse -Force
"Packaged $zip"
