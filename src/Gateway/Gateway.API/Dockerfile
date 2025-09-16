# Use the official .NET 8 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["Gateway/Gateway.API/Gateway.API.csproj", "Gateway/Gateway.API/"]
COPY ["Shared/Shared.csproj", "Shared/"]

RUN dotnet restore "Gateway/Gateway.API/Gateway.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Gateway/Gateway.API"
RUN dotnet build "Gateway.API.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "Gateway.API.csproj" -c Release -o /app/publish

# Use the official .NET 8 runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

# Set environment variable to use Docker configuration
ENV ASPNETCORE_ENVIRONMENT=Docker

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "Gateway.API.dll"]
