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
- Mapper Pattern - each layer has its data transfer object and mapping capabilities when necessary. API has contracts, Domain has models, Data has entities.
- Service Layer Pattern
- Background Service Pattern - for scheduled tasks like token cleanup
- Pagination Pattern - for efficient data retrieval with large datasets

## Key Architectural Decisions
- Separation of Concerns
- Modular Design
- Dependency Inversion
- Error Handling Consistency
- Token-based Authentication with Refresh Tokens

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

## Cross-Cutting Concerns
- Logging
- Authentication
- Error Handling
- Configuration Management
