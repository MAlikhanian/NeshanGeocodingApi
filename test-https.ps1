# Test HTTPS configuration specifically
Write-Host "Testing HTTPS configuration..." -ForegroundColor Green

# Stop and remove existing container if it exists
Write-Host "Cleaning up existing container..." -ForegroundColor Yellow
docker stop neshangeocodingapi-test 2>$null
docker rm neshangeocodingapi-test 2>$null

# Ensure certificate exists
if (!(Test-Path "certs\aspnetcore.pfx")) {
    Write-Host "Generating SSL certificate..." -ForegroundColor Yellow
    .\generate-cert.ps1
}

# Build and run with explicit port mapping
Write-Host "Building and running container with HTTPS..." -ForegroundColor Green
docker build -t neshangeocodingapi .
docker run -d -p 8080:8080 -p 8443:8443 --name neshangeocodingapi-test neshangeocodingapi

# Wait for application to start
Write-Host "Waiting for application to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Check container logs
Write-Host "`nContainer logs:" -ForegroundColor Cyan
docker logs neshangeocodingapi-test

# Test endpoints
Write-Host "`nTesting endpoints..." -ForegroundColor Green

# Test HTTP
try {
    $httpResponse = Invoke-WebRequest -Uri "http://localhost:8080" -UseBasicParsing -TimeoutSec 10
    Write-Host "✓ HTTP endpoint working: $($httpResponse.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "✗ HTTP endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test HTTPS
try {
    $httpsResponse = Invoke-WebRequest -Uri "https://localhost:8443" -UseBasicParsing -SkipCertificateCheck -TimeoutSec 10
    Write-Host "✓ HTTPS endpoint working: $($httpsResponse.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "✗ HTTPS endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nApplication URLs:" -ForegroundColor Yellow
Write-Host "  HTTP:  http://localhost:8080" -ForegroundColor Cyan
Write-Host "  HTTPS: https://localhost:8443" -ForegroundColor Green

Write-Host "`nTo view live logs: docker logs -f neshangeocodingapi-test" -ForegroundColor Cyan
