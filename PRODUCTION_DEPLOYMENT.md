# 🚀 Production Deployment Guide

## 📋 Prerequisites

### Required Software:
- **.NET 9.0 Runtime** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **SQL Server** (LocalDB, Express, or Full)
- **IIS** (for Windows deployment) or **Nginx** (for Linux)

### Environment Variables:
```bash
# Database Connection
ConnectionStrings__DefaultConnection="Server=your-server;Database=NeshanGeocodingDb;Trusted_Connection=true;MultipleActiveResultSets=true"

# Neshan API Configuration
Neshan__ApiKey="your-api-key"
Neshan__RateLimit__RequestsPerMinute=60
Neshan__RateLimit__DelayBetweenRequests=1000
Neshan__ProcessingLimits__MaxAddressesPerFile=50
```

## 🏗️ Build for Production

### 1. Publish the Application
```bash
# Navigate to project directory
cd C:\Source Control\Neshan\NeshanGeocodingApi

# Build for production
dotnet publish -c Release -o ./publish

# Or with specific runtime
dotnet publish -c Release -r win-x64 --self-contained -o ./publish
```

### 2. Production Configuration
The application uses `appsettings.Production.json` with:
- **Increased processing limits** (50 addresses per file)
- **Optimized logging** (Warning level for framework)
- **Production endpoints** (HTTP/HTTPS)
- **Enhanced security** settings

## 🌐 Deployment Options

### Option 1: Self-Hosted (Recommended for Development)

#### Start the Application:
```bash
# Development
dotnet run

# Production
dotnet run --environment Production

# With specific port
dotnet run --environment Production --urls "http://localhost:5000;https://localhost:5001"
```

#### Access URLs:
- **Web Interface:** http://localhost:5000/excel-geocoding.html
- **API Endpoints:** http://localhost:5000/api/excelgeocoding/
- **Live Logs:** http://localhost:5000/api/excelgeocoding/live-logs

### Option 2: IIS Deployment (Windows)

#### 1. Install IIS and .NET Hosting Bundle
```powershell
# Install IIS
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole

# Install .NET Hosting Bundle
# Download from: https://dotnet.microsoft.com/download/dotnet/9.0
```

#### 2. Deploy to IIS
```bash
# Publish application
dotnet publish -c Release -o C:\inetpub\wwwroot\NeshanGeocodingApi

# Configure IIS site
# Point to: C:\inetpub\wwwroot\NeshanGeocodingApi
# Application Pool: .NET CLR Version "No Managed Code"
```

#### 3. Configure web.config
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\NeshanGeocodingApi.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
```

### Option 3: Docker Deployment

#### 1. Create Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["NeshanGeocodingApi.csproj", "./"]
RUN dotnet restore "NeshanGeocodingApi.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "NeshanGeocodingApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NeshanGeocodingApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NeshanGeocodingApi.dll"]
```

#### 2. Build and Run Docker
```bash
# Build image
docker build -t neshan-geocoding-api .

# Run container
docker run -d -p 8080:80 --name neshan-api neshan-geocoding-api

# Access at: http://localhost:8080
```

## 🔧 Production Configuration

### Environment Variables
```bash
# Set environment variables
setx ASPNETCORE_ENVIRONMENT "Production"
setx ConnectionStrings__DefaultConnection "Server=your-server;Database=NeshanGeocodingDb;Trusted_Connection=true;MultipleActiveResultSets=true"
setx Neshan__ApiKey "your-api-key"
```

### Configuration Files
- **appsettings.Production.json** - Production settings
- **appsettings.json** - Default settings
- **web.config** - IIS configuration (if using IIS)

### Security Considerations
1. **API Key Protection** - Use environment variables or Azure Key Vault
2. **HTTPS Only** - Configure SSL certificates
3. **CORS Settings** - Restrict to specific domains
4. **Rate Limiting** - Monitor and adjust based on usage
5. **File Storage** - Secure the exports directory

## 📊 Monitoring and Logging

### Application Logs
```bash
# View logs
dotnet run --environment Production --urls "http://localhost:5000" > logs.txt 2>&1

# Or use Windows Event Log
# Application logs are written to Windows Event Log in production
```

### Performance Monitoring
- **Live Log Stream** - Real-time API call monitoring
- **Rate Limit Tracking** - Monitor API usage
- **File Export Tracking** - Track generated files
- **Error Logging** - Comprehensive error tracking

### Health Checks
```bash
# Health check endpoint
curl http://localhost:5000/health

# Configuration endpoint
curl http://localhost:5000/api/excelgeocoding/configuration
```

## 🚀 Quick Start Commands

### Development
```bash
cd C:\Source Control\Neshan\NeshanGeocodingApi
dotnet run
# Access: http://localhost:5136
```

### Production
```bash
cd C:\Source Control\Neshan\NeshanGeocodingApi
dotnet run --environment Production --urls "http://localhost:5000;https://localhost:5001"
# Access: http://localhost:5000
```

### Build and Deploy
```bash
# Build for production
dotnet publish -c Release -o ./publish

# Run from published folder
cd publish
dotnet NeshanGeocodingApi.dll --environment Production
```

## 📁 File Structure
```
NeshanGeocodingApi/
├── publish/                    ← Production build output
├── wwwroot/
│   ├── exports/               ← Generated Excel files
│   └── excel-geocoding.html   ← Web interface
├── appsettings.Production.json ← Production config
├── appsettings.json           ← Default config
└── web.config                 ← IIS config (if needed)
```

## 🔍 Troubleshooting

### Common Issues:
1. **Port Already in Use** - Change port in `--urls` parameter
2. **Database Connection** - Verify SQL Server is running
3. **API Key Issues** - Check environment variables
4. **File Permissions** - Ensure write access to exports folder
5. **CORS Errors** - Configure CORS in production

### Debug Commands:
```bash
# Check if application is running
netstat -an | findstr :5000

# Check environment
echo %ASPNETCORE_ENVIRONMENT%

# Test API endpoints
curl http://localhost:5000/api/excelgeocoding/configuration
```

## 📈 Performance Optimization

### Production Settings:
- **Processing Limits:** 50 addresses per file (vs 20 in development)
- **Rate Limiting:** 60 requests per minute
- **Concurrent Requests:** 5 maximum
- **Logging Level:** Warning (reduced verbosity)
- **HTTPS:** Enabled for security

### Monitoring:
- **Live API Call Logs** - Real-time monitoring
- **Rate Limit Alerts** - Automatic notifications
- **File Export Tracking** - Monitor disk usage
- **Error Tracking** - Comprehensive error logging

The application is now running in production mode with optimized settings! 🎉 