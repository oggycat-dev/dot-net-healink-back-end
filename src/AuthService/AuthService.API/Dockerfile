# Giai đoạn 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["AuthService/AuthService.API/AuthService.API.csproj", "AuthService/AuthService.API/"]
COPY ["AuthService/AuthService.Application/AuthService.Application.csproj", "AuthService/AuthService.Application/"]
COPY ["AuthService/AuthService.Domain/AuthService.Domain.csproj", "AuthService/AuthService.Domain/"]
COPY ["AuthService/AuthService.Infrastructure/AuthService.Infrastructure.csproj", "AuthService/AuthService.Infrastructure/"]
COPY ["SharedLibrary/SharedLibrary.csproj", "SharedLibrary/"]

# Copy các file .csproj trước để tận dụng Docker layer caching
# COPY ["src/AuthService/AuthService.API/AuthService.API.csproj", "AuthService/AuthService.API/"]
# COPY ["src/AuthService/AuthService.Application/AuthService.Application.csproj", "AuthService/AuthService.Application/"]
# COPY ["src/AuthService/AuthService.Domain/AuthService.Domain.csproj", "AuthService/AuthService.Domain/"]
# COPY ["src/AuthService/AuthService.Infrastructure/AuthService.Infrastructure.csproj", "AuthService/AuthService.Infrastructure/"]
# COPY ["src/Shared/Shared.csproj", "Shared/"]

# Restore các gói NuGet. Chú ý đường dẫn đến file project chính.
RUN dotnet restore "AuthService/AuthService.API/AuthService.API.csproj"


COPY . .
WORKDIR "/src/AuthService/AuthService.API"
RUN dotnet build "AuthService.API.csproj" -c Release -o /app/build

# Giai đoạn 2: Publish
FROM build AS publish
RUN dotnet publish "AuthService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Giai đoạn 3: Final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Giữ lại phần cài đặt curl và HEALTHCHECK rất tốt của bạn
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:80

EXPOSE 80
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "AuthService.API.dll"]