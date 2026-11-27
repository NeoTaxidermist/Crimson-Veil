# Keeps this computer from idle-sleeping (the screen may still turn off) for as long as it runs,
# or for -Hours hours. It tells Windows the system is needed (SetThreadExecutionState) - the way
# video players do; no settings are changed, no keys or mouse are simulated. A closed lid still sleeps.
#
#   Start (hidden, in the background):  powershell -File keep-awake.ps1 -Start [-Hours 8]
#   Stop:                               powershell -File keep-awake.ps1 -Stop
#   Status:                             powershell -File keep-awake.ps1 -Status
param([switch]$Start, [switch]$Stop, [switch]$Status, [double]$Hours = 0, [switch]$Run)

$pidFile = Join-Path $env:TEMP 'keep-awake.pid'

function Get-Running {
    if (-not (Test-Path $pidFile)) { return $null }
    $id = [int](Get-Content $pidFile -ErrorAction SilentlyContinue)
    $p = Get-Process -Id $id -ErrorAction SilentlyContinue
    if ($p -and $p.ProcessName -like 'powershell*') { return $p }
    return $null
}

if ($Stop) {
    $p = Get-Running
    if ($p) { Stop-Process -Id $p.Id -Force; Remove-Item $pidFile -ErrorAction SilentlyContinue; 'Stopped: the computer may sleep again.' }
    else { 'Not running.' }
    return
}

if ($Status) {
    $p = Get-Running
    if ($p) { "Running (PID $($p.Id), since $($p.StartTime)): the computer will not idle-sleep." } else { 'Not running.' }
    return
}

if ($Start) {
    if (Get-Running) { 'Already running.'; return }
    $argList = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-WindowStyle', 'Hidden', '-File', "`"$PSCommandPath`"", '-Run')
    if ($Hours -gt 0) { $argList += @('-Hours', $Hours) }
    $p = Start-Process powershell -ArgumentList $argList -WindowStyle Hidden -PassThru
    Set-Content -Path $pidFile -Value $p.Id
    if ($Hours -gt 0) { "Started (PID $($p.Id)) for $Hours h." } else { "Started (PID $($p.Id)) until stopped." }
    return
}

if ($Run) {
    Add-Type -Namespace KeepAwake -Name Power -MemberDefinition @'
[System.Runtime.InteropServices.DllImport("kernel32.dll")]
public static extern uint SetThreadExecutionState(uint esFlags);
'@
    $ES_CONTINUOUS = [uint32]'0x80000000'
    $ES_SYSTEM_REQUIRED = [uint32]'0x00000001'
    $until = if ($Hours -gt 0) { (Get-Date).AddHours($Hours) } else { [DateTime]::MaxValue }
    try {
        while ((Get-Date) -lt $until) {
            [void][KeepAwake.Power]::SetThreadExecutionState($ES_CONTINUOUS -bor $ES_SYSTEM_REQUIRED)
            Start-Sleep -Seconds 60
        }
    }
    finally {
        [void][KeepAwake.Power]::SetThreadExecutionState($ES_CONTINUOUS)
        Remove-Item $pidFile -ErrorAction SilentlyContinue
    }
    return
}

'Usage: keep-awake.ps1 -Start [-Hours 8] | -Stop | -Status'
