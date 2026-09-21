$json = Get-Content 'D:/DevData/ericksonlopez.dev/dotnet-specification/StrykerOutput/2026-09-21.19-15-43/reports/mutation-report.json' -Raw | ConvertFrom-Json
$file = $json.files.PSObject.Properties.Value
$file.mutants | Where-Object { $_.status -eq 'Survived' } | ForEach-Object {
    [PSCustomObject]@{
        Id = $_.id
        Line = $_.location.start.line
        Mutator = $_.mutatorName
        Description = $_.description
        Replacement = $_.replacement
    }
} | Format-List

