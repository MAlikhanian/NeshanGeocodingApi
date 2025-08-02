# Test HTTP to HTTPS Redirect
Write-Host "🔒 Testing HTTP to HTTPS Redirect..." -ForegroundColor Green

# Test HTTP redirect
Write-Host "`n📡 Testing HTTP redirect..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost/api/excelgeocoding/configuration" -Method GET -MaximumRedirection 0 -ErrorAction SilentlyContinue
    if ($response.StatusCode -eq 307) {
        Write-Host "✅ HTTP redirect working! Status: 307 (Temporary Redirect)" -ForegroundColor Green
        Write-Host "   Redirect URL: $($response.Headers.Location)" -ForegroundColor Cyan
    } else {
        Write-Host "❌ HTTP redirect not working. Status: $($response.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ HTTP redirect test failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test HTTPS direct access
Write-Host "`n🔐 Testing HTTPS direct access..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "https://localhost/api/excelgeocoding/configuration" -Method GET -SkipCertificateCheck
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ HTTPS direct access working! Status: 200 (OK)" -ForegroundColor Green
    } else {
        Write-Host "❌ HTTPS direct access failed. Status: $($response.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ HTTPS direct access failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test browser redirect
Write-Host "`n🌐 Browser Test Instructions:" -ForegroundColor Yellow
Write-Host "1. Open browser and go to: http://localhost/excel-geocoding.html" -ForegroundColor Cyan
Write-Host "2. Should automatically redirect to: https://localhost/excel-geocoding.html" -ForegroundColor Cyan
Write-Host "3. Check the address bar for HTTPS" -ForegroundColor Cyan

Write-Host "`n✅ Redirect Test Complete!" -ForegroundColor Green 