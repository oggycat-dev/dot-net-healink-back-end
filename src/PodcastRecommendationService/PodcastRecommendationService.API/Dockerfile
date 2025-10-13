# Giai đoạn 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy các file .csproj trước để tận dụng Docker layer caching
COPY ["src/PodcastRecommendationService/PodcastRecommendationService.API/PodcastRecommendationService.API.csproj", "PodcastRecommendationService/PodcastRecommendationService.API/"]
COPY ["src/PodcastRecommendationService/PodcastRecommendationService.Application/PodcastRecommendationService.Application.csproj", "PodcastRecommendationService/PodcastRecommendationService.Application/"]
COPY ["src/PodcastRecommendationService/PodcastRecommendationService.Domain/PodcastRecommendationService.Domain.csproj", "PodcastRecommendationService/PodcastRecommendationService.Domain/"]
COPY ["src/PodcastRecommendationService/PodcastRecommendationService.Infrastructure/PodcastRecommendationService.Infrastructure.csproj", "PodcastRecommendationService/PodcastRecommendationService.Infrastructure/"]
COPY ["src/SharedLibrary/SharedLibrary.csproj", "SharedLibrary/"]

# Restore các gói NuGet
RUN dotnet restore "PodcastRecommendationService/PodcastRecommendationService.API/PodcastRecommendationService.API.csproj"

# Copy toàn bộ source code
COPY ["src/", "."]
WORKDIR "/src/PodcastRecommendationService/PodcastRecommendationService.API"
RUN dotnet build "PodcastRecommendationService.API.csproj" -c Release -o /app/build

# Giai đoạn 2: Publish
FROM build AS publish
RUN dotnet publish "PodcastRecommendationService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Giai đoạn 3: Final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Create non-root user for security
RUN useradd --create-home --shell /bin/bash appuser && chown -R appuser:appuser /app
USER appuser

# Expose port
EXPOSE 80
EXPOSE 443

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "PodcastRecommendationService.API.dll"]