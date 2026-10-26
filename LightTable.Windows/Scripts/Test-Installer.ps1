$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
$install=[IO.Path]::GetFullPath((Join-Path $root 'TestResults/install-sandbox'))
$prefix=[IO.Path]::GetFullPath((Join-Path $root 'TestResults')).TrimEnd('\')+'\'
if(!$install.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Installation test path is outside TestResults'}
$registration='HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{8BD976E8-02E5-4E28-B9BA-C4BDA67FB44A}_is1'
if(Test-Path $registration){throw 'An existing LightTable install is registered. Refusing to replace it during sandbox acceptance.'}
$shortcut=Join-Path $env:APPDATA 'Microsoft/Windows/Start Menu/Programs/LightTable.lnk'
if(Test-Path -LiteralPath $shortcut){throw 'An existing LightTable Start Menu shortcut exists; preserving it.'}
$data=Join-Path $root ('TestResults/install-integration-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
$results=[Collections.Generic.List[object]]::new()
for($round=1;$round -le 2;$round++){
    $installer=Join-Path $root 'dist/LightTable-Windows-Setup-x64.exe'
    $log=Join-Path $root "logs/install-round-$round.log"
    $arguments='/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /DIR="{0}" /LOG="{1}"' -f $install,$log
    $process=Start-Process -FilePath $installer -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
    if($process.ExitCode -ne 0){throw "Installer exited $($process.ExitCode)"}
    $app=Join-Path $install 'LightTable.exe';$uninstaller=Join-Path $install 'unins000.exe'
    if(!(Test-Path -LiteralPath $app) -or !(Test-Path -LiteralPath $uninstaller) -or !(Test-Path -LiteralPath $shortcut)){throw 'Installed executable, uninstaller or Start Menu shortcut missing'}
    if($round -eq 1){
        $argsLine='--data-root "{0}" --integration-fixtures "{1}"' -f $data,(Join-Path $root 'TestResults/fixtures')
        $run=Start-Process -FilePath $app -ArgumentList $argsLine -WindowStyle Hidden -Wait -PassThru
        if(!(Test-Path -LiteralPath (Join-Path $data 'logs/integration.json'))){throw "Installed application exited $($run.ExitCode) without integration report"}
        $integration=Get-Content (Join-Path $data 'logs/integration.json') -Raw | ConvertFrom-Json
        if($run.ExitCode -ne 0 -or !$integration.Passed){throw 'Installed application integration check failed'}
        Copy-Item -LiteralPath (Join-Path $data 'logs/integration.json') -Destination (Join-Path $root 'docs/evidence/installed-integration.json')
    }
    $uninstallLog=Join-Path $root "logs/uninstall-round-$round.log"
    $resolvedUninstaller=[IO.Path]::GetFullPath($uninstaller)
    if(!$resolvedUninstaller.StartsWith($install+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Uninstaller escaped sandbox'}
    $uninstall=Start-Process -FilePath $resolvedUninstaller -ArgumentList ('/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /LOG="{0}"' -f $uninstallLog) -WindowStyle Hidden -Wait -PassThru
    if($uninstall.ExitCode -ne 0){throw "Uninstaller exited $($uninstall.ExitCode)"}
    if(Test-Path -LiteralPath $app){throw 'Application executable remains after uninstall'}
    if(Test-Path -LiteralPath $shortcut){throw 'Start Menu shortcut remains after uninstall'}
    if(!(Test-Path -LiteralPath (Join-Path $data 'layouts/table.json'))){throw 'Uninstall removed isolated user layout'}
    $results.Add([pscustomobject]@{Date=[DateTimeOffset]::Now;Round=$round;Installed=$true;StartMenu=$true;Uninstalled=$true;LayoutPreserved=$true;IntegrationChecks=$integration.Results.Count;InstallerSha256=(Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash.ToLowerInvariant();Environment='Current development host, not a clean VM'})
}
$results | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $root 'docs/evidence/installer.json') -Encoding utf8
$results | Format-Table
