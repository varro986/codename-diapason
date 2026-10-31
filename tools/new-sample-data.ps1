# Usage: pwsh -NoProfile -File tools/new-sample-data.ps1 [-Folder <folder>]
# Writes a synthetic data folder (invented people only): registry.xlsx, two valid plans and one plan with errors.
# Default folder: data/sample under the repository, ignored by git. Never overwrites: if a file exists, nothing is written.
param([string]$Folder = (Join-Path $PSScriptRoot '..' 'data' 'sample'))

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..' 'src' 'Diapason.Tool'
dotnet run --project $project -- sample-data $Folder
exit $LASTEXITCODE
