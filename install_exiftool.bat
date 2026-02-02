@echo off
echo [INSTALL] Downloading ExifTool from official site...
powershell -Command "Invoke-WebRequest -Uri 'https://exiftool.org/exiftool-13.46_64.zip' -OutFile 'exiftool.zip'"

echo [INSTALL] Extracting ExifTool...
powershell -Command "Expand-Archive -Path 'exiftool.zip' -DestinationPath 'temp_exif' -Force"

echo [INSTALL] Configuring Executable...
powershell -Command "Get-ChildItem -Path 'temp_exif' -Recurse -Filter 'exiftool(-k).exe' | Select-Object -First 1 | Move-Item -Destination 'MultiLangApp\bin\Debug\exiftool.exe' -Force"

echo [INSTALL] Checking for dependencies...
powershell -Command "Get-ChildItem -Path 'temp_exif' -Recurse -Directory -Filter 'exiftool_files' | Select-Object -First 1 | Move-Item -Destination 'MultiLangApp\bin\Debug' -Force"


if exist "MultiLangApp\bin\Debug\exiftool.exe" (
    echo [SUCCESS] ExifTool configured correctly.
) else (
    echo [ERROR] Could not find exiftool(-k).exe after extraction.
)

echo [INSTALL] Cleanup...
rmdir /s /q temp_exif

echo [INSTALL] Cleanup...
del exiftool.zip

echo [SUCCESS] ExifTool installed!
pause
