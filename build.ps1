param([switch]$FrameworkDependent, [switch]$Offline)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    dotnet run --project tests -c Release
    if ($LASTEXITCODE) { throw 'Mapping tests failed' }
    $selfContained = if ($FrameworkDependent) { 'false' } else { 'true' }
    $sourceArgs = @()
    if ($Offline) {
        $feed = Join-Path $PSScriptRoot 'artifacts/empty-feed'
        New-Item -ItemType Directory -Path $feed -Force | Out-Null
        $sourceArgs = @('--source', $feed)
    }
    dotnet publish src/KeyboardSwitcher.csproj -c Release -r win-x64 --self-contained $selfContained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -o dist @sourceArgs
    if ($LASTEXITCODE) { throw 'Publish failed' }
} finally { Pop-Location }
