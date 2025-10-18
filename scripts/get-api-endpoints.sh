#!/bin/bash

# ============================================
# Get API Endpoints for Frontend Team
# ============================================
# This script retrieves the deployed Gateway URL
# and generates configuration files for Frontend

set -e

ENVIRONMENT="${1:-free}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
APP_INFRA_DIR="$PROJECT_ROOT/terraform_healink/app-infra"

echo "╔════════════════════════════════════════════════════════════╗"
echo "║        🌐 Get API Endpoints for Frontend Team             ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""

# Check if terraform is installed
if ! command -v terraform &> /dev/null; then
    echo "❌ Terraform is not installed"
    echo "   Install: https://www.terraform.io/downloads"
    exit 1
fi

# Navigate to app-infra directory
cd "$APP_INFRA_DIR"

# Initialize and select workspace
echo "🔧 Initializing Terraform..."
terraform init -reconfigure > /dev/null 2>&1

echo "📦 Selecting workspace: $ENVIRONMENT"
terraform workspace select "$ENVIRONMENT" > /dev/null 2>&1

# Get Gateway URL
echo "🌐 Fetching Gateway endpoint..."
GATEWAY_URL=$(terraform output -raw gateway_url 2>/dev/null)

if [ -z "$GATEWAY_URL" ] || [ "$GATEWAY_URL" == "null" ]; then
    echo "❌ Gateway URL not found"
    echo "   Make sure you have deployed the infrastructure"
    exit 1
fi

echo ""
echo "✅ Gateway URL: $GATEWAY_URL"
echo ""

# Create output directory
OUTPUT_DIR="$PROJECT_ROOT/api-endpoints"
mkdir -p "$OUTPUT_DIR"

# Create JSON file
JSON_FILE="$OUTPUT_DIR/api-endpoints.json"
cat > "$JSON_FILE" << EOF
{
  "environment": "$ENVIRONMENT",
  "fetchedAt": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "gatewayUrl": "$GATEWAY_URL",
  "endpoints": {
    "base": "$GATEWAY_URL",
    "health": "$GATEWAY_URL/health",
    "auth": "$GATEWAY_URL/api/auth",
    "users": "$GATEWAY_URL/api/users",
    "content": "$GATEWAY_URL/api/content",
    "notifications": "$GATEWAY_URL/api/notifications",
    "subscriptions": "$GATEWAY_URL/api/subscriptions",
    "payments": "$GATEWAY_URL/api/payments",
    "recommendations": "$GATEWAY_URL/api/recommendations"
  }
}
EOF

echo "✅ Created: $JSON_FILE"

# Create React .env file
REACT_ENV_FILE="$OUTPUT_DIR/.env.production.react"
cat > "$REACT_ENV_FILE" << EOF
# Generated on $(date -u +%Y-%m-%d\ %H:%M:%S\ UTC)
# Environment: $ENVIRONMENT

REACT_APP_API_URL=$GATEWAY_URL
REACT_APP_API_GATEWAY=$GATEWAY_URL
REACT_APP_AUTH_URL=$GATEWAY_URL/api/auth
REACT_APP_USER_URL=$GATEWAY_URL/api/users
REACT_APP_CONTENT_URL=$GATEWAY_URL/api/content
REACT_APP_NOTIFICATION_URL=$GATEWAY_URL/api/notifications
REACT_APP_SUBSCRIPTION_URL=$GATEWAY_URL/api/subscriptions
REACT_APP_PAYMENT_URL=$GATEWAY_URL/api/payments
REACT_APP_RECOMMENDATION_URL=$GATEWAY_URL/api/recommendations
REACT_APP_ENVIRONMENT=$ENVIRONMENT
EOF

echo "✅ Created: $REACT_ENV_FILE"

# Create Next.js .env file
NEXT_ENV_FILE="$OUTPUT_DIR/.env.production.next"
cat > "$NEXT_ENV_FILE" << EOF
# Generated on $(date -u +%Y-%m-%d\ %H:%M:%S\ UTC)
# Environment: $ENVIRONMENT

NEXT_PUBLIC_API_URL=$GATEWAY_URL
NEXT_PUBLIC_API_GATEWAY=$GATEWAY_URL
NEXT_PUBLIC_AUTH_URL=$GATEWAY_URL/api/auth
NEXT_PUBLIC_USER_URL=$GATEWAY_URL/api/users
NEXT_PUBLIC_CONTENT_URL=$GATEWAY_URL/api/content
NEXT_PUBLIC_NOTIFICATION_URL=$GATEWAY_URL/api/notifications
NEXT_PUBLIC_SUBSCRIPTION_URL=$GATEWAY_URL/api/subscriptions
NEXT_PUBLIC_PAYMENT_URL=$GATEWAY_URL/api/payments
NEXT_PUBLIC_RECOMMENDATION_URL=$GATEWAY_URL/api/recommendations
NEXT_PUBLIC_ENVIRONMENT=$ENVIRONMENT
EOF

