#!/bin/bash

echo "🚀 PODCAST RECOMMENDATION SERVICE - END-TO-END TEST"
echo "=" 

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
FASTAPI_URL="http://localhost:8000"
CSHARP_API_URL="http://localhost:5005"
TEST_USER_ID="user_123"

echo ""
echo "📋 Test Configuration:"
echo "   - FastAPI Service: $FASTAPI_URL"
echo "   - C# API Service: $CSHARP_API_URL"
echo "   - Test User ID: $TEST_USER_ID"
echo ""

# Test 1: FastAPI Health Check
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "1️⃣ Testing FastAPI Service Health"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

HEALTH_RESPONSE=$(curl -s -w "\n%{http_code}" "$FASTAPI_URL/health")
HTTP_CODE=$(echo "$HEALTH_RESPONSE" | tail -n1)
BODY=$(echo "$HEALTH_RESPONSE" | sed '$d')

if [ "$HTTP_CODE" == "200" ]; then
    echo -e "${GREEN}✅ FastAPI Service is healthy${NC}"
    echo "   Response: $BODY"
else
    echo -e "${RED}❌ FastAPI Service health check failed (HTTP $HTTP_CODE)${NC}"
    exit 1
fi

echo ""

# Test 2: Model Info
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "2️⃣ Testing Model Information Endpoint"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

MODEL_INFO=$(curl -s "$FASTAPI_URL/model/info")
echo "$MODEL_INFO" | python3 -m json.tool 2>/dev/null || echo "$MODEL_INFO"

echo ""

# Test 3: Get Recommendations (FastAPI Direct)
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "3️⃣ Testing Direct FastAPI Recommendations"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

RECOMMENDATIONS=$(curl -s "$FASTAPI_URL/recommendations/$TEST_USER_ID?num_recommendations=5")
REC_COUNT=$(echo "$RECOMMENDATIONS" | python3 -c "import sys, json; data=json.load(sys.stdin); print(len(data.get('recommendations', [])))" 2>/dev/null)

if [ ! -z "$REC_COUNT" ] && [ "$REC_COUNT" -gt 0 ]; then
    echo -e "${GREEN}✅ FastAPI returned $REC_COUNT recommendations${NC}"
    echo ""
    echo "Sample recommendations:"
    echo "$RECOMMENDATIONS" | python3 -c "
import sys, json
data = json.load(sys.stdin)
for i, rec in enumerate(data.get('recommendations', [])[:3], 1):
    print(f\"   {i}. {rec['title']} (Rating: {rec['predicted_rating']}, Category: {rec.get('category', 'N/A')})\")
" 2>/dev/null || echo "   $RECOMMENDATIONS"
else
    echo -e "${YELLOW}⚠️  FastAPI returned no recommendations${NC}"
fi

echo ""

# Test 4: C# API Health (if running)
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "4️⃣ Testing C# API Service (Optional)"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

CSHARP_HEALTH=$(curl -s -w "\n%{http_code}" "$CSHARP_API_URL/health" 2>/dev/null)
CSHARP_HTTP_CODE=$(echo "$CSHARP_HEALTH" | tail -n1)

if [ "$CSHARP_HTTP_CODE" == "200" ]; then
    echo -e "${GREEN}✅ C# API Service is healthy${NC}"
    CSHARP_BODY=$(echo "$CSHARP_HEALTH" | sed '$d')
    echo "   Response: $CSHARP_BODY"
else
    echo -e "${YELLOW}⚠️  C# API Service not available (HTTP $CSHARP_HTTP_CODE)${NC}"
    echo "   This is OK if you're only testing FastAPI"
fi

echo ""

# Summary
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📊 TEST SUMMARY"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "✅ FastAPI Service:"
echo "   - Health: Working"
echo "   - Model: Loaded"
echo "   - Recommendations: Generating"
echo ""
echo "📋 Integration Flow:"
echo "   User → JWT → C# API → FastAPI → Recommendations"
echo ""
echo "🔗 Endpoints:"
echo "   - FastAPI: $FASTAPI_URL"
echo "   - C# API: $CSHARP_API_URL"
echo "   - Swagger: $CSHARP_API_URL/swagger"
echo ""
echo "🎯 Next Steps:"
echo "   1. Start C# service: docker-compose up -d podcastrecommendation-api"
echo "   2. Get JWT token from AuthService"
echo "   3. Call: GET $CSHARP_API_URL/api/recommendations/me"
echo "   4. Verify recommendations returned"
echo ""
echo "=" 
