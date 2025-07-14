# System Architecture and Patterns

## Architectural Layers
1. Presentation Layer (Intellishelf.Api)
   - Controllers
   - API Contracts
   - Configuration
   - Background Services

2. Domain Layer (Intellishelf.Domain)
   - Business Logic
   - Services
   - Models
   - Error Handling

3. Data Layer (Intellishelf.Data)
   - Data Access Objects (DAOs)
   - Entity Mappings
   - Database Interactions

4. Common Layer (Intellishelf.Common)
   - Shared Utilities
   - Error Handling
   - Cross-Cutting Concerns

## Design Patterns
- Result Pattern (TryResult) - no exception thrown, only TryResult with status and error code
- Dependency Injection
- Mapper Pattern - each layer has its data transfer object and mapping capabilities when necessary. API has contracts, Domain has models, Data has entities. Use dedicated mapper classes instead of inline mapping within methods.
- Service Layer Pattern
- Background Service Pattern - for scheduled tasks like token cleanup
- Pagination Pattern - for efficient data retrieval with large datasets
- **Data Annotations Validation** - Use attributes like `[EmailAddress]`, `[MinLength]` on API contracts for input validation when relevant

## Key Architectural Decisions
- Separation of Concerns
- Modular Design
- Dependency Inversion
- Error Handling Consistency
- Token-based Authentication with Refresh Tokens
- **Controller-Level File Processing** - File uploads handled at API layer before domain processing
- **Immutable Domain Models** - All models use `init` accessors for thread safety and data integrity
- **URL-Based Resource Management** - Store full public URLs instead of internal file paths for external resources

## Authentication System
- JWT-based authentication with short-lived access tokens (30 minutes)
- Refresh token rotation for enhanced security
- MongoDB storage for refresh tokens
- Automatic cleanup of expired tokens via background service
- Token revocation capabilities

## Book Collection Management
- Efficient pagination with configurable page size
- Multiple sorting options (Title, Author, Publication date, Creation date)
- Support for ascending/descending order
- MongoDB query optimization for large collections

## Dependency Flow
Domain Layer → Data Layer (Unidirectional)
Api Layer → Domain Layer → Data Layer

## File Storage Architecture
- **Public Azure Blob Storage** - Direct client access via public URLs
- **Controller-Level Upload Processing** - Files processed at API boundary before domain logic
- **URL Extraction for Deletion** - Parse blob paths from stored URLs for cleanup operations
- **Immutable URL Storage** - Store complete public URLs in domain models and entities
- **Unique File Naming** - GUID-based filenames to prevent conflicts and ensure uniqueness

## Error Handling Patterns
### Error Code Organization
- **Domain-Based Namespacing**: Each domain has its own error codes (Books, Users, Files, AI)
- **Consistent Naming**: Error codes follow pattern `Domain.ErrorType` (e.g., `Books.NotFound`, `User.Unauthorized`)
- **HTTP Status Mapping**: Centralized in `ApiControllerBase.MapErrorToStatusCode()`

### Error Code Lifecycle Management
- **Adding New Error Codes**: Always update `ApiControllerBase.MapErrorToStatusCode()` method
- **Testing**: Run full test suite after error code changes to verify no regressions

## Cross-Cutting Concerns
- Logging
- Authentication
- Error Handling
- Configuration Management
- File Processing and Storage

## Implementation Examples
- **TryResult Pattern**: `BookService.AddBookAsync()` returns `TryResult<Book>`
- **Mapper Pattern**: `BookMapper.ToModel()` in API layer, `BookEntityMapper.ToEntity()` in Data layer
- **Background Service**: `RefreshTokenCleanupService` runs scheduled cleanup every 24 hours
- **Pagination**: `BookQueryParameters` with Skip/Take implemented in `BookDao`
- **File Storage**: `FileStorageService` handles Azure Blob operations with GUID-based naming
- **Authentication Flow**: `AuthController` → `AuthService` → `UserDao` with JWT + refresh token rotation
- **Error Handling**: `ApiControllerBase.HandleErrorResponse()` maps domain error codes to appropriate HTTP status codes
