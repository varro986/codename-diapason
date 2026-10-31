# Usage: pwsh -NoProfile -File tools/new-templates.ps1 -Folder <folder>
# Writes an empty team registry (registry.xlsx) and an empty engagement plan (plan.xlsx), with the sheets and columns of
# specs/data-contract.md. Never overwrites: if either file exists in the folder, nothing is written.
param([Parameter(Mandatory)][string]$Folder)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..' 'src' 'Diapason.Tool'
dotnet run --project $project -- templates $Folder
exit $LASTEXITCODE
