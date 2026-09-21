param([string]$Id)
$json = Get-Content 'D:/DevData/ericksonlopez.dev/dotnet-specification/StrykerOutput/2026-09-21.19-15-43/reports/mutation-report.json' -Raw | ConvertFrom-Json
$file = $json.files.PSObject.Properties.Value
$m = $file.mutants | Where-Object { $_.id -eq $Id }
$m | Format-List *
$coveringTestIds = $m.coveredBy
Write-Host "Covered by test IDs: $($coveringTestIds -join ', ')"
foreach ($tId in $coveringTestIds) {
    $t = $json.testFiles.PSObject.Properties.Value.tests | Where-Object { $_.id -eq $tId }
    Write-Host "  Test: $($t.name)"
}
