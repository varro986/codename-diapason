# Usage: pwsh -NoProfile -File cicd/run.ps1
# The product's checks, the same on the machine before every push and in CI: build with warnings as errors, format
# check, tests. Stops at the first step that fails and says which.
$ErrorActionPreference = 'Stop'
$solution = Join-Path $PSScriptRoot '..' 'Diapason.slnx'

$steps = [ordered]@{
    'build'  = { dotnet build $solution --configuration Release --nologo }
    'format' = { dotnet format $solution --verify-no-changes --no-restore }
    'tests'  = { dotnet test $solution --configuration Release --no-build --nologo }
}

foreach ($step in $steps.GetEnumerator()) {
    Write-Host "cicd: $($step.Key)..."
    & $step.Value
    if ($LASTEXITCODE -ne 0) {
        Write-Host "cicd: step '$($step.Key)' failed (exit $LASTEXITCODE): fix it and run cicd/run.ps1 again."
        exit $LASTEXITCODE
    }
}
Write-Host "cicd: all $($steps.Count) steps passed."
