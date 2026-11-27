# SessionStart hook for Claude Code (.claude/settings.json).
# Pulls the latest work from the other computer and hands Claude the current hand-over state,
# so every session on either computer starts from what was pushed last.
# Output: JSON with additionalContext (for Claude) and systemMessage (shown to the user).
$ErrorActionPreference = 'Continue'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$repo = Split-Path -Parent $PSScriptRoot
Set-Location $repo

# Git may be installed after the Claude app started, so reload PATH from the registry.
$env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User') + ';' + $env:Path

$lines = @()
$status = ''
$branch = (git rev-parse --abbrev-ref HEAD 2>$null)
git fetch --quiet origin 2>$null
if ($LASTEXITCODE -ne 0) {
    $status = 'git fetch failed (offline or not signed in) - state may be stale'
} else {
    $incoming = @(git log --oneline "HEAD..origin/$branch" 2>$null)
    $outgoing = @(git log --oneline "origin/$branch..HEAD" 2>$null)
    if ($incoming.Count -gt 0) {
        $before = (git rev-parse --short HEAD)
        git pull --ff-only --quiet origin $branch 2>$null
        if ($LASTEXITCODE -eq 0) {
            $status = "Pulled $($incoming.Count) new commit(s) from the other computer ($before -> $(git rev-parse --short HEAD))"
            $lines += 'Incoming commits:'
            $lines += $incoming | ForEach-Object { "  $_" }
            $changed = @(git diff --name-only $before HEAD)
            if ($changed | Where-Object { $_ -like 'Assets/*' -or $_ -like 'Packages/*' -or $_ -like 'ProjectSettings/*' }) {
                $lines += 'Unity files changed: check EditorApplication.isPlaying before refresh_unity.'
            }
        } else {
            $status = "$($incoming.Count) new commit(s) on origin/$branch but git pull --ff-only FAILED (local changes or diverged) - resolve before working"
            $lines += $incoming | ForEach-Object { "  $_" }
        }
    } else {
        $status = "Up to date with origin/$branch"
    }
    if ($outgoing.Count -gt 0) { $lines += "Not pushed yet: $($outgoing.Count) local commit(s)" }
}
$dirty = @(git status --porcelain 2>$null)
if ($dirty.Count -gt 0) { $lines += "Uncommitted changes in working tree: $($dirty.Count) file(s)" }

# Hand-over: the whole Docs/Rozpracovano.md (who works where, last session on each computer, what is done/open).
$handover = '(Docs/Rozpracovano.md not found)'
$doc = Join-Path $repo 'Docs/Rozpracovano.md'
if (Test-Path $doc) { $handover = Get-Content $doc -Raw -Encoding UTF8 }

$context = @(
    "[session-start] $status (branch $branch)."
    $lines
    ''
    'This is the hand-over from previous sessions on both computers (Docs/Rozpracovano.md). Continue from it; the user may have just switched computers. Keep your computer''s section under the "last session" heading current after every finished step, then commit and push.'
    '----- Docs/Rozpracovano.md -----'
    $handover
) -join "`n"

@{
    systemMessage      = "Git: $status"
    hookSpecificOutput = @{ hookEventName = 'SessionStart'; additionalContext = $context }
} | ConvertTo-Json -Depth 4 -Compress
