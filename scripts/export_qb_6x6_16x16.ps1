$ErrorActionPreference = "Stop"

$base = "artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/resources/assets/config"
$keyStr = "SKpvYaOKKIz+2dpO"
$key = [System.Text.Encoding]::UTF8.GetBytes($keyStr)
$iv = New-Object byte[] 16

function Decrypt-Bytes([byte[]]$bytes) {
    $aes = [System.Security.Cryptography.Aes]::Create()
    $aes.Mode = [System.Security.Cryptography.CipherMode]::CBC
    $aes.Padding = [System.Security.Cryptography.PaddingMode]::PKCS7
    $aes.Key = $key
    $aes.IV = $iv
    $dec = $aes.CreateDecryptor()
    try {
        $out = $dec.TransformFinalBlock($bytes, 0, $bytes.Length)
        return [System.Text.Encoding]::UTF8.GetString($out)
    }
    finally {
        $dec.Dispose()
        $aes.Dispose()
    }
}

# ---- 6x6 from defaultQb*.json (decrypted) ----
$set6 = [System.Collections.Generic.HashSet[string]]::new()
$sourceFiles6 = [System.Collections.Generic.HashSet[string]]::new()
Get-ChildItem -File "$base/defaultQb*.json" | ForEach-Object {
    if ($_.Name -eq "defaultQbSolverDetails.json" -or $_.Name -eq "defaultQbSolverDetails.decrypted.json") {
        return
    }

    $json = Decrypt-Bytes([System.IO.File]::ReadAllBytes($_.FullName))
    $obj = $json | ConvertFrom-Json -AsHashtable

    foreach ($v in $obj.Values) {
        if ($v -is [System.Collections.IEnumerable] -and -not ($v -is [string])) {
            foreach ($item in $v) {
                $s = [string]$item
                if ([string]::IsNullOrWhiteSpace($s)) {
                    continue
                }
                $q = ($s -split "_")[0]
                if ($q.Contains(";")) {
                    $q = ($q -split ";")[0]
                }
                if ($q.Length -eq 36) {
                    $null = $set6.Add($q)
                    $null = $sourceFiles6.Add($_.Name)
                }
            }
        }
    }
}

$entries6 = @($set6) | Sort-Object
$out6 = [ordered]@{
    schema_version  = 1
    board_size      = "6x6"
    extraction_note = "Extracted from decrypted defaultQb*.json assets (question strings only)."
    total_unique    = $entries6.Count
    sources         = @($sourceFiles6 | Sort-Object)
    entries         = $entries6
}
$out6Path = Join-Path $base "defaultQb_6x6.extracted.json"
$out6 | ConvertTo-Json -Depth 6 | Set-Content -Encoding UTF8 $out6Path
# ---- 16x16 from decompiled providers ----
$set16 = [System.Collections.Generic.HashSet[string]]::new()
$sourceRefs16 = @(
    "artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/lf/g.java",
    "artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/lf/j.java"
)
$regex = '"([A-Za-z]+)_\d+"'

foreach ($src in $sourceRefs16) {
    $txt = Get-Content $src -Raw
    foreach ($m in [regex]::Matches($txt, $regex)) {
        $q = $m.Groups[1].Value
        if ($q.Length -eq 256) {
            $null = $set16.Add($q)
        }
    }
}

$entries16 = @($set16) | Sort-Object
$out16 = [ordered]@{
    schema_version  = 1
    board_size      = "16x16"
    extraction_note = "Extracted from decompiled provider strings in lf/g.java and lf/j.java (question strings only)."
    total_unique    = $entries16.Count
    sources         = $sourceRefs16
    entries         = $entries16
}
$out16Path = Join-Path $base "defaultQb_16x16.extracted.json"
$out16 | ConvertTo-Json -Depth 6 | Set-Content -Encoding UTF8 $out16Path

if (Test-Path "$base/write_test.tmp") {
    Remove-Item "$base/write_test.tmp" -Force
}

Write-Output "WROTE: $out6Path"
Write-Output "WROTE: $out16Path"
Write-Output "COUNT6: $($entries6.Count)"
Write-Output "COUNT16: $($entries16.Count)"


