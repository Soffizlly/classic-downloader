@echo off
setlocal

echo Buscando MSBuild...
taskkill /F /IM ClassicDownloader.exe 2>nul

set MSBUILD=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe

if not exist "%MSBUILD%" (
    echo MSBuild no encontrado en la ruta esperada.
    echo Buscando en rutas alternativas...
    for /r "C:\Windows\Microsoft.NET\Framework" %%f in (MSBuild.exe) do (
        set MSBUILD="%%f"
        goto :Found
    )
)

:Found
if not exist "%MSBUILD%" (
    echo No se pudo encontrar MSBuild.exe
    pause
    exit /b 1
)

echo Usando MSBuild en: %MSBUILD%
echo Compilando MultiLangApp...
"%MSBUILD%" MultiLangApp\MultiLangApp.csproj /t:Rebuild /p:Configuration=Debug

if %ERRORLEVEL% NEQ 0 (
    echo Error durante la compilacion.
    pause
    exit /b 1
)

echo.
echo Compilacion exitosa!
echo Ejecutando aplicacion...
start MultiLangApp\bin\Debug\ClassicDownloader.exe

endlocal
