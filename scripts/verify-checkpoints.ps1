[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$temporaryBase = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase ("orderflow-checkpoints-" + [guid]::NewGuid().ToString('N'))

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory)]
        [string] $Executable,

        [Parameter(Mandatory)]
        [string[]] $Arguments
    )

    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed ($LASTEXITCODE): $Executable $($Arguments -join ' ')"
    }
}

function Export-Checkpoint {
    param(
        [Parameter(Mandatory)]
        [string] $Tag
    )

    $archivePath = Join-Path $temporaryRoot "$Tag.zip"
    $checkpointPath = Join-Path $temporaryRoot $Tag

    Invoke-CheckedCommand git @('-C', $repositoryRoot, 'archive', '--format=zip', '--output', $archivePath, $Tag)
    Expand-Archive -LiteralPath $archivePath -DestinationPath $checkpointPath
    return $checkpointPath
}

function Invoke-Checkpoint {
    param(
        [Parameter(Mandatory)]
        [string] $Tag
    )

    Write-Host "`n=== Verifying exact Git checkpoint $Tag ===" -ForegroundColor Cyan
    $checkpointPath = Export-Checkpoint $Tag
    Push-Location $checkpointPath

    try {
        switch ($Tag) {
            'stage-01' {
                Invoke-CheckedCommand dotnet @('run', '--project', 'tutorial-snapshots/01-console', '--configuration', 'Release')
            }
            'stage-02' {
                Invoke-CheckedCommand dotnet @('run', '--project', 'tutorial-snapshots/02-domain-linq', '--configuration', 'Release')
            }
            'stage-03' {
                Invoke-CheckedCommand dotnet @('run', '--project', 'tutorial-snapshots/03-async-service', '--configuration', 'Release')
            }
            'stage-04' {
                Invoke-CheckedCommand dotnet @('tool', 'restore')
                Invoke-CheckedCommand dotnet @('restore', 'src/OrderFlow.Api/OrderFlow.Api.csproj')
                Invoke-CheckedCommand dotnet @('build', 'src/OrderFlow.Api/OrderFlow.Api.csproj', '--configuration', 'Release', '--no-restore', '-warnaserror')

                $databasePath = Join-Path $checkpointPath 'stage-04.db'
                $previousConnection = $env:ConnectionStrings__OrderDb
                try {
                    $env:ConnectionStrings__OrderDb = "Data Source=$databasePath"
                    Invoke-CheckedCommand dotnet @(
                        'ef', 'database', 'update',
                        '--project', 'src/OrderFlow.Infrastructure',
                        '--startup-project', 'src/OrderFlow.Api',
                        '--no-build', '--configuration', 'Release'
                    )
                }
                finally {
                    $env:ConnectionStrings__OrderDb = $previousConnection
                }
            }
            { $_ -in @('stage-05', 'stage-06', 'stage-07') } {
                Invoke-CheckedCommand dotnet @('test', 'OrderFlow.sln', '--configuration', 'Release', '-warnaserror')
                if ($Tag -eq 'stage-06' -and -not (Test-Path -LiteralPath 'docs/architecture.md')) {
                    throw 'stage-06 is missing docs/architecture.md'
                }
            }
            'stage-08' {
                & ./scripts/verify-all.ps1
                if ($LASTEXITCODE -ne 0) {
                    throw "stage-08 verifier failed with exit code $LASTEXITCODE"
                }
                # Git archives intentionally exclude the downloadable ZIP so
                # that a release archive never contains itself recursively.
                Invoke-CheckedCommand node @('./scripts/verify-site.mjs', '--package')
            }
            default {
                throw "Unknown checkpoint: $Tag"
            }
        }

        Write-Host "$Tag PASS" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
}

New-Item -ItemType Directory -Path $temporaryRoot | Out-Null

try {
    foreach ($tag in 1..8 | ForEach-Object { 'stage-{0:D2}' -f $_ }) {
        Invoke-Checkpoint $tag
    }

    Write-Host "`nALL 8 GIT CHECKPOINTS PASS" -ForegroundColor Green
}
finally {
    $resolvedTemporaryRoot = [System.IO.Path]::GetFullPath($temporaryRoot)
    $temporaryLeaf = Split-Path -Leaf $resolvedTemporaryRoot
    $isSafeTemporaryPath = $resolvedTemporaryRoot.StartsWith(
        $temporaryBase,
        [System.StringComparison]::OrdinalIgnoreCase
    ) -and $temporaryLeaf.StartsWith('orderflow-checkpoints-', [System.StringComparison]::Ordinal)

    if ($isSafeTemporaryPath -and (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}
