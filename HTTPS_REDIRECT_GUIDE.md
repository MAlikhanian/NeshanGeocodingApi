# 🔒 HTTP to HTTPS Redirect Guide

## 🎯 **Quick Start Commands**

### **1. Navigate to Project Directory:**
```cmd
cd "C:\Source Control\Neshan\NeshanGeocodingApi"
```

### **2. Run with HTTP to HTTPS Redirect:**
```cmd
dotnet run --environment Production --urls "http://*:80;https://*:443"
```

## 📋 **Complete Step-by-Step Process**

### **Step 1: Open Command Prompt as Administrator**
1. Press `Windows + R`
2. Type `cmd`
3. Press `Ctrl + Shift + Enter` (to run as Administrator)

### **Step 2: Navigate to Project Directory**
```cmd
cd "C:\Source Control\Neshan\NeshanGeocodingApi"
```

### **Step 3: Install Development SSL Certificate (First Time Only)**
```cmd
dotnet dev-certs https --trust
```

### **Step 4: Run Application with HTTPS Redirect**
```cmd
dotnet run --environment Production --urls "http://*:80;https://*:443"
```

## 🌐 **Access URLs**

### **HTTP (Will Redirect to HTTPS):**
- http://localhost/excel-geocoding.html → **Redirects to** → https://localhost/excel-geocoding.html
- http://localhost/api/excelgeocoding/ → **Redirects to** → https://localhost/api/excelgeocoding/

### **HTTPS (Direct Access):**
- **Web Interface:** https://localhost/excel-geocoding.html
- **API Base:** https://localhost/api/excelgeocoding/
- **Live Logs:** https://localhost/api/excelgeocoding/live-logs
- **Configuration:** https://localhost/api/excelgeocoding/configuration

## ⚙️ **Configuration Details**

### **Production Settings Applied:**
```json
{
  "HttpsRedirection": {
    "RedirectStatusCode": 307,
    "HttpsPort": 443
  },
  "Kestrel": {
    "Endpoints": {
      "Http": { "Url": "http://*:80" },
      "Https": { "Url": "https://*:443" }
    }
  }
}
```

### **Program.cs Configuration:**
```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // In production, redirect to HTTPS
    app.UseHttpsRedirection();
}
```

## 🔧 **Alternative Commands**

### **Standard Ports:**
```cmd
dotnet run --environment Production --urls "http://*:5000;https://*:5001"
```

### **Custom Ports:**
```cmd
dotnet run --environment Production --urls "http://*:8080;https://*:8443"
```

### **HTTP Only (No Redirect):**
```cmd
dotnet run --environment Production --urls "http://*:80"
```

### **HTTPS Only:**
```cmd
dotnet run --environment Production --urls "https://*:443"
```

## 🔍 **Testing HTTPS Redirect**

### **Test HTTP Redirect:**
```cmd
curl -I http://localhost/api/excelgeocoding/configuration
```
**Expected Response:** `HTTP/1.1 307 Temporary Redirect`

### **Test HTTPS Direct Access:**
```cmd
curl https://localhost/api/excelgeocoding/configuration
```
**Expected Response:** JSON configuration data

### **Browser Testing:**
1. Open browser
2. Navigate to: `http://localhost/excel-geocoding.html`
3. Should automatically redirect to: `https://localhost/excel-geocoding.html`

## ⚠️ **Important Notes**

### **1. Administrator Privileges Required:**
Ports 80 and 443 are privileged ports and require Administrator privileges.

### **2. SSL Certificate:**
- **Development:** Use `dotnet dev-certs https --trust`
- **Production:** Install proper SSL certificate

### **3. Firewall Settings:**
Allow the application through Windows Firewall for ports 80 and 443.

### **4. Network Access:**
Using `*` instead of `localhost` makes the application accessible from other devices on the network.

## 🚀 **Recommended Command:**

```cmd
cd "C:\Source Control\Neshan\NeshanGeocodingApi"
dotnet run --environment Production --urls "http://*:80;https://*:443"
```

## 📊 **What Happens:**

1. **HTTP Requests** (port 80) → **Automatically Redirect** → **HTTPS** (port 443)
2. **HTTPS Requests** (port 443) → **Direct Access**
3. **All traffic** → **Encrypted via SSL/TLS**

## 🔒 **Security Benefits:**

- ✅ **Automatic HTTPS redirect**
- ✅ **SSL/TLS encryption**
- ✅ **Secure API communication**
- ✅ **Browser security compliance**
- ✅ **Production-ready security**

The application will now automatically redirect all HTTP traffic to HTTPS for enhanced security! 🔒 