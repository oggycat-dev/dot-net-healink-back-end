#!/bin/bash
set -e

echo "Running EF Core database migrations..."

# Chạy lệnh update database của EF Core
dotnet ef database update

echo "Migrations completed. Starting application..."

# Dòng này sẽ thực thi lệnh gốc của Dockerfile (chính là lệnh khởi động app)
# exec "$@"

echo "Migration step finished. Exiting for debug purposes."