$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
$zip=Join-Path $root 'dist/LightTable-Windows-Portable-x64.zip'
$target=[IO.Path]::GetFullPath((Join-Path $root ('TestResults/portable-'+(Get-Date -Format yyyyMMdd-HHmmss)+'-'+[Guid]::NewGuid().ToString('N').Substring(0,6))))
$prefix=[IO.Path]::GetFullPath((Join-Path $root 'TestResults')).TrimEnd('\')+'\'
if(!$target.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Portable test escaped workspace TestResults'}
if(Test-Path -LiteralPath $target){throw 'Portable test folder must be fresh'}
$zipHash=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
$entry=Get-Content -LiteralPath (Join-Path $root 'dist/SHA256SUMS-Windows.txt') | Where-Object {$_ -match '  LightTable-Windows-Portable-x64.zip$'}
if(!$entry -or !$entry.StartsWith($zipHash+'  ')){throw 'Portable archive does not match local integrity manifest'}
Expand-Archive -LiteralPath $zip -DestinationPath $target
foreach($name in 'LightTable.exe','LightTable.pri','coreclr.dll'){if(!(Test-Path -LiteralPath (Join-Path $target $name))){throw "Portable runtime missing $name"}}
$data=Join-Path $target 'isolated-data'
$arguments='--data-root "{0}" --integration-fixtures "{1}"' -f $data,(Join-Path $root 'TestResults/fixtures')
$run=Start-Process -FilePath (Join-Path $target 'LightTable.exe') -ArgumentList $arguments -WindowStyle Hidden -PassThru
if(!$run.WaitForExit(60000)){$run.Kill();throw 'Portable integration exceeded 60 seconds'}
$reportPath=Join-Path $data 'logs/integration.json'
if(!(Test-Path -LiteralPath $reportPath)){throw "Portable application exited $($run.ExitCode) without integration report"}
$report=Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
if($run.ExitCode -ne 0 -or !$report.Passed){
    [pscustomobject]@{Date=[DateTimeOffset]::Now;Passed=$false;ExitCode=$run.ExitCode;ReportedChecksPassed=$report.Passed;ArchiveSha256=$zipHash;Report=$reportPath} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'docs/evidence/portable-current-failure.json') -Encoding utf8
    throw "Portable application failed: process exit $($run.ExitCode), reported checks passed=$($report.Passed)"
}
Copy-Item -LiteralPath $reportPath -Destination (Join-Path $root 'docs/evidence/portable-integration.json')
[pscustomobject]@{Date=[DateTimeOffset]::Now;ArchiveSha256=$zipHash;ExitCode=$run.ExitCode;Checks=$report.Results.Count;Passed=$report.Passed;LocalRuntime=$true;ApplicationPri=$true;Environment='Current development host, not clean Windows';Report=$reportPath} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'docs/evidence/portable.json') -Encoding utf8
Write-Output "Portable integration passed: $($report.Results.Count) checks, exit $($run.ExitCode)"
