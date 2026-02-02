$sourceDir = "bin\Debug"
$outputZip = "ClassicDownloader_Portable.zip"

# Ensure zip is not inside the source dir to avoid recursion
if (Test-Path $outputZip) { Remove-Item $outputZip }

Write-Host "Creating Portable ZIP..."
Compress-Archive -Path "$sourceDir\*" -DestinationPath $outputZip -Force

Write-Host "Done! Portable release created at: $outputZip"
