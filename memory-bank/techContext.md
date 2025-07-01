# Technology Context

## Development Environment
- .NET Core
- C# 10+
- Visual Studio Code / Visual Studio
- Git for Version Control

## Backend Technologies
- ASP.NET Core Web API
- Entity Framework Core
- Dependency Injection
- JWT Authentication
- Azure Services Integration

## Configuration Management
- appsettings.json
- Environment-specific configurations
- Secrets management

## Dependency Management
- NuGet Package Manager
- Modular project structure

## Authentication
- JWT Token-based authentication with short-lived access tokens
- Refresh token rotation for enhanced security
- MongoDB storage for refresh tokens
- Background service for automatic token cleanup
- User registration and login flows

## AI Integration
- AI Service for book metadata extraction
- Configurable AI modules

## Database
- **MongoDB** - Primary database for all data storage
- **Repository pattern** - Data access through DAO classes
- **Entity and DAO patterns** - Clear separation between domain models and data entities

## File Storage
- **Azure Blob Storage** - Public containers for book covers and user files
- **GUID-based file naming** - Unique filenames to prevent conflicts
- **Direct URL access** - Public URLs stored in database for easy client access

## Error Handling
- **TryResult Pattern** - Custom error codes and centralized error management
- **No exceptions in business logic** - All services return TryResult<T>
- **Consistent API responses** - Standardized error response format

## Testing
- **Unit Tests** - Basic test framework in place
- **xUnit** - Testing framework for .NET
- **Integration Tests** - Planned for next development phase

## Development Tools
- **Docker** - Containerization support
- **Swagger/OpenAPI** - API documentation
- **Postman/HTTP files** - API testing (intellishelf.http)

## Technology Constraints
- **Database**: MongoDB - no Entity Framework migrations needed
- **File Storage**: Azure Blob Storage with public URLs and GUID naming
- **Authentication**: JWT + refresh token rotation pattern established
- **Error Handling**: TryResult pattern - NO exceptions in business logic
- **Architecture**: Clean Architecture - maintain layer separation

## Testing Protocol

### Standard Workflow (Most Tasks)
**Before Changes**: `dotnet test Tests/Intellishelf.Unit.Tests/`
**After Changes**: `dotnet test Tests/Intellishelf.Unit.Tests/` (must pass)

### Extended Workflow (Infrastructure Changes)
Use when touching:
- Database queries/schema
- Authentication/authorization logic
- External services (Azure Blob, AI)
- File storage functionality
- Major API changes

**Process**:
1. Run unit tests first
2. `docker-compose up --build` 
3. Run integration tests
4. `docker-compose down` (cleanup)

### Manual Testing
- Use `intellishelf.http` file for API endpoint verification
- Test file upload if storage functionality modified

## Deployment Considerations
- **Local Development** - Currently running locally only
- **Docker support** - Ready for containerized deployment
- **Azure Cloud** - Prepared for Azure deployment
- **Environment-specific configurations** - Development, Production settings ready
