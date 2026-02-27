param(
    [string]$PackageName = "easy.sudoku.puzzle.solver.free",
    [string]$DatabaseName = "Sudoku.db",
    [string]$OutputRoot = "artifacts/device-db"
)

$ErrorActionPreference = "Stop"

function Require-Command {
    param([string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command not found in PATH: $Name"
    }
}

function Get-ConnectedDeviceId {
    $lines = adb devices
    $devices = @()
    foreach ($line in $lines) {
        if ($line -match "^\s*$") { continue }
        if ($line -match "^List of devices attached") { continue }
        if ($line -match "^\*") { continue }
        if ($line -match "^(?<id>\S+)\s+device$") {
            $devices += $Matches["id"]
        }
    }

    if ($devices.Count -eq 0) {
        throw "No ADB device in 'device' state. Connect/unlock a phone and accept RSA prompt."
    }
    if ($devices.Count -gt 1) {
        throw "Multiple ADB devices detected. Leave only one connected and retry."
    }

    return $devices[0]
}

function Pull-RunAsFile {
    param(
        [string]$Package,
        [string]$RemotePath,
        [string]$LocalPath,
        [switch]$Optional,
        [switch]$ExpectSqliteHeader
    )

    $command = "adb exec-out run-as $Package cat $RemotePath > `"$LocalPath`""
    cmd /c $command | Out-Null
    $exitCode = $LASTEXITCODE

    $exists = Test-Path $LocalPath
    $length = 0
    if ($exists) {
        $length = (Get-Item $LocalPath).Length
    }

    if ($exitCode -ne 0 -or -not $exists -or $length -eq 0) {
        if ($exists -and $length -eq 0) {
            Remove-Item -Force $LocalPath
        }
        if ($Optional) {
            Write-Host "Optional file missing: $RemotePath"
            return $false
        }
        throw "Failed pulling required file: $RemotePath"
    }

    # Common run-as failure text can be redirected into the output file.
    $headText = ""
    if ($length -ge 5) {
        $headText = Get-Content -Path $LocalPath -TotalCount 1 -ErrorAction SilentlyContinue
    }
    if ($headText -like "run-as:*" -or $headText -like "cat:*") {
        Remove-Item -Force $LocalPath -ErrorAction SilentlyContinue
        if ($Optional) {
            Write-Host "Optional file unavailable via run-as: $RemotePath ($headText)"
            return $false
        }
        throw "run-as read failed for ${RemotePath}: $headText"
    }

    if ($ExpectSqliteHeader) {
        $header = [System.Text.Encoding]::ASCII.GetString([System.IO.File]::ReadAllBytes($LocalPath), 0, [Math]::Min(16, [int]$length))
        if (-not $header.StartsWith("SQLite format 3")) {
            Remove-Item -Force $LocalPath -ErrorAction SilentlyContinue
            throw "Pulled file is not a SQLite database: $RemotePath"
        }
    }

    Write-Host ("Pulled {0} ({1} bytes)" -f $RemotePath, $length)
    return $true
}

Require-Command "adb"

$deviceId = Get-ConnectedDeviceId
Write-Host "Using ADB device: $deviceId"

$pkgPath = (adb shell pm path $PackageName | Select-String -Pattern "^package:")
if (-not $pkgPath) {
    throw "Package not found on device: $PackageName"
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$destDir = Join-Path $OutputRoot "$PackageName`_$timestamp"
New-Item -ItemType Directory -Force -Path $destDir | Out-Null

$mainLocal = Join-Path $destDir $DatabaseName
$walLocal = Join-Path $destDir "$DatabaseName-wal"
$shmLocal = Join-Path $destDir "$DatabaseName-shm"

$baseRemote = "/data/data/$PackageName/databases"

Pull-RunAsFile -Package $PackageName -RemotePath "$baseRemote/$DatabaseName" -LocalPath $mainLocal -ExpectSqliteHeader | Out-Null
Pull-RunAsFile -Package $PackageName -RemotePath "$baseRemote/$DatabaseName-wal" -LocalPath $walLocal -Optional | Out-Null
Pull-RunAsFile -Package $PackageName -RemotePath "$baseRemote/$DatabaseName-shm" -LocalPath $shmLocal -Optional | Out-Null

Write-Host ""
Write-Host "DB export complete:"
Write-Host "  $destDir"

if (Get-Command sqlite3 -ErrorAction SilentlyContinue) {
    Write-Host ""
    Write-Host "scoreVersion distribution (sqlite3):"
    sqlite3 $mainLocal "select coalesce(scoreVersion,-1) as scoreVersion, count(*) as cnt from SudokuGame group by coalesce(scoreVersion,-1) order by scoreVersion;"
}
else {
    Write-Host ""
    Write-Host "sqlite3 not found in PATH. Run manually when available:"
    Write-Host "  sqlite3 `"$mainLocal`" `"select coalesce(scoreVersion,-1) as scoreVersion, count(*) as cnt from SudokuGame group by coalesce(scoreVersion,-1) order by scoreVersion;`""
}
