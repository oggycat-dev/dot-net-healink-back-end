#!/bin/bash

# ===================================
# Environment Variables Validation Script
# Validates that all required environment variables are set
# ===================================

echo "🔍 Validating Healink Microservices Environment Variables..."

# Track validation status
VALIDATION_FAILED=false

# Function to check required variable
check_required() {
    local var_name=$1
    local var_value=${!var_name}
    
    if [ -z "$var_value" ]; then
        echo "❌ MISSING: $var_name"
        VALIDATION_FAILED=true
    else
        echo "✅ OK: $var_name"
    fi
}

# Function to check optional variable with default
check_optional() {
    local var_name=$1
    local default_value=$2
    local var_value=${!var_name}
    
    if [ -z "$var_value" ]; then
        echo "⚠️  DEFAULT: $var_name (using default: $default_value)"
    else
        echo "✅ OK: $var_name = $var_value"
    fi
}

echo ""
echo "📊 DATABASE CONFIGURATION"
echo "=========================="
check_required "DB_HOST"
check_required "DB_USER" 
check_required "DB_PASSWORD"
check_required "AUTH_DB_NAME"
check_required "USER_DB_NAME"
check_optional "DB_PORT" "5432"

echo ""
echo "🔐 JWT CONFIGURATION"
echo "===================="
check_required "JWT_SECRET_KEY"
check_required "JWT_ISSUER"
check_required "JWT_AUDIENCE"
check_optional "JWT_ACCESS_TOKEN_EXPIRATION" "60"
check_optional "JWT_REFRESH_TOKEN_EXPIRATION" "7"

echo ""
echo "🐰 RABBITMQ CONFIGURATION"
echo "=========================="
check_required "RABBITMQ_USER"
check_required "RABBITMQ_PASSWORD"
check_optional "RABBITMQ_HOSTNAME" "rabbitmq"
check_optional "RABBITMQ_PORT" "5672"
check_optional "RABBITMQ_EXCHANGE" "healink.events"

echo ""
echo "🔴 REDIS CONFIGURATION"
echo "======================="
check_required "REDIS_PASSWORD"
check_optional "REDIS_HOST" "redis"
check_optional "REDIS_PORT" "6379"

echo ""
echo "👤 ADMIN CONFIGURATION"
echo "======================="
check_required "ADMIN_EMAIL"
check_required "ADMIN_PASSWORD"

echo ""
echo "🌐 ENVIRONMENT CONFIGURATION"
echo "============================="
check_optional "ASPNETCORE_ENVIRONMENT" "Development"
check_optional "DATA_ENABLE_AUTO_MIGRATIONS" "true"

# Validate JWT Secret Key length
if [ ! -z "$JWT_SECRET_KEY" ]; then
    JWT_KEY_LENGTH=${#JWT_SECRET_KEY}
    if [ $JWT_KEY_LENGTH -lt 32 ]; then
        echo "⚠️  WARNING: JWT_SECRET_KEY should be at least 32 characters long (current: $JWT_KEY_LENGTH)"
    fi
fi

# Validate password strength (basic check)
if [ ! -z "$DB_PASSWORD" ]; then
    if [ ${#DB_PASSWORD} -lt 8 ]; then
        echo "⚠️  WARNING: DB_PASSWORD should be at least 8 characters long"
    fi
fi

if [ ! -z "$ADMIN_PASSWORD" ]; then
    if [ ${#ADMIN_PASSWORD} -lt 8 ]; then
        echo "⚠️  WARNING: ADMIN_PASSWORD should be at least 8 characters long"
    fi
fi

echo ""
echo "📋 VALIDATION SUMMARY"
echo "====================="

if [ "$VALIDATION_FAILED" = true ]; then
    echo "❌ VALIDATION FAILED: Some required environment variables are missing"
    echo ""
    echo "📝 To fix this:"
    echo "1. Copy environment-variables-template.md content to .env file"
    echo "2. Update the values according to your environment" 
    echo "3. Run this script again to validate"
    echo ""
    echo "💡 Example:"
    echo "   cp environment-variables-template.md .env"
    echo "   # Edit .env file with your values"
    echo "   source .env"
    echo "   ./scripts/validate-env.sh"
    exit 1
else
    echo "✅ ALL VALIDATIONS PASSED"
    echo "🚀 Your environment is ready for Healink Microservices!"
    echo ""
    echo "🐳 Next steps:"
    echo "   docker-compose up -d"
    exit 0
fi
