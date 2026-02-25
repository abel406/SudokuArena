param(
    [string]$DatasetPath = ""
)

$ErrorActionPreference = "Stop"

function Fail([string]$message) {
    throw [System.InvalidOperationException]::new($message)
}

function Test-AsciiDigit([char]$ch) {
    return [int][char]$ch -ge [int][char]'1' -and [int][char]$ch -le [int][char]'9'
}

function Get-BoardSpec([string]$boardKind) {
    switch ($boardKind) {
        "Classic9x9" {
            return [ordered]@{
                Size = 9
                CellCount = 81
                BoxRows = 3
                BoxCols = 3
                Symbols = "123456789"
            }
        }
        "SixBySix" {
            return [ordered]@{
                Size = 6
                CellCount = 36
                BoxRows = 2
                BoxCols = 3
                Symbols = "123456"
            }
        }
        "SixteenBySixteen" {
            return [ordered]@{
                Size = 16
                CellCount = 256
                BoxRows = 4
                BoxCols = 4
                Symbols = "123456789ABCDEFG"
            }
        }
        default {
            Fail("Unsupported board_kind '$boardKind'.")
        }
    }
}

function Test-SymbolChar([char]$ch, [string]$symbols) {
    return $symbols.Contains([string]$ch)
}

function Test-PuzzleChar([char]$ch, [string]$symbols) {
    return (Test-SymbolChar $ch $symbols) -or $ch -eq '.' -or $ch -eq '0'
}

