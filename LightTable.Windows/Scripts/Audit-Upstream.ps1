param([string]$Root = (Resolve-Path "$PSScriptRoot/../..").Path)
$ErrorActionPreference='Stop'
$sourceRoot=Join-Path $Root 'upstream'
$files=git -C $sourceRoot -c core.quotepath=false ls-files
$inventory=@()
$symbols=[Collections.Generic.List[string]]::new()
$symbols.Add('# 上游源码符号与交互索引')
$symbols.Add('')
$symbols.Add('自动索引读取所有受版本控制文件；二进制仅记录哈希。此索引不能替代逐项行为和视觉验收。')
foreach($relative in $files){
    $path=Join-Path $sourceRoot $relative
    $info=Get-Item -LiteralPath $path
    $hash=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    $lines=0
    if($relative -match '\.(swift|md|sh|py|plist|json)$' -or $relative -match '(^|/)(LICENSE|VERSION)$'){
        $content=[IO.File]::ReadAllLines($path)
        $lines=$content.Count
        $symbols.Add('')
        $symbols.Add("## $relative")
        for($i=0;$i -lt $content.Count;$i++){
            $line=$content[$i]
            if($line -match '^\s*(?:(?:public|private|static|final|@MainActor|@Observable|nonisolated|override|mutating)\s+)*(?:func|class|struct|enum|actor)\s' -or $line -match '(Button\(|Menu\(|keyboardShortcut\(|\.on[A-Z]\w+\(|check\(|expect\(|XCTAssert|case\s+(waiting|departing|black|arriving|settled))'){
                $symbols.Add(('- L{0}: `{1}`' -f ($i+1),$line.Trim().Replace('`','\`')))
            }
        }
    }
    $inventory+= [pscustomobject]@{Path=$relative;Bytes=$info.Length;Lines=$lines;SHA256=$hash}
}
$inventory | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $Root 'docs/SOURCE_INVENTORY.json') -Encoding utf8
$symbols | Set-Content -LiteralPath (Join-Path $Root 'docs/SOURCE_SYMBOLS.md') -Encoding utf8
Write-Output "Indexed $($inventory.Count) files at $(git -C $sourceRoot rev-parse HEAD)"
