# Build and test Docker image in HTTP-only mode
Write-Host "Building Docker image for HTTP-only mode..." -ForegroundColor Green

# Stop and remove existing container if it exists
Write-Host "Cleaning up existing container..." -ForegroundColor Yellow
docker stop neshangeocodingapi-test 2>$null
docker rm neshangeocodingapi-test 2>$null

# Build the image
Write-Host "Building Docker image..." -ForegroundColor Green
docker build -t neshangeocodingapi .

if ($LASTEXITCODE -eq 0) {
    Write-Host "Docker image built successfully!" -ForegroundColor Green
    
    Write-Host "Running container in HTTP-only mode..." -ForegroundColor Green
    docker run -d -p 8080:8080 --name neshangeocodingapi-test neshangeocodingapi
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Container started successfully!" -ForegroundColor Green
        Write-Host "Application is available at: http://localhost:8080" -ForegroundColor Yellow
        
        # Wait for application to start
        Write-Host "Waiting for application to start..." -ForegroundColor Yellow
        Start-Sleep -Seconds 10
        
        # Check container logs
        Write-Host "`nContainer logs:" -ForegroundColor Cyan
        docker logs neshangeocodingapi-test
        
        # Test HTTP endpoint
        Write-Host "`nTesting HTTP endpoint..." -ForegroundColor Green
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:8080" -UseBasicParsing -TimeoutSec 10
            Write-Host "✓ HTTP endpoint working: $($response.StatusCode)" -ForegroundColor Green
        } catch {
            Write-Host "✗ HTTP endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        Write-Host "`nApplication URL: http://localhost:8080" -ForegroundColor Yellow
        Write-Host "`nContainer management commands:" -ForegroundColor Cyan
        Write-Host "  Stop:  docker stop neshangeocodingapi-test" -ForegroundColor Cyan
        Write-Host "  Remove: docker rm neshangeocodingapi-test" -ForegroundColor Cyan
        Write-Host "  Logs:  docker logs -f neshangeocodingapi-test" -ForegroundColor Cyan
    }
    else {
        Write-Host "Failed to start container!" -ForegroundColor Red
    }
}
else {
    Write-Host "Failed to build Docker image!" -ForegroundColor Red
}
