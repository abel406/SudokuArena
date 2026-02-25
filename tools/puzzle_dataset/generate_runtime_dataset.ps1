param(
    [int]$MaxPerTier = 250,
    [int]$TargetTotal = 0,
    [int]$MinTotal = 0
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Web.Extensions

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$configDir = Join-Path $repoRoot "artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/resources/assets/config"
$solverDetailsPath = Join-Path $configDir "defaultQbSolverDetails.decrypted.json"
$timeMapEncryptedPath = Join-Path $configDir "question_time_map.json"

$outputSeedPath = Join-Path $repoRoot "src/SudokuArena.Desktop/PuzzleSeed/puzzles.runtime.v1.json"
$outputDataPath = Join-Path $repoRoot "src/SudokuArena.Desktop/Data/puzzles.runtime.v1.json"

$maxPerTier = $MaxPerTier
$schemaVersion = "sudokuarena.puzzle_dataset.v1"

$aesKey = [System.Text.Encoding]::UTF8.GetBytes("SKpvYaOKKIz+2dpO")
$aesIv = New-Object byte[] 16

function Decrypt-AesToString([byte[]]$cipherBytes) {
    $aes = [System.Security.Cryptography.Aes]::Create()
    $aes.Mode = [System.Security.Cryptography.CipherMode]::CBC
    $aes.Padding = [System.Security.Cryptography.PaddingMode]::PKCS7
    $aes.Key = $aesKey
    $aes.IV = $aesIv
    $dec = $aes.CreateDecryptor()
    try {
        $plainBytes = $dec.TransformFinalBlock($cipherBytes, 0, $cipherBytes.Length)
        return [System.Text.Encoding]::UTF8.GetString($plainBytes)
    }
    finally {
        $dec.Dispose()
        $aes.Dispose()
    }
}

function ConvertTo-Hashtable($value) {
    if ($null -eq $value) {
        return $null
    }

    if ($value -is [string] -or $value.GetType().IsPrimitive) {
        return $value
    }

    if ($value -is [System.Collections.IDictionary]) {
        $map = [System.Collections.Generic.Dictionary[string, object]]::new([System.StringComparer]::Ordinal)
        foreach ($key in $value.Keys) {
            $map[[string]$key] = ConvertTo-Hashtable $value[$key]
        }
        return $map
    }

    if ($value -is [pscustomobject]) {
        $map = [System.Collections.Generic.Dictionary[string, object]]::new([System.StringComparer]::Ordinal)
        foreach ($property in $value.PSObject.Properties) {
            $map[$property.Name] = ConvertTo-Hashtable $property.Value
        }
        return $map
    }

    if ($value -is [System.Collections.IEnumerable]) {
        $list = @()
        foreach ($item in $value) {
            $list += ,(ConvertTo-Hashtable $item)
        }
        return $list
    }

    return $value
}

function Load-JsonHashtable([string]$path, [bool]$tryDecrypt) {
    $parseJson = {
        param([string]$json)
        $serializer = New-Object System.Web.Script.Serialization.JavaScriptSerializer
        $serializer.MaxJsonLength = [int]::MaxValue
        $serializer.RecursionLimit = 1024
        return ConvertTo-Hashtable ($serializer.DeserializeObject($json))
    }

    $raw = Get-Content $path -Raw
    try {
        return & $parseJson $raw
    }
    catch {
        if (-not $tryDecrypt) {
            throw
        }
    }

    $cipher = [System.IO.File]::ReadAllBytes($path)
    $decrypted = Decrypt-AesToString $cipher
    return & $parseJson $decrypted
}

function Decode-Qid([string]$qid) {
    if ([string]::IsNullOrWhiteSpace($qid) -or $qid.Length -ne 81) {
        return $null
    }

    $puzzleChars = New-Object System.Collections.Generic.List[char]
    $solutionChars = New-Object System.Collections.Generic.List[char]
    $givenCount = 0
    foreach ($ch in $qid.ToCharArray()) {
        $ascii = [int][char]$ch
        $isUpper = $ascii -ge [int][char]'A' -and $ascii -le [int][char]'I'
        $isLower = $ascii -ge [int][char]'a' -and $ascii -le [int][char]'i'
        if (-not $isUpper -and -not $isLower) {
            return $null
        }

        $base = if ($isUpper) { [int][char]'A' } else { [int][char]'a' }
        $digit = [char]([int][char]'1' + ($ascii - $base))
        $solutionChars.Add($digit)
        if ($isUpper) {
            $puzzleChars.Add($digit)
            $givenCount++
        }
        else {
            $puzzleChars.Add('.')
        }
    }

    return [pscustomobject]@{
        Puzzle = -join $puzzleChars
        Solution = -join $solutionChars
        GivenCount = $givenCount
    }
}

function Get-Percentile([double[]]$values, [double]$p) {
    if ($values.Count -eq 0) {
        return 0.0
    }

    $sorted = $values | Sort-Object
    $index = [int][math]::Floor(($sorted.Count - 1) * $p)
    return [double]$sorted[$index]
}

$techniqueWeights = @{
    "last_free_cell" = 1.0
    "last_possible_number" = 1.1
    "cross_hatching_block" = 2.0
    "cross_hatching_row" = 2.0
    "cross_hatching_column" = 2.0
    "naked_single" = 2.3
    "hidden_single" = 1.5
    "locked_candidates_claiming" = 2.5
    "locked_candidates_pointing" = 2.6
    "naked_pair" = 3.0
    "hidden_pair" = 3.4
    "naked_triple" = 3.6
    "hidden_triple" = 4.0
    "naked_quadruple" = 5.0
    "hidden_quadruple" = 5.4
    "x_wing" = 3.2
    "swordfish" = 3.8
    "jellyfish" = 5.2
    "skyscraper" = 4.0
    "bug" = 5.6
    "xy_wing" = 4.2
    "w_wing" = 4.0
    "2_string_kite" = 3.0
    "turbot_fish" = 4.0
    "xyz_wing" = 4.4
    "finned_x_wing" = 3.6
    "finned_swordfish" = 4.2
    "finned_jellyfish" = 5.8
    "uniqueness" = 4.5
    "xy_chain" = 7.5
}

if (-not (Test-Path $solverDetailsPath)) {
    throw "Missing file: $solverDetailsPath"
}

if (-not (Test-Path $timeMapEncryptedPath)) {
    throw "Missing file: $timeMapEncryptedPath"
}

if ($maxPerTier -le 0) {
    throw "MaxPerTier must be > 0."
}

if ($TargetTotal -lt 0) {
    throw "TargetTotal cannot be negative."
}

if ($MinTotal -lt 0) {
    throw "MinTotal cannot be negative."
}

$solverMap = Load-JsonHashtable $solverDetailsPath $false
$timeMapRaw = Load-JsonHashtable $timeMapEncryptedPath $true

$timeMap81 = @{}
foreach ($k in $timeMapRaw.Keys) {
    if ($k.Length -eq 81) {
        $vals = @($timeMapRaw[$k])
        if ($vals.Count -eq 4 -and ($vals | Where-Object { $_ -le 0 }).Count -eq 0) {
            $timeMap81[$k] = [int[]]$vals
        }
    }
}

$rows = New-Object System.Collections.Generic.List[object]
foreach ($qid in ($solverMap.Keys | Sort-Object)) {
    if ($qid.Length -ne 81) {
        continue
    }

    $decoded = Decode-Qid $qid
    if ($null -eq $decoded) {
        continue
    }

    $techCounts = @{}
    $weighted = 0.0
    $maxRate = 1.0
    $advancedHits = 0

    $entry = $solverMap[$qid]
    foreach ($tech in $entry.Keys) {
        $count = [int]$entry[$tech]
        if ($count -le 0) {
            continue
        }

        $techCounts[$tech] = $count
        $w = if ($techniqueWeights.ContainsKey($tech)) { [double]$techniqueWeights[$tech] } else { 3.0 }
        $weighted += $count * $w
        if ($w -gt $maxRate) {
            $maxRate = $w
        }
        if ($w -ge 3.5) {
            $advancedHits += $count
        }
    }

    $emptyCount = 81 - $decoded.GivenCount
    if ($emptyCount -le 0) {
        continue
    }

    $norm = $weighted / $emptyCount
    $rows.Add([pscustomobject]@{
        Qid = $qid
        Puzzle = $decoded.Puzzle
        Solution = $decoded.Solution
        GivenCount = $decoded.GivenCount
        Weighted = [math]::Round($norm, 3)
        MaxRate = [int][math]::Ceiling($maxRate)
        AdvancedHits = $advancedHits
        TechniqueCounts = $techCounts
    }) | Out-Null
}

if ($rows.Count -eq 0) {
    throw "No valid 9x9 rows produced."
}

$weights = $rows | ForEach-Object { [double]$_.Weighted }
$rowsSortedByDifficulty = $rows | Sort-Object Weighted, Qid
$rowCount = $rowsSortedByDifficulty.Count
$cut1 = [int][math]::Floor($rowCount * 0.20)
$cut2 = [int][math]::Floor($rowCount * 0.40)
$cut3 = [int][math]::Floor($rowCount * 0.60)
$cut4 = [int][math]::Floor($rowCount * 0.80)
for ($i = 0; $i -lt $rowCount; $i++) {
    $row = $rowsSortedByDifficulty[$i]
    if ($i -lt $cut1) {
        $row | Add-Member -NotePropertyName Tier -NotePropertyValue "Beginner"
    }
    elseif ($i -lt $cut2) {
        $row | Add-Member -NotePropertyName Tier -NotePropertyValue "Easy"
    }
    elseif ($i -lt $cut3) {
        $row | Add-Member -NotePropertyName Tier -NotePropertyValue "Medium"
    }
    elseif ($i -lt $cut4) {
        $row | Add-Member -NotePropertyName Tier -NotePropertyValue "Hard"
    }
    else {
        $row | Add-Member -NotePropertyName Tier -NotePropertyValue "Expert"
    }
}

$tierOrder = @("Beginner", "Easy", "Medium", "Hard", "Expert")
$prefixByTier = @{
    "Beginner" = "bgn"
    "Easy" = "eas"
    "Medium" = "med"
    "Hard" = "hrd"
    "Expert" = "exp"
}

$desiredCountByTier = @{}
$availableCountByTier = @{}
foreach ($tier in $tierOrder) {
    $availableCountByTier[$tier] = @($rows | Where-Object { $_.Tier -eq $tier }).Count
}

if ($TargetTotal -gt 0) {
    if ($TargetTotal -ge $rows.Count) {
        foreach ($tier in $tierOrder) {
            $desiredCountByTier[$tier] = [int]$availableCountByTier[$tier]
        }
    }
    else {
        $tierCount = $tierOrder.Count
        $base = [int][math]::Floor($TargetTotal / $tierCount)
        $remainder = $TargetTotal % $tierCount
        for ($i = 0; $i -lt $tierCount; $i++) {
            $tier = $tierOrder[$i]
            $desiredCountByTier[$tier] = $base + $(if ($i -lt $remainder) { 1 } else { 0 })
        }

        $deficit = 0
        foreach ($tier in $tierOrder) {
            $available = [int]$availableCountByTier[$tier]
            if ($desiredCountByTier[$tier] -gt $available) {
                $deficit += ($desiredCountByTier[$tier] - $available)
                $desiredCountByTier[$tier] = $available
            }
        }

        while ($deficit -gt 0) {
            $allocated = $false
            foreach ($tier in $tierOrder) {
                $remaining = [int]$availableCountByTier[$tier] - [int]$desiredCountByTier[$tier]
                if ($remaining -le 0) {
                    continue
                }

                $desiredCountByTier[$tier]++
                $deficit--
                $allocated = $true
                if ($deficit -le 0) {
                    break
                }
            }

            if (-not $allocated) {
                break
            }
        }
    }
}
else {
    foreach ($tier in $tierOrder) {
        $desiredCountByTier[$tier] = $maxPerTier
    }
}

$questionBank = New-Object System.Collections.Generic.List[object]
$solverDetails = @{}
$timeMap = @{}

foreach ($tier in $tierOrder) {
    $desiredForTier = [int]$desiredCountByTier[$tier]
    $tierRows = $rows |
        Where-Object { $_.Tier -eq $tier } |
        Sort-Object Qid |
        Select-Object -First $desiredForTier

    $index = 1
    foreach ($row in $tierRows) {
        $puzzleId = "{0}-{1}" -f $prefixByTier[$tier], $index.ToString("D4")
        $index++

        $questionBank.Add([ordered]@{
            puzzle_id = $puzzleId
            puzzle = $row.Puzzle
            solution = $row.Solution
            difficulty_tier = $tier
            given_count = [int]$row.GivenCount
            board_kind = "Classic9x9"
            mode = $tier
        }) | Out-Null

        $solverDetails[$puzzleId] = [ordered]@{
            weighted_se = [double]$row.Weighted
            max_rate = [int]$row.MaxRate
            advanced_hits = [int]$row.AdvancedHits
            technique_counts = $row.TechniqueCounts
        }

        if ($timeMap81.ContainsKey($row.Qid)) {
            $timeMap[$puzzleId] = [int[]]$timeMap81[$row.Qid]
        }
        else {
            $base = switch ($tier) {
                "Beginner" { @(90, 130, 190, 270) }
                "Easy" { @(110, 160, 230, 330) }
                "Medium" { @(140, 200, 290, 420) }
                "Hard" { @(180, 250, 360, 520) }
                default { @(220, 320, 470, 680) }
            }
            $timeMap[$puzzleId] = [int[]]$base
        }
    }
}

$dataset = [ordered]@{
    schema_version = $schemaVersion
    generated_at_utc = [DateTimeOffset]::UtcNow.ToString("o")
    question_bank = $questionBank
    solver_details = $solverDetails
    time_map = $timeMap
}

$jsonOut = $dataset | ConvertTo-Json -Depth 25
$jsonOut | Set-Content -Encoding UTF8 $outputSeedPath
$jsonOut | Set-Content -Encoding UTF8 $outputDataPath

$counts = @{}
foreach ($entry in $questionBank) {
    $tier = [string]$entry["difficulty_tier"]
    if (-not $counts.ContainsKey($tier)) {
        $counts[$tier] = 0
    }
    $counts[$tier] = [int]$counts[$tier] + 1
}
Write-Output "Generated: $outputSeedPath"
Write-Output "Synced: $outputDataPath"
Write-Output ("Total puzzles: " + $questionBank.Count)
foreach ($tier in $tierOrder) {
    if ($counts.ContainsKey($tier)) {
        Write-Output ("  {0}: {1}" -f $tier, $counts[$tier])
    }
}
if ($TargetTotal -gt 0) {
    Write-Output ("TargetTotal requested: {0}" -f $TargetTotal)
}
if ($MinTotal -gt 0) {
    Write-Output ("MinTotal required: {0}" -f $MinTotal)
}

if ($MinTotal -gt 0 -and $questionBank.Count -lt $MinTotal) {
    throw ("Generated dataset has {0} puzzles, below required MinTotal {1}." -f $questionBank.Count, $MinTotal)
}
