# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/ContentService/ContentService.API/ContentService.API.csproj", "src/ContentService/ContentService.API/"]
COPY ["src/ContentService/ContentService.Application/ContentService.Application.csproj", "src/ContentService/ContentService.Application/"]
COPY ["src/ContentService/ContentService.Infrastructure/ContentService.Infrastructure.csproj", "src/ContentService/ContentService.Infrastructure/"]
COPY ["src/ContentService/ContentService.Domain/ContentService.Domain.csproj", "src/ContentService/ContentService.Domain/"]
COPY ["src/SharedLibrary/SharedLibrary.csproj", "src/SharedLibrary/"]

RUN dotnet restore "src/ContentService/ContentService.API/ContentService.API.csproj"

# Copy source code
COPY . .
WORKDIR "/src/src/ContentService/ContentService.API"

# Build the application
RUN dotnet build "ContentService.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ContentService.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ContentService.API.dll"]