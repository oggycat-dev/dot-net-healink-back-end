# Giai đoạn 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy các file .csproj trước để tận dụng Docker layer caching
COPY ["src/ContentService/ContentService.API/ContentService.API.csproj", "ContentService/ContentService.API/"]
COPY ["src/ContentService/ContentService.Application/ContentService.Application.csproj", "ContentService/ContentService.Application/"]
COPY ["src/ContentService/ContentService.Domain/ContentService.Domain.csproj", "ContentService/ContentService.Domain/"]
COPY ["src/ContentService/ContentService.Infrastructure/ContentService.Infrastructure.csproj", "ContentService/ContentService.Infrastructure/"]
COPY ["src/SharedLibrary/SharedLibrary.csproj", "SharedLibrary/"]

# Restore các gói NuGet với SSL configuration
RUN dotnet restore "ContentService/ContentService.API/ContentService.API.csproj" --disable-parallel --verbosity normal

COPY ["src/", "."]
WORKDIR "/src/ContentService/ContentService.API"
RUN dotnet build "ContentService.API.csproj" -c Release -o /app/build

# Giai đoạn 2: Publish
FROM build AS publish
RUN dotnet publish "ContentService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Giai đoạn 3: Final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Cài đặt curl và các dependencies cần thiết
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:80

EXPOSE 80
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "ContentService.API.dll"]