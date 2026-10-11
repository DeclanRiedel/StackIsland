# Regenerate data (optional), build the Windows player and run it in autoshot mode.
param([switch]$Seed, [switch]$NoBuild)
$root = Split-Path -Parent $PSScriptRoot
if ($Seed) { pwsh -File (Join-Path $PSScriptRoot "unity-batch.ps1") StackIsland.EditorTools.ProjectSetup.RunBatch | Out-Null }
if (-not $NoBuild) { pwsh -File (Join-Path $PSScriptRoot "unity-batch.ps1") StackIsland.EditorTools.BuildScript.BuildWindows }
$shots = Join-Path $root "Screenshots/build"
New-Item -ItemType Directory -Force $shots | Out-Null
Get-ChildItem $shots -Filter *.png | Remove-Item
$exe = Join-Path $root "Builds/Windows/StackIsland.exe"
$p = Start-Process -FilePath $exe -ArgumentList @('-autoshot', $shots, '-autoshot-full', '-screen-width', '1600', '-screen-height', '900', '-screen-fullscreen', '0', '-logFile', (Join-Path $root "Logs/player.log")) -PassThru
if (-not $p.WaitForExit(240000)) { $p.Kill(); Write-Host "player timed out" }
Get-ChildItem $shots -Filter *.png | ForEach-Object { $_.Name }
Select-String -Path (Join-Path $root "Logs/player.log") -Pattern "Exception|Error" | Select-Object -First 15 | ForEach-Object { $_.Line }
