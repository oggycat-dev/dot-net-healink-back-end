# Giai đoạn 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src


# Copy các file .csproj trước để tận dụng Docker layer caching
COPY ["src/UserService/UserService.API/UserService.API.csproj", "UserService/UserService.API/"]
COPY ["src/UserService/UserService.Application/UserService.Application.csproj", "UserService/UserService.Application/"]
COPY ["src/UserService/UserService.Domain/UserService.Domain.csproj", "UserService/UserService.Domain/"]
COPY ["src/UserService/UserService.Infrastructure/UserService.Infrastructure.csproj", "UserService/UserService.Infrastructure/"]
COPY ["src/SharedLibrary/SharedLibrary.csproj", "SharedLibrary/"]

# Restore các gói NuGet. Chú ý đường dẫn đến file project chính.
RUN dotnet restore "UserService/UserService.API/UserService.API.csproj"


COPY ["src/", "."]
WORKDIR "/src/UserService/UserService.API"
RUN dotnet build "UserService.API.csproj" -c Release -o /app/build

# Giai đoạn 2: Publish
FROM build AS publish
RUN dotnet publish "UserService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Giai đoạn 3: Final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Giữ lại phần cài đặt curl và HEALTHCHECK rất tốt của bạn
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:80

EXPOSE 80
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "UserService.API.dll"]