# syntax=docker/dockerfile:1
# Multi-stage build for the OYPA CRM .NET 9 API.
# IMPORTANT: build context is the repository ROOT (see docker-compose.yml),
# so all COPY paths are relative to the repo root.

# ---- Build / publish stage ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Pin the SDK feature band exactly as the repo does.
COPY global.json ./

# Copy solution + shared build props first.
COPY backend/OypaCrm.sln ./backend/OypaCrm.sln
COPY backend/Directory.Build.props ./backend/Directory.Build.props

# Copy every project file so 'dotnet restore' can resolve the full graph
# (referenced by the .sln) with maximum layer caching.
COPY backend/src/Domain/Oypa.Crm.Domain.csproj                       ./backend/src/Domain/
COPY backend/src/Contracts/Oypa.Crm.Contracts.csproj                 ./backend/src/Contracts/
COPY backend/src/Application/Oypa.Crm.Application.csproj             ./backend/src/Application/
COPY backend/src/Infrastructure/Oypa.Crm.Infrastructure.csproj      ./backend/src/Infrastructure/
COPY backend/src/Api/Oypa.Crm.Api.csproj                            ./backend/src/Api/
COPY backend/tests/UnitTests/Oypa.Crm.UnitTests.csproj              ./backend/tests/UnitTests/
COPY backend/tests/IntegrationTests/Oypa.Crm.IntegrationTests.csproj ./backend/tests/IntegrationTests/

RUN dotnet restore backend/OypaCrm.sln

# Copy the rest of the backend sources and publish the API.
COPY backend/ ./backend/

RUN dotnet publish backend/src/Api/Oypa.Crm.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "Oypa.Crm.Api.dll"]
