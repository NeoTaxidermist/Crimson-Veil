# Measures a built game (Benchmark.cs, -cvbench) in a window: median frame time and a screenshot.
#   powershell -ExecutionPolicy Bypass -File Tools\bench.ps1 -ExeDir C:\Dev\CrimsonVeil-build -Tag base
#   ... -Res 960x540                        (another resolution: pass it here, NOT as -cvres in -Extra -
#                                            the game takes the first -cvres it finds)
#   ... -Extra "-cvoff","Dekorace"          (switch objects off to see what they cost)
#   ... -Extra "-cvoff",'"Global Volume"'   (a name with a space needs inner quotes)
param([string]$ExeDir = "C:\Dev\CrimsonVeil-build", [string]$Tag = "run", [string]$Res = "1920x1080", [string[]]$Extra = @())
$out = Join-Path $ExeDir "bench_$Tag.txt"
if (Test-Path $out) { Remove-Item $out }
$shot = Join-Path $ExeDir "shot_$Tag.png"
$a = @("-cvbench", $out, "-cvres", $Res, "-cvshot", $shot) + $Extra
$p = Start-Process -FilePath (Join-Path $ExeDir "CrimsonVeil.exe") -ArgumentList $a -PassThru
$p.WaitForExit(60000) | Out-Null
if (-not $p.HasExited) { $p.Kill() }
if (Test-Path $out) { Get-Content $out } else { "$($Tag): no result" }
