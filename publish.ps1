$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

dotnet publish -r win-x64 -c Release  /p:Platform=x64
dotnet publish -r win-arm64 -c Release  /p:Platform=arm64

# Get files and extract version numbers
$FilesWithVersions_x64 = Get-ChildItem -Path .\msix\x64 -File -Recurse -Filter "DocSpy*.msix"| ForEach-Object {
    # Example: Assuming version is in format X.Y.Z.W
    if ($_.BaseName -match '(\d+\.\d+\.\d+\.\d+)') {
        [PSCustomObject]@{
            File = $_
            Version = [System.Version]$Matches[1]
        }
    }
}

# Sort by version in descending order and select the first (highest)
$HighestVersionFile = $FilesWithVersions_x64 | Sort-Object -Property Version -Descending | Select-Object -First 1

# Output the result
if ($HighestVersionFile) {
    Write-Host "File with the biggest version number: $($HighestVersionFile.File.Name)"
    Write-Host "Version: $($HighestVersionFile.Version)"
} else {
    Write-Host "No files with a recognizable version number found."
}

#$ZipFileName_x64 = ".\Releases\DocSpy_x64-$($HighestVersionFile.Version).zip"
#$ZipFileName_arm64 = ".\Releases\DocSpy_arm64-$($HighestVersionFile.Version).zip"
#if (-Not (Test-Path -Path ".\Releases")) {
#    New-Item -ItemType Directory -Path ".\Releases"
#}

$ZipSource_x64 = ($HighestVersionFile).File.FullName
$ZipSource_arm64 = ($HighestVersionFile).File.FullName -Replace "x64", "arm64"
#Compress-Archive -Path $ZipSource_x64 -DestinationPath $ZipFileName_x64 -Force
#Compress-Archive -Path $ZipSource_arm64 -DestinationPath $ZipFileName_arm64 -Force

gh release create "v$($HighestVersionFile.Version)" -n "Release of DocSpy $($HighestVersionFile.Version)" $ZipSource_x64 $ZipSource_arm64
Write-Host "Release created with version: $($HighestVersionFile.Version)"
