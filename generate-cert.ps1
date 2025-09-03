# Generate self-signed certificate for HTTPS in Docker container
Write-Host "Generating self-signed certificate for HTTPS..." -ForegroundColor Green

# Create certs directory if it doesn't exist
if (!(Test-Path "certs")) {
    New-Item -ItemType Directory -Path "certs" -Force
    Write-Host "Created certs directory" -ForegroundColor Yellow
}

# Generate self-signed certificate
$certPassword = "YourSecurePassword123!"
$certPath = "certs\aspnetcore.pfx"

try {
    # Create certificate with simpler parameters
    $cert = New-SelfSignedCertificate -DnsName "localhost" -CertStoreLocation "cert:\CurrentUser\My" -KeyUsage DigitalSignature, KeyEncipherment -KeySpec KeyExchange -KeyLength 2048 -KeyAlgorithm RSA -HashAlgorithm SHA256 -NotAfter (Get-Date).AddYears(1) -FriendlyName "NeshanGeocodingApi"

    # Export certificate to PFX file
    $securePassword = ConvertTo-SecureString -String $certPassword -Force -AsPlainText
    Export-PfxCertificate -Cert $cert -FilePath $certPath -Password $securePassword | Out-Null

    Write-Host "Certificate generated successfully at: $certPath" -ForegroundColor Green
    Write-Host "Certificate password: $certPassword" -ForegroundColor Yellow

    # Clean up certificate from store
    Remove-Item -Path "cert:\CurrentUser\My\$($cert.Thumbprint)" -Force -ErrorAction SilentlyContinue

    Write-Host "`nCertificate is ready for Docker container!" -ForegroundColor Green
    Write-Host "The certificate will be copied to the container at: /app/certs/aspnetcore.pfx" -ForegroundColor Cyan
}
catch {
    Write-Host "Error generating certificate: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Creating a dummy certificate file for testing..." -ForegroundColor Yellow
    
    # Create a dummy file if certificate generation fails
    New-Item -ItemType File -Path $certPath -Force | Out-Null
    Write-Host "Dummy certificate file created. HTTPS may not work properly." -ForegroundColor Yellow
}
