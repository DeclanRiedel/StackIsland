# Runs Unity tests in batchmode and prints a short summary.
# Usage: pwsh Tools/run-tests.ps1 [EditMode|PlayMode|All] [testFilter]
param([string]$Platform = "All", [string]$Filter = "")

$unity = "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe"
$project = Split-Path -Parent $PSScriptRoot
$out = Join-Path $project "Logs"
New-Item -ItemType Directory -Force $out | Out-Null
$platforms = if ($Platform -eq "All") { @("EditMode", "PlayMode") } else { @($Platform) }
$failed = 0

foreach ($p in $platforms) {
    $xml = Join-Path $out "results-$p.xml"
    $log = Join-Path $out "tests-$p.log"
    if (Test-Path $xml) { Remove-Item $xml }
    $args = @('-batchmode', '-projectPath', $project, '-runTests', '-testPlatform', $p, '-testResults', $xml, '-logFile', $log)
    if ($Filter) { $args += @('-testFilter', $Filter) }
    $proc = Start-Process -FilePath $unity -ArgumentList $args -Wait -PassThru
    Select-String -Path $log -Pattern "error CS" | Select-Object -First 20 | ForEach-Object { $_.Line }
    if (-not (Test-Path $xml)) { Write-Host "[$p] no results (exit $($proc.ExitCode))"; $failed++; continue }
    [xml]$x = Get-Content $xml
    $r = $x.'test-run'
    Write-Host "[$p] total=$($r.total) passed=$($r.passed) failed=$($r.failed)"
    foreach ($tc in $x.SelectNodes("//test-case[@result='Failed']")) {
        $failed++
        Write-Host "  FAIL $($tc.fullname)"
        Write-Host "       $($tc.failure.message.'#cdata-section'.Trim())"
        $st = ($tc.failure.'stack-trace'.'#cdata-section' -split "`n" | Where-Object { $_ -match "StackIsland" } | Select-Object -First 2) -join "`n       "
        if ($st) { Write-Host "       $st" }
    }
    Select-String -Path $log -Pattern "\[Automation\]|\[Perf\]" | ForEach-Object { $_.Line }
}
exit $failed
