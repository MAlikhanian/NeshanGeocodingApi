# Kubernetes deployment script for Neshan Geocoding API
param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("basic", "production")]
    [string]$Mode = "basic",
    
    [Parameter(Mandatory=$false)]
    [string]$Namespace = "alborzDC",
    
    [Parameter(Mandatory=$false)]
    [string]$ImageTag = "latest"
)

Write-Host "Deploying Neshan Geocoding API to Kubernetes..." -ForegroundColor Green
Write-Host "Mode: $Mode" -ForegroundColor Yellow
Write-Host "Namespace: $Namespace" -ForegroundColor Yellow
Write-Host "Image Tag: $ImageTag" -ForegroundColor Yellow

# Check if kubectl is available
try {
    $kubectlVersion = kubectl version --client --short 2>$null
    Write-Host "✓ kubectl is available" -ForegroundColor Green
} catch {
    Write-Host "✗ kubectl is not available. Please install kubectl first." -ForegroundColor Red
    exit 1
}

# Check if cluster is accessible
try {
    $clusterInfo = kubectl cluster-info 2>$null
    Write-Host "✓ Kubernetes cluster is accessible" -ForegroundColor Green
} catch {
    Write-Host "✗ Cannot access Kubernetes cluster. Please check your kubeconfig." -ForegroundColor Red
    exit 1
}

# Create namespace if it doesn't exist
Write-Host "Creating namespace: $Namespace" -ForegroundColor Yellow
kubectl create namespace $Namespace 2>$null

# Check if cert-manager is installed
Write-Host "Checking cert-manager installation..." -ForegroundColor Yellow
$certManagerInstalled = kubectl get pods -n cert-manager 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "⚠️  cert-manager is not installed. Installing cert-manager..." -ForegroundColor Yellow
    kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.2/cert-manager.yaml
    Write-Host "Waiting for cert-manager to be ready..." -ForegroundColor Yellow
    kubectl wait --for=condition=ready pod -l app.kubernetes.io/instance=cert-manager -n cert-manager --timeout=300s
} else {
    Write-Host "✓ cert-manager is already installed" -ForegroundColor Green
}

# Select deployment file based on mode
$deploymentFile = if ($Mode -eq "production") { "k8s-production.yaml" } else { "k8s-deployment.yaml" }

Write-Host "Using deployment file: $deploymentFile" -ForegroundColor Yellow

# Apply cert-manager ClusterIssuer
Write-Host "Applying cert-manager configuration..." -ForegroundColor Green
kubectl apply -f k8s-cert-manager.yaml

# Apply the deployment
Write-Host "Applying Kubernetes manifests..." -ForegroundColor Green
kubectl apply -f $deploymentFile -n $Namespace

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Deployment applied successfully!" -ForegroundColor Green
    
    # Wait for deployment to be ready
    Write-Host "Waiting for deployment to be ready..." -ForegroundColor Yellow
    kubectl wait --for=condition=available --timeout=300s deployment/neshangeocodingapi -n $Namespace
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Deployment is ready!" -ForegroundColor Green
        
        # Show deployment status
        Write-Host "`nDeployment Status:" -ForegroundColor Cyan
        kubectl get deployments -n $Namespace
        
        Write-Host "`nPod Status:" -ForegroundColor Cyan
        kubectl get pods -n $Namespace -l app=neshangeocodingapi
        
        Write-Host "`nService Status:" -ForegroundColor Cyan
        kubectl get services -n $Namespace -l app=neshangeocodingapi
        
        Write-Host "`nIngress Status:" -ForegroundColor Cyan
        kubectl get ingress -n $Namespace
        
        # Show access information
        Write-Host "`nAccess Information:" -ForegroundColor Yellow
        $serviceIP = kubectl get service neshangeocodingapi-service -n $Namespace -o jsonpath='{.spec.clusterIP}' 2>$null
        if ($serviceIP) {
            Write-Host "  Cluster IP: $serviceIP" -ForegroundColor Cyan
        }
        
        $ingressHost = kubectl get ingress neshangeocodingapi-ingress -n $Namespace -o jsonpath='{.spec.rules[0].host}' 2>$null
        if ($ingressHost) {
            Write-Host "  Ingress Host: https://$ingressHost" -ForegroundColor Cyan
        }
        
        # Check certificate status
        Write-Host "`nCertificate Status:" -ForegroundColor Yellow
        kubectl get certificate -n $Namespace 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  Certificate will be automatically issued by cert-manager" -ForegroundColor Green
        }
        
        Write-Host "`nUseful Commands:" -ForegroundColor Yellow
        Write-Host "  View logs: kubectl logs -f deployment/neshangeocodingapi -n $Namespace" -ForegroundColor Cyan
        Write-Host "  Scale deployment: kubectl scale deployment neshangeocodingapi --replicas=3 -n $Namespace" -ForegroundColor Cyan
        Write-Host "  Port forward: kubectl port-forward service/neshangeocodingapi-service 8080:80 -n $Namespace" -ForegroundColor Cyan
        Write-Host "  Delete deployment: kubectl delete -f $deploymentFile -n $Namespace" -ForegroundColor Cyan
        
    } else {
        Write-Host "✗ Deployment failed to become ready within timeout" -ForegroundColor Red
        Write-Host "Check pod status with: kubectl get pods -n $Namespace" -ForegroundColor Yellow
    }
} else {
    Write-Host "✗ Failed to apply deployment" -ForegroundColor Red
}

Write-Host "`nDeployment completed!" -ForegroundColor Green
