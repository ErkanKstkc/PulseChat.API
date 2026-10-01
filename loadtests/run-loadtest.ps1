param (
    [int]$Users = 20,
    [int]$RampUp = 5,
    [int]$Loops = 1,
    [int]$Messages = 5,
    [string]$TargetHost = "localhost",
    [int]$TargetPort = 5000
)

$baseDir = $PSScriptRoot
$jmeterBat = "$baseDir\jmeter\apache-jmeter-5.6.3\bin\jmeter.bat"
$testPlan = "$baseDir\pulsechat-loadtest.jmx"
$resultsDir = "$baseDir\results"
$resultsFile = "$resultsDir\results.jtl"
$reportDir = "$baseDir\reports\report-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

if (-not (Test-Path $jmeterBat)) {
    Write-Error "JMeter executable not found at $jmeterBat. Please run setup-jmeter.ps1 first."
    exit 1
}

# Clean old raw results
if (-not (Test-Path $resultsDir)) {
    New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null
}
$reportsBase = "$baseDir\reports"
if (-not (Test-Path $reportsBase)) {
    New-Item -ItemType Directory -Path $reportsBase -Force | Out-Null
}
if (Test-Path $resultsFile) {
    Remove-Item -Path $resultsFile -Force
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  PulseChat JMeter Load Test Starting                     " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  Target:        http://${TargetHost}:${TargetPort}"
Write-Host "  Users:         $Users"
Write-Host "  Ramp-up (s):   $RampUp"
Write-Host "  Loops:         $Loops"
Write-Host "  Messages/User: $Messages"
Write-Host "  Report Folder: $reportDir"
Write-Host "==========================================================" -ForegroundColor Cyan

# Run JMeter in Non-GUI mode (-n) and generate HTML dashboard (-e -o)
$jmeterBin = "$baseDir\jmeter\apache-jmeter-5.6.3\bin"
Push-Location $jmeterBin

& cmd.exe /c "jmeter.bat -n -t `"$testPlan`" -l `"$resultsFile`" -e -o `"$reportDir`" -Jhost=$TargetHost -Jport=$TargetPort -Jusers=$Users -Jrampup=$RampUp -Jloops=$Loops -Jmessages=$Messages"

Pop-Location

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nTest finished successfully!" -ForegroundColor Green
    Write-Host "Opening HTML Dashboard: $reportDir\index.html" -ForegroundColor Green
    Start-Process "$reportDir\index.html"
} else {
    Write-Host "`nJMeter execution encountered errors (Exit Code: $LASTEXITCODE)." -ForegroundColor Yellow
    if (Test-Path "$reportDir\index.html") {
        Start-Process "$reportDir\index.html"
    }
}
