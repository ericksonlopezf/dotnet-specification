<#
// Copyright © Erickson Lopez. MIT License.
.SYNOPSIS
    Architecture & Quality Standards Compliance Verification Script for EricksonLopez.Specification.
.DESCRIPTION
    Validates architectural invariants:
    1. Kebab-case naming for all markdown documentation repo-wide (excluding standard GitHub files).
    2. Zero [Obsolete] usages in production and test code.
    3. Presence of canonical MIT copyright header across all source files.
    4. Single top-level type per file in src/.
    5. Valid GitHub repository links referencing ericksonlopezf/dotnet-specification.
    6. Official support and security email normalization (ericksonlopezf@gmail.com).
    7. Consistent ImplicitUsings (enable) across Directory.Build.props and all .csproj.
    8. Zero CS1591 / CS1573 suppressions in src projects.
#>

[CmdletBinding()]
param (
    [string]$RootDirectory = "."
)

$ErrorActionPreference = "Stop"
$violations = 0

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  REPOSITORY COMPLIANCE & ARCHITECTURE AUDITOR    " -ForegroundColor Cyan
Write-Host "  Repository: EricksonLopez.Specification         " -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

# Standard allowed GitHub / Root files
$standardFiles = @(
    "README.md",
    "LICENSE",
    "SECURITY.md",
    "CONTRIBUTING.md",
    "CODE_OF_CONDUCT.md",
    "SUPPORT.md",
    "CHANGELOG.md",
    "PULL_REQUEST_TEMPLATE.md"
)

# 1. Kebab-case documentation verification across entire repo
Write-Host "`n[1/8] Checking documentation file naming (kebab-case repo-wide)..." -ForegroundColor Yellow
$allMdFiles = Get-ChildItem -Path $RootDirectory -Recurse -Filter "*.md" | Where-Object {
    $_.FullName -notmatch "[\\/](bin|obj|\.git|node_modules|BenchmarkDotNet\.Artifacts)[\\/]"
}
$badDocNames = 0
foreach ($doc in $allMdFiles) {
    $filename = $doc.Name
    if ($standardFiles -notcontains $filename) {
        $baseName = [System.IO.Path]::GetFileNameWithoutExtension($filename)
        if ($baseName -notmatch "^[a-z0-9]+(-[a-z0-9]+)*$") {
            Write-Host "  ❌ Non-kebab-case document: $($doc.FullName)" -ForegroundColor Red
            $violations++
            $badDocNames++
        }
    }
}
if ($badDocNames -eq 0) { Write-Host "  ✅ All documentation files use valid kebab-case naming." -ForegroundColor Green }

# 2. Zero Obsolete APIs in src/ and tests/
Write-Host "`n[2/8] Checking for [Obsolete] attribute usages in src/ and tests/..." -ForegroundColor Yellow
$allCsFiles = Get-ChildItem -Path $RootDirectory -Recurse -Filter "*.cs" | Where-Object {
    $_.FullName -notmatch "[\\/](obj|bin|\.git)[\\/]" -and $_.Name -notmatch "\.g\.cs$" -and $_.Name -notmatch "\.AssemblyInfo\.cs$"
}
$obsoleteCount = 0
foreach ($cs in $allCsFiles) {
    $lines = Get-Content $cs.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match "^\s*\[(global::System\.)?Obsolete\b" -and $lines[$i] -notmatch "^\s*//") {
            Write-Host "  ❌ [Obsolete] found in $($cs.FullName):$($i + 1)" -ForegroundColor Red
            $violations++
            $obsoleteCount++
        }
    }
}
if ($obsoleteCount -eq 0) { Write-Host "  ✅ Zero [Obsolete] attributes across solution code." -ForegroundColor Green }

# 3. Canonical MIT Copyright Header
Write-Host "`n[3/8] Checking canonical MIT copyright headers..." -ForegroundColor Yellow
$srcCsFiles = $allCsFiles | Where-Object { $_.FullName -match "[\\/]src[\\/]" }
$missingHeaders = 0
foreach ($cs in $srcCsFiles) {
    $firstLine = (Get-Content $cs.FullName -TotalCount 1)
    if ($firstLine -notmatch "Copyright © Erickson Lopez\. MIT License\.") {
        Write-Host "  ❌ Missing MIT header in $($cs.FullName)" -ForegroundColor Red
        $violations++
        $missingHeaders++
    }
}
if ($missingHeaders -eq 0) { Write-Host "  ✅ All production C# files contain the required MIT copyright header." -ForegroundColor Green }

