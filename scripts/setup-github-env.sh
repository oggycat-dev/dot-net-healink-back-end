#!/bin/bash
# Setup GitHub Secrets and Variables from env.txt
# Based on your actual .env file

set -e

echo "🔐 Setting up GitHub Secrets and Variables for Healink"
echo "=================================================="
echo ""

# Check if GitHub CLI is installed
if ! command -v gh &> /dev/null; then
    echo "❌ GitHub CLI (gh) is not installed!"
    echo "Install it first:"
    echo "  macOS: brew install gh"
    echo "  Linux: sudo apt install gh"
    echo "  Windows: winget install GitHub.cli"
    exit 1
fi

# Check if logged in
if ! gh auth status &> /dev/null; then
    echo "❌ Not logged in to GitHub CLI!"
    echo "Run: gh auth login"
    exit 1
fi

echo "✅ GitHub CLI is ready"
echo ""

# ==============================================
# GITHUB SECRETS (Sensitive data)
# ==============================================

echo "🔐 Adding GitHub Secrets..."
echo ""

# Database Secrets
echo "Adding database secrets..."
gh secret set DB_PASSWORD -b"admin@123"
gh secret set DB_USERNAME -b"admin"

# JWT Secrets
echo "Adding JWT secrets..."
gh secret set JWT_SECRET_KEY -b"HealinkMicroserviceSecretKeyIsLongEnoughToBeUsedWithJWT"
gh secret set JWT_ISSUER -b"Healink"
gh secret set JWT_AUDIENCE -b"Healink.Users"

# RabbitMQ Secrets
echo "Adding RabbitMQ secrets..."
gh secret set RABBITMQ_PASSWORD -b"admin@123"
gh secret set RABBITMQ_USER -b"admin"

# Redis Secrets
echo "Adding Redis secrets..."
gh secret set REDIS_PASSWORD -b"admin@123"

# Email SMTP Secrets
echo "Adding email secrets..."
gh secret set EMAIL_SENDER_PASSWORD -b"ezrn myqu nphw qqnz"
gh secret set EMAIL_SENDER_EMAIL -b"nguyenhoainamvt99@gmail.com"

# AWS S3 Secrets (⚠️ WARNING: These are EXPOSED!)
echo "⚠️  WARNING: Adding AWS S3 secrets (EXPOSED CREDENTIALS!)"
echo "⚠️  You MUST rotate these credentials immediately!"

# MoMo Payment Secrets
echo "Adding MoMo payment secrets..."
gh secret set MOMO_ACCESS_KEY -b"Tyz9FMyviI6mOYEn"
gh secret set MOMO_SECRET_KEY -b"ozTlfYxWaTPr3WrrlSvBvbKNvyc5fqCz"
gh secret set MOMO_PARTNER_CODE -b"MOMO8UK320250913_TEST"

# Security Secrets
echo "Adding security secrets..."
gh secret set PASSWORD_ENCRYPTION_KEY -b"K9ltF1d2jlYvLsaN6AdmiaPHY8qwqUIW"
gh secret set ADMIN_PASSWORD -b"admin@123"
gh secret set ADMIN_EMAIL -b"admin@healink.com"

echo "✅ All secrets added!"
echo ""

# ==============================================
# GITHUB VARIABLES (Public configuration)
# ==============================================

echo "🌍 Adding GitHub Variables..."
echo ""

# Application Variables
echo "Adding application variables..."
gh variable set APP_NAME -v"Healink"
gh variable set SUPPORT_EMAIL -v"healinksupport@gmail.com"

# Environment Variables
echo "Adding environment variables..."
gh variable set ENVIRONMENT -v"free"
gh variable set AWS_REGION -v"ap-southeast-2"
gh variable set PROJECT_NAME -v"healink-free"
gh variable set ECR_REGISTRY -v"855160720656.dkr.ecr.ap-southeast-2.amazonaws.com"

