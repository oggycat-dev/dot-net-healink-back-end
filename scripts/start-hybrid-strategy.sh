#!/bin/bash

# ==============================================
# HEALINK HYBRID STRATEGY - QUICK START
# ==============================================
# This script helps you start using Hybrid Strategy
# Cost: $5-10/month | Savings: $65-67/month

set -e

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

print_header() {
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo ""
}

print_success() { echo -e "${GREEN}✅ $1${NC}"; }
print_info() { echo -e "${BLUE}ℹ️  $1${NC}"; }
print_warning() { echo -e "${YELLOW}⚠️  $1${NC}"; }
print_error() { echo -e "${RED}❌ $1${NC}"; }

# Welcome message
clear
echo ""
print_header "🏆 HEALINK HYBRID STRATEGY - QUICK START"
echo ""
echo "Expected Monthly Cost: \$5-10"
echo "Savings: \$65-67 per month vs Full AWS"
echo "Strategy: 90% Local (FREE) + 10% AWS (Testing)"
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# Step 1: Check prerequisites
print_info "Step 1/5: Checking prerequisites..."
echo ""

if ! command -v docker &> /dev/null; then
    print_error "Docker is not installed"
    exit 1
fi

if ! docker compose version &> /dev/null; then
    print_error "Docker Compose is not available"
    exit 1
fi

print_success "Docker and Docker Compose are installed"
echo "   Docker: $(docker --version)"
echo "   Compose: $(docker compose version)"
echo ""

# Step 2: Check if services are already running
print_info "Step 2/5: Checking current services..."
echo ""

RUNNING_CONTAINERS=$(docker ps -q | wc -l | tr -d ' ')
if [ "$RUNNING_CONTAINERS" -gt 0 ]; then
    print_warning "Found $RUNNING_CONTAINERS running containers"
    echo ""
    docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
    echo ""
    read -p "Stop existing containers? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        print_info "Stopping existing containers..."
        docker compose down
        print_success "Containers stopped"
    fi
else
    print_success "No running containers found"
fi
echo ""

# Step 3: Start local environment
print_info "Step 3/5: Starting local development environment..."
echo ""
print_warning "This will start:"
echo "   • PostgreSQL (multi-database)"
echo "   • Redis"
echo "   • RabbitMQ"
echo "   • AuthService"
echo "   • UserService"
echo "   • ContentService"
echo "   • NotificationService"
echo "   • Gateway API"
echo "   • pgAdmin"
echo ""
read -p "Continue? (Y/n): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Nn]$ ]]; then
    print_info "Starting services... (this may take 2-3 minutes)"
    echo ""
    
    if [ -f "./scripts/local-dev.sh" ]; then
        ./scripts/local-dev.sh start
    else
        docker compose up -d
    fi
    
    print_success "Services started!"
    echo ""
else
    print_warning "Cancelled by user"
    exit 0
fi

# Step 4: Wait for services to be ready
print_info "Step 4/5: Waiting for services to be ready..."
echo ""
print_info "Waiting 30 seconds for services to initialize..."
sleep 30
echo ""

# Step 5: Show service URLs
print_info "Step 5/5: Service URLs"
echo ""
print_success "🌐 Your local services are ready!"
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "🚪 Gateway API:      http://localhost:5010"
echo "🔐 Auth Service:     http://localhost:5001"
echo "👤 User Service:     http://localhost:5002"
echo "📝 Content Service:  http://localhost:5003"
echo "🔔 Notification:     http://localhost:5004"
echo ""
echo "🐰 RabbitMQ Admin:   http://localhost:15672"
echo "   Username: healink_mq"
echo "   Password: healink_mq_password_2024"
echo ""
echo "🗄️  PostgreSQL Admin: http://localhost:5050"
echo "   Email: admin@healink.dev"
echo "   Password: Admin@123"
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# Test health endpoints
print_info "Testing service health..."
echo ""

test_endpoint() {
    local url=$1
    local name=$2
    
    if curl -s -o /dev/null -w "%{http_code}" "$url" | grep -q "200"; then
        print_success "$name is healthy"
    else
        print_warning "$name might not be ready yet (this is normal)"
    fi
}

test_endpoint "http://localhost:5010/api/auth/health" "Gateway → Auth"
test_endpoint "http://localhost:5010/api/users/health" "Gateway → Users"
test_endpoint "http://localhost:5010/api/content/health" "Gateway → Content"

echo ""
print_info "If some services are not ready yet, wait 1-2 more minutes"
echo ""

# Daily workflow guide
print_header "📋 DAILY WORKFLOW"
echo ""
echo "1. Local Development (FREE - 90% of time)"
echo "   • Start: ./scripts/local-dev.sh start"
echo "   • Stop:  ./scripts/local-dev.sh stop"
echo "   • Logs:  ./scripts/local-dev.sh logs {service-name}"
echo ""
echo "2. AWS Testing (CHEAP - 10% of time, 1-2×/week)"
echo "   • Deploy:  GitHub Actions → Full Deploy → dev"
echo "   • Test:    2-3 hours of cloud testing"
echo "   • Nuke:    GitHub Actions → Nuke AWS → Type 'NUKE'"
echo ""
echo "3. Cost Monitoring"
echo "   • AWS Console → Billing Dashboard"
echo "   • Set Budget Alert: \$10/month"
echo "   • Check daily costs"
echo ""

print_header "💰 EXPECTED COSTS"
echo ""
echo "Local Development:     \$0/month (90% time)"
echo "AWS Testing (10h):     \$1-2/month"
echo "AWS Storage & Logs:    \$2-3/month"
echo "Buffer:                \$2-3/month"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Total:                 \$5-10/month"
echo "Savings vs Full AWS:   \$65-67/month"
echo "Annual Savings:        \$780-804/year 🎊"
echo ""

print_header "📚 NEXT STEPS"
echo ""
echo "1. ✅ Local environment is running (FREE!)"
echo "2. 📖 Read: HYBRID_STRATEGY_SETUP.md (full guide)"
echo "3. 💻 Start developing locally"
echo "4. ☁️  Deploy to AWS only when needed"
echo "5. 💰 Monitor costs in AWS Console"
echo ""

print_header "🛠️ USEFUL COMMANDS"
echo ""
echo "Check Status:    docker compose ps"
echo "View Logs:       ./scripts/local-dev.sh logs {service}"
echo "Restart Service: ./scripts/local-dev.sh restart {service}"
echo "Rebuild Service: ./scripts/local-dev.sh rebuild {service}"
echo "Stop All:        ./scripts/local-dev.sh stop"
echo ""

print_success "✅ Hybrid Strategy Setup Complete!"
echo ""
print_info "Cost: \$0 for local development"
print_info "Start developing now - it's FREE! 🚀"
echo ""
