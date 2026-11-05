param(
    [string]$SuitePath = 'evals/s1-images-v1/suite.json',
    [string[]]$Models = @('gemma3:4b', 'qwen3-vl:4b-instruct')
)

$ErrorActionPreference = 'Stop'
$repoDirectory = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoDirectory 'src/MealBrain.ModelLab/MealBrain.ModelLab.csproj'
if (-not [System.IO.Path]::IsPathRooted($SuitePath)) {
    $SuitePath = Join-Path $repoDirectory $SuitePath
}

Push-Location -LiteralPath $repoDirectory
try {
    & dotnet build $projectPath --nologo
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & dotnet run --no-build --project $projectPath -- --check-image-suite $SuitePath
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    $lockPath = [System.IO.Path]::ChangeExtension($SuitePath, '.lock.json')
    if (-not (Test-Path -LiteralPath $lockPath)) {
        Write-Error 'Najpierw wykonaj --freeze-image-suite po przygotowaniu obrazow i wzorcow.'
        exit 1
    }
    $failedModels = @()
    foreach ($modelName in $Models) {
        Write-Host "S1: $modelName"
        & dotnet run --no-build --project $projectPath -- --image-suite $modelName $SuitePath
        if ($LASTEXITCODE -ne 0) { $failedModels += $modelName }
    }
    if ($failedModels.Count -gt 0) {
        Write-Host ('Proby z bledem technicznym lub kontraktu: ' + ($failedModels -join ', '))
        exit 1
    }
    Write-Host 'Przebiegi zakonczone. Ocena jakosci pozostaje reczna w katalogach runs.'
}
finally { Pop-Location }
