param(
    [string]$HistorySource = 'origin'
)

$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$ManifestPath = Join-Path $RepoRoot 'docs/lesson-manifest.json'

function Invoke-Git {
    param([string[]]$Arguments, [switch]$AllowFailure)
    $output = & git @Arguments 2>&1
    $code = $LASTEXITCODE
    if (-not $AllowFailure -and $code -ne 0) {
        throw "git $($Arguments -join ' ') failed with exit code $code`n$output"
    }
    return [pscustomobject]@{ ExitCode = $code; Output = ($output -join "`n").Trim() }
}

Push-Location $RepoRoot
try {
    if (-not (Test-Path -LiteralPath $ManifestPath)) {
        throw "Lesson manifest not found: $ManifestPath"
    }
    $manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
    if ($manifest.schemaVersion -ne 2) { throw "Expected lesson manifest schema 2, found $($manifest.schemaVersion)" }
    if ($manifest.lessons.Count -ne 176) { throw "Expected 176 lesson states, found $($manifest.lessons.Count)" }

    $firstCommit = [string]$manifest.lessons[0].exercise.gitRef
    $present = Invoke-Git -Arguments @('cat-file', '-e', "$firstCommit^{commit}") -AllowFailure
    if ($present.ExitCode -ne 0) {
        Write-Host "[lesson-history] Fetching lesson-history from $HistorySource"
        Invoke-Git -Arguments @('fetch', '--no-tags', $HistorySource, 'lesson-history') | Out-Null
    }

    $seen = [System.Collections.Generic.HashSet[string]]::new()
    $previous = $null
    foreach ($lesson in $manifest.lessons) {
        $commit = [string]$lesson.exercise.gitRef
        if ($commit -notmatch '^[a-f0-9]{40}$') { throw "Lesson $($lesson.id) has an invalid commit reference: $commit" }
        if (-not $seen.Add($commit)) { throw "Duplicate lesson commit reference: $commit" }
        Invoke-Git -Arguments @('cat-file', '-e', "$commit^{commit}") | Out-Null

        if ($previous) {
            $ancestor = Invoke-Git -Arguments @('merge-base', '--is-ancestor', $previous, $commit) -AllowFailure
            if ($ancestor.ExitCode -ne 0) { throw "Lesson history is not ordered between $previous and $commit" }
        }

        $stateText = (Invoke-Git -Arguments @('show', "$commit`:.lesson-state.json")).Output
        $state = $stateText | ConvertFrom-Json
        if ($state.sequence -ne $lesson.sequence -or $state.lessonId -ne $lesson.id -or $state.chapter -ne $lesson.chapter) {
            throw "Lesson-state metadata mismatch for $($lesson.id)"
        }
        if ($state.stateId -ne $lesson.exercise.stateId -or $state.exerciseType -ne $lesson.exercise.type) {
            throw "Exercise metadata mismatch for $($lesson.id)"
        }

        $tree = (Invoke-Git -Arguments @('rev-parse', "$commit^{tree}")).Output
        if ($tree -ne $lesson.exercise.treeHash) { throw "Tree hash mismatch for $($lesson.id)" }

        $stage = [string]$lesson.codeStage
        $stageDiff = Invoke-Git -Arguments @('diff', '--quiet', $stage, $commit, '--', '.', ':(exclude).lesson-state.json', ':(exclude)docs/orderflow-verified.zip') -AllowFailure
        if ($stageDiff.ExitCode -ne 0) { throw "Production tree for $($lesson.id) differs from verified $stage" }
        $previous = $commit
    }

    $historyCount = [int](Invoke-Git -Arguments @('rev-list', '--count', $previous)).Output
    if ($historyCount -ne 176) { throw "Expected 176 commits in lesson-history, found $historyCount" }
    Write-Host "[lesson-history] PASS — 176 ordered commits map exactly to eight verified production trees"
}
finally {
    Pop-Location
}
