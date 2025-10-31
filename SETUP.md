# 🚀 Quick Setup Guide

## Prerequisites
- .NET 10.0 SDK
- Azure AD application for Microsoft Graph
- Git configured with SSH keys

## Setup Steps

### 1. Clone Repository
```bash
git clone git@github.com:iamjonadev/JobbCoacherBE.git
cd JobbCoacherBE
```

### 2. Environment Configuration
```bash
# Copy the example environment file
cp .env.example .env

# Edit .env with your actual values:
# - AZUREAD_TENANTID: Your Azure AD tenant ID
# - AZUREAD_CLIENTID: Your Azure AD application client ID  
# - AZUREAD_CLIENTSECRET: Your Azure AD application secret
# - MAIL_FROM: The email address to send from
```

### 3. Install Dependencies
```bash
dotnet restore
```

### 4. Update Configuration
Edit `appsettings.json` to match your requirements:
- Update Company information
- Configure CORS AllowedOrigins for your frontend
- Adjust any other settings as needed

### 5. Run Application
```bash
# Development mode
dotnet run

# Or with hot reload
dotnet watch run
```

### 6. Test the API
The API will be available at:
- HTTPS: https://localhost:7001
- HTTP: http://localhost:5001

Test endpoints:
- GET `/api/contact/info` - API information
- GET `/api/contact/health` - Health check
- POST `/api/contact/submit` - Submit contact form

## Production Deployment

For production deployment, see the comprehensive [README.md](README.md) file which includes:
- Docker deployment instructions
- Security considerations
- Environment variable configuration
- CORS setup for production domains

## Security Notes
- ✅ `.env` file is automatically ignored by Git
- ✅ No sensitive data is committed to the repository
- ✅ All secrets should be configured in your environment
- ✅ Rate limiting and XSS protection are enabled

## Support
- Check the [README.md](README.md) for comprehensive documentation
- Review the [Docs/](Docs/) folder for detailed technical documentation
- Create GitHub issues for bugs or feature requests

---
**Ready to empower Swedish job coaching with modern technology! 🎯**