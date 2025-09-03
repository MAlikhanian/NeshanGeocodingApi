# Test script for Docker container with HTTPS support
Write-Host "Checking for SSL certificate..." -ForegroundColor Green

# Check if certificate exists, if not generate it
if (!(Test-Path "certs\aspnetcore.pfx")) {
    Write-Host "SSL certificate not found. Generating one..." -ForegroundColor Yellow
    .\generate-cert.ps1
}

Write-Host "Building Docker image..." -ForegroundColor Green
docker build -t neshangeocodingapi .

if ($LASTEXITCODE -eq 0) {
    Write-Host "Docker image built successfully!" -ForegroundColor Green
    
    Write-Host "Running Docker container with HTTPS support..." -ForegroundColor Green
    docker run -d -p 8080:8080 -p 8443:8443 --name neshangeocodingapi-test neshangeocodingapi
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Container started successfully!" -ForegroundColor Green
        Write-Host "Application is available at:" -ForegroundColor Yellow
        Write-Host "  HTTP:  http://localhost:8080" -ForegroundColor Cyan
        Write-Host "  HTTPS: https://localhost:8443" -ForegroundColor Green
        
        # Wait a moment for the app to start
        Start-Sleep -Seconds 5
        
        # Test the HTTP endpoint
        Write-Host "`nTesting HTTP endpoint..." -ForegroundColor Green
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:8080" -UseBasicParsing
            Write-Host "HTTP endpoint is responding! Status: $($response.StatusCode)" -ForegroundColor Green
        }
        catch {
            Write-Host "HTTP endpoint might still be starting up." -ForegroundColor Yellow
        }
        
        # Test the HTTPS endpoint
        Write-Host "Testing HTTPS endpoint..." -ForegroundColor Green
        try {
            # Skip certificate validation for self-signed cert
            $response = Invoke-WebRequest -Uri "https://localhost:8443" -UseBasicParsing -SkipCertificateCheck
            Write-Host "HTTPS endpoint is responding! Status: $($response.StatusCode)" -ForegroundColor Green
        }
        catch {
            Write-Host "HTTPS endpoint might still be starting up or certificate issue." -ForegroundColor Yellow
            Write-Host "Note: You may need to accept the self-signed certificate in your browser." -ForegroundColor Yellow
        }
        
        Write-Host "`nContainer management commands:" -ForegroundColor Cyan
        Write-Host "  Stop:  docker stop neshangeocodingapi-test" -ForegroundColor Cyan
        Write-Host "  Remove: docker rm neshangeocodingapi-test" -ForegroundColor Cyan
        Write-Host "  Logs:  docker logs neshangeocodingapi-test" -ForegroundColor Cyan
    }
    else {
        Write-Host "Failed to start container!" -ForegroundColor Red
    }
}
else {
    Write-Host "Failed to build Docker image!" -ForegroundColor Red
}
