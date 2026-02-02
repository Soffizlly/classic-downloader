@echo off
setlocal

echo ==========================================
echo   Classic Downloader - Publish Script
echo ==========================================

set MSBUILD=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe
set OUT_DIR=Publish\ClassicDownloader

if not exist "%MSBUILD%" (
    echo MSBuild no encontrado.
    exit /b 1
)

echo 1. Limpiando versiones anteriores...
if exist "Publish" rmdir /s /q "Publish"
mkdir "%OUT_DIR%"
mkdir "%OUT_DIR%\Tools"

echo.
echo 2. Compilando en modo RELEASE...
"%MSBUILD%" MultiLangApp\MultiLangApp.csproj /t:Rebuild /p:Configuration=Release /p:OutputPath="..\Publish\ClassicDownloader"

if %ERRORLEVEL% NEQ 0 (
    echo Error de compilacion.
    pause
    exit /b 1
)

echo.
echo 3. Copiando herramientas externas...
REM Intentamos copiar desde la carpeta Debug donde sabemos que funcionan
if exist "MultiLangApp\bin\Debug\Tools\ffmpeg.exe" (
    copy "MultiLangApp\bin\Debug\Tools\ffmpeg.exe" "%OUT_DIR%\Tools\"
    echo FFMPEG copiado.
) else (
    echo WARNING: ffmpeg.exe no encontrado en Debug\Tools.
)

if exist "MultiLangApp\bin\Debug\Tools\yt-dlp.exe" (
    copy "MultiLangApp\bin\Debug\Tools\yt-dlp.exe" "%OUT_DIR%\Tools\"
    echo YT-DLP copiado.
) else (
    echo WARNING: yt-dlp.exe no encontrado en Debug\Tools.
)


if exist "MultiLangApp\bin\Debug\exiftool.exe" (
    copy "MultiLangApp\bin\Debug\exiftool.exe" "%OUT_DIR%\"
    echo ExifTool [exe] copiado.
)
if exist "MultiLangApp\bin\Debug\exiftool_files" (
    robocopy "MultiLangApp\bin\Debug\exiftool_files" "%OUT_DIR%\exiftool_files" /E /IS /IT
    echo ExifTool [dependencies] copiados.
)

echo.
echo 4. Limpiando archivos innecesarios (.pdb, .xml)...
del "%OUT_DIR%\*.pdb"
del "%OUT_DIR%\*.xml"

echo.
echo 5. Creando ZIP portable...
powershell -Command "Compress-Archive -Path '%OUT_DIR%' -DestinationPath 'Publish\ClassicDownloader_Alpha.zip' -Force"

echo.
echo ==========================================
echo   LISTO!
echo   La version portable esta en: Publish\ClassicDownloader
echo   ZIP: Publish\ClassicDownloader_Alpha.zip
echo ==========================================
