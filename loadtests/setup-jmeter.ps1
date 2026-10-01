$ErrorActionPreference = "Stop"

$jmeterDir = "$PSScriptRoot\jmeter"
$zipPath = "$jmeterDir\apache-jmeter-5.6.3.zip"
$jmeterHome = "$jmeterDir\apache-jmeter-5.6.3"

if (-not (Test-Path $jmeterDir)) {
    New-Item -ItemType Directory -Path $jmeterDir -Force | Out-Null
}

if (-not (Test-Path $jmeterHome)) {
    Write-Host "Downloading Apache JMeter 5.6.3 via curl..."
    $jmeterUrl = "https://dlcdn.apache.org//jmeter/binaries/apache-jmeter-5.6.3.zip"
    $backupUrl = "https://archive.apache.org/dist/jmeter/binaries/apache-jmeter-5.6.3.zip"
    
    & curl.exe -L --fail --output "$zipPath" "$jmeterUrl"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Primary mirror failed, trying archive.apache.org..."
        & curl.exe -L --fail --output "$zipPath" "$backupUrl"
    }

    Write-Host "Extracting JMeter..."
    tar.exe -xf "$zipPath" -C "$jmeterDir"
    Remove-Item -Path $zipPath -Force -ErrorAction SilentlyContinue
    Write-Host "JMeter extracted successfully."
} else {
    Write-Host "JMeter is already extracted."
}

$libExt = "$jmeterHome\lib\ext"
$wsJar = "$libExt\jmeter-websocket-samplers-1.3.2.jar"
if (-not (Test-Path $wsJar)) {
    Write-Host "Downloading WebSocket Samplers plugin v1.3.2..."
    $wsUrl = "https://repo1.maven.org/maven2/net/luminis/jmeter/jmeter-websocket-samplers/1.3.2/jmeter-websocket-samplers-1.3.2.jar"
    & curl.exe -L --fail --output "$wsJar" "$wsUrl"
    Write-Host "WebSocket Samplers plugin installed."
}

$pmJar = "$libExt\jmeter-plugins-manager-1.10.jar"
if (-not (Test-Path $pmJar)) {
    Write-Host "Downloading JMeter Plugins Manager..."
    $pmUrl = "https://jmeter-plugins.org/get/"
    & curl.exe -L --fail --output "$pmJar" "$pmUrl"
    Write-Host "Plugins Manager installed."
}

Write-Host "JMeter setup complete!"
