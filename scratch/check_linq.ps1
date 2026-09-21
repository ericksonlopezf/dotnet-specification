$json = Get-Content 'D:/DevData/ericksonlopez.dev/dotnet-specification/StrykerOutput/2026-09-21.19-15-43/reports/mutation-report.json' -Raw | ConvertFrom-Json
Write-Host "Files: $($json.files.PSObject.Properties.Name -join ', ')"
$file = $json.files.PSObject.Properties.Value

$total = $file.mutants.Count
$killed = ($file.mutants | Where-Object { $_.status -eq 'Killed' }).Count
$survived = ($file.mutants | Where-Object { $_.status -eq 'Survived' }).Count
$ignored = ($file.mutants | Where-Object { $_.status -eq 'Ignored' }).Count
Write-Host "Total: $total, Killed: $killed, Survived: $survived, Ignored: $ignored"
$score = ($killed / ($killed + $survived)) * 100
Write-Host "Score: $score %"
