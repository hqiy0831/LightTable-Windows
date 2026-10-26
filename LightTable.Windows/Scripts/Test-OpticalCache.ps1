param([string]$App='',[ValidateRange(1,1000)][int]$Count=10,[ValidateRange(128,2200)][int]$Bucket=512,[ValidateRange(1,2200)][int]$RasterLimit=384)
$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
if(!$App){$App=Join-Path $root 'LightTable.Windows/App/bin/Release/net10.0-windows10.0.19041.0/win-x64/LightTable.exe'}
$data=[IO.Path]::GetFullPath((Join-Path $root ('TestResults/optical-process-'+(Get-Date -Format yyyyMMdd-HHmmss)+'-'+[Guid]::NewGuid().ToString('N').Substring(0,6))))
$prefix=[IO.Path]::GetFullPath((Join-Path $root 'TestResults')).TrimEnd('\')+'\'
if(!$data.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $data)){throw 'Optical test must use a fresh folder below workspace TestResults'}
New-Item -ItemType Directory -Path $data | Out-Null
$paths=@(Get-ChildItem -LiteralPath (Join-Path $root 'TestResults/real-photos-nasa') -Filter '*.jpg' -File | Sort-Object Name | Select-Object -First $Count | ForEach-Object FullName)
if($paths.Count -ne $Count){throw "Need $Count distinct real-photo fixtures"}
$request=Join-Path $data 'request.json'
function Invoke-Probe([bool]$hot){
    [ordered]@{Paths=$paths;Bucket=$Bucket;RasterLimit=$RasterLimit;RequireCached=$hot} | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $request -Encoding utf8
    $run=Start-Process -FilePath $App -ArgumentList ('--data-root "{0}" --optical-cache-probe "{1}"' -f $data,$request) -WindowStyle Hidden -PassThru
    Write-Output "Optical probe hot=$hot count=$Count pid=$($run.Id) data=$data" | Out-Host
    if(!$run.WaitForExit(120000)){$run.Kill();throw 'Optical probe exceeded 120 seconds'}
    $reportFile=Join-Path $data 'logs/optical-probe.json'
    if(!(Test-Path -LiteralPath $reportFile)){throw "Optical probe exited $($run.ExitCode) without report"}
    $report=Get-Content -LiteralPath $reportFile -Raw | ConvertFrom-Json
    Copy-Item -LiteralPath $reportFile -Destination (Join-Path $data ($(if($hot){'hot'}else{'cold'})+'.json'))
    if($run.ExitCode -ne 0 -or !$report.Passed){throw "Optical probe failed: exit=$($run.ExitCode), failure=$($report.Failure)"}
    if(Test-Path -LiteralPath (Join-Path $data 'logs/crash.log')){throw 'Optical probe wrote a crash log'}
    return $report
}
$cold=Invoke-Probe $false
foreach($directory in 'cache/previews','cache/thumbnails'){
    $path=Join-Path $data $directory
    if(Test-Path -LiteralPath $path){foreach($file in Get-ChildItem -LiteralPath $path -Filter '*.br' -File){if(!$file.FullName.StartsWith($data+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Preview removal escaped isolated data'};Remove-Item -LiteralPath $file.FullName}}
}
$hot=Invoke-Probe $true
if($cold.Count -ne $Count -or $hot.Count -ne $Count -or $hot.Cached -ne $Count -or $hot.DecodeCount -ne 0 -or $hot.OpticalBuilds -ne 0 -or $hot.SourceDiskHits -ne 0){throw 'Cross-process optical hot reuse failed'}
for($i=0;$i -lt $Count;$i++){
    if(($cold.Results[$i].LayerSha256 -join ',') -ne ($hot.Results[$i].LayerSha256 -join ',')){throw "Cross-process five-layer GPU hashes differ at $i"}
}
[ordered]@{Date=[DateTimeOffset]::Now;Version=$hot.Version;Passed=$true;Count=$Count;Bucket=$Bucket;RasterLimit=$RasterLimit;ColdMs=$cold.Milliseconds;HotMs=$hot.Milliseconds;HotSourceDecodeCount=$hot.DecodeCount;HotSourceDiskHits=$hot.SourceDiskHits;HotOpticalBuildCount=$hot.OpticalBuilds;HotOpticalDiskHits=$hot.OpticalDiskHits;FiveGpuLayerHashesEqual=$true;ColdProcessExit=0;HotProcessExit=0;SourcePreviewsRemoved=$true;DataRoot=$data;AppSha256=(Get-FileHash -LiteralPath $App).Hash.ToLowerInvariant();Scope='Two independent native app processes using production optical factory; not full startup or presentation FPS'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $data 'result.json') -Encoding utf8
Get-Content -LiteralPath (Join-Path $data 'result.json')
