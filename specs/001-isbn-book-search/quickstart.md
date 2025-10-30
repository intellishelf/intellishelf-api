# ISBN Book Search Quickstart

**Feature**: 001-isbn-book-search  
**Date**: 2025-01-29

---

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Environment Setup](#environment-setup)
3. [API Configuration](#api-configuration)
4. [Database Setup](#database-setup)
5. [Running the Feature](#running-the-feature)
6. [Testing the API](#testing-the-api)
7. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Software
- **.NET 9.0 SDK** (verify: `dotnet --version`)
- **MongoDB** 6.0+ (local or cloud)
- **Azure Storage Account** (for cover images)
- **Docker Desktop** (for integration tests with Testcontainers)

### External API Accounts
1. **Google Books API**:
   - Go to [Google Cloud Console](https://console.cloud.google.com/)
   - Create a new project or select existing
   - Enable "Books API" (APIs & Services → Library)
   - Create API Key (APIs & Services → Credentials → Create Credentials → API Key)
   - **Copy the API key** (needed for configuration)

2. **Amazon Product Advertising API** (Optional):
   - Sign up for [Amazon Associates](https://affiliate-program.amazon.com/)
   - Register for [Product Advertising API](https://webservices.amazon.com/paapi5/documentation/)
   - Generate Access Key and Secret Key
   - **Note**: This is optional for MVP; Google Books is the primary source
   - **Implementation**: Custom HTTP client with AWS Signature v4 (no third-party packages)

---

## Environment Setup

### 1. Clone Repository and Install Dependencies
```bash
cd /path/to/intellishelf-api
dotnet restore Intellishelf.Api.sln
```

### 2. Install New NuGet Packages
```bash
# Navigate to Domain project
cd src/Intellishelf.Domain

# Install Google Books API client
dotnet add package Google.Apis.Books.v1

# Amazon API: No package needed - custom HTTP client implementation

# Return to solution root
cd ../..
dotnet build Intellishelf.Api.sln -c Release
```

### 3. Verify Installation
```bash
dotnet list src/Intellishelf.Domain/Intellishelf.Domain.csproj package | grep Google
# Expected: Google.Apis.Books.v1  1.68.0.3463
```

---

## API Configuration

### 1. Configure User Secrets (Recommended)
```bash
# Navigate to API project
cd src/Intellishelf.Api

# Initialize user secrets
dotnet user-secrets init

# Set Google Books API key
dotnet user-secrets set "ExternalApis:GoogleBooks:ApiKey" "YOUR_GOOGLE_BOOKS_API_KEY_HERE"

# (Optional) Set Amazon API credentials
dotnet user-secrets set "ExternalApis:Amazon:AccessKey" "YOUR_AMAZON_ACCESS_KEY_HERE"
dotnet user-secrets set "ExternalApis:Amazon:SecretKey" "YOUR_AMAZON_SECRET_KEY_HERE"
dotnet user-secrets set "ExternalApis:Amazon:PartnerTag" "YOUR_AMAZON_ASSOCIATE_TAG_HERE"
```

### 2. Alternative: Environment Variables
```bash
export ExternalApis__GoogleBooks__ApiKey="YOUR_GOOGLE_BOOKS_API_KEY_HERE"
export ExternalApis__Amazon__AccessKey="YOUR_AMAZON_ACCESS_KEY_HERE"
export ExternalApis__Amazon__SecretKey="YOUR_AMAZON_SECRET_KEY_HERE"
export ExternalApis__Amazon__PartnerTag="YOUR_AMAZON_ASSOCIATE_TAG_HERE"
```

### 3. Update appsettings.Development.json
Add external API configuration to `src/Intellishelf.Api/appsettings.Development.json`:

```json
{
  "ExternalApis": {
    "GoogleBooks": {
      "BaseUrl": "https://www.googleapis.com/books/v1",
      "TimeoutSeconds": 5,
      "ApiKey": "" 
    },
    "Amazon": {
      "Marketplace": "www.amazon.com",
      "TimeoutSeconds": 5,
      "AccessKey": "",
      "SecretKey": "",
      "PartnerTag": ""
    }
  }
}
```

**⚠️ Note**: Leave `ApiKey`, `AccessKey`, `SecretKey`, and `PartnerTag` **empty** in `appsettings.json`. Use user secrets or environment variables for actual values.

### 4. Update secrets-example.json
Update `src/Intellishelf.Api/secrets-example.json` to document new secrets:

```json
{
  "Database": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "intellishelf"
  },
  "Azure": {
    "Storage": {
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
    }
  },
  "ExternalApis": {
    "GoogleBooks": {
      "ApiKey": "YOUR_GOOGLE_BOOKS_API_KEY_HERE"
    },
    "Amazon": {
      "AccessKey": "YOUR_AMAZON_ACCESS_KEY_HERE",
      "SecretKey": "YOUR_AMAZON_SECRET_KEY_HERE",
      "PartnerTag": "YOUR_AMAZON_ASSOCIATE_TAG_HERE"
    }
  }
}
```

---

## Database Setup

### 1. Start MongoDB (Local)
```bash
# Option 1: Docker
docker run -d -p 27017:27017 --name mongodb mongo:6.0

# Option 2: Homebrew (macOS)
brew services start mongodb-community@6.0

# Option 3: Manual
mongod --dbpath /path/to/data/db
```

### 2. Create Indexes
Indexes are created automatically on first run by `BookDao`. To verify manually:

```bash
# Connect to MongoDB shell
mongosh mongodb://localhost:27017/intellishelf

# Check indexes
db.books.getIndexes()

# Expected output:
# [
#   { "v": 2, "key": { "_id": 1 }, "name": "_id_" },
#   { "v": 2, "key": { "userId": 1, "isbn13": 1 }, "name": "idx_userId_isbn13", "unique": true },
#   { "v": 2, "key": { "userId": 1, "isbn10": 1 }, "name": "idx_userId_isbn10" },
#   { "v": 2, "key": { "createdAt": -1 }, "name": "idx_createdAt" }
# ]
```

### 3. Seed Test Data (Optional)
```bash
mongosh mongodb://localhost:27017/intellishelf

# Insert test user's book (replace userId with actual Google OAuth ID)
db.books.insertOne({
  userId: "google-oauth2|123456789",
  isbn10: "0306406152",
  isbn13: "9780306406157",
  title: "The Pragmatic Programmer",
  authors: ["Andrew Hunt", "David Thomas"],
  publisher: "Addison-Wesley",
  publishedDate: "1999-10-30",
  description: "Your journey to mastery...",
  coverBlobUrl: null,
  source: "Google",
  createdAt: new Date()
})
```

---

## Running the Feature

### 1. Start the API
```bash
cd src/Intellishelf.Api
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:8080
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### 2. Verify API is Running
```bash
curl http://localhost:8080/api/health
# Expected: 200 OK
```

### 3. Authenticate (Required)
Before testing the ISBN endpoint, you must authenticate:

**For Web Clients (Google OAuth)**:
1. Navigate to `http://localhost:8080/api/auth/login` in browser
2. Sign in with Google account
3. Cookie `.AspNetCore.Cookies` is set automatically

**For Mobile Clients (JWT)**:
```bash
# Login and get JWT token
TOKEN=$(curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com", "password": "password"}' \
  | jq -r '.token')

# Use token in subsequent requests
curl -H "Authorization: Bearer $TOKEN" http://localhost:8080/api/books
```

---

## Testing the API

### 1. Add Book by ISBN-10
```bash
curl -X POST http://localhost:8080/api/books/isbn \
  -H "Content-Type: application/json" \
  -H "Cookie: .AspNetCore.Cookies=YOUR_COOKIE_HERE" \
  -d '{"isbn": "0-306-40615-2"}' \
  | jq
```

**Expected Response** (201 Created):
```json
{
  "bookId": "507f1f77bcf86cd799439011",
  "isbn10": "0306406152",
  "isbn13": "9780306406157",
  "title": "The Pragmatic Programmer",
  "authors": ["Andrew Hunt", "David Thomas"],
  "publisher": "Addison-Wesley",
  "publishedDate": "1999-10-30",
  "description": "Your journey to mastery...",
  "coverUrl": "https://intellishelf.blob.core.windows.net/book-covers/user123/books/507f1f77bcf86cd799439011.jpg",
  "source": "Google",
  "createdAt": "2025-01-29T10:30:00Z"
}
```

### 2. Add Book by ISBN-13
```bash
curl -X POST http://localhost:8080/api/books/isbn \
  -H "Content-Type: application/json" \
  -H "Cookie: .AspNetCore.Cookies=YOUR_COOKIE_HERE" \
  -d '{"isbn": "9780134685991"}' \
  | jq
```

### 3. Test Invalid ISBN (400 Bad Request)
```bash
curl -X POST http://localhost:8080/api/books/isbn \
  -H "Content-Type: application/json" \
  -H "Cookie: .AspNetCore.Cookies=YOUR_COOKIE_HERE" \
  -d '{"isbn": "invalid-isbn"}' \
  | jq
```

**Expected Response**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Invalid Request",
  "status": 400,
  "detail": "Invalid ISBN: Invalid ISBN format: invalid-isbn"
}
```

### 4. Test Duplicate Book (409 Conflict)
```bash
# Add same book twice
curl -X POST http://localhost:8080/api/books/isbn \
  -H "Content-Type: application/json" \
  -H "Cookie: .AspNetCore.Cookies=YOUR_COOKIE_HERE" \
  -d '{"isbn": "0-306-40615-2"}' \
  | jq

# Second request should fail
curl -X POST http://localhost:8080/api/books/isbn \
  -H "Content-Type: application/json" \
  -H "Cookie: .AspNetCore.Cookies=YOUR_COOKIE_HERE" \
  -d '{"isbn": "9780306406157"}' \
  | jq
```

**Expected Response**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
  "title": "Conflict",
  "status": 409,
  "detail": "Duplicate book: Book already in your collection"
}
```

### 5. Test Book Not Found (404)
```bash
curl -X POST http://localhost:8080/api/books/isbn \
  -H "Content-Type: application/json" \
  -H "Cookie: .AspNetCore.Cookies=YOUR_COOKIE_HERE" \
  -d '{"isbn": "9999999999"}' \
  | jq
```

### 6. Using VS Code REST Client
Create `intellishelf.http` file (already exists in repo):

```http
### Add book by ISBN-10
POST http://localhost:8080/api/books/isbn
Content-Type: application/json

{
  "isbn": "0-306-40615-2"
}

### Add book by ISBN-13
POST http://localhost:8080/api/books/isbn
Content-Type: application/json

{
  "isbn": "9780134685991"
}

### Test invalid ISBN
POST http://localhost:8080/api/books/isbn
Content-Type: application/json

{
  "isbn": "invalid-isbn"
}
```

---

## Running Tests

### Unit Tests
```bash
dotnet test Tests/Intellishelf.Unit.Tests/Intellishelf.Unit.Tests.csproj --filter "FullyQualifiedName~IsbnValidator"
```

Expected output:
```
Passed! - Failed: 0, Passed: 3, Skipped: 0, Total: 3
```

### Integration Tests
**⚠️ Ensure Docker Desktop is running** (Testcontainers requirement)

```bash
# Run all ISBN-related integration tests
dotnet test Tests/Intellishelf.Integration.Tests/Intellishelf.Integration.Tests.csproj \
  --filter "FullyQualifiedName~AddBookByIsbn"
```

Expected output:
```
Passed! - Failed: 0, Passed: 6, Skipped: 0, Total: 6
- AddBookByIsbn_ValidIsbn10_ReturnsCreated
- AddBookByIsbn_ValidIsbn13_ReturnsCreated
- AddBookByIsbn_InvalidFormat_ReturnsBadRequest
- AddBookByIsbn_NotFound_ReturnsNotFound
- AddBookByIsbn_Duplicate_ReturnsConflict
- AddBookByIsbn_GoogleFails_FallsBackToAmazon
```

### Run All Tests
```bash
dotnet test Intellishelf.Api.sln -c Release
```

---

## Troubleshooting

### Issue: "Google Books API key is missing"
**Error**: `ArgumentNullException: GoogleBooks:ApiKey configuration is missing`

**Solution**:
```bash
dotnet user-secrets set "ExternalApis:GoogleBooks:ApiKey" "YOUR_API_KEY_HERE"
```

### Issue: "MongoDB connection failed"
**Error**: `MongoConnectionException: Unable to connect to localhost:27017`

**Solution**:
1. Verify MongoDB is running: `mongosh mongodb://localhost:27017`
2. Check connection string in `appsettings.Development.json`
3. Restart MongoDB: `docker restart mongodb` or `brew services restart mongodb-community@6.0`

### Issue: "Azure Blob Storage connection failed"
**Error**: `StorageException: Unable to connect to Azure Storage`

**Solution**:
1. Verify Azure Storage connection string: `dotnet user-secrets list | grep Azure:Storage`
2. Use Azurite for local development:
   ```bash
   docker run -d -p 10000:10000 mcr.microsoft.com/azure-storage/azurite azurite-blob --blobHost 0.0.0.0
   dotnet user-secrets set "Azure:Storage:ConnectionString" "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"
   ```

### Issue: "Invalid ISBN format"
**Error**: `InvalidIsbn: Invalid ISBN format: input must be 10 or 13 digits`

**Expected Behavior**: ISBN validation is simplified (no check digit verification).

**Valid formats**:
- ISBN-10: 10 characters (9 digits + digit or 'X'), with or without hyphens
- ISBN-13: 13 digits starting with 978 or 979, with or without hyphens

**Solution**: Verify input matches one of the valid patterns. External APIs will handle normalization.

### Issue: "Google Books API quota exceeded"
**Error**: `ExternalApiFailure: Rate limit exceeded (403 Forbidden)`

**Solution**:
1. Check quota in [Google Cloud Console](https://console.cloud.google.com/apis/api/books.googleapis.com/quotas)
2. Request quota increase or wait 24 hours for reset
3. Amazon API will be used as fallback automatically

### Issue: "Integration tests fail with Docker error"
**Error**: `ContainerLaunchException: Docker daemon is not running`

**Solution**:
1. Start Docker Desktop (macOS/Windows) or Docker daemon (Linux)
2. Verify: `docker ps` (should not error)
3. Re-run tests: `dotnet test`

---

## Next Steps

1. **Implement Service Logic**: Follow `specs/001-isbn-book-search/data-model.md`
2. **Add Integration Tests**: Follow `specs/001-isbn-book-search/spec.md` (Test Scenarios section)
3. **Update Agent Context**: Run `.specify/scripts/bash/update-agent-context.sh copilot`
4. **Generate Tasks**: Run `/speckit.tasks` to create task breakdown

---

**Feature Documentation**:
- Spec: `specs/001-isbn-book-search/spec.md`
- Research: `specs/001-isbn-book-search/research.md`
- Data Model: `specs/001-isbn-book-search/data-model.md`
- OpenAPI Contract: `specs/001-isbn-book-search/contracts/openapi.yaml`

**Support**:
- Check `AGENTS.md` for repository conventions
- Review `.specify/memory/constitution.md` for architectural principles
