# Configuration Setup

## Initial Setup

1. Copy the template files to create your local configuration:

   ```bash
   cp appsettings.template.json appsettings.json
   cp appsettings.Development.template.json appsettings.Development.json
   ```

2. Edit `appsettings.json` and replace the placeholder values:
   - `YOUR_RECAPTCHA_SITE_KEY_HERE` - Your Google reCAPTCHA site key
   - `YOUR_RECAPTCHA_SECRET_KEY_HERE` - Your Google reCAPTCHA secret key
   - `YOUR_SENDGRID_API_KEY_HERE` - Your SendGrid API key
   - `your-email@example.com` - Your sender email address

## Important Notes

- **NEVER commit** `appsettings.json` or `appsettings.Development.json` - they are in `.gitignore`
- Only the `.template.json` files should be committed to Git
- Keep your API keys and secrets secure
