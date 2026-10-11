# Runs a static editor method in Unity batchmode. Usage: pwsh Tools/unity-batch.ps1 StackIsland.EditorTools.ProjectSetup.RunBatch
param([Parameter(Mandatory = $true)][string]$Method)
$unity = "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe"
$project = Split-Path -Parent $PSScriptRoot
$log = Join-Path $project "Logs/batch.log"
$proc = Start-Process -FilePath $unity -ArgumentList @('-batchmode', '-projectPath', $project, '-executeMethod', $Method, '-quit', '-logFile', $log) -Wait -PassThru
Select-String -Path $log -Pattern "error CS|\[Setup\]|\[Seeder\]|\[BalanceSim\]|\[Build\]|Exception" -Context 0, 12 | ForEach-Object { $_.Line; $_.Context.PostContext | Where-Object { $_ -match "^\s+(Region|Purchases|Median|  [0-9])|^\s*[0-9]+-|^\s+[0-9]" } }
exit $proc.ExitCode
