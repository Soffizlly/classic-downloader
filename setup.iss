; Script generado para Inno Setup
; Descarga Inno Setup gratis en: https://jrsoftware.org/isdl.php

#define MyAppName "Classic Downloader"
#define MyAppVersion "1.0"
#define MyAppPublisher "LexCore Apps"
#define MyAppExeName "MultiLangApp.exe"

[Setup]
; Identificador unico (Generar uno nuevo para cada app)
AppId={{4A26C689-A282-4112-9952-B4887309B8E3}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
; Ruta donde se guardará el instalador generado
OutputBaseFilename=ClassicDownloader_Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Archivos a incluir (Se asume que corriste publish.bat primero)
Source: "Publish\ClassicDownloader\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "Publish\ClassicDownloader\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Publish\ClassicDownloader\Tools\*"; DestDir: "{app}\Tools"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTA: Asegurate de que los archivos existan en la carpeta Publish\ClassicDownloader

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent
