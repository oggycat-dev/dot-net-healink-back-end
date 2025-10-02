# Giai đoạn 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy các file .csproj trước để tận dụng Docker layer caching
COPY ["src/PaymentService/PaymentService.API/PaymentService.API.csproj", "PaymentService/PaymentService.API/"]
COPY ["src/PaymentService/PaymentService.Application/PaymentService.Application.csproj", "PaymentService/PaymentService.Application/"]
COPY ["src/PaymentService/PaymentService.Domain/PaymentService.Domain.csproj", "PaymentService/PaymentService.Domain/"]
COPY ["src/PaymentService/PaymentService.Infrastructure/PaymentService.Infrastructure.csproj", "PaymentService/PaymentService.Infrastructure/"]
COPY ["src/SharedLibrary/SharedLibrary.csproj", "SharedLibrary/"]

# Restore các gói NuGet. Chú ý đường dẫn đến file project chính.
RUN dotnet restore "PaymentService/PaymentService.API/PaymentService.API.csproj"

COPY ["src/", "."]
WORKDIR "/src/PaymentService/PaymentService.API"
RUN dotnet build "PaymentService.API.csproj" -c Release -o /app/build

# Giai đoạn 2: Publish
FROM build AS publish
RUN dotnet publish "PaymentService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Giai đoạn 3: Final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Giữ lại phần cài đặt curl và HEALTHCHECK rất tốt của bạn
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:80

EXPOSE 80

# 1. Sao chép file entrypoint.sh từ build stage
COPY --from=build /src/PaymentService/PaymentService.API/entrypoint.sh ./entrypoint.sh

# 2. Cấp quyền thực thi cho file script và đảm bảo line endings đúng
RUN chmod +x ./entrypoint.sh && \
    sed -i 's/\r$//' ./entrypoint.sh

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost/health || exit 1

# 3. Đặt script này làm lệnh khởi động chính của container.
ENTRYPOINT ["./entrypoint.sh"]

# 4. Lệnh khởi động ứng dụng của anh bây giờ trở thành CMD.
#    Script entrypoint.sh sẽ gọi lệnh này sau khi đã chạy migration xong.
CMD ["dotnet", "PaymentService.API.dll"]