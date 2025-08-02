# Production Status Check Script
Write-Host "🚀 Checking Neshan Geocoding API Production Status..." -ForegroundColor Green

# Check if application is running
Write-Host "`n📊 Checking Application Status..." -ForegroundColor Yellow
$process = Get-Process -Name "NeshanGeocodingApi" -ErrorAction SilentlyContinue
if ($process) {
    Write-Host "✅ Application is running (PID: $($process.Id))" -ForegroundColor Green
} else {
    Write-Host "❌ Application is not running" -ForegroundColor Red
}

# Check ports
Write-Host "`n🌐 Checking Port Status..." -ForegroundColor Yellow
$port5000 = netstat -an | findstr ":5000"
$port5001 = netstat -an | findstr ":5001"

if ($port5000) {
    Write-Host "✅ Port 5000 is active" -ForegroundColor Green
} else {
    Write-Host "❌ Port 5000 is not active" -ForegroundColor Red
}

if ($port5001) {
    Write-Host "✅ Port 5001 is active" -ForegroundColor Green
} else {
    Write-Host "⚠️ Port 5001 is not active (HTTPS)" -ForegroundColor Yellow
}

# Test API endpoints
Write-Host "`n🔗 Testing API Endpoints..." -ForegroundColor Yellow

try {
    $configResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/excelgeocoding/configuration" -Method GET -TimeoutSec 5
    Write-Host "✅ Configuration endpoint is working" -ForegroundColor Green
    Write-Host "   Processing Limits: $($configResponse.processingLimits.maxAddressesPerFile) addresses per file" -ForegroundColor Cyan
    Write-Host "   Rate Limits: $($configResponse.rateLimit.requestsPerMinute) requests per minute" -ForegroundColor Cyan
} catch {
    Write-Host "❌ Configuration endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
}

try {
    $exportsResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/excelgeocoding/list-exports" -Method GET -TimeoutSec 5
    Write-Host "✅ Exports endpoint is working" -ForegroundColor Green
    Write-Host "   Available exports: $($exportsResponse.files.Count) files" -ForegroundColor Cyan
} catch {
    Write-Host "❌ Exports endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Check wwwroot files
Write-Host "`n📁 Checking Static Files..." -ForegroundColor Yellow
$wwwrootPath = "wwwroot"
$htmlFile = "wwwroot\excel-geocoding.html"
$exportsDir = "wwwroot\exports"

if (Test-Path $htmlFile) {
    Write-Host "✅ Web interface file exists" -ForegroundColor Green
} else {
    Write-Host "❌ Web interface file missing" -ForegroundColor Red
}

if (Test-Path $exportsDir) {
    $exportFiles = Get-ChildItem $exportsDir -Filter "*.xlsx" | Measure-Object
    Write-Host "✅ Exports directory exists with $($exportFiles.Count) Excel files" -ForegroundColor Green
} else {
    Write-Host "❌ Exports directory missing" -ForegroundColor Red
}

# Environment check
Write-Host "`n⚙️ Environment Information..." -ForegroundColor Yellow
$env = $env:ASPNETCORE_ENVIRONMENT
if ($env -eq "Production") {
    Write-Host "✅ Running in Production environment" -ForegroundColor Green
} else {
    Write-Host "⚠️ Environment: $env" -ForegroundColor Yellow
}

Write-Host "`n🎯 Access URLs:" -ForegroundColor Green
Write-Host "   Web Interface: http://localhost:5000/excel-geocoding.html" -ForegroundColor Cyan
Write-Host "   API Base: http://localhost:5000/api/excelgeocoding/" -ForegroundColor Cyan
Write-Host "   Live Logs: http://localhost:5000/api/excelgeocoding/live-logs" -ForegroundColor Cyan
Write-Host "   Configuration: http://localhost:5000/api/excelgeocoding/configuration" -ForegroundColor Cyan

Write-Host "`n✅ Production Status Check Complete!" -ForegroundColor Green 