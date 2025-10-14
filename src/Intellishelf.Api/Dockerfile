# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln ./
COPY src/Intellishelf.Api/Intellishelf.Api.csproj ./src/Intellishelf.Api/
COPY src/Intellishelf.Data/Intellishelf.Data.csproj ./src/Intellishelf.Data/
COPY src/Intellishelf.Domain/Intellishelf.Domain.csproj ./src/Intellishelf.Domain/
COPY src/Intellishelf.Common/Intellishelf.Common.csproj ./src/Intellishelf.Common/
COPY tests/Intellishelf.Unit.Tests/Intellishelf.Unit.Tests.csproj ./tests/Intellishelf.Unit.Tests/
COPY tests/Intellishelf.Integration.Tests/Intellishelf.Integration.Tests.csproj ./tests/Intellishelf.Integration.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Set correct working directory (remove duplicate src)
WORKDIR /src/src/Intellishelf.Api

# Publish the application
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Start the application
ENTRYPOINT ["dotnet", "Intellishelf.Api.dll"]