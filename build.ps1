$ErrorActionPreference = "Stop"

$ProjectDir = "XTranslate"
$ProjectFile = "$ProjectDir\XTranslate.csproj"
$OutputBase = "PublishOutput"

Write-Host "XTranslate Build Optimizations Script" -ForegroundColor Cyan
Write-Host "====================================="

# 1. Clean previous build
Write-Host "Cleaning old builds..." -ForegroundColor Yellow
if (Test-Path $OutputBase) {
    Remove-Item -Recurse -Force $OutputBase
}
New-Item -ItemType Directory -Force -Path "$OutputBase\Lightweight" | Out-Null
New-Item -ItemType Directory -Force -Path "$OutputBase\Standalone" | Out-Null

# 2. Build Lightweight version (Not self-contained, requires .NET 8 on target machine)
Write-Host "`nBuilding [Lightweight] Mode (Fast to download, requires .NET 8 runtime)..." -ForegroundColor Green
dotnet publish $ProjectFile -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o "$OutputBase\Lightweight"

# 3. Build Standalone version (Self-contained, huge but runs anywhere without installation)
Write-Host "`nBuilding [Standalone] Mode (Includes .NET 8, very large but portable)..." -ForegroundColor Green
dotnet publish $ProjectFile -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$OutputBase\Standalone"

Write-Host "`nBuild Complete!" -ForegroundColor Cyan
Write-Host "Check the $OutputBase directory for the generated files."
