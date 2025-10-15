#!/bin/bash
# Generate secure random secrets for GitHub Secrets
# Usage: ./scripts/generate-secrets.sh

set -e

echo "🔐 Generating secure random secrets for Healink..."
echo ""
echo "================================================"
echo "  COPY these values to GitHub Secrets"
echo "  Settings → Secrets and variables → Actions"
echo "================================================"
echo ""

# Database
echo "### 1️⃣ Database Secrets ###"
echo "DB_USERNAME=healink_admin"
echo "DB_PASSWORD=$(openssl rand -base64 32 | tr -d '=+/' | cut -c1-32)"
echo ""

# JWT
echo "### 2️⃣ JWT & Authentication ###"
echo "JWT_SECRET=$(openssl rand -base64 48 | tr -d '\n')"
echo "JWT_ISSUER=https://healink.com"
echo "JWT_AUDIENCE=https://healink.com"
echo ""

# RabbitMQ
echo "### 3️⃣ RabbitMQ (Amazon MQ) ###"
echo "RABBITMQ_USERNAME=healink_mq"
echo "RABBITMQ_PASSWORD=$(openssl rand -base64 32 | tr -d '=+/' | cut -c1-32)"
echo ""

# Email (Optional)
echo "### 4️⃣ Email Service (Optional) ###"
echo "SMTP_HOST=smtp.gmail.com"
echo "SMTP_PORT=587"
echo "SMTP_USERNAME=noreply@healink.com"
echo "SMTP_PASSWORD=$(openssl rand -base64 32 | tr -d '=+/' | cut -c1-32)"
echo ""

# AWS S3 (Optional)
echo "### 5️⃣ AWS S3 (Optional) ###"
echo "AWS_S3_BUCKET=healink-media-storage-free"
echo "AWS_S3_REGION=ap-southeast-2"
echo ""

echo "================================================"
echo "✅ Secrets generated successfully!"
echo ""
echo "📝 Next steps:"
echo "  1. Copy secrets above"
echo "  2. Go to: https://github.com/oggycat-dev/dot-net-healink-back-end/settings/secrets/actions"
echo "  3. Click 'New repository secret'"
echo "  4. Add each secret with name and value"
echo "  5. Run deployment workflow"
echo ""
echo "⚠️  SECURITY:"
echo "  - DO NOT commit these secrets to Git"
echo "  - DO NOT share via email/chat"
echo "  - Store in password manager if needed"
echo "================================================"

