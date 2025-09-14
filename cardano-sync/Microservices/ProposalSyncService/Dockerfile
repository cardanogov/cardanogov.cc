# ============================
# 1. Build & Publish Stage
# ============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj và props để restore cache
COPY Microservices/ProposalSyncService/ProposalSyncService.csproj Microservices/ProposalSyncService/
COPY SharedLibrary/SharedLibrary.csproj SharedLibrary/
COPY Directory.Build.props .

# Restore dependencies cho project ProposalSyncService
RUN dotnet restore Microservices/ProposalSyncService/ProposalSyncService.csproj

# Copy toàn bộ source
COPY . .

# Publish trực tiếp (không restore lại)
RUN dotnet publish Microservices/ProposalSyncService/ProposalSyncService.csproj \
    -c Release -o /app/publish \
    /p:UseAppHost=false

# ============================
# 2. Runtime Stage
# ============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Copy app đã publish từ build stage
COPY --from=build /app/publish .

# Healthcheck
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD dotnet ProposalSyncService.dll --health-check || exit 1

# Chạy app
ENTRYPOINT ["dotnet", "ProposalSyncService.dll"] 