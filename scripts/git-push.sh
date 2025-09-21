#!/bin/bash

# Git commit helper với skip CI option
# Usage: ./scripts/git-push.sh "commit message" [--skip-ci]

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

print_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
print_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
print_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }

show_usage() {
    echo "🚀 GIT PUSH HELPER"
    echo "=================="
    echo ""
    echo "Usage: $0 \"commit message\" [--skip-ci]"
    echo ""
    echo "Examples:"
    echo "  $0 \"Add new feature\"              # Normal push (triggers CI/CD)"
    echo "  $0 \"Update README\" --skip-ci     # Skip CI/CD"
    echo "  $0 \"WIP: development\" --skip-ci  # Work in progress"
    echo ""
    echo "Skip CI keywords that work:"
    echo "  [skip ci], [ci skip], [no ci], [skip actions]"
    echo ""
}

if [ $# -lt 1 ]; then
    show_usage
    exit 1
fi

commit_message="$1"
skip_ci="$2"

# Add skip ci to message if requested
if [ "$skip_ci" = "--skip-ci" ]; then
    # Check if skip ci is already in the message
    if [[ ! "$commit_message" =~ \[(skip ci|ci skip|no ci|skip actions)\] ]]; then
        commit_message="$commit_message [skip ci]"
    fi
    print_warning "CI/CD will be skipped for this commit"
fi

print_info "Preparing to commit and push..."
echo "Commit message: $commit_message"
echo ""

# Show git status
print_info "Current git status:"
git status --short

echo ""
read -p "Continue with commit and push? (y/N): " -n 1 -r
echo

if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    print_warning "Operation cancelled"
    exit 0
fi

# Add all changes
print_info "Adding all changes..."
git add .

# Commit
print_info "Committing changes..."
git commit -m "$commit_message"

# Push
print_info "Pushing to origin main..."
git push origin main

print_success "✅ Changes pushed successfully!"

if [ "$skip_ci" = "--skip-ci" ]; then
    print_success "🚫 CI/CD was skipped"
else
    print_info "🚀 CI/CD pipeline may be triggered"
fi