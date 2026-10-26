param([Parameter(Mandatory=$true)][string]$Output)
$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
$outputRoot=[IO.Path]::GetFullPath($Output)
if(!(Test-Path -LiteralPath (Join-Path $outputRoot 'LightTable.dll'))){throw 'Native published application is missing'}
$sources=[ordered]@{}
Push-Location $root
try{
    $sourceFiles=@(& rg --files LightTable.Windows)
    if($LASTEXITCODE -ne 0){throw 'Could not enumerate Windows sources'}
    foreach($file in $sourceFiles | Sort-Object){$sources[$file.Replace('\','/')]=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()}
    foreach($file in 'Directory.Build.props','global.json'){$sources[$file]=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()}
    $baseline=(& git -C upstream rev-parse HEAD).Trim();if($LASTEXITCODE -ne 0){throw 'Upstream baseline could not be resolved'}
}finally{Pop-Location}
$binaries=[ordered]@{}
foreach($file in 'LightTable.exe','LightTable.dll','LightTable.Core.dll','LightTable.pri','Resources/LoupeEdge.bin','Resources/FusedOptical.bin','Resources/FusedLamp.bin'){
    $binaries[$file]=(Get-FileHash -LiteralPath (Join-Path $outputRoot $file) -Algorithm SHA256).Hash.ToLowerInvariant()
}
[ordered]@{Version=[Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $outputRoot 'LightTable.dll')).FileVersion;Configuration='Release';Runtime='win-x64';CreatedUtc=[DateTime]::UtcNow.ToString('O');Baseline=$baseline;Signed=$false;SourceSha256=$sources;BinarySha256=$binaries;CoreEvidenceSha256=(Get-FileHash -LiteralPath (Join-Path $root 'docs/evidence/core.trx') -Algorithm SHA256).Hash.ToLowerInvariant()} |
    ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outputRoot 'build-manifest.json') -Encoding utf8