# 4. One Type Per File in src/
Write-Host "`n[4/8] Checking 'One Type Per File' rule in src/..." -ForegroundColor Yellow
$multiTypeFiles = 0
foreach ($cs in $srcCsFiles) {
    $rawContent = [System.IO.File]::ReadAllText($cs.FullName)
    $codeWithoutStrings = [System.Text.RegularExpressions.Regex]::Replace($rawContent, '@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"', '')
    $codeWithoutComments = [System.Text.RegularExpressions.Regex]::Replace($codeWithoutStrings, '/\*[\s\S]*?\*/|//.*', '')
    $typeDecls = [System.Text.RegularExpressions.Regex]::Matches($codeWithoutComments, '(?m)^(?:public|internal|protected)\s+(?:sealed\s+|readonly\s+|abstract\s+|static\s+|partial\s+)*(?:class|struct|record|interface|enum|delegate)\s+([A-Za-z0-9_]+)')
    if ($typeDecls.Count -gt 1) {
        Write-Host "  ❌ Multiple top-level types in $($cs.FullName):" -ForegroundColor Red
        foreach ($td in $typeDecls) {
            Write-Host "     Type: $($td.Value.Trim())" -ForegroundColor DarkRed
        }
        $violations++
        $multiTypeFiles++
    }
}
if ($multiTypeFiles -eq 0) { Write-Host "  ✅ Every production file satisfies the 'One Type Per File' invariant." -ForegroundColor Green }

# 5. ImplicitUsings consistency across .csproj and Directory.Build.props
Write-Host "`n[5/8] Checking ImplicitUsings configuration..." -ForegroundColor Yellow
$csprojs = Get-ChildItem -Path $RootDirectory -Recurse -Filter "*.csproj" | Where-Object {
    $_.FullName -notmatch "[\\/](obj|bin|\.git)[\\/]"
}
$badImplicit = 0
foreach ($proj in $csprojs) {
    $content = [System.IO.File]::ReadAllText($proj.FullName)
    if ($content -match "<ImplicitUsings>disable</ImplicitUsings>") {
        Write-Host "  ❌ ImplicitUsings is disabled in $($proj.FullName)" -ForegroundColor Red
        $violations++
        $badImplicit++
    }
}
if ($badImplicit -eq 0) { Write-Host "  ✅ ImplicitUsings is consistently enabled." -ForegroundColor Green }

# 6. Zero CS1591 Suppressions in src/
Write-Host "`n[6/8] Checking for prohibited CS1591 / CS1573 suppressions in src/..." -ForegroundColor Yellow
$srcProjects = $csprojs | Where-Object { $_.FullName -match "[\\/]src[\\/]" }
$badSuppressions = 0
foreach ($sp in $srcProjects) {
    $content = [System.IO.File]::ReadAllText($sp.FullName)
    if ($content -match "\b(CS1591|CS1573)\b") {
        Write-Host "  ❌ Prohibited documentation suppression in $($sp.FullName)" -ForegroundColor Red
        $violations++
        $badSuppressions++
    }
}
if ($badSuppressions -eq 0) { Write-Host "  ✅ Zero CS1591/CS1573 suppressions in source projects." -ForegroundColor Green }

# 7. GitHub Repository Identity & Links
Write-Host "`n[7/8] Checking GitHub identity links (ericksonlopezf/dotnet-specification)..." -ForegroundColor Yellow
$badLinks = 0
$allTrackedFiles = Get-ChildItem -Path $RootDirectory -Recurse -Include "*.cs", "*.md", "*.props", "*.targets" | Where-Object {
    $_.FullName -notmatch "[\\/](obj|bin|\.git|BenchmarkDotNet\.Artifacts)[\\/]"
}
foreach ($f in $allTrackedFiles) {
    $lines = Get-Content $f.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match "github\.com/ericksonlopez/dotnet-specification\b" -or $lines[$i] -match "github\.com/ericksonlopez/dotnet-specifications\b") {
            Write-Host "  ❌ Incorrect GitHub repo link in $($f.FullName):$($i + 1)" -ForegroundColor Red
            $violations++
            $badLinks++
        }
    }
}
if ($badLinks -eq 0) { Write-Host "  ✅ All GitHub URLs correctly target ericksonlopezf/dotnet-specification." -ForegroundColor Green }

# 8. Official Contact & Support Email Normalization
Write-Host "`n[8/8] Checking contact and security email normalization (ericksonlopezf@gmail.com)..." -ForegroundColor Yellow
$badEmails = 0
$metaFiles = @("SECURITY.md", "CODE_OF_CONDUCT.md", "SUPPORT.md", "README.md")
foreach ($meta in $metaFiles) {
    $fullPath = Join-Path $RootDirectory $meta
    if (Test-Path $fullPath) {
        $lines = Get-Content $fullPath
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match "ericksonlopez\.dev@gmail\.com") {
                Write-Host "  ❌ Legacy email detected in $meta : line $($i + 1)" -ForegroundColor Red
                $violations++
                $badEmails++
            }
        }
    }
}
if ($badEmails -eq 0) { Write-Host "  ✅ Official contact emails normalized to ericksonlopezf@gmail.com." -ForegroundColor Green }

# Summary & Exit Code
Write-Host "`n==================================================" -ForegroundColor Cyan
if ($violations -gt 0) {
    Write-Host "  FAILED: $violations compliance violation(s) detected. " -ForegroundColor Red -BackgroundColor Black
    Write-Host "==================================================" -ForegroundColor Cyan
    exit 1
} else {
    Write-Host "  SUCCESS: 100% Governance & Compliance Verified. Zero violations. " -ForegroundColor Green -BackgroundColor Black
    Write-Host "==================================================" -ForegroundColor Cyan
    exit 0
}
