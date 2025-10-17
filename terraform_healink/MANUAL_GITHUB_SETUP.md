# 🔧 Manual GitHub Environment Setup

Nếu không có GitHub CLI, bạn có thể setup thủ công qua Web UI.

---

## 🔐 GitHub Secrets (Sensitive Data)

### Cách thêm:
1. Vào: https://github.com/oggycat-dev/dot-net-healink-back-end/settings/secrets/actions
2. Click **"New repository secret"**
3. Add từng secret:

### Database Secrets
```
Name: DB_PASSWORD
Value: admin@123

Name: DB_USERNAME  
Value: admin
```

### JWT Secrets
```
Name: JWT_SECRET_KEY
Value: HealinkMicroserviceSecretKeyIsLongEnoughToBeUsedWithJWT

Name: JWT_ISSUER
Value: Healink

Name: JWT_AUDIENCE
Value: Healink.Users
```

### RabbitMQ Secrets
```
Name: RABBITMQ_PASSWORD
Value: admin@123

Name: RABBITMQ_USER
Value: admin
```

### Redis Secrets
```
Name: REDIS_PASSWORD
Value: admin@123
```

### Email SMTP Secrets
```
Name: EMAIL_SENDER_PASSWORD
Value: ezrn myqu nphw qqnz

Name: EMAIL_SENDER_EMAIL
Value: nguyenhoainamvt99@gmail.com
```

### AWS S3 Secrets ⚠️ EXPOSED!
```
Name: AWS_S3_ACCESS_KEY

Name: AWS_S3_SECRET_KEY
```

**⚠️ CRITICAL: Rotate these AWS credentials immediately!**

### MoMo Payment Secrets
```
Name: MOMO_ACCESS_KEY
Value: Tyz9FMyviI6mOYEn

Name: MOMO_SECRET_KEY
Value: ozTlfYxWaTPr3WrrlSvBvbKNvyc5fqCz

Name: MOMO_PARTNER_CODE
Value: MOMO8UK320250913_TEST
```

### Security Secrets
```
Name: PASSWORD_ENCRYPTION_KEY
Value: K9ltF1d2jlYvLsaN6AdmiaPHY8qwqUIW

Name: ADMIN_PASSWORD
Value: admin@123

Name: ADMIN_EMAIL
Value: admin@healink.com
```

---

## 🌍 GitHub Variables (Public Configuration)

### Cách thêm:
1. Vào: https://github.com/oggycat-dev/dot-net-healink-back-end/settings/variables/actions
2. Click tab **"Variables"**
3. Click **"New repository variable"**
4. Add từng variable:

### Application Variables
```
Name: APP_NAME
Value: Healink

Name: SUPPORT_EMAIL
Value: healinksupport@gmail.com
```

### Environment Variables
```
Name: ENVIRONMENT
Value: free

Name: AWS_REGION
Value: ap-southeast-2

Name: PROJECT_NAME
Value: healink-free

Name: ECR_REGISTRY
Value: 855160720656.dkr.ecr.ap-southeast-2.amazonaws.com
```

### Database Configuration
```
Name: DB_PORT
Value: 5432

Name: AUTH_DB_NAME
Value: authservicedb

Name: USER_DB_NAME
Value: userservicedb

Name: CONTENT_DB_NAME
Value: contentservicedb

Name: SUBSCRIPTION_DB_NAME
Value: subscriptiondb

Name: PAYMENT_DB_NAME
Value: paymentdb
```

### JWT Configuration
```
Name: JWT_EXPIRES_IN_MINUTES
Value: 60

Name: JWT_REFRESH_TOKEN_EXPIRES_IN_DAYS
Value: 7
```

### RabbitMQ Configuration
```
Name: RABBITMQ_PORT
Value: 5672

Name: RABBITMQ_VHOST
Value: /

Name: RABBITMQ_EXCHANGE
Value: healink_exchange
```

### Redis Configuration
```
Name: REDIS_PORT
Value: 6379

Name: REDIS_DATABASE
Value: 0
```

### AWS S3 Configuration
```
Name: AWS_S3_BUCKET_NAME
Value: healink-upload-file

Name: AWS_S3_REGION
Value: ap-southeast-2
```

### MoMo Configuration
```
Name: MOMO_API_ENDPOINT
Value: https://test-payment.momo.vn/v2/gateway/api

Name: MOMO_PARTNER_NAME
Value: Healink

Name: MOMO_STORE_ID
Value: HealinkStore
```

### CORS Configuration
```
Name: CORS_ALLOWED_ORIGINS
Value: http://localhost:3000,http://localhost:3001,http://localhost:5173,http://localhost:4200

Name: CORS_ALLOWED_METHODS
Value: GET,POST,PUT,DELETE,OPTIONS
```

### Service URLs (Placeholders)
```
Name: AUTH_SERVICE_URL
Value: http://authservice-api

Name: USER_SERVICE_URL
Value: http://userservice-api

Name: CONTENT_SERVICE_URL
Value: http://contentservice-api

Name: SUBSCRIPTION_SERVICE_URL
Value: http://subscription-api

Name: PAYMENT_SERVICE_URL
Value: http://payment-api

Name: PODCAST_RECOMMENDATION_SERVICE_URL
Value: http://podcastrecommendation-api
```

---

## 📊 Summary

### Secrets (Sensitive): 15 items
- Database: 2 secrets
- JWT: 3 secrets  
- RabbitMQ: 2 secrets
- Redis: 1 secret
- Email: 2 secrets
- AWS S3: 2 secrets ⚠️
- MoMo: 3 secrets
- Security: 3 secrets

### Variables (Public): 25 items
- Application: 2 variables
- Environment: 4 variables
- Database: 6 variables
- JWT: 2 variables
- RabbitMQ: 3 variables
- Redis: 2 variables
- AWS S3: 2 variables
- MoMo: 3 variables
- CORS: 2 variables
- Service URLs: 6 variables

---

## ⚠️ CRITICAL SECURITY WARNING

**AWS S3 credentials have been EXPOSED in your env.txt file!**

**IMMEDIATE ACTION REQUIRED:**
1. Go to: https://console.aws.amazon.com/iam/home#/security_credentials
2. Find Access Key: `AKIA4OG4NBUIBJ4MVLM6`

**Why this is critical:**
- They can upload/download/delete files
- They can create EC2 instances and charge your account
- They can access other AWS services if permissions allow

---

## 🚀 After Setup

1. **Rotate AWS credentials** (CRITICAL!)
2. **Update workflow** to use these secrets/variables
3. **Test deployment**
4. **Monitor logs** for any missing variables

---

## 🔧 Quick Commands

### Using GitHub CLI (if available):
```bash
./scripts/setup-github-env.sh
```

### Manual verification:
```bash
# Check secrets
gh secret list

# Check variables  
gh variable list
```

---

## 📚 Useful Links

- **Secrets**: https://github.com/oggycat-dev/dot-net-healink-back-end/settings/secrets/actions
- **Variables**: https://github.com/oggycat-dev/dot-net-healink-back-end/settings/variables/actions
- **AWS IAM Console**: https://console.aws.amazon.com/iam/home#/security_credentials
- **Workflow**: https://github.com/oggycat-dev/dot-net-healink-back-end/actions
