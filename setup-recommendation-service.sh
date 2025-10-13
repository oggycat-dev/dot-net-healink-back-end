#!/bin/bash

echo "🚀 PODCAST RECOMMENDATION SERVICE - COMPLETE SETUP"
echo "================================================================"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Step 1: Check model files
echo ""
echo "📦 Step 1: Checking model files..."
if [ ! -f "src/PodcastRecommendationService/ai_service/models/mappings.pkl" ]; then
    echo -e "${YELLOW}⚠️  Model files not found. Creating dummy files...${NC}"
    cd src/PodcastRecommendationService/ai_service
    python3 create_dummy_models.py
    cd ../../..
    echo -e "${GREEN}✅ Dummy model files created${NC}"
else
    echo -e "${GREEN}✅ Model files exist${NC}"
fi

# Step 2: Build services
echo ""
echo "🔨 Step 2: Building services..."
docker-compose build podcast-ai-service podcastrecommendation-api
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Services built successfully${NC}"
else
    echo -e "${RED}❌ Build failed${NC}"
    exit 1
fi

# Step 3: Start dependencies
echo ""
echo "🗄️ Step 3: Starting dependencies..."
docker-compose up -d postgres redis rabbitmq
sleep 5
echo -e "${GREEN}✅ Dependencies started${NC}"

# Step 4: Start core services
echo ""
echo "⚙️ Step 4: Starting core services..."
docker-compose up -d authservice-api userservice-api contentservice-api
echo "   Waiting for services to be healthy (30s)..."
sleep 30
echo -e "${GREEN}✅ Core services started${NC}"

# Step 5: Start recommendation services
echo ""
echo "🤖 Step 5: Starting recommendation services..."
docker-compose up -d podcast-ai-service
sleep 10
docker-compose up -d podcastrecommendation-api
sleep 10
echo -e "${GREEN}✅ Recommendation services started${NC}"

# Step 6: Health checks
echo ""
echo "🏥 Step 6: Checking service health..."
echo ""

# FastAPI health
echo "   Checking FastAPI service..."
FASTAPI_HEALTH=$(curl -s http://localhost:8000/health)
if echo "$FASTAPI_HEALTH" | grep -q "healthy"; then
    echo -e "   ${GREEN}✅ FastAPI: Healthy${NC}"
else
    echo -e "   ${RED}❌ FastAPI: Unhealthy${NC}"
fi

# C# API health
echo "   Checking C# API service..."
CSHARP_HEALTH=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5005/health)
if [ "$CSHARP_HEALTH" == "200" ]; then
    echo -e "   ${GREEN}✅ C# API: Healthy${NC}"
else
    echo -e "   ${YELLOW}⚠️  C# API: Not ready (HTTP $CSHARP_HEALTH)${NC}"
fi

# Step 7: Summary
echo ""
echo "================================================================"
echo "📊 SETUP COMPLETE!"
echo "================================================================"
echo ""
echo "✅ Services Status:"
echo "   - FastAPI (AI Engine):    http://localhost:8000"
echo "   - C# API:                 http://localhost:5005"
echo "   - Swagger UI:             http://localhost:5005/swagger"
echo ""
echo "🧪 Test Commands:"
echo "   # Test FastAPI directly"
echo "   curl http://localhost:8000/health"
echo "   curl http://localhost:8000/recommendations/user_123?num_recommendations=5"
echo ""
echo "   # Test with JWT (after login)"
echo "   curl -H \"Authorization: Bearer \$TOKEN\" http://localhost:5005/api/recommendations/me"
echo ""
echo "📚 Documentation:"
echo "   - Complete Guide: src/PodcastRecommendationService/README.md"
echo "   - Business Flow:  src/PodcastRecommendationService/BUSINESS_FLOW.md"
echo ""
echo "🔗 Next Steps:"
echo "   1. Login to get JWT token: POST http://localhost:8080/api/auth/user/login"
echo "   2. Use token to call: GET http://localhost:5005/api/recommendations/me"
echo "   3. Check Swagger UI: http://localhost:5005/swagger"
echo ""
echo "📋 View logs:"
echo "   docker-compose logs -f podcast-ai-service"
echo "   docker-compose logs -f podcastrecommendation-api"
echo ""
echo "🎉 Ready to use!"
