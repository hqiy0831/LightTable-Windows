#ifndef BuildRoot
  #define BuildRoot "..\..\dist\Windows-x64"
#endif
#ifndef OutputRoot
  #define OutputRoot "..\..\dist"
#endif
#define AppBuildVersion GetFileVersion(AddBackslash(BuildRoot) + "LightTable.dll")
#ifndef AppIdentity
  #define AppIdentity "{{8BD976E8-02E5-4E28-B9BA-C4BDA67FB44A}"
#endif
#ifndef ShortcutName
  #define ShortcutName "LightTable"
#endif
#ifndef BuildOutputName
  #define BuildOutputName "LightTable-Windows-Setup-x64"
#endif
[Setup]
AppId={#AppIdentity}
AppName=LightTable for Windows
AppVersion={#AppBuildVersion}
AppVerName=LightTable for Windows {#AppBuildVersion} (Unsigned development build)
AppCopyright=Copyright (c) 2026 fuyuan9972-svg
DefaultDirName={localappdata}\Programs\LightTable
DefaultGroupName=LightTable
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
OutputDir={#OutputRoot}
OutputBaseFilename={#BuildOutputName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\LightTable.exe
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
LicenseFile=..\..\LICENSE
InfoBeforeFile=Unsigned-build.txt
VersionInfoVersion={#AppBuildVersion}
VersionInfoDescription=LightTable Windows Setup - Unsigned development build
SetupIconFile=..\Resources\AppIcon.ico
[Tasks]
Name: desktopicon; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked
[Files]
Source: "{#BuildRoot}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{autoprograms}\{#ShortcutName}"; Filename: "{app}\LightTable.exe"
Name: "{autodesktop}\{#ShortcutName}"; Filename: "{app}\LightTable.exe"; Tasks: desktopicon
[Run]
Filename: "{app}\LightTable.exe"; Description: "Launch LightTable"; Flags: nowait postinstall skipifsilent
; User layouts, config and cache live outside {app} and remain after uninstall.
