# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY src/Domain/*.csproj ./Domain/
COPY src/Application/*.csproj ./Application/
COPY src/Infrastructure/*.csproj ./Infrastructure/
COPY src/WebAPI/*.csproj ./WebAPI/

RUN dotnet restore WebAPI/TelegramMarketplace.WebAPI.csproj

# Copy source code and build
COPY src/ ./
RUN dotnet publish WebAPI/TelegramMarketplace.WebAPI.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser:appuser /app
USER appuser

# Expose port
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:5000/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "TelegramMarketplace.WebAPI.dll"]
