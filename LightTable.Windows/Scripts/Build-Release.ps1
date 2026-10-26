param([string]$Dotnet='', [string]$Iscc='', [switch]$SkipInstaller, [switch]$NoBuild)
$ErrorActionPreference='Stop'
$projectRoot=(Resolve-Path "$PSScriptRoot/../..").Path
if(!$Dotnet){$bundled=Join-Path $env:LOCALAPPDATA 'LightTableBuild/dotnet/dotnet.exe';if(Test-Path -LiteralPath $bundled){$Dotnet=$bundled}else{$Dotnet=(Get-Command dotnet).Source}}
if(!$Iscc){$Iscc=Join-Path $env:LOCALAPPDATA 'Programs/Inno Setup 7/ISCC.exe'}
$out=Join-Path $projectRoot 'dist/Windows-x64'
$dist=Join-Path $projectRoot 'dist'
New-Item -ItemType Directory -Force (Join-Path $projectRoot 'logs'),$dist | Out-Null
Push-Location $projectRoot
try{
  & $Dotnet test LightTable.Windows/Tests/LightTable.Core.Tests.csproj -c Release --logger 'trx;LogFileName=release-core.trx' --results-directory TestResults
  if($LASTEXITCODE -ne 0){throw 'Core tests failed'}
  Copy-Item -LiteralPath (Join-Path $projectRoot 'TestResults/release-core.trx') -Destination (Join-Path $projectRoot 'docs/evidence/core.trx')
  $resolvedOutput=[IO.Path]::GetFullPath($out)
  $workspacePrefix=[IO.Path]::GetFullPath($projectRoot).TrimEnd('\')+'\'
  if(!$resolvedOutput.StartsWith($workspacePrefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($resolvedOutput) -ne 'Windows-x64'){throw 'Refusing to clean output outside the expected workspace/dist/Windows-x64 folder'}
  if(Test-Path -LiteralPath $resolvedOutput){Remove-Item -LiteralPath $resolvedOutput -Recurse -Force}
  $publishArgs=@('publish','LightTable.Windows/App/LightTable.Windows.csproj','-c','Release','-r','win-x64','--self-contained','true','-o',$out,'-p:WindowsAppSDKSelfContained=true','-v','minimal')
  if($NoBuild){$publishArgs+='--no-build'}
  & $Dotnet @publishArgs
  if($LASTEXITCODE -ne 0){throw 'Publish failed'}
  if(!(Test-Path -LiteralPath (Join-Path $out 'LightTable.pri'))){throw 'Published application PRI missing'}
  & (Join-Path $projectRoot 'LightTable.Windows/Scripts/Write-BuildManifest.ps1') -Output $out
  Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md'),(Join-Path $projectRoot 'README-Windows.md'),(Join-Path $projectRoot 'LICENSE'),(Join-Path $projectRoot 'THIRD_PARTY_NOTICES.md') -Destination $out
  Copy-Item -LiteralPath (Join-Path $projectRoot 'docs') -Destination $out -Recurse -Force
  & (Join-Path $projectRoot 'LightTable.Windows/Scripts/Export-Licenses.ps1') -Output $out
  $zip=Join-Path $dist 'LightTable-Windows-Portable-x64.zip'
  Compress-Archive -Path "$out/*" -DestinationPath $zip -Force
  if(!$SkipInstaller){
    if(!(Test-Path -LiteralPath $Iscc)){throw 'Inno Setup 7 ISCC.exe not found. Pass -Iscc or install the official tool.'}
    & $Iscc "/DBuildRoot=$out" "/DOutputRoot=$dist" (Join-Path $projectRoot 'LightTable.Windows/Installer/LightTable.iss')
    if($LASTEXITCODE -ne 0){throw 'Installer build failed'}
  }
  Get-ChildItem -LiteralPath $dist -File | Where-Object Extension -in '.zip','.exe' | ForEach-Object {"$((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLower())  $($_.Name)"} | Set-Content -LiteralPath (Join-Path $dist 'SHA256SUMS-Windows.txt') -Encoding ascii
}finally{Pop-Location}
