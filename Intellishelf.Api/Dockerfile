# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln ./
COPY Intellishelf.Api/Intellishelf.Api.csproj ./Intellishelf.Api/
COPY Intellishelf.Data/Intellishelf.Data.csproj ./Intellishelf.Data/
COPY Intellishelf.Domain/Intellishelf.Domain.csproj ./Intellishelf.Domain/
COPY Intellishelf.Common/Intellishelf.Common.csproj ./Intellishelf.Common/
COPY Tests/Intellishelf.Unit.Tests/Intellishelf.Unit.Tests.csproj ./Tests/Intellishelf.Unit.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Publish the application (includes build)
WORKDIR /src/Intellishelf.Api
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
