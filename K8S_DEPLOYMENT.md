# Kubernetes Deployment Guide for Neshan Geocoding API

This guide provides instructions for deploying the Neshan Geocoding API to a Kubernetes cluster.

## Prerequisites

- Kubernetes cluster (v1.19+)
- kubectl configured to access your cluster
- Docker image built and available in your cluster's registry
- Persistent storage class available in your cluster
- NGINX Ingress Controller installed
- cert-manager installed (will be installed automatically by the script)
- Domain `geocode.alborzdcoffice.ir` pointing to your cluster's ingress IP

## Quick Deployment

### Option 1: Using the PowerShell Script (Recommended)

```powershell
# Basic deployment
.\deploy-k8s.ps1

# Production deployment
.\deploy-k8s.ps1 -Mode production

# Deploy to specific namespace
.\deploy-k8s.ps1 -Namespace "alborzDC" -Mode production
```

### Option 2: Manual Deployment

```bash
# Apply cert-manager ClusterIssuer
kubectl apply -f k8s-cert-manager.yaml

# Apply basic deployment
kubectl apply -f k8s-deployment.yaml

# Apply production deployment
kubectl apply -f k8s-production.yaml

# Apply configuration
kubectl apply -f k8s-configmap.yaml
```

## Deployment Files

### k8s-deployment.yaml
- Basic deployment with 2 replicas
- Simple resource limits
- Basic health checks
- Suitable for development/testing

### k8s-production.yaml
- Production-ready deployment with 3 replicas
- Enhanced resource limits and requests
- Horizontal Pod Autoscaler (HPA)
- Rolling update strategy
- Security context
- Extended health checks

### k8s-configmap.yaml
- Environment configuration
- Secrets management
- Application settings

### k8s-cert-manager.yaml
- Let's Encrypt ClusterIssuer configuration
- Automatic SSL certificate management
- Production and staging certificate issuers

## Components Deployed

1. **Deployment**: Manages the application pods
2. **Service**: Exposes the application within the cluster
3. **PersistentVolumeClaim**: Provides storage for SQLite database and exports
4. **Ingress**: Routes external HTTPS traffic to the service
5. **Certificate**: Automatic SSL certificate from Let's Encrypt
6. **ClusterIssuer**: cert-manager configuration for certificate management
7. **HorizontalPodAutoscaler**: Automatically scales pods based on resource usage

## Configuration

### Environment Variables
- `ASPNETCORE_ENVIRONMENT`: Set to "Docker"
- `ASPNETCORE_URLS`: Set to "http://+:8080"

### Resource Limits
- **Basic**: 256Mi-512Mi memory, 250m-500m CPU
- **Production**: 512Mi-1Gi memory, 250m-1000m CPU

### Storage
- **Database**: 1-2Gi persistent storage
- **Exports**: 5-10Gi persistent storage

## Accessing the Application

### Production Access (HTTPS)
Your application will be available at:
**https://geocode.alborzdcoffice.ir**

The SSL certificate will be automatically issued by Let's Encrypt via cert-manager.

### Port Forward (for testing)
```bash
kubectl port-forward service/neshangeocodingapi-service 8080:80 -n alborzDC
```
Then access: http://localhost:8080

### Domain Configuration
Make sure your domain `geocode.alborzdcoffice.ir` points to your cluster's ingress controller IP address.

## Monitoring and Management

### View Logs
```bash
kubectl logs -f deployment/neshangeocodingapi -n alborzDC
```

### Scale Deployment
```bash
kubectl scale deployment neshangeocodingapi --replicas=5 -n alborzDC
```

### Check Status
```bash
kubectl get pods -l app=neshangeocodingapi -n alborzDC
kubectl get services -l app=neshangeocodingapi -n alborzDC
kubectl get ingress -n alborzDC
kubectl get certificate -n alborzDC
kubectl get certificaterequest -n alborzDC
```

### Update Image
```bash
kubectl set image deployment/neshangeocodingapi neshangeocodingapi=your-registry/neshangeocodingapi:v2.0.0 -n alborzDC
```

## Troubleshooting

### Common Issues

1. **Pods not starting**
   ```bash
   kubectl describe pod <pod-name> -n alborzDC
   kubectl logs <pod-name> -n alborzDC
   ```

2. **Storage issues**
   ```bash
   kubectl get pvc -n alborzDC
   kubectl describe pvc neshangeocodingapi-data-pvc -n alborzDC
   ```

3. **Service not accessible**
   ```bash
   kubectl get endpoints neshangeocodingapi-service -n alborzDC
   kubectl describe service neshangeocodingapi-service -n alborzDC
   ```

4. **SSL Certificate issues**
   ```bash
   kubectl describe certificate neshangeocodingapi-tls -n alborzDC
   kubectl describe certificaterequest -n alborzDC
   kubectl get clusterissuer
   ```

### Health Checks
The application includes:
- **Liveness Probe**: Checks if the application is running
- **Readiness Probe**: Checks if the application is ready to serve traffic

## Security Considerations

### Production Deployment
- Uses non-root user (UID 1000)
- Drops all capabilities
- Disables privilege escalation
- Resource limits prevent resource exhaustion

### Secrets Management
- API keys stored in Kubernetes secrets
- Base64 encoded (not encrypted by default)
- Consider using external secret management for production

### SSL Certificate Management
- Automatic SSL certificates via Let's Encrypt
- cert-manager handles certificate issuance and renewal
- Production and staging certificate issuers available
- Certificates are automatically renewed before expiration

## Cleanup

To remove the deployment:
```bash
kubectl delete -f k8s-deployment.yaml
# or
kubectl delete -f k8s-production.yaml
kubectl delete -f k8s-configmap.yaml
kubectl delete -f k8s-cert-manager.yaml
# or delete the entire namespace
kubectl delete namespace alborzDC
```

## Customization

### Modify Resource Limits
Edit the `resources` section in the deployment YAML:
```yaml
resources:
  requests:
    memory: "1Gi"
    cpu: "500m"
  limits:
    memory: "2Gi"
    cpu: "1000m"
```

### Change Storage Size
Edit the PVC specifications:
```yaml
resources:
  requests:
    storage: 20Gi
```

### Update Environment Variables
Add or modify environment variables in the deployment:
```yaml
env:
- name: CUSTOM_VAR
  value: "custom-value"
```

## Support

For issues or questions:
1. Check the application logs
2. Verify cluster resources
3. Ensure storage classes are available
4. Check network policies if applicable
