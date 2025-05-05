# Build stage for .NET WebApi project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj files and restore as distinct layers
COPY src/JobTriggerPlatform.Domain/*.csproj ./src/JobTriggerPlatform.Domain/
COPY src/JobTriggerPlatform.Application/*.csproj ./src/JobTriggerPlatform.Application/
COPY src/JobTriggerPlatform.Infrastructure/*.csproj ./src/JobTriggerPlatform.Infrastructure/
COPY src/JobTriggerPlatform.WebApi/*.csproj ./src/JobTriggerPlatform.WebApi/

# Restore packages
RUN dotnet restore ./src/JobTriggerPlatform.WebApi/JobTriggerPlatform.WebApi.csproj

# Copy everything else and build
COPY . ./
RUN dotnet publish ./src/JobTriggerPlatform.WebApi/JobTriggerPlatform.WebApi.csproj -c Release -o /app/publish --no-restore

# Runtime stage using distroless
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS runtime
WORKDIR /app

# Install curl for healthchecks
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

# Create non-root user
USER $APP_UID

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Copy the published app
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Set the entrypoint
ENTRYPOINT ["./JobTriggerPlatform.WebApi"]