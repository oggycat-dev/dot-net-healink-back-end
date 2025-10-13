#!/bin/bash
set -e

echo "=== Running Database Migrations ==="

# Set connection string from environment
export DB_CONNECTION_STRING="Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}"

echo "Connecting to database at: ${DB_HOST}:${DB_PORT}"
echo "Database: ${DB_NAME}"
echo "User: ${DB_USER}"

# Run EF migrations
dotnet ef database update --no-build --connection "$DB_CONNECTION_STRING"

echo "=== Migrations completed successfully ==="