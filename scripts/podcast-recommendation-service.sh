#!/bin/bash

# PodcastRecommendationService Setup and Start Script
# This script helps setup the AI models and start the recommendation service

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MODELS_DIR="$PROJECT_ROOT/src/PodcastRecommendationService/models"

echo -e "${GREEN}🎯 PodcastRecommendationService Setup${NC}"
echo "================================================="

# Function to print colored output
print_status() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

# Check if models directory exists
if [ ! -d "$MODELS_DIR" ]; then
    print_error "Models directory not found: $MODELS_DIR"
    exit 1
fi

# Check if models directory exists (optional now)
if [ -d "$MODELS_DIR" ]; then
    print_status "Models directory found (optional pre-trained models)"
    
    # Check for optional model files
    optional_files=(
        "collaborative_filtering_model.h5"
        "mappings.pkl"
        "podcasts.pkl"
        "ratings.pkl"
    )
    
    echo ""
    echo "📊 Optional Model Files Status:"
    echo "==============================="
    found_files=0
    for file in "${optional_files[@]}"; do
        if [ -f "$MODELS_DIR/$file" ]; then
            size=$(du -h "$MODELS_DIR/$file" | cut -f1)
            modified=$(stat -f "%Sm" -t "%Y-%m-%d %H:%M" "$MODELS_DIR/$file" 2>/dev/null || stat -c "%y" "$MODELS_DIR/$file" 2>/dev/null | cut -d' ' -f1,2 | cut -d'.' -f1)
            echo "✅ $file ($size) - Modified: $modified"
            found_files=$((found_files + 1))
        else
            echo "❌ $file (not found)"
        fi
    done
    
    if [ $found_files -eq 0 ]; then
        print_warning "No pre-trained models found - AI service will train on real data from microservices"
    else
        print_status "Found $found_files pre-trained model files - AI service may use them as fallback"
    fi
else
    print_warning "Models directory not found - AI service will train dynamically using real data"
    print_status "This is the recommended setup for production!"
fi

echo ""

# Check if Docker is running
if ! docker info >/dev/null 2>&1; then
    print_error "Docker is not running. Please start Docker Desktop and try again."
    exit 1
fi

print_status "Docker is running"

# Function to start services
start_services() {
    echo ""
    print_status "🚀 Starting PodcastRecommendationService..."
    
    cd "$PROJECT_ROOT"
    
    # Start the C# API service first (it fetches data from other services)
    print_status "Starting Recommendation API Service..."
    docker-compose up -d podcastrecommendation-api
    
    # Wait for API service to be ready
    echo "⏳ Waiting for Recommendation API to be ready..."
    timeout=90 # 1.5 minutes
    elapsed=0
    while ! docker-compose ps podcastrecommendation-api | grep -q "healthy" && [ $elapsed -lt $timeout ]; do
        sleep 5
        elapsed=$((elapsed + 5))
        echo "   Waiting... (${elapsed}s/${timeout}s)"
    done
    
    if [ $elapsed -ge $timeout ]; then
        print_error "Recommendation API failed to start within ${timeout} seconds"
        echo "Showing API logs:"
        docker-compose logs --tail=20 podcastrecommendation-api
        exit 1
    fi
    
    print_status "Recommendation API is ready!"
    
    # Start the AI service (it will fetch data from C# API)
    print_status "Starting AI Service..."
    docker-compose up -d podcast-ai-service
    
    # Wait for AI service to be healthy  
    echo "⏳ Waiting for AI Service to train and be ready..."
    timeout=180 # 3 minutes (training takes time)
    elapsed=0
    while ! docker-compose ps podcast-ai-service | grep -q "healthy" && [ $elapsed -lt $timeout ]; do
        sleep 10
        elapsed=$((elapsed + 10))
        echo "   Training/Loading... (${elapsed}s/${timeout}s)"
        
        # Show AI service logs periodically
        if [ $((elapsed % 30)) -eq 0 ]; then
            echo "   Recent AI service logs:"
            docker-compose logs --tail=5 podcast-ai-service | sed 's/^/   /'
        fi
    done
    
    if [ $elapsed -ge $timeout ]; then
        print_error "AI Service failed to start within ${timeout} seconds"
        echo "Showing AI service logs:"
        docker-compose logs --tail=20 podcast-ai-service
        exit 1
    fi
    
    print_status "AI Service is ready!"
}

# Function to show service status
show_status() {
    echo ""
    echo "📋 Service Status:"
    echo "=================="
    docker-compose ps podcast-ai-service podcastrecommendation-api
    
    echo ""
    echo "🌐 Service URLs:"
    echo "================"
    echo "AI Service:          http://localhost:8000"
    echo "API Service:         http://localhost:5005"
    echo "API Documentation:   http://localhost:5005 (when in development)"
    echo "Gateway (if running): http://localhost:5010"
    
    echo ""
    echo "🧪 Quick Tests:"
    echo "==============="
    echo "curl http://localhost:8000/health"
    echo "curl http://localhost:5005/health"
}

# Function to show logs
show_logs() {
    echo ""
    print_status "📝 Showing service logs..."
    docker-compose logs -f podcast-ai-service podcastrecommendation-api
}

# Function to stop services
stop_services() {
    echo ""
    print_status "🛑 Stopping PodcastRecommendationService..."
    docker-compose stop podcastrecommendation-api podcast-ai-service
    print_status "Services stopped"
}

# Main script logic
case "${1:-start}" in
    "start")
        start_services
        show_status
        ;;
    "stop")
        stop_services
        ;;
    "status")
        show_status
        ;;
    "logs")
        show_logs
        ;;
    "restart")
        stop_services
        sleep 2
        start_services
        show_status
        ;;
    "setup")
        echo "Setting up development environment..."
        print_status "Models directory: $MODELS_DIR"
        print_status "Copy your trained model files to the models directory"
        ;;
    *)
        echo "Usage: $0 {start|stop|status|logs|restart|setup}"
        echo ""
        echo "Commands:"
        echo "  start   - Start the recommendation services"
        echo "  stop    - Stop the recommendation services"  
        echo "  status  - Show service status and URLs"
        echo "  logs    - Show service logs"
        echo "  restart - Restart the services"
        echo "  setup   - Show setup information"
        exit 1
        ;;
esac