echo "✅ Created: $NEXT_ENV_FILE"

# Create Vue.js .env file
VUE_ENV_FILE="$OUTPUT_DIR/.env.production.vue"
cat > "$VUE_ENV_FILE" << EOF
# Generated on $(date -u +%Y-%m-%d\ %H:%M:%S\ UTC)
# Environment: $ENVIRONMENT

VUE_APP_API_URL=$GATEWAY_URL
VUE_APP_API_GATEWAY=$GATEWAY_URL
VUE_APP_AUTH_URL=$GATEWAY_URL/api/auth
VUE_APP_USER_URL=$GATEWAY_URL/api/users
VUE_APP_CONTENT_URL=$GATEWAY_URL/api/content
VUE_APP_NOTIFICATION_URL=$GATEWAY_URL/api/notifications
VUE_APP_SUBSCRIPTION_URL=$GATEWAY_URL/api/subscriptions
VUE_APP_PAYMENT_URL=$GATEWAY_URL/api/payments
VUE_APP_RECOMMENDATION_URL=$GATEWAY_URL/api/recommendations
VUE_APP_ENVIRONMENT=$ENVIRONMENT
EOF

echo "✅ Created: $VUE_ENV_FILE"

# Create TypeScript constants file
TS_FILE="$OUTPUT_DIR/api-config.ts"
cat > "$TS_FILE" << EOF
/**
 * API Configuration
 * Generated on $(date -u +%Y-%m-%d\ %H:%M:%S\ UTC)
 * Environment: $ENVIRONMENT
 */

export const API_CONFIG = {
  environment: '$ENVIRONMENT',
  gatewayUrl: '$GATEWAY_URL',
  endpoints: {
    base: '$GATEWAY_URL',
    health: '$GATEWAY_URL/health',
    auth: '$GATEWAY_URL/api/auth',
    users: '$GATEWAY_URL/api/users',
    content: '$GATEWAY_URL/api/content',
    notifications: '$GATEWAY_URL/api/notifications',
    subscriptions: '$GATEWAY_URL/api/subscriptions',
    payments: '$GATEWAY_URL/api/payments',
    recommendations: '$GATEWAY_URL/api/recommendations',
  },
} as const;

export default API_CONFIG;
EOF

echo "✅ Created: $TS_FILE"

# Create README
README_FILE="$OUTPUT_DIR/README.md"
cat > "$README_FILE" << EOF
# API Endpoints - Frontend Configuration

Generated on: $(date -u +%Y-%m-%d\ %H:%M:%S\ UTC)  
Environment: **$ENVIRONMENT**  
Gateway URL: **$GATEWAY_URL**

## 📦 Files Included

- \`api-endpoints.json\` - JSON configuration
- \`.env.production.react\` - React environment variables
- \`.env.production.next\` - Next.js environment variables
- \`.env.production.vue\` - Vue.js environment variables
- \`api-config.ts\` - TypeScript configuration

## 🚀 Quick Start

### React / Create React App

\`\`\`bash
cp .env.production.react /path/to/your/frontend/.env.production
\`\`\`

### Next.js

\`\`\`bash
cp .env.production.next /path/to/your/frontend/.env.production
\`\`\`

### Vue.js

\`\`\`bash
cp .env.production.vue /path/to/your/frontend/.env.production
\`\`\`

### TypeScript/JavaScript

\`\`\`bash
cp api-config.ts /path/to/your/frontend/src/config/
\`\`\`

## 🧪 Test Endpoint

\`\`\`bash
curl $GATEWAY_URL/health
\`\`\`

## 📋 Available Endpoints

| Service | Endpoint |
|---------|----------|
| Health | $GATEWAY_URL/health |
| Auth | $GATEWAY_URL/api/auth |
| Users | $GATEWAY_URL/api/users |
| Content | $GATEWAY_URL/api/content |
| Notifications | $GATEWAY_URL/api/notifications |
| Subscriptions | $GATEWAY_URL/api/subscriptions |
| Payments | $GATEWAY_URL/api/payments |
| Recommendations | $GATEWAY_URL/api/recommendations |

## 🔄 Update Endpoints

To regenerate this configuration:

\`\`\`bash
cd /path/to/backend
./scripts/get-api-endpoints.sh $ENVIRONMENT
\`\`\`
EOF

echo "✅ Created: $README_FILE"

echo ""
echo "╔════════════════════════════════════════════════════════════╗"
echo "║                    ✅ SUCCESS!                             ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""
echo "📁 Files created in: $OUTPUT_DIR/"
echo ""
echo "📋 Quick commands:"
echo ""
echo "   # View JSON"
echo "   cat $OUTPUT_DIR/api-endpoints.json"
echo ""
echo "   # Copy React .env"
echo "   cp $OUTPUT_DIR/.env.production.react /path/to/frontend/.env.production"
echo ""
echo "   # Test endpoint"
echo "   curl $GATEWAY_URL/health"
echo ""
echo "🌐 Gateway URL: $GATEWAY_URL"
echo ""

