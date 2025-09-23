# Use the official .NET 8 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/NotificaitonService/NotificaitonService.API/NotificaitonService.API.csproj", "NotificaitonService/NotificaitonService.API/"]
COPY ["src/NotificaitonService/NotificaitonService.Application/NotificaitonService.Application.csproj", "NotificaitonService/NotificaitonService.Application/"]
COPY ["src/NotificaitonService/NotificaitonService.Domain/NotificationService.Domain.csproj", "NotificaitonService/NotificaitonService.Domain/"]
COPY ["src/NotificaitonService/NotificaitonService.Infrastructure/NotificaitonService.Infrastructure.csproj", "NotificaitonService/NotificaitonService.Infrastructure/"]
COPY ["src/SharedLibrary/SharedLibrary.csproj", "SharedLibrary/"]

RUN dotnet restore "NotificaitonService/NotificaitonService.API/NotificaitonService.API.csproj"

# Copy everything else and build
COPY ["src/", "."]
WORKDIR "/src/NotificaitonService/NotificaitonService.API"
RUN dotnet build "NotificaitonService.API.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "NotificaitonService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the official .NET 8 runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:80

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "NotificaitonService.API.dll"]