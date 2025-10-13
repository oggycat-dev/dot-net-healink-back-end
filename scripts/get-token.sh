#!/bin/bash
# Get admin token
TOKEN=$(curl -s -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@healink.com",
    "password": "admin@123"
  }' | grep -o '"accessToken":"[^"]*"' | cut -d'"' -f4)

if [ -n "$TOKEN" ]; then
  echo "Token: $TOKEN"
  echo "Testing upload..."
  curl -X POST http://localhost:5002/api/FileUpload/avatar \
    -H "Accept: text/plain" \
    -H "Authorization: Bearer $TOKEN" \
    -F "file=@test-image.png"
else
  echo "Failed to get token"
fi
