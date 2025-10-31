# Use the official ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 5170

# Use the official SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["HaidersAPI.csproj", "./"]
RUN dotnet restore "HaidersAPI.csproj"

# Copy source code and build
COPY . .
RUN dotnet build "HaidersAPI.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "HaidersAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Create final runtime image
FROM base AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Create .env file placeholder
RUN touch .env

# Set environment variables
ENV ASPNETCORE_URLS=http://0.0.0.0:5170
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:5170/api/contact/info || exit 1

ENTRYPOINT ["dotnet", "HaidersAPI.dll"]