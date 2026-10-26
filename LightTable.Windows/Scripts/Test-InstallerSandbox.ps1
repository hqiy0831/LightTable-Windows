param([Parameter(Mandatory)][string]$Iscc)
$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
$testRoot=Join-Path $root ('TestResults/install-acceptance-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
$install=[IO.Path]::GetFullPath((Join-Path $testRoot 'app'))
$prefix=[IO.Path]::GetFullPath((Join-Path $root 'TestResults')).TrimEnd('\')+'\'
if(!$install.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Install path escaped workspace TestResults'}
$identity='{C58AA8D4-73EF-4C73-83EB-6E35E6A43295}'
$registration="HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\${identity}_is1"
$shortcut=Join-Path $env:APPDATA 'Microsoft/Windows/Start Menu/Programs/LightTable-Acceptance-Sandbox.lnk'
if((Test-Path $registration) -or (Test-Path $shortcut)){throw 'Acceptance identity already exists; refusing to replace it'}
$productionRegistration='HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{8BD976E8-02E5-4E28-B9BA-C4BDA67FB44A}_is1'
$productionShortcut=Join-Path $env:APPDATA 'Microsoft/Windows/Start Menu/Programs/LightTable.lnk'
function ProductionState {
    $registered=if(Test-Path $productionRegistration){Get-ItemProperty $productionRegistration | Select-Object * -ExcludeProperty PS* | ConvertTo-Json -Compress}else{''}
    $linkHash=if(Test-Path $productionShortcut){(Get-FileHash -LiteralPath $productionShortcut -Algorithm SHA256).Hash}else{''}
    return [pscustomobject]@{Registration=$registered;ShortcutSha256=$linkHash} | ConvertTo-Json -Compress
}
$before=ProductionState
New-Item -ItemType Directory -Path $testRoot | Out-Null
$build=Join-Path $root 'dist/Windows-x64'
& $Iscc "/DBuildRoot=$build" "/DOutputRoot=$testRoot" "/DAppIdentity={$identity" '/DShortcutName=LightTable-Acceptance-Sandbox' '/DBuildOutputName=LightTable-Acceptance-Setup-x64' (Join-Path $root 'LightTable.Windows/Installer/LightTable.iss')
if($LASTEXITCODE -ne 0){throw 'Acceptance installer compilation failed'}
$installer=Join-Path $testRoot 'LightTable-Acceptance-Setup-x64.exe'
$data=Join-Path $testRoot 'isolated-data'
$rounds=@()
for($round=1;$round -le 2;$round++){
    $argsLine='/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /DIR="{0}" /LOG="{1}"' -f $install,(Join-Path $testRoot "install-$round.log")
    $p=Start-Process -FilePath $installer -ArgumentList $argsLine -WindowStyle Hidden -Wait -PassThru
    if($p.ExitCode -ne 0){throw "Acceptance installer failed with exit $($p.ExitCode)"}
    $app=Join-Path $install 'LightTable.exe'
    if(!(Test-Path $app) -or !(Test-Path $shortcut) -or !(Test-Path $registration)){throw 'Sandbox install did not register its own executable/shortcut/uninstaller'}
    if($round -eq 1){
        $argsLine='--data-root "{0}" --integration-fixtures "{1}"' -f $data,(Join-Path $root 'TestResults/fixtures')
        $run=Start-Process -FilePath $app -ArgumentList $argsLine -WindowStyle Hidden -Wait -PassThru
        $report=Get-Content -Raw (Join-Path $data 'logs/integration.json') | ConvertFrom-Json
        if($run.ExitCode -ne 0 -or !$report.Passed -or (Test-Path (Join-Path $data 'logs/crash.log'))){throw 'Installed sandbox native checks failed'}
    }
    $uninstaller=[IO.Path]::GetFullPath((Join-Path $install 'unins000.exe'))
    if(!$uninstaller.StartsWith($install+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Uninstaller escaped sandbox'}
    $argsLine='/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /LOG="{0}"' -f (Join-Path $testRoot "uninstall-$round.log")
    $u=Start-Process -FilePath $uninstaller -ArgumentList $argsLine -WindowStyle Hidden -Wait -PassThru
    if($u.ExitCode -ne 0 -or (Test-Path $app) -or (Test-Path $shortcut) -or (Test-Path $registration)){throw 'Acceptance uninstall failed'}
    if(!(Test-Path (Join-Path $data 'layouts/table.json'))){throw 'Uninstall removed isolated user layout'}
    if((ProductionState) -ne $before){throw 'Existing production installation metadata changed'}
    $rounds+=@{Round=$round;Installed=$true;Uninstalled=$true;LayoutPreserved=$true;ExistingProductionUnchanged=$true;Checks=$report.Results.Count;ExitCode=$run.ExitCode}
}
@{Passed=$true;Scope='Same installer source/payload with separate AppId and shortcut; current host, not clean Windows; ordinary production installer not installed over existing user app';Rounds=$rounds;InstallerSha256=(Get-FileHash $installer -Algorithm SHA256).Hash;ProductionState=$before} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $testRoot 'acceptance.json')
Write-Output "Acceptance installer passed two rounds. Evidence: $testRoot/acceptance.json"
