# HaidersAPI - Contact Form & Authentication

## 🚀 Overview
HaidersAPI is a secure enterprise API built with the Adoteam Enterprise template, featuring a professional contact form system with CV file upload capabilities and comprehensive authentication.

## 📧 Contact Form Features
- **Secure Form Submission**: Multi-part form data with file upload support
- **CV File Upload**: Supports PDF, DOC, DOCX, TXT, RTF files (max 5MB)
- **Email Integration**: Microsoft Graph integration with Azure AD authentication
- **Professional Email Templates**: HTML and plain text email formatting
- **Comprehensive Validation**: Field validation with Swedish localization
- **Audit Trail**: Complete logging and tracking of submissions

## 🔐 Authentication System
- **External JWT Integration**: Uses https://api.adoteam.dev/auth/unified-login
- **Role-Based Authorization**: AdoteamOwner, AdoteamUser, AdoteamViewer policies
- **Token Management**: Access and refresh token handling
- **User Profile Management**: Profile retrieval and session management

## 🛠️ API Endpoints

### Contact Form
- `POST /api/contact/submit` - Submit contact form with CV file
- `POST /api/contact/test-email` - Test email connectivity
- `GET /api/contact/info` - Get form configuration info

### Authentication
- `POST /api/auth/login` - Login with external service
- `POST /api/auth/refresh` - Refresh access token
- `GET /api/auth/profile` - Get user profile
- `POST /api/auth/logout` - Logout user

### System
- `GET /api/config/test` - Test configuration
- `GET /api/database/test` - Test database connectivity

## 📝 Contact Form Fields
- **Name** (required): För- och efternamn
- **Email** (required): Valid email address
- **Phone** (required): Swedish phone number
- **Kommun** (required): Home municipality
- **About** (required): Self description
- **IsRegisteredAF** (required): "Ja" or "Nej" for Arbetsförmedlingen
- **AFRegistrationDate** (optional): Registration date if registered
- **CvFile** (optional): CV file upload

## 🔒 Security Features
- **File Validation**: Type and size restrictions for uploads
- **Input Sanitization**: XSS protection and validation
- **Rate Limiting**: Request throttling
- **IP Whitelisting**: Configurable IP restrictions
- **Audit Logging**: Complete request/response logging
- **CORS Configuration**: Secure cross-origin requests

## 📧 Email Configuration
Emails are sent to: `jona@adoteam.dev`
- **Professional HTML Templates**: Responsive design with Swedish text
- **Attachment Support**: CV files attached to emails
- **Reply-To Setup**: Emails set to reply to form submitter
- **Error Handling**: Robust error handling with user feedback

## 🧪 Testing with Postman
1. Import the collection: `postman/HaidersAPI_Contact_Collection.json`
2. Set base URL: `http://localhost:5170`
3. Test contact form submission with sample data
4. Verify email delivery to jona@adoteam.dev

## 🚀 Running the API
```bash
cd c:\AdoteamAB\api\HaidersAPI
dotnet run --urls="http://localhost:5170"
```

## 📊 Response Examples

### Successful Contact Form Submission
```json
{
  "isSuccess": true,
  "message": "Tack för din ansökan! Vi kommer att kontakta dig inom kort.",
  "submissionId": "A1B2C3D4",
  "submittedAt": "2024-01-15T10:30:00Z",
  "errors": []
}
```

### Validation Errors
```json
{
  "isSuccess": false,
  "message": "Formuläret innehåller fel",
  "submissionId": "E5F6G7H8",
  "submittedAt": "2024-01-15T10:30:00Z",
  "errors": [
    "För- och efternamn krävs",
    "Ogiltig e-postadress"
  ]
}
```

## 🔧 Configuration Requirements
- Database connection string in `.env`
- Azure AD credentials for Microsoft Graph
- Mail configuration for email service
- JWT secret for authentication

## 📈 Production Considerations
- Configure HTTPS certificates
- Set up proper Azure AD application
- Configure mail service credentials
- Enable production logging
- Set up file upload limits
- Configure CORS for production domains

---
**Built with Adoteam Enterprise Template** | **Secure by Design** | **Production Ready**