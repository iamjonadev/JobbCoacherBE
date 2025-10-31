# HaidersAPI Deployment Guide

## 🚀 Deployment Summary

HaidersAPI has been successfully added to the AdoteamAB infrastructure and configured for deployment at **https://haiders.adoteam.dev**

## 📋 What Was Added

### 1. Docker Compose Configuration
- Added `haiders_api_service` to `docker-compose.yml`
- Configured environment variables for production
- Set up health checks and proper networking
- Container runs on port 5170

### 2. Nginx Reverse Proxy
- Created `reverse-proxy/nginx/conf.d/haiders.adoteam.dev.conf`
- Configured SSL termination and HTTPS redirection
- Added CORS support for web applications
- Special handling for file uploads (10MB max)
- Enhanced security headers

### 3. SSL Certificates
- Created SSL directory: `ssl/haiders.adoteam.dev/`
- Generated self-signed certificates for development
- Added README with certificate setup instructions

### 4. Docker Configuration
- Created `api/HaidersAPI/Dockerfile` for containerization
- Uses .NET 10.0 runtime and SDK
- Includes health checks and proper environment setup

## 🔧 Deployment Steps

### Step 1: DNS Configuration
Add an A record in your DNS provider (GoDaddy):
```
Type: A
Name: haiders
Value: [Your Server IP Address]
TTL: 600
```

### Step 2: SSL Certificate Setup
Replace the self-signed certificates with real ones:

#### Option A: ZeroSSL (Recommended)
1. Go to [ZeroSSL](https://zerossl.com)
2. Create a certificate for `haiders.adoteam.dev`
3. Download and place files:
   ```bash
   # Replace these files:
   ssl/haiders.adoteam.dev/certificate.crt
   ssl/haiders.adoteam.dev/private.key
   ```

#### Option B: Let's Encrypt
```bash
# Generate certificate
certbot certonly --standalone -d haiders.adoteam.dev

# Copy files
cp /etc/letsencrypt/live/haiders.adoteam.dev/fullchain.pem ssl/haiders.adoteam.dev/certificate.crt
cp /etc/letsencrypt/live/haiders.adoteam.dev/privkey.pem ssl/haiders.adoteam.dev/private.key
```

### Step 3: Environment Variables
Ensure your `.env` file contains all required variables:
```bash
# Azure AD Configuration (for email service)
AZUREAD_INSTANCE=https://login.microsoftonline.com/
AZUREAD_DOMAIN=adoteam.dev
AZUREAD_TENANTID=your-tenant-id
AZUREAD_CLIENTID=your-client-id
AZUREAD_CLIENTSECRET=your-client-secret
AZUREAD_CALLBACKPATH=/signin-oidc

# Mail Configuration
MAIL_FROM=jona@adoteam.dev

# Security
TOKEN_KEY=your-jwt-secret-key
```

### Step 4: Build and Deploy
```bash
# Build and start the services
docker-compose up -d --build haiders_api_service

# Restart reverse proxy to load new configuration
docker-compose restart reverse-proxy

# Check logs
docker-compose logs -f haiders_api_service
```

## 🧪 Testing

### Health Check
```bash
curl -k https://haiders.adoteam.dev/health
```

### API Endpoints
```bash
# Contact info
curl https://haiders.adoteam.dev/api/contact/info

# Email connectivity test
curl -X POST https://haiders.adoteam.dev/api/contact/test-email

# Contact form submission (without file)
curl -X POST "https://haiders.adoteam.dev/api/contact/submit" \
  -F "Name=Test User" \
  -F "Email=test@example.com" \
  -F "Phone=0701234567" \
  -F "Kommun=Stockholm" \
  -F "About=Test submission" \
  -F "IsRegisteredAF=Nej"
```

## 📊 Monitoring

### Container Status
```bash
docker ps | grep haiders
docker-compose ps haiders_api_service
```

### Logs
```bash
# View real-time logs
docker-compose logs -f haiders_api_service

# View nginx logs
docker-compose logs -f reverse-proxy
```

### Health Checks
```bash
# Internal health check
docker exec haiders_api_service curl http://localhost:5170/api/contact/info

# External health check
curl https://haiders.adoteam.dev/health
```

## 🔒 Security Features

- **HTTPS Only**: Automatic HTTP to HTTPS redirection
- **Security Headers**: CSP, XSS protection, frame options
- **CORS Configuration**: Restricted to adoteam.dev domains
- **File Upload Limits**: 10MB maximum file size
- **Rate Limiting**: Implemented via nginx
- **Input Validation**: Server-side validation for all endpoints

## 🌐 Available Endpoints

- **Contact Info**: `GET /api/contact/info`
- **Email Test**: `POST /api/contact/test-email`
- **Contact Form**: `POST /api/contact/submit`
- **Auth Endpoints**: `GET|POST /api/auth/*`
- **Health Check**: `GET /health`

## 🚨 Troubleshooting

### Common Issues

1. **SSL Certificate Errors**
   - Verify certificate files exist and have correct permissions
   - Check certificate validity: `openssl x509 -in ssl/haiders.adoteam.dev/certificate.crt -text -noout`

2. **Container Won't Start**
   - Check logs: `docker-compose logs haiders_api_service`
   - Verify environment variables in `.env` file
   - Ensure port 5170 is not in use

3. **Email Service Not Working**
   - Verify Azure AD credentials in `.env`
   - Check Microsoft Graph permissions
   - Test email connectivity endpoint

4. **DNS Not Resolving**
   - Verify A record in DNS provider
   - Check DNS propagation: `nslookup haiders.adoteam.dev`
   - Wait up to 24 hours for DNS propagation

## 📞 Support

If you encounter issues:
1. Check the logs: `docker-compose logs -f haiders_api_service`
2. Verify DNS and SSL configuration
3. Test individual endpoints with curl
4. Review environment variables and configuration

## ✅ Production Checklist

- [ ] DNS A record configured
- [ ] Real SSL certificates installed
- [ ] Environment variables configured
- [ ] Azure AD permissions granted
- [ ] Email service tested
- [ ] Container health checks passing
- [ ] Nginx configuration validated
- [ ] HTTPS redirection working
- [ ] File upload functionality tested
- [ ] CORS headers configured correctly