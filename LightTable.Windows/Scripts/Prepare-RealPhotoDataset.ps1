param([int]$Count=1000)
$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
$dataset=Join-Path $root 'TestResults/real-photos-nasa'
New-Item -ItemType Directory -Force $dataset | Out-Null
$records=[Collections.Generic.List[object]]::new()
for($page=1;$records.Count -lt $Count -and $page -le 20;$page++){
    $url="https://images-api.nasa.gov/search?q=earth&center=JSC&media_type=image&page_size=100&page=$page"
    $metadata=Invoke-RestMethod $url
    foreach($item in $metadata.collection.items){
        $id=$item.data[0].nasa_id
        $link=$item.links | Where-Object {$_.rel -eq 'preview'} | Select-Object -First 1
        if(!$link -or !$id -or $records.nasa_id -contains $id){continue}
        $records.Add([pscustomobject]@{nasa_id=$id;url=$link.href;title=$item.data[0].title;center=$item.data[0].center;path=(Join-Path $dataset (($id -replace '[^a-zA-Z0-9_-]','_')+'.jpg'))})
        if($records.Count -ge $Count){break}
    }
    Write-Output "Indexed $($records.Count) distinct NASA image records"
}
$records | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $dataset 'sources.json') -Encoding utf8
$downloads=$records | ForEach-Object -Parallel {
    $item=$_
    if(Test-Path -LiteralPath $item.path){return [pscustomobject]@{id=$item.nasa_id;ok=$true;cached=$true;error=''}}
    try{Invoke-WebRequest $item.url -OutFile $item.path -TimeoutSec 30;[pscustomobject]@{id=$item.nasa_id;ok=$true;cached=$false;error=''}}
    catch{[pscustomobject]@{id=$item.nasa_id;ok=$false;cached=$false;error=$_.Exception.Message}}
} -ThrottleLimit 4
$downloads | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $dataset 'downloads.json') -Encoding utf8
$ok=($downloads | Where-Object ok).Count
Write-Output "Downloaded $ok / $($records.Count) actual NASA photographs. Dataset is test-only and not redistributed."
if($ok -lt $Count){throw "Only $ok photos downloaded; do not claim $Count-photo verification."}
