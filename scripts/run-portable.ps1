# Download & start PrintSwitchPro portable build (avoids Inno Setup Error 4551 / Smart App Control).
param(
    [string]$Tag = "latest"
)

$ErrorActionPreference = "Stop"
$repo = "AniGerm/PrintSwitchPro"
$destRoot = Join-Path $env:LOCALAPPDATA "Programs\PrintSwitchPro"

if ($Tag -eq "latest") {
    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases/latest" -Headers @{
        "User-Agent" = "PrintSwitchPro-Portable"
        "Accept"     = "application/vnd.github+json"
    }
} else {
    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases/tags/$Tag" -Headers @{
        "User-Agent" = "PrintSwitchPro-Portable"
        "Accept"     = "application/vnd.github+json"
    }
}

$asset = $release.assets | Where-Object { $_.name -match '^PrintSwitchPro-Portable-.*-win-x64\.zip$' } | Select-Object -First 1
if (-not $asset) {
    throw "Kein Portable-ZIP im Release gefunden. Bitte Release-Assets prüfen."
}

New-Item -ItemType Directory -Force -Path $destRoot | Out-Null
$zipPath = Join-Path $env:TEMP $asset.name
Write-Host "Lade $($asset.name) …"
Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zipPath -UseBasicParsing
Unblock-File -Path $zipPath -ErrorAction SilentlyContinue

$extract = Join-Path $destRoot "app"
if (Test-Path $extract) { Remove-Item -Recurse -Force $extract }
Expand-Archive -Path $zipPath -DestinationPath $extract -Force
Get-ChildItem -Path $extract -Recurse -Include *.exe,*.dll | ForEach-Object {
    Unblock-File -Path $_.FullName -ErrorAction SilentlyContinue
}

$exe = Join-Path $extract "PrintSwitchPro.exe"
if (-not (Test-Path $exe)) { throw "PrintSwitchPro.exe fehlt nach dem Entpacken." }
Write-Host "Starte $exe"
Start-Process -FileName $exe
