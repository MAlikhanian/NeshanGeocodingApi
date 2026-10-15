# Production deployment script for Neshan Geocoding API
param(
    [Parameter(Mandatory=$false)]
    [string]$Namespace = "alborzdc-neshan-geo-coding-api",
    
    [Parameter(Mandatory=$false)]
    [string]$ImageTag = "latest"
)

Write-Host "🚀 Deploying Neshan Geocoding API to Production..." -ForegroundColor Green
Write-Host "Namespace: $Namespace" -ForegroundColor Yellow
Write-Host "Image: alikhanian/neshangeocodingapi:$ImageTag" -ForegroundColor Yellow
Write-Host "Domain: geocode.alborzdcoffice.ir" -ForegroundColor Yellow

# Check prerequisites
Write-Host "`n📋 Checking prerequisites..." -ForegroundColor Cyan

# Check kubectl
try {
    $kubectlVersion = kubectl version --client --short 2>$null
    Write-Host "✓ kubectl is available" -ForegroundColor Green
} catch {
    Write-Host "✗ kubectl is not available. Please install kubectl first." -ForegroundColor Red
    exit 1
}

# Check cluster connectivity
try {
    $clusterInfo = kubectl cluster-info 2>$null
    Write-Host "✓ Kubernetes cluster is accessible" -ForegroundColor Green
} catch {
    Write-Host "✗ Cannot access Kubernetes cluster. Please check your kubeconfig." -ForegroundColor Red
    exit 1
}

# Check NGINX Ingress Controller
Write-Host "`n🔍 Checking NGINX Ingress Controller..." -ForegroundColor Cyan
$ingressController = kubectl get pods -n ingress-nginx 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "⚠️  NGINX Ingress Controller not found. Please install it first:" -ForegroundColor Yellow
    Write-Host "   kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.8.2/deploy/static/provider/cloud/deploy.yaml" -ForegroundColor Cyan
    exit 1
} else {
    Write-Host "✓ NGINX Ingress Controller is installed" -ForegroundColor Green
}

# Create namespace
Write-Host "`n📁 Creating namespace: $Namespace" -ForegroundColor Cyan
kubectl create namespace $Namespace 2>$null

# Install cert-manager if not present
Write-Host "`n🔐 Checking cert-manager installation..." -ForegroundColor Cyan
$certManagerInstalled = kubectl get pods -n cert-manager 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "⚠️  Installing cert-manager..." -ForegroundColor Yellow
    kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.2/cert-manager.yaml
    Write-Host "⏳ Waiting for cert-manager to be ready..." -ForegroundColor Yellow
    kubectl wait --for=condition=ready pod -l app.kubernetes.io/instance=cert-manager -n cert-manager --timeout=300s
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ cert-manager is ready" -ForegroundColor Green
    } else {
        Write-Host "✗ cert-manager installation failed" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "✓ cert-manager is already installed" -ForegroundColor Green
}

# Apply cert-manager ClusterIssuer
Write-Host "`n🔑 Applying cert-manager ClusterIssuer..." -ForegroundColor Cyan
kubectl apply -f k8s-cert-manager.yaml
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ ClusterIssuer applied successfully" -ForegroundColor Green
} else {
    Write-Host "✗ Failed to apply ClusterIssuer" -ForegroundColor Red
    exit 1
}

# Apply configuration
Write-Host "`n⚙️  Applying configuration..." -ForegroundColor Cyan
kubectl apply -f k8s-configmap.yaml -n $Namespace
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Configuration applied successfully" -ForegroundColor Green
} else {
    Write-Host "✗ Failed to apply configuration" -ForegroundColor Red
    exit 1
}

# Apply production deployment
Write-Host "`n🚀 Applying production deployment..." -ForegroundColor Cyan
kubectl apply -f k8s-production.yaml -n $Namespace
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Production deployment applied successfully" -ForegroundColor Green
} else {
    Write-Host "✗ Failed to apply production deployment" -ForegroundColor Red
    exit 1
}

# Wait for deployment to be ready
Write-Host "`n⏳ Waiting for deployment to be ready..." -ForegroundColor Yellow
kubectl wait --for=condition=available --timeout=300s deployment/neshangeocodingapi -n $Namespace

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Deployment is ready!" -ForegroundColor Green
    
    # Show deployment status
    Write-Host "`n📊 Deployment Status:" -ForegroundColor Cyan
    kubectl get deployments -n $Namespace
    
    Write-Host "`n🔄 Pod Status:" -ForegroundColor Cyan
    kubectl get pods -n $Namespace -l app=neshangeocodingapi
    
    Write-Host "`n🌐 Service Status:" -ForegroundColor Cyan
    kubectl get services -n $Namespace -l app=neshangeocodingapi
    
    Write-Host "`n🔗 Ingress Status:" -ForegroundColor Cyan
    kubectl get ingress -n $Namespace
    
    Write-Host "`n🔐 Certificate Status:" -ForegroundColor Cyan
    kubectl get certificate -n $Namespace
    
    # Show access information
    Write-Host "`n🌍 Access Information:" -ForegroundColor Yellow
    Write-Host "  Production URL: https://geocode.alborzdcoffice.ir" -ForegroundColor Green
    Write-Host "  Image: alikhanian/neshangeocodingapi:$ImageTag" -ForegroundColor Cyan
    
    # Get ingress IP
    $ingressIP = kubectl get ingress neshangeocodingapi-ingress -n $Namespace -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>$null
    if ($ingressIP) {
        Write-Host "  Ingress IP: $ingressIP" -ForegroundColor Cyan
        Write-Host "  Make sure your DNS points geocode.alborzdcoffice.ir to $ingressIP" -ForegroundColor Yellow
    }
    
    Write-Host "`n🛠️  Management Commands:" -ForegroundColor Yellow
    Write-Host "  View logs: kubectl logs -f deployment/neshangeocodingapi -n $Namespace" -ForegroundColor Cyan
    Write-Host "  Scale deployment: kubectl scale deployment neshangeocodingapi --replicas=5 -n $Namespace" -ForegroundColor Cyan
    Write-Host "  Check certificate: kubectl describe certificate neshangeocodingapi-tls -n $Namespace" -ForegroundColor Cyan
    Write-Host "  Port forward (testing): kubectl port-forward service/neshangeocodingapi-service 8080:80 -n $Namespace" -ForegroundColor Cyan
    
    Write-Host "`n✅ Production deployment completed successfully!" -ForegroundColor Green
    Write-Host "Your application will be available at: https://geocode.alborzdcoffice.ir" -ForegroundColor Green
    
} else {
    Write-Host "✗ Deployment failed to become ready within timeout" -ForegroundColor Red
    Write-Host "Check pod status with: kubectl get pods -n $Namespace" -ForegroundColor Yellow
    Write-Host "Check pod logs with: kubectl logs -l app=neshangeocodingapi -n $Namespace" -ForegroundColor Yellow
    exit 1
}
