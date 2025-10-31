# JobbCoacherBE 🎯

> **Professional contact form API for ABC Jobbcoacher - Empowering Swedish job coaching services with modern technology.**

[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Web%20API-green.svg)](https://docs.microsoft.com/en-us/aspnet/core/)
[![Microsoft Graph](https://img.shields.io/badge/Microsoft%20Graph-Email%20API-orange.svg)](https://graph.microsoft.com/)
[![Security](https://img.shields.io/badge/Security-Hardened-red.svg)](https://www.owasp.org/)

## 📋 Table of Contents

- [Overview](#-overview)
- [Features](#-features)
- [Architecture](#-architecture)
- [Prerequisites](#-prerequisites)
- [Installation](#-installation)
- [Configuration](#-configuration)
- [API Documentation](#-api-documentation)
- [Security](#-security)
- [Usage Examples](#-usage-examples)
- [Deployment](#-deployment)
- [Contributing](#-contributing)

## 🎯 Overview

JobbCoacherBE is a secure, enterprise-grade contact form API designed specifically for ABC Jobbcoacher's Swedish job coaching services. Built with modern .NET technologies and following security best practices, it provides a reliable backend for client contact forms with professional email integration.

### Key Highlights

- **🔐 Security Hardened**: XSS protection, rate limiting, input validation, and security headers
- **� Professional Email Integration**: Microsoft Graph API for reliable email delivery
- **🏗️ Clean Architecture**: Service-oriented design with proper separation of concerns
- **� Comprehensive Logging**: Structured logging for monitoring and debugging
- **🌐 CORS Ready**: Configured for cross-origin requests from frontend applications
- **🛡️ GDPR Compliant**: Privacy-focused data handling for Swedish regulations

## ✨ Features

### � Contact Form Management
- **Professional Contact Forms**: Comprehensive client information collection
- **File Upload Support**: CV and document attachment handling
- **Input Validation**: Server-side validation with Swedish language error messages
- **Data Sanitization**: XSS protection and HTML encoding for all inputs

### � Email Integration
- **Microsoft Graph API**: Enterprise-grade email delivery
- **Professional Templates**: HTML and plain text email formatting
- **Attachment Support**: Automatic file handling and validation
- **Delivery Confirmation**: Email status tracking and error handling

### � Security Features
- **Rate Limiting**: Protection against spam and abuse (3 submissions per hour)
- **XSS Protection**: Comprehensive input sanitization
- **Security Headers**: OWASP recommended security headers
- **Input Validation**: Multi-layer validation with error handling
- **Audit Logging**: Complete request and response logging

### 🏗️ Technical Excellence
- **Clean Architecture**: Controllers, Services, DTOs, and Helpers separation
- **Dependency Injection**: Proper IoC container configuration
- **Error Handling**: Comprehensive exception management
- **Performance Optimized**: Async/await patterns throughout
- **Documentation**: Comprehensive code documentation and API specs

## 🏗️ Architecture

JobbCoacherBE follows **Clean Architecture** principles with clear separation of concerns:

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Controllers   │ → │    Services     │ → │   External APIs │
│   (HTTP Layer)  │    │ (Business Logic)│    │ (Microsoft Graph)│
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │
         ▼                       ▼
┌─────────────────┐    ┌─────────────────┐
│      DTOs       │    │    Helpers      │
│ (Data Transfer) │    │   (Utilities)   │
└─────────────────┘    └─────────────────┘
```

### Core Components

- **ContactController**: RESTful API endpoints with validation and security
- **ContactEmailService**: Email content generation and formatting
- **HaidersGraphMailService**: Microsoft Graph API integration
- **Security Middleware**: XSS protection, rate limiting, and headers
- **DTOs**: Data transfer objects with validation attributes

## 🛠️ Prerequisites

- **.NET 10.0 SDK** or later
- **Microsoft Graph API Access** (Azure AD application)
- **Visual Studio 2022** or **VS Code** with C# extension
- **Azure Active Directory** tenant for email integration

## 📦 Installation

### 1. Clone the Repository

```bash
git clone git@github.com:iamjonadev/JobbCoacherBE.git
cd JobbCoacherBE
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Configure Environment

Copy the example environment file and configure your settings:

```bash
cp .env.example .env
```

Update `.env` with your configuration:

```env
# Azure AD / Microsoft Graph
AZUREAD_INSTANCE=https://login.microsoftonline.com/
AZUREAD_DOMAIN=your-domain.com
AZUREAD_TENANTID=your-tenant-id
AZUREAD_CLIENTID=your-client-id
AZUREAD_CLIENTSECRET=your-client-secret
AZUREAD_CALLBACKPATH=/signin-oidc

# Email Configuration
MAIL_FROM=your-email@domain.com
MAIL_SMTP=smtp.office365.com
MAIL_PORT=587
```

### 4. Update Configuration

Update `appsettings.json` with your specific settings:

```json
{
  "Company": {
    "Name": "ABC Jobbcoacher",
    "Email": "info@abcjobbcoacher.se",
    "Phone": "+46 XX XXX XX XX",
    "Website": "https://abc.adoteam.dev"
  },
  "Security": {
    "AllowedOrigins": [
      "https://abc.adoteam.dev",
      "https://localhost:3000"
    ]
  }
}
```

## 🚀 Running the Application

### Development Mode

```bash
# Start the development server
dotnet run

# Or with hot reload
dotnet watch run
```

The API will be available at:
- **HTTPS**: `https://localhost:7001`
- **HTTP**: `http://localhost:5001`

### Production Mode

```bash
# Build for production
dotnet build --configuration Release

# Run in production mode
dotnet run --configuration Release
```

## 📚 API Documentation

### Contact Form Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/contact/submit` | Submit contact form |
| `GET` | `/api/contact/info` | Get API information |
| `GET` | `/api/contact/health` | Health check endpoint |

### Contact Form Submission

**Endpoint**: `POST /api/contact/submit`

**Request Body**:
```json
{
  "name": "Anna Andersson",
  "email": "anna@example.com",
  "phone": "+46 70 123 45 67",
  "kommun": "Stockholm",
  "about": "Jag behöver hjälp med att hitta ett nytt jobb inom IT-sektorn...",
  "isRegisteredWithArbetsformedlingen": true,
  "hasWorkExperience": true,
  "cvFile": "base64-encoded-file-data"
}
```

**Response**:
```json
{
  "success": true,
  "message": "Tack för ditt meddelande! Vi återkommer inom 24 timmar.",
  "submissionId": "123e4567-e89b-12d3-a456-426614174000",
  "timestamp": "2024-12-31T15:30:00Z"
}
```

### Example Request

```bash
curl -X POST "https://your-api-domain.com/api/contact/submit" \
     -H "Content-Type: application/json" \
     -d '{
       "name": "Anna Andersson",
       "email": "anna@example.com", 
       "phone": "+46 70 123 45 67",
       "kommun": "Stockholm",
       "about": "Jag behöver hjälp med att hitta jobb",
       "isRegisteredWithArbetsformedlingen": true,
       "hasWorkExperience": true
     }'
```

## 🔐 Security

### Security Features Implemented

- **XSS Protection**: All inputs are HTML encoded and sanitized
- **Rate Limiting**: Maximum 3 submissions per hour per IP address
- **Input Validation**: Comprehensive server-side validation
- **Security Headers**: OWASP recommended headers implemented
- **CORS Protection**: Configured allowed origins only
- **File Upload Security**: Size and type validation for attachments

### Security Headers

```
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Referrer-Policy: strict-origin-when-cross-origin
Content-Security-Policy: default-src 'self'
```

### Rate Limiting

- **3 submissions per hour** per IP address
- **HTTP 429 Too Many Requests** response when limit exceeded
- **Automatic reset** after the time window expires

## 💻 Development

### Project Structure

```
JobbCoacherBE/
├── Controllers/              # API endpoints
│   └── ContactController.cs
├── Services/                 # Business logic
│   ├── ContactEmailService.cs
│   └── HaidersGraphMailService.cs
├── DTOs/                     # Data transfer objects
│   └── ContactFormDTO.cs
├── Middleware/               # Security middleware
│   ├── XssProtectionMiddleware.cs
│   ├── RateLimitingMiddleware.cs
│   └── SecurityHeadersMiddleware.cs
├── Helpers/                  # Utility classes
│   └── FileUploadHelper.cs
├── Docs/                     # Documentation
│   ├── SECURITY_REPORT.md
│   └── CODE_ORGANIZATION_REPORT.md
└── Configuration/            # App configuration
    └── (configuration files)
```

### Adding New Features

1. **Create DTOs** for new data structures
2. **Add validation attributes** for input validation
3. **Implement business logic** in Service classes
4. **Add controller endpoints** with proper error handling
5. **Update security middleware** as needed
6. **Add comprehensive logging**

### Code Standards

- **Follow Clean Architecture** principles
- **Use async/await** for all I/O operations
- **Implement comprehensive logging** with structured data
- **Add input validation** for all endpoints
- **Handle exceptions gracefully** with proper error responses
- **Follow OWASP security guidelines**

## 🚢 Deployment

### Docker Deployment

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["JobbCoacherBE.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JobbCoacherBE.dll"]
```

### Production Checklist

- [ ] **Environment variables** properly configured
- [ ] **Microsoft Graph permissions** granted
- [ ] **HTTPS certificates** installed and configured
- [ ] **CORS policies** set for production domains
- [ ] **Rate limiting** configured appropriately
- [ ] **Security headers** enabled
- [ ] **Logging configuration** for production monitoring
- [ ] **Health checks** enabled for monitoring

## 🧪 Testing

### Manual Testing

```bash
# Test contact form submission
curl -X POST "https://localhost:7001/api/contact/submit" \
     -H "Content-Type: application/json" \
     -d @test-contact-form.json

# Test health endpoint
curl -X GET "https://localhost:7001/api/contact/health"

# Test API info
curl -X GET "https://localhost:7001/api/contact/info"
```

### Security Testing

```bash
# Test XSS protection
curl -X POST "https://localhost:7001/api/contact/submit" \
     -H "Content-Type: application/json" \
     -d '{"name": "<script>alert(\"xss\")</script>", "email": "test@example.com"}'

# Test rate limiting (run multiple times quickly)
for i in {1..5}; do
  curl -X POST "https://localhost:7001/api/contact/submit" \
       -H "Content-Type: application/json" \
       -d @test-contact-form.json
done
```

## 📊 Monitoring & Logging

### Logging Features

- **Structured logging** with JSON format
- **Request/Response logging** with timing
- **Security event logging** for rate limiting and XSS attempts
- **Email delivery status** tracking
- **Error tracking** with stack traces

### Health Monitoring

- **Health check endpoint** at `/api/contact/health`
- **Microsoft Graph connectivity** testing
- **Application performance** metrics
- **Error rate monitoring**

## 🤝 Contributing

We welcome contributions! Please follow these guidelines:

1. **Fork the repository** and create a feature branch
2. **Follow coding standards** and Clean Architecture principles
3. **Add security considerations** for new functionality
4. **Update documentation** for any API changes
5. **Submit a pull request** with detailed description

### Development Setup

```bash
# Clone and setup development environment
git clone git@github.com:iamjonadev/JobbCoacherBE.git
cd JobbCoacherBE
dotnet restore
cp .env.example .env  # Update with your settings
dotnet run
```

## 📞 Support

For support and questions:

- **Documentation**: Check the `Docs/` folder for detailed documentation
- **Issues**: Create an issue on GitHub for bugs or feature requests
- **Security Issues**: Report security vulnerabilities responsibly

---

**Built with ❤️ for ABC Jobbcoacher - Empowering Swedish job seekers with professional coaching services**