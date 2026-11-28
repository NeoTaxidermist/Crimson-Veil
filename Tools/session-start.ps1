# SessionStart hook for Claude Code (.claude/settings.json).
# Pulls the latest work from the other computer and hands Claude the current hand-over state,
# so every session on either computer starts from what was pushed last.
# With -OnPrompt it runs as the UserPromptSubmit hook: on every user message it fetches and stays
# silent unless the other computer pushed something meanwhile - then it pulls and hands over again,
# so a session left open all day still picks up the other computer's work.
# Output: JSON with additionalContext (for Claude) and systemMessage (shown to the user).
param([switch]$OnPrompt)
$ErrorActionPreference = 'Continue'
$env:GIT_TERMINAL_PROMPT = '0'   # never hang on a credential prompt
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$repo = Split-Path -Parent $PSScriptRoot
Set-Location $repo

# The user wants the computer awake at all times (long Unity jobs, Remote Control from the phone),
# so make sure keep-awake runs - on every session start and every message, because it dies with a
# reboot or an app restart. Cheap: a PID-file check when it already runs. Output captured, not printed.
$awake = & (Join-Path $PSScriptRoot 'keep-awake.ps1') -Start
$awakeLine = if ("$awake" -like 'Started*') { "Keep-awake was not running, the hook started it: $awake" } else { '' }

# Git may be installed after the Claude app started, so reload PATH from the registry.
$env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User') + ';' + $env:Path

$lines = @()
if ($awakeLine) { $lines += $awakeLine }
$status = ''
$pulledOrFailed = $false
$branch = (git rev-parse --abbrev-ref HEAD 2>$null)
git fetch --quiet origin 2>$null
if ($LASTEXITCODE -ne 0) {
    $status = 'git fetch failed (offline or not signed in) - state may be stale'
    if ($OnPrompt) { exit 0 }   # don't nag on every message; SessionStart reports it
} else {
    $incoming = @(git log --oneline "HEAD..origin/$branch" 2>$null)
    $outgoing = @(git log --oneline "origin/$branch..HEAD" 2>$null)
    if ($incoming.Count -gt 0) {
        $pulledOrFailed = $true
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
# On a user message: say nothing unless the other computer's work just came in (or failed to).
if ($OnPrompt -and -not $pulledOrFailed) { exit 0 }
$dirty = @(git status --porcelain 2>$null)
if ($dirty.Count -gt 0) { $lines += "Uncommitted changes in working tree: $($dirty.Count) file(s)" }

# Hand-over: the whole Docs/Rozpracovano.md (who works where, last session on each computer, what is done/open).
# Claude Code shows hook context over ~10 KB only as a preview (the rest goes to a file), so the
# context is kept under $maxChars: the hand-over without its rules intro (CLAUDE.md has the rules)
# and the backlog as an index (sections, items, their state); the full backlog is read from the file.
$maxChars = 9500
$handover = '(Docs/Rozpracovano.md not found)'
$doc = Join-Path $repo 'Docs/Rozpracovano.md'
if (Test-Path $doc) {
    $text = Get-Content $doc -Raw -Encoding UTF8
    $cut = $text.IndexOf("`n## ")
    $handover = if ($cut -ge 0) { $text.Substring($cut + 1) } else { $text }
}
# Backlog index: "## section", "### item" and its "Stav:" line.
$backlog = '(Docs/Backlog.md not found)'
$bl = Join-Path $repo 'Docs/Backlog.md'
if (Test-Path $bl) {
    $backlog = (Get-Content $bl -Encoding UTF8 | Where-Object { $_ -match '^##' -or $_ -match '^\s*- \*\*Stav:\*\*' }) -join "`n"
}

$tag = if ($OnPrompt) { '[git-sync: the other computer pushed new work while this session was open]' } else { '[session-start]' }
$context = @(
    "$tag $status (branch $branch)."
    $lines
    ''
    'This is the hand-over from previous sessions on both computers (Docs/Rozpracovano.md). Continue from it; the user may have just switched computers. Keep your computer''s section under the "last session" heading current after every finished step, then commit and push.'
    '----- Docs/Rozpracovano.md -----'
    $handover
    ''
    'Backlog index (Docs/Backlog.md: everything discussed but not done yet). READ THE FULL FILE before planning or starting any item: it holds the reasons, impacts and links. Anything newly discussed and postponed goes there in the same reply (commit + push).'
    '----- Docs/Backlog.md (index) -----'
    $backlog
) -join "`n"
if ($context.Length -gt $maxChars) {
    $context = $context.Substring(0, $maxChars) + "`n[cut to stay under the hook size limit - read Docs/Rozpracovano.md and Docs/Backlog.md in full]"
}

@{
    systemMessage      = "Git: $status"
    hookSpecificOutput = @{ hookEventName = $(if ($OnPrompt) { 'UserPromptSubmit' } else { 'SessionStart' }); additionalContext = $context }
} | ConvertTo-Json -Depth 4 -Compress
