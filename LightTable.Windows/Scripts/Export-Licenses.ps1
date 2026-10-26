param([Parameter(Mandatory)][string]$Output)
$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
$lock=Get-Content (Join-Path $root 'LightTable.Windows/App/packages.lock.json') -Raw | ConvertFrom-Json -AsHashtable
$packages=@{}
foreach($target in $lock.dependencies.Values){foreach($entry in $target.GetEnumerator()){if($entry.Value.type -ne 'Project'){$packages[$entry.Key]=$entry.Value.resolved}}}
$destination=Join-Path $Output 'licenses';New-Item -ItemType Directory -Force $destination | Out-Null
$manifest=@()
foreach($entry in $packages.GetEnumerator() | Sort-Object Key){
    $id=$entry.Key;$version=$entry.Value
    if($id -notmatch '^[a-zA-Z0-9_.-]+$' -or $version -notmatch '^[a-zA-Z0-9_.+-]+$'){throw 'Invalid package identity'}
    $dir=Join-Path $env:USERPROFILE ".nuget/packages/$($id.ToLower())/$version"
    $packageOut=Join-Path $destination "$id-$version";New-Item -ItemType Directory -Force $packageOut | Out-Null
    $nuspec=Get-ChildItem -LiteralPath $dir -Filter '*.nuspec' | Select-Object -First 1
    if(!$nuspec){throw "Missing restored package metadata: $id $version"}
    Copy-Item -LiteralPath $nuspec.FullName -Destination $packageOut
    [xml]$xml=Get-Content -LiteralPath $nuspec.FullName
    $files=Get-ChildItem -LiteralPath $dir -File | Where-Object {$_.Name -match '(?i)license|notice|copying|copyright'}
    foreach($file in $files){Copy-Item -LiteralPath $file.FullName -Destination $packageOut}
    $url=[string]$xml.package.metadata.licenseUrl
    $manifest+=[pscustomobject]@{Package=$id;Version=$version;License=[string]$xml.package.metadata.license;LicenseUrl=$url;IncludedFiles=($files.Name -join ', ')}
}
# These runtime packs are not listed as application NuGet dependencies.
foreach($name in 'Microsoft.NETCore.App.Runtime.win-x64','Microsoft.WindowsDesktop.App.Runtime.win-x64'){
    $base=Join-Path $env:USERPROFILE ".nuget/packages/$($name.ToLower())"
    if(Test-Path -LiteralPath $base){foreach($version in Get-ChildItem -LiteralPath $base -Directory){
        $folder=Join-Path $destination "$name-$($version.Name)";New-Item -ItemType Directory -Force $folder | Out-Null
        Get-ChildItem -LiteralPath $version.FullName -File | Where-Object {$_.Name -match '(?i)license|notice'} | Copy-Item -Destination $folder
    }}
}
$win2d=Join-Path $destination 'Microsoft.Graphics.Win2D-1.4.0'
Copy-Item -LiteralPath (Join-Path $root 'LightTable.Windows/Resources/Win2D-SOURCE-LICENSE.txt') -Destination $win2d
$sourceCommit=(Get-Content (Join-Path $root 'LightTable.Windows/Resources/Win2D-LICENSE-COMMIT.txt')).Trim()
"NuGet 1.4.0 metadata links to an obsolete Microsoft EULA URL which now redirects to an unrelated Windows Web page. The original URL is preserved in the nuspec and manifest. The package-recorded source hash 57e06e2c24703526f500035129251d063881a44d was not retrievable from the public repository. A separate upstream MIT source LICENSE.txt is preserved from commit $sourceCommit; it is not labelled as a replacement binary-package EULA. Exact package-license reconciliation remains an open release audit item." | Set-Content (Join-Path $win2d 'LICENSE-SOURCE-NOTE.txt') -Encoding utf8
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $destination 'NuGet-licenses.json') -Encoding utf8
Write-Output "Exported notices for $($manifest.Count) application/build packages plus installed runtime packs"
