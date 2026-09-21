# Copyright © Erickson Lopez. MIT License.
<#
.SYNOPSIS
    Executes targeted Stryker.NET mutation testing on projects affected by current git changes.
.DESCRIPTION
    Maps changed files from git diff (against target branch, default 'origin/main') to their
    corresponding source projects and Stryker configurations. Executes mutation testing exclusively
    on affected components to prevent CI bottlenecks during Pull Requests while upholding quality gates.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$TargetBranch = "origin/main",

    [Parameter(Mandatory = $false)]
    [string]$MutationLevel = "Standard"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Resolve-Path (Join-Path $ScriptDir "..")

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  TARGETED STRYKER.NET MUTATION RUNNER (PR MODE)  " -ForegroundColor Cyan
Write-Host "  Target Branch: $TargetBranch                    " -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

# Project-to-config mapping
$ProjectMap = @{
    "src/EricksonLopez.Specification"                   = "stryker-config.json"
    "src/EricksonLopez.Specification.Abstractions"      = "stryker-abstractions-config.json"
    "src/EricksonLopez.Specification.Analyzers"         = "stryker-analyzers-config.json"
    "src/EricksonLopez.Specification.Dapper"            = "stryker-dapper-config.json"
    "src/EricksonLopez.Specification.DapperExtensions"  = "stryker-dapperextensions-config.json"
    "src/EricksonLopez.Specification.EntityFrameworkCore" = "stryker-efcore-config.json"
    "src/EricksonLopez.Specification.Generators"        = "stryker-generators-config.json"
    "src/EricksonLopez.Specification.Linq"              = "stryker-linq-config.json"
    "src/EricksonLopez.Specification.MariaDb"           = "stryker-mariadb-config.json"
    "src/EricksonLopez.Specification.MongoDB"           = "stryker-mongodb-config.json"
    "src/EricksonLopez.Specification.MsSql"             = "stryker-mssql-config.json"
    "src/EricksonLopez.Specification.MySql"             = "stryker-mysql-config.json"
    "src/EricksonLopez.Specification.Oracle"            = "stryker-oracle-config.json"
    "src/EricksonLopez.Specification.PostgreSql"        = "stryker-postgresql-config.json"
    "src/EricksonLopez.Specification.Result"            = "stryker-result-config.json"
    "src/EricksonLopez.Specification.Sql"               = "stryker-sql-config.json"
    "src/EricksonLopez.Specification.Sqlite"            = "stryker-sqlite-config.json"
}

# Also map test projects to corresponding source configs
$TestToConfigMap = @{
    "tests/EricksonLopez.Specification.Tests"                   = "stryker-config.json"
    "tests/EricksonLopez.Specification.Analyzers.Tests"         = "stryker-analyzers-config.json"
    "tests/EricksonLopez.Specification.Dapper.Tests"            = "stryker-dapper-config.json"
    "tests/EricksonLopez.Specification.DapperExtensions.Tests"  = "stryker-dapperextensions-config.json"
    "tests/EricksonLopez.Specification.EntityFrameworkCore.Tests" = "stryker-efcore-config.json"
    "tests/EricksonLopez.Specification.Generators.Tests"        = "stryker-generators-config.json"
    "tests/EricksonLopez.Specification.MariaDb.Tests"           = "stryker-mariadb-config.json"
    "tests/EricksonLopez.Specification.MongoDB.Tests"           = "stryker-mongodb-config.json"
    "tests/EricksonLopez.Specification.MsSql.Tests"             = "stryker-mssql-config.json"
    "tests/EricksonLopez.Specification.MySql.Tests"             = "stryker-mysql-config.json"
    "tests/EricksonLopez.Specification.Oracle.Tests"            = "stryker-oracle-config.json"
    "tests/EricksonLopez.Specification.PostgreSql.Tests"        = "stryker-postgresql-config.json"
    "tests/EricksonLopez.Specification.Sql.Tests"               = "stryker-sql-config.json"
    "tests/EricksonLopez.Specification.Sqlite.Tests"            = "stryker-sqlite-config.json"
}

# Determine changed files safely without triggering PowerShell NativeCommandError on stderr
$prevEAP = $ErrorActionPreference
$ErrorActionPreference = "SilentlyContinue"

$hasBranch = $false
& git rev-parse --verify "$TargetBranch" 2>$null
if ($LASTEXITCODE -eq 0) {
    $hasBranch = $true
}

