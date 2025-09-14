# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution file and restore dependencies
COPY ["CardanoPlatform.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["MainAPI/MainAPI.csproj", "MainAPI/"]
COPY ["MainAPI.Core/MainAPI.Core.csproj", "MainAPI.Core/"]
COPY ["MainAPI.Application/MainAPI.Application.csproj", "MainAPI.Application/"]
COPY ["MainAPI.Infrastructure/MainAPI.Infrastructure.csproj", "MainAPI.Infrastructure/"]
COPY ["SharedLibrary/SharedLibrary.csproj", "SharedLibrary/"]

# Restore dependencies
RUN dotnet restore "MainAPI/MainAPI.csproj"

# Copy the rest of the source code
COPY . .

# Build
RUN dotnet build "MainAPI/MainAPI.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MainAPI/MainAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Expose port
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "MainAPI.dll"] 