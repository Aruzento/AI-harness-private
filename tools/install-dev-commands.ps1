Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot =
    Split-Path `
        -Parent `
        $PSScriptRoot

$runnerPath =
    Join-Path `
        $PSScriptRoot `
        "dev.ps1"

if (-not (Test-Path -LiteralPath $runnerPath)) {
    throw "tools\dev.ps1 was not found."
}

$profilePath =
    $PROFILE.CurrentUserAllHosts

$profileDirectory =
    Split-Path `
        -Parent `
        $profilePath

if (-not (Test-Path -LiteralPath $profileDirectory)) {
    New-Item `
        -ItemType Directory `
        -Path $profileDirectory `
        -Force |
        Out-Null
}

if (-not (Test-Path -LiteralPath $profilePath)) {
    New-Item `
        -ItemType File `
        -Path $profilePath `
        -Force |
        Out-Null
}

$startMarker =
    "# >>> AI Harness dev commands >>>"

$endMarker =
    "# <<< AI Harness dev commands <<<"

$currentContent = ""

$loadedContent =
    Get-Content `
        -LiteralPath $profilePath `
        -Raw `
        -ErrorAction SilentlyContinue

if ($null -ne $loadedContent) {
    $currentContent =
        [string]$loadedContent
}

$escapedRunner =
    $runnerPath.Replace(
        "'",
        "''")

$block = @"
$startMarker

`$script:AIHarnessDevRunner = '$escapedRunner'

function build {
    & `$script:AIHarnessDevRunner build @args
}

function run {
    & `$script:AIHarnessDevRunner run @args
}

function push {
    & `$script:AIHarnessDevRunner push @args
}

function smoke {
    & `$script:AIHarnessDevRunner smoke @args
}

$endMarker
"@

$pattern =
    [regex]::Escape($startMarker) +
    ".*?" +
    [regex]::Escape($endMarker)

if ([regex]::IsMatch(
        $currentContent,
        $pattern,
        [System.Text.RegularExpressions.RegexOptions]::Singleline)) {

    $newContent =
        [regex]::Replace(
            $currentContent,
            $pattern,
            [System.Text.RegularExpressions.MatchEvaluator]{
                param($match)
                return $block
            },
            [System.Text.RegularExpressions.RegexOptions]::Singleline)
}
else {
    if ($currentContent.Length -gt 0) {
        $newContent =
            $currentContent.TrimEnd() +
            [Environment]::NewLine +
            [Environment]::NewLine +
            $block +
            [Environment]::NewLine
    }
    else {
        $newContent =
            $block +
            [Environment]::NewLine
    }
}

Set-Content `
    -LiteralPath $profilePath `
    -Value $newContent `
    -Encoding UTF8

Write-Host ""
Write-Host "AI Harness commands installed."
Write-Host ""
Write-Host "Repository:"
Write-Host $repoRoot
Write-Host ""
Write-Host "Runner:"
Write-Host $runnerPath
Write-Host ""
Write-Host "PowerShell profile:"
Write-Host $profilePath
Write-Host ""
Write-Host "Reload with:"
Write-Host '. $PROFILE.CurrentUserAllHosts'