$rawDiff = @()
if ($hasBranch) {
    $rawDiff = & git diff --name-only "$TargetBranch...HEAD" 2>$null
}
if (-not $rawDiff -or $rawDiff.Count -eq 0) {
    $rawDiff = & git diff --name-only "HEAD" 2>$null
}
if (-not $rawDiff -or $rawDiff.Count -eq 0) {
    $statusLines = & git status --porcelain 2>$null
    if ($statusLines) {
        $rawDiff = $statusLines | ForEach-Object {
            if ($_.Length -ge 3) { $_.Substring(3).Trim() }
        }
    }
}
$ErrorActionPreference = $prevEAP

$changedFiles = $rawDiff | Where-Object { [string]::IsNullOrWhiteSpace($_) -eq $false -and $_ -notmatch '^\s*fatal:' }

Write-Host "Detected $($changedFiles.Count) changed file(s):" -ForegroundColor Yellow
$changedFiles | ForEach-Object { Write-Host "  • $_" -ForegroundColor DarkGray }

$affectedConfigs = [System.Collections.Generic.HashSet[string]]::new()

foreach ($file in $changedFiles) {
    $normalized = $file.Replace("\", "/")
    
    foreach ($sourceKey in $ProjectMap.Keys) {
        if ($normalized.StartsWith($sourceKey)) {
            $config = $ProjectMap[$sourceKey]
            [void]$affectedConfigs.Add($config)
            Write-Host "  -> Affected component: $sourceKey ($config)" -ForegroundColor Green
        }
    }
    
    foreach ($testKey in $TestToConfigMap.Keys) {
        if ($normalized.StartsWith($testKey)) {
            if ($testKey -eq "tests/EricksonLopez.Specification.Tests") {
                if ($normalized -match "Result") {
                    [void]$affectedConfigs.Add("stryker-result-config.json")
                    Write-Host "  -> Affected test suite: $normalized (stryker-result-config.json)" -ForegroundColor Green
                }
                if ($normalized -match "Linq") {
                    [void]$affectedConfigs.Add("stryker-linq-config.json")
                    Write-Host "  -> Affected test suite: $normalized (stryker-linq-config.json)" -ForegroundColor Green
                }
                if ($normalized -match "Abstractions|Contract") {
                    [void]$affectedConfigs.Add("stryker-abstractions-config.json")
                    Write-Host "  -> Affected test suite: $normalized (stryker-abstractions-config.json)" -ForegroundColor Green
                }
                [void]$affectedConfigs.Add("stryker-config.json")
                Write-Host "  -> Affected test suite: $testKey (stryker-config.json)" -ForegroundColor Green
            } else {
                $config = $TestToConfigMap[$testKey]
                [void]$affectedConfigs.Add($config)
                Write-Host "  -> Affected test suite: $testKey ($config)" -ForegroundColor Green
            }
        }
    }
}

if ($affectedConfigs.Count -eq 0) {
    Write-Host "`n✅ No production C# source or test files changed in this changeset." -ForegroundColor Green
    Write-Host "Targeted mutation testing skipped. Zero CI cycle penalty." -ForegroundColor Green
    exit 0
}

Write-Host "`nExecuting Stryker for $($affectedConfigs.Count) affected package(s):" -ForegroundColor Cyan
$failedCount = 0

foreach ($config in $affectedConfigs) {
    $configPath = Join-Path $RepoRoot $config
    if (-not (Test-Path $configPath)) {
        Write-Host "⚠️ Warning: Config file $config not found, skipping." -ForegroundColor Yellow
        continue
    }

    Write-Host "`n--------------------------------------------------" -ForegroundColor Cyan
    Write-Host "Running Stryker with config: $config (Level: $MutationLevel)" -ForegroundColor Cyan
    Write-Host "--------------------------------------------------" -ForegroundColor Cyan

    & dotnet stryker --config-file $config --mutation-level $MutationLevel
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Stryker mutation test failed for $config (Exit code: $LASTEXITCODE)" -ForegroundColor Red
        $failedCount++
    } else {
        Write-Host "✅ Stryker passed for $config" -ForegroundColor Green
    }
}

if ($failedCount -gt 0) {
    Write-Host "`n❌ Targeted mutation testing failed for $failedCount package(s)." -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ All targeted mutation tests passed successfully." -ForegroundColor Green
exit 0