# Database Configuration (non-sensitive)
echo "Adding database configuration..."
gh variable set DB_PORT -v"5432"
gh variable set AUTH_DB_NAME -v"authservicedb"
gh variable set USER_DB_NAME -v"userservicedb"
gh variable set CONTENT_DB_NAME -v"contentservicedb"
gh variable set SUBSCRIPTION_DB_NAME -v"subscriptiondb"
gh variable set PAYMENT_DB_NAME -v"paymentdb"

# JWT Configuration (non-sensitive)
echo "Adding JWT configuration..."
gh variable set JWT_EXPIRES_IN_MINUTES -v"60"
gh variable set JWT_REFRESH_TOKEN_EXPIRES_IN_DAYS -v"7"

# RabbitMQ Configuration (non-sensitive)
echo "Adding RabbitMQ configuration..."
gh variable set RABBITMQ_PORT -v"5672"
gh variable set RABBITMQ_VHOST -v"/"
gh variable set RABBITMQ_EXCHANGE -v"healink_exchange"

# Redis Configuration (non-sensitive)
echo "Adding Redis configuration..."
gh variable set REDIS_PORT -v"6379"
gh variable set REDIS_DATABASE -v"0"

# AWS S3 Configuration (non-sensitive)
echo "Adding AWS S3 configuration..."
gh variable set AWS_S3_BUCKET_NAME -v"healink-upload-file"
gh variable set AWS_S3_REGION -v"ap-southeast-2"

# MoMo Configuration (non-sensitive)
echo "Adding MoMo configuration..."
gh variable set MOMO_API_ENDPOINT -v"https://test-payment.momo.vn/v2/gateway/api"
gh variable set MOMO_PARTNER_NAME -v"Healink"
gh variable set MOMO_STORE_ID -v"HealinkStore"

# CORS Configuration
echo "Adding CORS configuration..."
gh variable set CORS_ALLOWED_ORIGINS -v"http://localhost:3000,http://localhost:3001,http://localhost:5173,http://localhost:4200"
gh variable set CORS_ALLOWED_METHODS -v"GET,POST,PUT,DELETE,OPTIONS"

# Service URLs (will be updated after deployment)
echo "Adding service URL placeholders..."
gh variable set AUTH_SERVICE_URL -v"http://authservice-api"
gh variable set USER_SERVICE_URL -v"http://userservice-api"
gh variable set CONTENT_SERVICE_URL -v"http://contentservice-api"
gh variable set SUBSCRIPTION_SERVICE_URL -v"http://subscription-api"
gh variable set PAYMENT_SERVICE_URL -v"http://payment-api"
gh variable set PODCAST_RECOMMENDATION_SERVICE_URL -v"http://podcastrecommendation-api"

echo "✅ All variables added!"
echo ""

# ==============================================
# VERIFICATION
# ==============================================

echo "🔍 Verifying setup..."
echo ""

echo "📋 Secrets list:"
gh secret list

echo ""
echo "📋 Variables list:"
gh variable list

echo ""
echo "=================================================="
echo "✅ GITHUB ENVIRONMENT SETUP COMPLETE!"
echo "=================================================="
echo ""
echo "📊 Summary:"
echo "  🔐 Secrets: $(gh secret list --json name | jq length) items"
echo "  🌍 Variables: $(gh variable list --json name | jq length) items"
echo ""
echo "⚠️  CRITICAL SECURITY WARNING:"
echo "  AWS S3 credentials have been EXPOSED!"
echo "  You MUST rotate them immediately:"
echo "  1. Go to: https://console.aws.amazon.com/iam/home#/security_credentials"
echo "  3. Create new Access Key"
echo "  4. Update AWS_S3_ACCESS_KEY and AWS_S3_SECRET_KEY secrets"
echo ""
echo "🚀 Next steps:"
echo "  1. Rotate AWS credentials (CRITICAL!)"
echo "  2. Update workflow to use these secrets/variables"
echo "  3. Test deployment"
echo ""
echo "📚 Documentation:"
echo "  - Secrets: https://github.com/oggycat-dev/dot-net-healink-back-end/settings/secrets/actions"
echo "  - Variables: https://github.com/oggycat-dev/dot-net-healink-back-end/settings/variables/actions"
echo ""
