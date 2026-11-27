# Stop hook for Claude Code (.claude/settings.json).
# Before Claude ends a reply: if there are commits not pushed, or Docs/Rozpracovano.md has
# uncommitted edits, send Claude back once to finish the hand-over (update, commit, push),
# so the other computer can continue at any moment. Runs at most once per stop (stop_hook_active).
$ErrorActionPreference = 'Continue'
$payload = [Console]::In.ReadToEnd()
try { $data = $payload | ConvertFrom-Json } catch { $data = $null }
if ($data -and $data.stop_hook_active) { exit 0 }

$repo = Split-Path -Parent $PSScriptRoot
Set-Location $repo
$env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User') + ';' + $env:Path

$branch = (git rev-parse --abbrev-ref HEAD 2>$null)
$problems = @()
$unpushed = @(git log --oneline "origin/$branch..HEAD" 2>$null)
if ($unpushed.Count -gt 0) { $problems += "$($unpushed.Count) commit(s) not pushed to origin/$branch" }
foreach ($f in 'Docs/Rozpracovano.md', 'Docs/Backlog.md') {
    if (git status --porcelain -- $f 2>$null) { $problems += "$f has uncommitted edits" }
}

if ($problems.Count -gt 0) {
    $reason = 'Hand-over not finished: ' + ($problems -join '; ') + '. Update your computer''s section in Docs/Rozpracovano.md (the "last session" heading: discussed, done, not done, in progress, decisions, next step), put anything discussed but postponed into Docs/Backlog.md, commit with a version per CLAUDE.md and push. If that cannot be done now, say why in your reply.'
    @{ decision = 'block'; reason = $reason } | ConvertTo-Json -Compress
}
exit 0
