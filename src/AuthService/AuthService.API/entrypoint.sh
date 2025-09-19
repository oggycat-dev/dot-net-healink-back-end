#!/bin/bash
set -e

echo "Running EF Core database migrations..."

# Chạy lệnh update database của EF Core
# Nó sẽ tự động đọc connection string từ biến môi trường
dotnet ef database update

echo "Migrations completed. Starting application..."

# Dòng này sẽ thực thi lệnh gốc của Dockerfile (chính là lệnh khởi động app)
exec "$@"