# Check deployment status script
param(
    [Parameter(Mandatory=$false)]
    [string]$Namespace = "alborzdc-neshan-geo-coding-api"
)

Write-Host "🔍 Checking Neshan Geocoding API Deployment Status..." -ForegroundColor Green
Write-Host "Namespace: $Namespace" -ForegroundColor Yellow

# Check namespace
Write-Host "`n📁 Namespace Status:" -ForegroundColor Cyan
kubectl get namespace $Namespace 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Namespace $Namespace not found" -ForegroundColor Red
    exit 1
}

# Check deployment
Write-Host "`n🚀 Deployment Status:" -ForegroundColor Cyan
kubectl get deployment neshangeocodingapi -n $Namespace -o wide

# Check pods
Write-Host "`n🔄 Pod Status:" -ForegroundColor Cyan
kubectl get pods -n $Namespace -l app=neshangeocodingapi -o wide

# Check service
Write-Host "`n🌐 Service Status:" -ForegroundColor Cyan
kubectl get service neshangeocodingapi-service -n $Namespace -o wide

# Check ingress
Write-Host "`n🔗 Ingress Status:" -ForegroundColor Cyan
kubectl get ingress neshangeocodingapi-ingress -n $Namespace -o wide

# Check certificate
Write-Host "`n🔐 Certificate Status:" -ForegroundColor Cyan
kubectl get certificate neshangeocodingapi-tls -n $Namespace -o wide 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "Certificate details:" -ForegroundColor Yellow
    kubectl describe certificate neshangeocodingapi-tls -n $Namespace | Select-String -Pattern "Status|Events|Not After"
}

# Check HPA
Write-Host "`n📈 HPA Status:" -ForegroundColor Cyan
kubectl get hpa neshangeocodingapi-hpa -n $Namespace -o wide 2>$null

# Check PVCs
Write-Host "`n💾 Storage Status:" -ForegroundColor Cyan
kubectl get pvc -n $Namespace

# Get ingress IP
Write-Host "`n🌍 Access Information:" -ForegroundColor Yellow
$ingressIP = kubectl get ingress neshangeocodingapi-ingress -n $Namespace -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>$null
if ($ingressIP) {
    Write-Host "  Ingress IP: $ingressIP" -ForegroundColor Green
    Write-Host "  Production URL: https://geocode.alborzdcoffice.ir" -ForegroundColor Green
    Write-Host "  DNS should point geocode.alborzdcoffice.ir to $ingressIP" -ForegroundColor Yellow
} else {
    Write-Host "  Ingress IP: Not assigned yet" -ForegroundColor Yellow
    Write-Host "  Production URL: https://geocode.alborzdcoffice.ir" -ForegroundColor Green
}

# Check pod logs for any errors
Write-Host "`n📋 Recent Pod Logs (last 10 lines):" -ForegroundColor Cyan
$pods = kubectl get pods -n $Namespace -l app=neshangeocodingapi -o jsonpath='{.items[0].metadata.name}' 2>$null
if ($pods) {
    kubectl logs $pods -n $Namespace --tail=10
} else {
    Write-Host "No pods found" -ForegroundColor Red
}

Write-Host "`n✅ Status check completed!" -ForegroundColor Green
