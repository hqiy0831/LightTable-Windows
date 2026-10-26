param([string]$App='', [ValidateRange(30,1800)][int]$TimeoutSeconds=300)
$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
if(!$App){$App=Join-Path $root 'LightTable.Windows/App/bin/Release/net10.0-windows10.0.19041.0/win-x64/LightTable.exe'}
$photos=Join-Path $root 'TestResults/real-photos-nasa'
foreach($count in 10,100,500,1000){
    $data=Join-Path $root ("TestResults/benchmark-{0}-{1}" -f $count,(Get-Date -Format 'yyyyMMdd-HHmmss'))
    $arguments='--data-root "{0}" --benchmark-photos "{1}" --benchmark-count {2}' -f $data,$photos,$count
    $p=Start-Process -FilePath $App -ArgumentList $arguments -WindowStyle Hidden -PassThru
    Write-Output "Started real-photo benchmark count=$count pid=$($p.Id) data=$data"
    if(!$p.WaitForExit($TimeoutSeconds*1000)){$p.Kill();throw "Benchmark $count exceeded $TimeoutSeconds seconds"}
    $reportPath=Join-Path $data 'logs/benchmark.json'
    if(!(Test-Path -LiteralPath $reportPath)){throw "Benchmark $count exited $($p.ExitCode) without report"}
    $report=Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
    Write-Output ("count={0} imported={1} passed={2} importMs={3:N1} cpuDrawP95={4:N2}ms report={5}" -f $count,$report.Imported,$report.Passed,$report.Timings.ImportAndThumbnailsMs,$report.DrawCallbackCpuMs.P95,$reportPath)
    if($p.ExitCode -ne 0 -or !$report.Passed){throw "Benchmark $count failed: process exit $($p.ExitCode), report passed=$($report.Passed), failure=$($report.Failure)"}
    if(Test-Path -LiteralPath (Join-Path $data 'logs/crash.log')){throw "Benchmark $count produced crash.log"}
    [pscustomobject]@{ExitCode=$p.ExitCode;CrashLog=$false;AppDllSha256=(Get-FileHash -LiteralPath (Join-Path (Split-Path $App) 'LightTable.dll')).Hash;FusedLampSha256=(Get-FileHash -LiteralPath (Join-Path (Split-Path $App) 'Resources/FusedLamp.bin')).Hash} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $data 'logs/process.json')
}
