#!/bin/bash
# Script này tạo ra các file appsettings.json cho các service dựa trên template chung và biến môi trường

# Đọc các biến từ .env
if [ -f .env ]; then
    export $(grep -v '^#' .env | xargs)
else
    echo "Error: .env file not found!"
    exit 1
fi

TEMPLATE_FILE="src/appsettings.template.json"
if [ ! -f "$TEMPLATE_FILE" ]; then
    echo "Error: Template file $TEMPLATE_FILE not found!"
    exit 1
fi

# Tạo thư mục Logs nếu chưa có
mkdir -p src/AuthService/AuthService.API/Logs
mkdir -p src/UserService/UserService.API/Logs
mkdir -p src/ContentService/ContentService.API/Logs
mkdir -p src/NotificaitonService/NotificaitonService.API/Logs


# Hàm tạo appsettings.json cho từng service
generate_appsettings() {
    SERVICE_NAME=$1
    DB_NAME=$2
    OUTPUT_DIR=$3
    
    echo "Generating appsettings.json for $SERVICE_NAME..."
    
    # Tạo file tạm thời
    TMP_FILE=$(mktemp)
    
    # Copy template
    cp $TEMPLATE_FILE $TMP_FILE
    
    # Thay thế các biến
    sed -i '' "s|Host=localhost|Host=$DB_HOST|g" $TMP_FILE
    sed -i '' "s|Port=5432|Port=$DB_PORT|g" $TMP_FILE
    sed -i '' "s|service_db|$DB_NAME|g" $TMP_FILE
    sed -i '' "s|db_user|$DB_USER|g" $TMP_FILE
    sed -i '' "s|db_password|$DB_PASSWORD|g" $TMP_FILE
    
    sed -i '' "s|your_jwt_secret_key|$JWT_SECRET_KEY|g" $TMP_FILE
    sed -i '' "s|\"Issuer\": \"Healink\"|\"Issuer\": \"$JWT_ISSUER\"|g" $TMP_FILE
    sed -i '' "s|\"Audience\": \"Healink.Users\"|\"Audience\": \"$JWT_AUDIENCE\"|g" $TMP_FILE
    sed -i '' "s|\"ExpiresInMinutes\": 60|\"ExpiresInMinutes\": $JWT_EXPIRES_MINUTES|g" $TMP_FILE
    
    sed -i '' "s|\"HostName\": \"localhost\"|\"HostName\": \"$RABBITMQ_HOST\"|g" $TMP_FILE
    sed -i '' "s|\"UserName\": \"guest\"|\"UserName\": \"$RABBITMQ_USER\"|g" $TMP_FILE
    sed -i '' "s|\"Password\": \"guest\"|\"Password\": \"$RABBITMQ_PASSWORD\"|g" $TMP_FILE
    sed -i '' "s|\"VirtualHost\": \"/\"|\"VirtualHost\": \"$RABBITMQ_VHOST\"|g" $TMP_FILE
    sed -i '' "s|\"ExchangeName\": \"healink_exchange\"|\"ExchangeName\": \"$RABBITMQ_EXCHANGE\"|g" $TMP_FILE
    
    sed -i '' "s|\"Host\": \"localhost\"|\"Host\": \"$REDIS_HOST\"|g" $TMP_FILE
    sed -i '' "s|\"Port\": 6379|\"Port\": $REDIS_PORT|g" $TMP_FILE
    sed -i '' "s|your_redis_password|$REDIS_PASSWORD|g" $TMP_FILE
    
    sed -i '' "s|http://localhost:5001|$AUTH_SERVICE_URL|g" $TMP_FILE
    sed -i '' "s|http://localhost:5002|$USER_SERVICE_URL|g" $TMP_FILE
    sed -i '' "s|http://localhost:5003|$CONTENT_SERVICE_URL|g" $TMP_FILE
    sed -i '' "s|http://localhost:5004|$NOTIFICATION_SERVICE_URL|g" $TMP_FILE
    sed -i '' "s|http://localhost:5010|$GATEWAY_URL|g" $TMP_FILE
    
    sed -i '' "s|your-email@gmail.com|$EMAIL_USERNAME|g" $TMP_FILE
    sed -i '' "s|your-app-password|$EMAIL_PASSWORD|g" $TMP_FILE
    sed -i '' "s|noreply@healink.com|$EMAIL_FROM|g" $TMP_FILE
    sed -i '' "s|Healink System|$EMAIL_FROM_NAME|g" $TMP_FILE
    
    sed -i '' "s|your_aws_access_key|$AWS_ACCESS_KEY|g" $TMP_FILE
    sed -i '' "s|your_aws_secret_key|$AWS_SECRET_KEY|g" $TMP_FILE
    sed -i '' "s|ap-southeast-1|$AWS_REGION|g" $TMP_FILE
    sed -i '' "s|your-s3-bucket-name|$AWS_S3_BUCKET|g" $TMP_FILE
    
    # Di chuyển file tạm thời đến thư mục đích
    mkdir -p $OUTPUT_DIR
    mv $TMP_FILE "$OUTPUT_DIR/appsettings.json"
    cp "$OUTPUT_DIR/appsettings.json" "$OUTPUT_DIR/appsettings.Development.json"
    
    echo "✅ Created appsettings.json for $SERVICE_NAME"
}

# Tạo appsettings.json cho từng service
generate_appsettings "AuthService" "$AUTH_DB_NAME" "src/AuthService/AuthService.API"
generate_appsettings "UserService" "$USER_DB_NAME" "src/UserService/UserService.API"
generate_appsettings "ContentService" "$CONTENT_DB_NAME" "src/ContentService/ContentService.API"
generate_appsettings "NotificationService" "$NOTIFICATION_DB_NAME" "src/NotificaitonService/NotificaitonService.API"

# Tạo appsettings.json cho Gateway
echo "Generating appsettings.json for Gateway..."
mkdir -p src/Gateway/Gateway.API
cp $TEMPLATE_FILE "src/Gateway/Gateway.API/appsettings.json"
cp $TEMPLATE_FILE "src/Gateway/Gateway.API/appsettings.Development.json"
echo "✅ Created appsettings.json for Gateway"

echo "Done! All appsettings.json files have been generated."
