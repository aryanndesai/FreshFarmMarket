# Configuration Setup

## Required Configuration Files

This project requires `appsettings.json` with your API keys.

### Setup Instructions

1. **Copy the template:**

```bash
   copy appsettings.template.json appsettings.json
```

2. **Fill in your keys in `appsettings.json`:**
   - `GoogleReCaptcha:SiteKey` - Your reCAPTCHA Site Key
   - `GoogleReCaptcha:SecretKey` - Your reCAPTCHA Secret Key
   - `Email:SendGridApiKey` - Your SendGrid API Key
   - `Email:FromEmail` - Your verified sender email
   - `Email:FromName` - Your sender name

3. **NEVER commit `appsettings.json` to Git!**
   - It's already in `.gitignore`
   - Only commit `appsettings.template.json`

## Getting API Keys

### Google reCAPTCHA

1. Go to: https://www.google.com/recaptcha/admin
2. Register your site with reCAPTCHA v3
3. Copy Site Key and Secret Key

### SendGrid

1. Go to: https://sendgrid.com
2. Create account and verify email
3. Settings → API Keys → Create API Key
4. Copy the API key (starts with `SG.`)
5. Settings → Sender Authentication → Verify a Single Sender
6. Verify your sender email address

## Security Notes

- `appsettings.json` is git-ignored and contains your real keys
- `appsettings.template.json` is committed with placeholder values
- Never share your API keys publicly
