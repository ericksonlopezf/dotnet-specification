param(
    [string]$Package = ""
)

$csv = Import-Csv 'C:\Users\erick\.gemini\antigravity-ide\brain\99e4a639-62e0-4dca-83e6-fe2c7eff93ae\scratch\stryker_artifacts\all_surviving_mutants.csv'
if ($Package) {
    $filtered = $csv | Where-Object { $_.Package -eq $Package }
    Write-Host "=== $Package Mutants ($($filtered.Count)) ==="
    $filtered | Group-Object File | ForEach-Object {
        Write-Host "  File: $($_.Name) ($($_.Count) mutants)"
        $_.Group | ForEach-Object {
            Write-Host "    Line $($_.Line): [$($_.Mutator)] ($($_.Status)) -> $($_.Replacement)"
        }
    }
} else {
    $csv | Group-Object Package | ForEach-Object {
        Write-Host "$($_.Name): $($_.Count)"
    }
}