function ConvertTo-Hashtable($value) {
    if ($null -eq $value) {
        return $null
    }

    if ($value -is [string] -or $value.GetType().IsPrimitive) {
        return $value
    }

    if ($value -is [System.Collections.IDictionary]) {
        $map = @{}
        foreach ($key in $value.Keys) {
            $map[[string]$key] = ConvertTo-Hashtable $value[$key]
        }
        return $map
    }

    if ($value -is [pscustomobject]) {
        $map = @{}
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

function Assert-SudokuSolution([string]$puzzleId, [string]$solution, [hashtable]$spec) {
    $size = [int]$spec.Size
    $boxRows = [int]$spec.BoxRows
    $boxCols = [int]$spec.BoxCols
    $symbols = [string]$spec.Symbols

    for ($row = 0; $row -lt $size; $row++) {
        $seen = @{}
        for ($col = 0; $col -lt $size; $col++) {
            $value = $solution[$row * $size + $col]
            if (-not (Test-SymbolChar $value $symbols)) {
                Fail("Puzzle '$puzzleId' has invalid solution symbol '$value' at row $row, col $col.")
            }
            if ($seen.ContainsKey($value)) {
                Fail("Puzzle '$puzzleId' has duplicated value '$value' in row $row.")
            }
            $seen[$value] = $true
        }
        if ($seen.Count -ne $size) {
            Fail("Puzzle '$puzzleId' row $row does not contain $size unique values.")
        }
    }

    for ($col = 0; $col -lt $size; $col++) {
        $seen = @{}
        for ($row = 0; $row -lt $size; $row++) {
            $value = $solution[$row * $size + $col]
            if ($seen.ContainsKey($value)) {
                Fail("Puzzle '$puzzleId' has duplicated value '$value' in column $col.")
            }
            $seen[$value] = $true
        }
        if ($seen.Count -ne $size) {
            Fail("Puzzle '$puzzleId' column $col does not contain $size unique values.")
        }
    }

    $boxRowCount = [int]($size / $boxRows)
    $boxColCount = [int]($size / $boxCols)
    for ($boxRow = 0; $boxRow -lt $boxRowCount; $boxRow++) {
        for ($boxCol = 0; $boxCol -lt $boxColCount; $boxCol++) {
            $seen = @{}
            for ($dy = 0; $dy -lt $boxRows; $dy++) {
                for ($dx = 0; $dx -lt $boxCols; $dx++) {
                    $row = $boxRow * $boxRows + $dy
                    $col = $boxCol * $boxCols + $dx
                    $value = $solution[$row * $size + $col]
                    if ($seen.ContainsKey($value)) {
                        Fail("Puzzle '$puzzleId' has duplicated value '$value' in box ($boxRow,$boxCol).")
                    }
                    $seen[$value] = $true
                }
            }
            if ($seen.Count -ne $size) {
                Fail("Puzzle '$puzzleId' box ($boxRow,$boxCol) does not contain $size unique values.")
            }
        }
    }
}

if ([string]::IsNullOrWhiteSpace($DatasetPath)) {
    $repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
    $DatasetPath = Join-Path $repoRoot "src/SudokuArena.Desktop/PuzzleSeed/puzzles.runtime.v1.json"
}

if (-not (Test-Path $DatasetPath)) {
    Fail("Dataset not found: $DatasetPath")
}

$raw = Get-Content $DatasetPath -Raw
$data = ConvertTo-Hashtable ($raw | ConvertFrom-Json)

if (-not $data.ContainsKey("schema_version")) {
    Fail("Missing required field 'schema_version'.")
}

if ($data.schema_version -ne "sudokuarena.puzzle_dataset.v1") {
    Fail("Unsupported schema_version '$($data.schema_version)'.")
}

if (-not $data.ContainsKey("question_bank") -or $null -eq $data.question_bank) {
    Fail("Missing required field 'question_bank'.")
}

if (-not $data.ContainsKey("solver_details") -or $null -eq $data.solver_details) {
    Fail("Missing required field 'solver_details'.")
}

if (-not $data.ContainsKey("time_map") -or $null -eq $data.time_map) {
    Fail("Missing required field 'time_map'.")
}

$questions = @($data.question_bank)
if ($questions.Count -eq 0) {
    Fail("'question_bank' must contain at least one puzzle.")
}

$solverDetails = $data.solver_details
$timeMap = $data.time_map
$seenIds = New-Object "System.Collections.Generic.HashSet[string]"
$tierCounts = @{}

foreach ($entry in $questions) {
    $puzzleId = [string]$entry.puzzle_id
    if ([string]::IsNullOrWhiteSpace($puzzleId)) {
        Fail("Question entry with empty puzzle_id.")
    }

    if (-not $seenIds.Add($puzzleId)) {
        Fail("Duplicate puzzle_id '$puzzleId'.")
    }

    $puzzle = [string]$entry.puzzle
    $solution = [string]$entry.solution
    $tier = [string]$entry.difficulty_tier
    $boardKind = [string]$(if ($entry.ContainsKey("board_kind")) { $entry.board_kind } else { "Classic9x9" })
    $mode = [string]$(if ($entry.ContainsKey("mode")) { $entry.mode } else { "" })
    $givenCount = [int]$entry.given_count
    $spec = Get-BoardSpec $boardKind
    $symbols = [string]$spec.Symbols

    if ($puzzle.Length -ne [int]$spec.CellCount) {
        Fail("Puzzle '$puzzleId' must contain $($spec.CellCount) chars for $boardKind. Found $($puzzle.Length).")
    }

    if ($solution.Length -ne [int]$spec.CellCount) {
        Fail("Solution '$puzzleId' must contain $($spec.CellCount) chars for $boardKind. Found $($solution.Length).")
    }

    for ($i = 0; $i -lt [int]$spec.CellCount; $i++) {
        $p = $puzzle[$i]
        $s = $solution[$i]
        if (-not (Test-PuzzleChar $p $symbols)) {
            Fail("Puzzle '$puzzleId' contains invalid character '$p' at index $i.")
        }

        if (-not (Test-SymbolChar $s $symbols)) {
            Fail("Solution '$puzzleId' contains invalid character '$s' at index $i.")
        }

        if ((Test-SymbolChar $p $symbols) -and $p -ne $s) {
            Fail("Puzzle '$puzzleId' has given '$p' that mismatches solution '$s' at index $i.")
        }
    }

    $computedGiven = ($puzzle.ToCharArray() | Where-Object { Test-SymbolChar $_ $symbols }).Count
    if ($computedGiven -ne $givenCount) {
        Fail("Puzzle '$puzzleId' has mismatched given_count. expected=$computedGiven got=$givenCount")
    }

    Assert-SudokuSolution -puzzleId $puzzleId -solution $solution -spec $spec

    if (-not [string]::IsNullOrWhiteSpace($mode)) {
        $allowedModes = @("Beginner", "Easy", "Medium", "Hard", "Expert", "Extreme", "Six", "Sixteen", "Unknown")
        if ($allowedModes -notcontains $mode) {
            Fail("Puzzle '$puzzleId' has unsupported mode '$mode'.")
        }
    }

    if (-not $solverDetails.ContainsKey($puzzleId)) {
        Fail("Missing solver_details for puzzle '$puzzleId'.")
    }

    $detail = $solverDetails[$puzzleId]
    if ($null -eq $detail.weighted_se) {
        Fail("solver_details '$puzzleId' missing weighted_se.")
    }
    if ($null -eq $detail.max_rate) {
        Fail("solver_details '$puzzleId' missing max_rate.")
    }
    if ($null -eq $detail.advanced_hits) {
        Fail("solver_details '$puzzleId' missing advanced_hits.")
    }

    if (-not $timeMap.ContainsKey($puzzleId)) {
        Fail("Missing time_map for puzzle '$puzzleId'.")
    }

    $buckets = @($timeMap[$puzzleId])
    if ($buckets.Count -ne 4) {
        Fail("time_map '$puzzleId' must have 4 entries.")
    }

    foreach ($bucket in $buckets) {
        if ([int]$bucket -le 0) {
            Fail("time_map '$puzzleId' contains non-positive value '$bucket'.")
        }
    }

    if (-not $tierCounts.ContainsKey($tier)) {
        $tierCounts[$tier] = 0
    }

    $tierCounts[$tier] = [int]$tierCounts[$tier] + 1
}

foreach ($id in $solverDetails.Keys) {
    if (-not $seenIds.Contains([string]$id)) {
        Fail("solver_details contains unknown puzzle_id '$id'.")
    }
}

foreach ($id in $timeMap.Keys) {
    if (-not $seenIds.Contains([string]$id)) {
        Fail("time_map contains unknown puzzle_id '$id'.")
    }
}

Write-Host "Dataset OK: $DatasetPath"
Write-Host "Schema: $($data.schema_version)"
Write-Host "Puzzles: $($questions.Count)"
Write-Host "Tier distribution:"
foreach ($key in ($tierCounts.Keys | Sort-Object)) {
    Write-Host "  - ${key}: $($tierCounts[$key])"
}
