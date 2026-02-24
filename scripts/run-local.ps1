param(
    [string]$ServerUrl = "http://localhost:5055",
    [switch]$SkipRestore,
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [switch]$ServerOnly
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$serverProcess = $null

Push-Location $repoRoot
try {
    if (-not $SkipRestore) {
        dotnet restore SudokuArena.slnx
    }

    if (-not $SkipBuild) {
        dotnet build SudokuArena.slnx --no-restore
    }

    if (-not $SkipTests) {
        dotnet test SudokuArena.slnx --no-build
    }

    $serverOutLog = Join-Path $env:TEMP "SudokuArena-server.out.log"
    $serverErrLog = Join-Path $env:TEMP "SudokuArena-server.err.log"
    if (Test-Path $serverOutLog) {
        Remove-Item $serverOutLog -Force
    }
    if (Test-Path $serverErrLog) {
        Remove-Item $serverErrLog -Force
    }

    $serverProcess = Start-Process `
        -FilePath "dotnet" `
        -ArgumentList @("run", "--project", "src/SudokuArena.Server", "--urls", $ServerUrl) `
        -WorkingDirectory $repoRoot `
        -RedirectStandardOutput $serverOutLog `
        -RedirectStandardError $serverErrLog `
        -PassThru

    $healthOk = $false
    for ($i = 0; $i -lt 40; $i++) {
        Start-Sleep -Milliseconds 500
        try {
            $null = Invoke-RestMethod -Uri "$ServerUrl/api/health" -Method Get -TimeoutSec 2
            $healthOk = $true
            break
        }
        catch {
            # Keep waiting until server is ready.
        }
    }

    if (-not $healthOk) {
        throw "Server did not become healthy. See logs: $serverOutLog | $serverErrLog"
    }

    Write-Host "Server ready at $ServerUrl (PID $($serverProcess.Id))."
    Write-Host "Server out log: $serverOutLog"
    Write-Host "Server err log: $serverErrLog"

    if ($ServerOnly) {
        Write-Host "Server-only mode active. Press Ctrl+C to stop."
        Wait-Process -Id $serverProcess.Id
        return
    }

    dotnet run --project src/SudokuArena.Desktop --no-build
}
finally {
    if ($serverProcess -and -not $serverProcess.HasExited) {
        Stop-Process -Id $serverProcess.Id -Force
        Write-Host "Server process stopped."
    }

    Pop-Location
}
