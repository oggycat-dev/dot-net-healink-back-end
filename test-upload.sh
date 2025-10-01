#!/bin/bash

# Create a simple test token (this is just for testing, not secure)
TEST_TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIwMDAwMDAwMC0wMDAwLTAwMDAtMDAwMC0wMDAwMDAwMDAwMDEiLCJlbWFpbCI6ImRhdGRhbmgwMzAxQGdtYWlsLmNvbSIsInJvbGVzIjpbIlVzZXIiXSwibmJmIjoxNzM1Njg0NDAzLCJleHAiOjE3MzU2ODgwMDMsImlzcyI6IkhlYWxpbmsiLCJhdWQiOiJIZWFsaW5rLlVzZXJzIn0.test"

echo "Testing file upload with token..."
echo "Token: $TEST_TOKEN"

# Test upload
curl -X POST http://localhost:5002/api/FileUpload/avatar \
  -H "Accept: text/plain" \
  -H "Authorization: Bearer $TEST_TOKEN" \
  -F "file=@test-image.png" \
  -v

echo ""
echo "Test completed"
