# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app

# Create data directory for SQLite database
RUN mkdir -p /app/data

# Create certs directory for SSL certificates
RUN mkdir -p /app/certs

# Set environment to Docker
ENV ASPNETCORE_ENVIRONMENT=Docker

EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NeshanGeocodingApi.csproj", "."]
RUN dotnet restore "./NeshanGeocodingApi.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./NeshanGeocodingApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./NeshanGeocodingApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app

# Create data directory for SQLite database
RUN mkdir -p /app/data

# Create certs directory for SSL certificates
RUN mkdir -p /app/certs

# Set environment to Docker
ENV ASPNETCORE_ENVIRONMENT=Docker

COPY --from=publish /app/publish .

# Copy SSL certificate if it exists (optional)
COPY certs/ /app/certs/

ENTRYPOINT ["dotnet", "NeshanGeocodingApi.dll"]