param(
    [Parameter(
        Mandatory = $true,
        Position = 0)]
    [ValidateSet(
        "build",
        "run",
        "push",
        "smoke")]
    [string]$Command,

    [Parameter(
        Position = 1,
        ValueFromRemainingArguments = $true)]
    [string[]]$Arguments
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($null -eq $Arguments) {
    $Arguments = @()
}

$RepoRoot =
    Split-Path `
        -Parent `
        $PSScriptRoot

$Solution =
    Join-Path `
        $RepoRoot `
        "MyAgent.sln"

$DesktopProject =
    Join-Path `
        $RepoRoot `
        "src\MyAgent.Desktop\MyAgent.Desktop.csproj"

$CoreProject =
    Join-Path `
        $RepoRoot `
        "src\MyAgent.Core\MyAgent.Core.csproj"

$SmokeRoot =
    Join-Path `
        $env:TEMP `
        "MyAgent.Smoke"


function Invoke-External {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [string[]]$ArgumentList = @()
    )

    & $FilePath @ArgumentList

    if ($LASTEXITCODE -ne 0) {
        $message =
            "{0} failed with exit code {1}" -f `
                $FilePath,
                $LASTEXITCODE

        throw $message
    }
}


function Invoke-Build {
    Write-Host ""
    Write-Host "=== BUILD ==="
    Write-Host ""

    Invoke-External `
        "dotnet" `
        @(
            "build",
            $Solution
        )
}


function Invoke-Run {
    Write-Host ""
    Write-Host "=== RUN DESKTOP ==="
    Write-Host ""

    Invoke-External `
        "dotnet" `
        @(
            "run",
            "--project",
            $DesktopProject
        )
}

function Invoke-Push {
    Write-Host ""
    Write-Host "=== CHECK ==="
    Write-Host ""

    Invoke-External `
        "git" `
        @(
            "diff",
            "--check"
        )

    Invoke-External `
        "git" `
        @(
            "diff",
            "--cached",
            "--check"
        )

    Invoke-Build

    $status =
        @(
            & git status --porcelain
        )

    if ($LASTEXITCODE -ne 0) {
        throw "git status failed."
    }

    if ($status.Count -eq 0) {
        Write-Host ""
        Write-Host "No local changes."
        Write-Host "Pushing current branch..."
        Write-Host ""

        Invoke-External `
            "git" `
            @(
                "push"
            )

        return
    }

    Write-Host ""
    Write-Host "=== CHANGES ==="
    Write-Host ""

    & git status --short

    if ($LASTEXITCODE -ne 0) {
        throw "git status failed."
    }

    Write-Host ""

    if ($Arguments.Count -gt 0) {
        $commitMessage =
            ($Arguments -join " ").Trim()
    }
    else {
        $commitMessage =
            (Read-Host "Commit message").Trim()
    }

    if ([string]::IsNullOrWhiteSpace($commitMessage)) {
        throw "Commit message cannot be empty."
    }

    Write-Host ""

    $prompt =
        "Stage ALL listed changes, commit and push? [y/N]"

    $confirmation =
        Read-Host $prompt

    if ($confirmation -ne "y" -and $confirmation -ne "Y") {
        Write-Host "Push cancelled."
        return
    }

    Write-Host ""
    Write-Host "=== STAGE ==="
    Write-Host ""

    Invoke-External `
        "git" `
        @(
            "add",
            "-A"
        )

    Invoke-External `
        "git" `
        @(
            "diff",
            "--cached",
            "--check"
        )

    Write-Host ""
    Write-Host "=== COMMIT ==="
    Write-Host ""

    Invoke-External `
        "git" `
        @(
            "commit",
            "-m",
            $commitMessage
        )

    Write-Host ""
    Write-Host "=== PUSH ==="
    Write-Host ""

    Invoke-External `
        "git" `
        @(
            "push"
        )
}


function Get-SmokeProject {
    if (-not (Test-Path -LiteralPath $SmokeRoot)) {
        throw "Smoke project does not exist. Run: smoke"
    }

    $project =
        Get-ChildItem `
            -LiteralPath $SmokeRoot `
            -Filter "*.csproj" |
        Select-Object -First 1

    if ($null -eq $project) {
        throw "Smoke .csproj was not found."
    }

    return $project.FullName
}


function Open-Smoke {
    if (-not (Test-Path -LiteralPath $SmokeRoot)) {
        throw "Smoke project does not exist. Run: smoke"
    }

    $explorerArgument =
        '"{0}"' -f $SmokeRoot

    Start-Process `
        "explorer.exe" `
        -ArgumentList $explorerArgument
}


function New-Smoke {
    Write-Host ""
    Write-Host "=== CREATE SMOKE ==="
    Write-Host ""

    Invoke-External `
        "dotnet" `
        @(
            "new",
            "console",
            "--framework",
            "net10.0",
            "--output",
            $SmokeRoot
        )

    $project =
        Get-SmokeProject

    $projectText =
        Get-Content `
            -LiteralPath $project `
            -Raw

    $oldTarget =
        "<TargetFramework>net10.0</TargetFramework>"

    $newTarget =
        "<TargetFramework>net10.0-windows</TargetFramework>"

    if (-not $projectText.Contains($oldTarget)) {
        throw "Could not change smoke project target framework."
    }

    $projectText =
        $projectText.Replace(
            $oldTarget,
            $newTarget)

    Set-Content `
        -LiteralPath $project `
        -Value $projectText `
        -Encoding UTF8

    Invoke-External `
        "dotnet" `
        @(
            "add",
            $project,
            "reference",
            $CoreProject
        )

    $program =
        Join-Path `
            $SmokeRoot `
            "Program.cs"

    $programContent = @'
Console.WriteLine(
    "Smoke project ready. Replace Program.cs with your smoke test.");
'@

    Set-Content `
        -LiteralPath $program `
        -Value $programContent `
        -Encoding UTF8

    Write-Host ""
    Write-Host "Smoke project:"
    Write-Host $SmokeRoot

    Write-Host ""
    Write-Host "Edit:"
    Write-Host $program

    Write-Host ""
    Write-Host "Then run:"
    Write-Host "smoke run"

    Open-Smoke
}


function Invoke-Smoke {
    if ($Arguments.Count -gt 0) {
        $action =
            $Arguments[0].ToLowerInvariant()
    }
    else {
        $action =
            "default"
    }

    switch ($action) {
        "default" {
            if (Test-Path -LiteralPath $SmokeRoot) {
                Write-Host ""
                Write-Host "Existing smoke project:"
                Write-Host $SmokeRoot
                Write-Host ""
                Write-Host "Use 'smoke run' to execute it."
                Write-Host "Use 'smoke new' to recreate it."

                Open-Smoke
            }
            else {
                New-Smoke
            }
        }

        "open" {
            Open-Smoke
        }

        "run" {
            $project =
                Get-SmokeProject

            Write-Host ""
            Write-Host "=== RUN SMOKE ==="
            Write-Host ""

            Invoke-External `
                "dotnet" `
                @(
                    "run",
                    "--project",
                    $project
                )
        }

        "new" {
            if (Test-Path -LiteralPath $SmokeRoot) {
                $confirmation =
                    Read-Host "Delete current smoke project and recreate it? [y/N]"

                if ($confirmation -ne "y" -and $confirmation -ne "Y") {
                    Write-Host "Smoke recreation cancelled."
                    return
                }

                Remove-Item `
                    -LiteralPath $SmokeRoot `
                    -Recurse `
                    -Force
            }

            New-Smoke
        }

        "clean" {
            if (Test-Path -LiteralPath $SmokeRoot) {
                Remove-Item `
                    -LiteralPath $SmokeRoot `
                    -Recurse `
                    -Force

                Write-Host "Smoke project removed."
            }
            else {
                Write-Host "Smoke project does not exist."
            }
        }

        default {
            throw "Unknown smoke command. Use: smoke, smoke run, smoke open, smoke new, smoke clean."
        }
    }
}


Push-Location $RepoRoot

try {
    switch ($Command) {
        "build" {
            Invoke-Build
        }

        "run" {
            Invoke-Run
        }

        "push" {
            Invoke-Push
        }

        "smoke" {
            Invoke-Smoke
        }
    }
}
finally {
    Pop-Location
}