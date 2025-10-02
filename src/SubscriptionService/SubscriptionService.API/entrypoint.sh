#!/bin/bash
set -e

echo "Starting SubscriptionService..."
echo "Note: Database migrations should be run separately in production"

# Dòng này sẽ thực thi lệnh gốc của Dockerfile (chính là lệnh khởi động app)
exec "$@"