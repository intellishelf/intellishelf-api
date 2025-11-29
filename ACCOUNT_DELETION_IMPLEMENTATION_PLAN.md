# Account Deletion Feature - Implementation Plan

**Status:** Ready for Implementation
**Created:** 2025-11-29
**Feature:** End-to-end user account deletion from Settings page
**Related Memory Bank:** `memory-bank/release-gaps-todo.md` - Gap #3 (No Account Deletion / Profile Management)

---

## Overview

Implement hard-delete account deletion feature allowing users to permanently delete their accounts from the Settings page, including all associated data (books, images, tokens).

### Key Decisions
- ✅ **Hard delete** (permanent, immediate) - no soft delete
- ✅ **No password confirmation** - just confirmation dialog with checkbox in red palette
- ✅ **Best-effort deletion** - delete what we can, log warnings for failures, don't fail entire operation
- ✅ **GDPR-compliant** - permanent deletion of all user data

---

## Data Cascade Deletion

When a user deletes their account, the following data must be removed:

1. **Books** (MongoDB: `Books` collection)
   - All books where `UserId` matches
   - Foreign key: `UserId` (ObjectId)

2. **Book Cover Images** (Azure Blob Storage)
   - All blobs in `userFiles/{userId}/` directory
   - Referenced via `CoverImageUrl` in book records

3. **Refresh Tokens** (MongoDB: `RefreshTokens` collection)
   - All tokens where `UserId` matches
   - Foreign key: `UserId` (string)

4. **User Record** (MongoDB: `Users` collection)
   - The user document itself

5. **Chat History** (No action needed)
   - ✅ Chat is stateless, not persisted server-side

---

## Backend Implementation

### 1. Error Codes & Mappings

#### File: `src/Intellishelf.Domain/Users/Models/UserErrorCodes.cs`
```csharp
public const string DeletionFailed = "User.DeletionFailed";
```

#### File: `src/Intellishelf.Api/Controllers/ApiControllerBase.cs`
Add to `MapErrorToStatusCode` switch:
```csharp
UserErrorCodes.DeletionFailed => StatusCodes.Status500InternalServerError,
```

---

### 2. Data Access Layer (DAO)

#### IUserDao Interface
**File:** `src/Intellishelf.Domain/Users/DataAccess/IUserDao.cs`

Add method:
```csharp
Task<TryResult<bool>> TryDeleteUserAsync(string userId);
```

#### UserDao Implementation
**File:** `src/Intellishelf.Data/Users/DataAccess/UserDao.cs`

```csharp
public async Task<TryResult<bool>> TryDeleteUserAsync(string userId)
{
    var result = await _usersCollection.DeleteOneAsync(u => u.Id == userId);

    if (result.DeletedCount == 0)
        return new Error(UserErrorCodes.UserNotFound, $"User with id {userId} not found");

    return true;
}
```

#### IRefreshTokenDao Interface
**File:** `src/Intellishelf.Domain/Users/DataAccess/IRefreshTokenDao.cs`

Add method:
```csharp
Task<TryResult<long>> TryDeleteAllByUserIdAsync(string userId);
```

#### RefreshTokenDao Implementation
**File:** `src/Intellishelf.Data/Users/DataAccess/RefreshTokenDao.cs`

```csharp
public async Task<TryResult<long>> TryDeleteAllByUserIdAsync(string userId)
{
    var result = await _refreshTokensCollection.DeleteManyAsync(rt => rt.UserId == userId);
    return result.DeletedCount;
}
```

#### IBookDao Interface
**File:** `src/Intellishelf.Domain/Books/DataAccess/IBookDao.cs`

Add method:
```csharp
Task<TryResult<long>> DeleteAllBooksByUserAsync(string userId);
```

**Note:** `GetBooksAsync(string userId)` already exists at line 8, so we reuse that.

#### BookDao Implementation
**File:** `src/Intellishelf.Data/Books/DataAccess/BookDao.cs`

Add method (after `DeleteBookAsync` around line 197):
```csharp
public async Task<TryResult<long>> DeleteAllBooksByUserAsync(string userId)
{
    var userIdObject = ObjectId.Parse(userId);
    var result = await _booksCollection.DeleteManyAsync(b => b.UserId == userIdObject);
    return result.DeletedCount;
}
```

---

### 3. File Storage Layer

#### IFileStorageService Interface
**File:** `src/Intellishelf.Domain/Files/Services/IFileStorageService.cs`

Add method:
```csharp
Task<TryResult<int>> DeleteAllUserFilesAsync(string userId);
```

#### FileStorageService Implementation
**File:** `src/Intellishelf.Domain/Files/Services/FileStorageService.cs`

```csharp
public async Task<TryResult<int>> DeleteAllUserFilesAsync(string userId)
{
    try
    {
        await containerClient.CreateIfNotExistsAsync();

        var prefix = $"userFiles/{userId}/";
        var deletedCount = 0;

        await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix))
        {
            try
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                await blobClient.DeleteIfExistsAsync();
                deletedCount++;
            }
            catch (Exception ex)
            {
                // Log warning but continue - best effort deletion
                Console.WriteLine($"Warning: Failed to delete blob {blobItem.Name}: {ex.Message}");
            }
        }

        return deletedCount;
    }
    catch (Exception ex)
    {
        return new Error(FileErrorCodes.DeletionFailed, $"Failed to delete user files: {ex.Message}");
    }
}
```

---

### 4. Service Layer

Add account deletion logic directly to `AuthService` or existing services. No new service needed.

The deletion orchestration will be handled directly in the AuthController endpoint using injected DAOs and services that already exist.

---

### 5. Controller Layer

#### Add Delete Endpoint to AuthController
**File:** `src/Intellishelf.Api/Controllers/AuthController.cs`

Add this endpoint after `Me()` method:
```csharp
[HttpDelete("account")]
public async Task<IActionResult> DeleteAccount()
{
    var userId = CurrentUserId;

    var result = await authService.TryDeleteAccountAsync(userId);

    if (!result.IsSuccess)
        return HandleErrorResponse(result.Error);

    // Clear authentication cookie after successful deletion
    ClearRefreshCookie();
    await HttpContext.SignOutAsync(AuthConfig.CookieScheme);

    return NoContent();
}
```

#### Add Deletion Method to AuthService
**File:** `src/Intellishelf.Domain/Users/Services/AuthService.cs`

Extend the constructor to inject:
- `IBookDao bookDao`
- `IRefreshTokenDao refreshTokenDao`
- `IFileStorageService fileStorageService`
- `ILogger<AuthService> logger`

Add method:
```csharp
public async Task<TryResult> TryDeleteAccountAsync(string userId)
{
    // 1. Delete all book cover images (best effort)
    // 2. Delete all books (best effort)
    // 3. Delete all refresh tokens (best effort)
    // 4. Delete user record (CRITICAL)

    // Best-effort cascading deletion with logging
    // (See implementation plan for full code)
}
```

---

## Frontend Implementation

### 6. API Hook

#### File: `frontend/src/hooks/auth/useDeleteAccount.ts` *(NEW FILE)*

```typescript
import { useMutation } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { api } from '@/lib/api';
import { toast } from 'sonner';

export const useDeleteAccount = () => {
  const navigate = useNavigate();

  return useMutation({
    mutationFn: () => api.delete('/auth/account'),
    onSuccess: () => {
      toast.success('Account deleted successfully');
      // Redirect to auth page
      navigate('/auth');
    },
    onError: (error: Error) => {
      toast.error(`Failed to delete account: ${error.message}`);
    },
  });
};
```

---

### 7. Settings UI - Account Deletion Section

#### File: `frontend/src/pages/Settings.tsx`

Add "Danger Zone" card at the end of the settings page (after Data card):

```typescript
import { useState } from "react";
import { useDeleteAccount } from "@/hooks/auth/useDeleteAccount";
import { Checkbox } from "@/components/ui/checkbox";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";

// Inside component:
const [confirmationChecked, setConfirmationChecked] = useState(false);
const [isDialogOpen, setIsDialogOpen] = useState(false);
const deleteAccountMutation = useDeleteAccount();

const handleDeleteAccount = () => {
  if (!confirmationChecked) {
    toast.error("Please confirm that you understand this action cannot be undone");
    return;
  }

  deleteAccountMutation.mutate();
};

// Add this card at the end of the page:
<Card className="bg-card border-destructive p-6">
  <h2 className="text-xl font-semibold text-destructive mb-2">
    Danger Zone
  </h2>
  <p className="text-sm text-muted-foreground mb-4">
    Irreversible actions that will permanently affect your account
  </p>

  <AlertDialog open={isDialogOpen} onOpenChange={(open) => {
    setIsDialogOpen(open);
    if (!open) {
      setConfirmationChecked(false);
    }
  }}>
    <AlertDialogTrigger asChild>
      <Button
        variant="destructive"
        className="w-full"
        disabled={deleteAccountMutation.isPending}
      >
        Delete Account
      </Button>
    </AlertDialogTrigger>
    <AlertDialogContent>
      <AlertDialogHeader>
        <AlertDialogTitle className="text-destructive">
          Delete Account Permanently?
        </AlertDialogTitle>
        <AlertDialogDescription className="space-y-4">
          <p>
            This action <span className="font-semibold text-destructive">cannot be undone</span>.
            This will permanently delete your account and remove all your data from our servers.
          </p>
          <p className="text-sm">
            The following data will be permanently deleted:
          </p>
          <ul className="text-sm list-disc list-inside space-y-1 ml-2">
            <li>All your books and reading lists</li>
            <li>All book cover images</li>
            <li>Your account information</li>
            <li>All session data</li>
          </ul>

          <div className="flex items-start space-x-2 bg-destructive/10 p-3 rounded-md border border-destructive/20">
            <Checkbox
              id="confirm-delete"
              checked={confirmationChecked}
              onCheckedChange={(checked) => setConfirmationChecked(checked === true)}
              className="mt-0.5 border-destructive data-[state=checked]:bg-destructive data-[state=checked]:border-destructive"
            />
            <label
              htmlFor="confirm-delete"
              className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70 cursor-pointer"
            >
              I understand that this action is permanent and cannot be undone
            </label>
          </div>
        </AlertDialogDescription>
      </AlertDialogHeader>
      <AlertDialogFooter>
        <AlertDialogCancel disabled={deleteAccountMutation.isPending}>
          Cancel
        </AlertDialogCancel>
        <AlertDialogAction
          onClick={(e) => {
            e.preventDefault();
            handleDeleteAccount();
          }}
          disabled={!confirmationChecked || deleteAccountMutation.isPending}
          className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
        >
          {deleteAccountMutation.isPending ? "Deleting..." : "Delete Account"}
        </AlertDialogAction>
      </AlertDialogFooter>
    </AlertDialogContent>
  </AlertDialog>
</Card>
```

---

## Integration Tests

### 8. Add User Deletion Tests to Existing Test Infrastructure

Add tests to existing test patterns (can be in AuthControllerTests or create minimal test class)

```csharp
using System.Net;
using Intellishelf.Data.Books.Entities;
using Intellishelf.Integration.Tests.Infra;
using Intellishelf.Integration.Tests.Infra.Fixtures;
using MongoDB.Bson;
using Xunit;

namespace Intellishelf.Integration.Tests;

[Collection("Integration Tests")]
public sealed class UserDeletionTests : IAsyncLifetime, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly MongoDbFixture _mongoDbFixture;
    private readonly AzuriteFixture _azuriteFixture;

    public UserDeletionTests(MongoDbFixture mongoDbFixture, AzuriteFixture azuriteFixture)
    {
        _factory = new TestWebApplicationFactory(mongoDbFixture, azuriteFixture);
        _client = _factory.CreateClient();
        _mongoDbFixture = mongoDbFixture;
        _azuriteFixture = azuriteFixture;
    }

    public async Task InitializeAsync()
    {
        await _mongoDbFixture.ClearBooksAsync();
        await _mongoDbFixture.SeedDefaultUserAsync();
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenDeleteAccount_ThenAllDataRemoved()
    {
        // Arrange
        var userId = DefaultTestUsers.Authenticated.Id;

        // Seed books with cover images
        var coverUrl1 = await _azuriteFixture.SeedBlobAsync(
            $"userFiles/{userId}/cover1.jpg",
            "cover1"u8.ToArray());
        var coverUrl2 = await _azuriteFixture.SeedBlobAsync(
            $"userFiles/{userId}/cover2.jpg",
            "cover2"u8.ToArray());

        var book1 = CreateBookEntity("Book 1", "Author 1", coverImageUrl: coverUrl1);
        var book2 = CreateBookEntity("Book 2", "Author 2", coverImageUrl: coverUrl2);
        var book3 = CreateBookEntity("Book 3", "Author 3");

        await _mongoDbFixture.SeedBooksAsync(book1, book2, book3);
        await _mongoDbFixture.SeedRefreshTokenAsync(userId, "test-token-1");

        // Act
        var response = await _client.DeleteAsync("/api/auth/account");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify all data deleted
        var userExists = await _mongoDbFixture.UserExistsAsync(userId);
        Assert.False(userExists);

        var books = await _mongoDbFixture.GetBooksByUserIdAsync(userId);
        Assert.Empty(books);

        var tokens = await _mongoDbFixture.GetRefreshTokensByUserIdAsync(userId);
        Assert.Empty(tokens);

        var blob1Exists = await _azuriteFixture.BlobExistsFromUrlAsync(coverUrl1);
        var blob2Exists = await _azuriteFixture.BlobExistsFromUrlAsync(coverUrl2);
        Assert.False(blob1Exists);
        Assert.False(blob2Exists);
    }

    [Fact]
    public async Task GivenUserWithNoBooksOrFiles_WhenDeleteAccount_ThenUserDeleted()
    {
        // Arrange - user exists but has no books or files
        var userId = DefaultTestUsers.Authenticated.Id;

        // Act
        var response = await _client.DeleteAsync("/api/auth/account");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var userExists = await _mongoDbFixture.UserExistsAsync(userId);
        Assert.False(userExists);
    }

    [Fact]
    public async Task GivenUnauthenticatedRequest_WhenDeleteAccount_ThenUnauthorized()
    {
        // Arrange
        var unauthFactory = new TestWebApplicationFactory(_mongoDbFixture, _azuriteFixture, authenticated: false);
        using var unauthClient = unauthFactory.CreateClient();

        // Act
        var response = await unauthClient.DeleteAsync("/api/auth/account");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GivenUserWithManyBooks_WhenDeleteAccount_ThenAllBooksDeleted()
    {
        // Arrange
        var userId = DefaultTestUsers.Authenticated.Id;
        var books = Enumerable.Range(1, 20)
            .Select(i => CreateBookEntity($"Book {i}", $"Author {i}"))
            .ToArray();

        await _mongoDbFixture.SeedBooksAsync(books);

        // Act
        var response = await _client.DeleteAsync("/api/auth/account");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var remainingBooks = await _mongoDbFixture.GetBooksByUserIdAsync(userId);
        Assert.Empty(remainingBooks);
    }

    private static BookEntity CreateBookEntity(
        string title,
        string author,
        string? userId = null,
        string? coverImageUrl = null)
    {
        var timestamp = DateTime.UtcNow;
        return new BookEntity
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Title = title,
            Authors = [author],
            CreatedDate = timestamp,
            ModifiedDate = timestamp,
            UserId = ObjectId.Parse(userId ?? DefaultTestUsers.Authenticated.Id),
            CoverImageUrl = coverImageUrl,
            Status = ReadingStatus.Unread
        };
    }

    public void Dispose() => _factory.Dispose();
    public Task DisposeAsync() => Task.CompletedTask;
}
```

---

### 9. Extend Test Fixtures (Optional)

#### MongoDbFixture Helper Methods
**File:** `tests/Intellishelf.Integration.Tests/Infra/Fixtures/MongoDbFixture.cs`

Optionally add these helper methods for convenience:
```csharp
public async Task<bool> UserExistsAsync(string userId) =>
    await Database.GetCollection<UserEntity>(UserEntity.CollectionName)
        .Find(u => u.Id == userId)
        .AnyAsync();

public async Task<List<BookEntity>> GetBooksByUserIdAsync(string userId)
{
    var userIdObject = ObjectId.Parse(userId);
    return await Database.GetCollection<BookEntity>(BookEntity.CollectionName)
        .Find(b => b.UserId == userIdObject)
        .ToListAsync();
}

public async Task SeedRefreshTokenAsync(string userId, string token)
{
    var entity = new RefreshTokenEntity
    {
        Id = ObjectId.GenerateNewId().ToString(),
        Token = token,
        UserId = userId,
        ExpiryDate = DateTime.UtcNow.AddDays(7),
        IsRevoked = false,
        CreatedAt = DateTime.UtcNow
    };

    await Database.GetCollection<RefreshTokenEntity>("RefreshTokens")
        .InsertOneAsync(entity);
}

public async Task<List<RefreshTokenEntity>> GetRefreshTokensByUserIdAsync(string userId) =>
    await Database.GetCollection<RefreshTokenEntity>("RefreshTokens")
        .Find(rt => rt.UserId == userId)
        .ToListAsync();
```

---

## Implementation Order

Execute in this sequence:

### Backend (2 hours)
1. **Error codes** (5 min)
   - Update `UserErrorCodes.cs`
   - Update `ApiControllerBase.cs`

2. **DAO layer** (30 min)
   - Extend `IUserDao` + implement in `UserDao`
   - Extend `IRefreshTokenDao` + implement in `RefreshTokenDao`
   - Add `DeleteAllBooksByUserAsync` to `IBookDao` + implement in `BookDao`

3. **File storage** (20 min)
   - Extend `IFileStorageService` + implement in `FileStorageService`

4. **Service layer** (30 min)
   - Add dependencies to `AuthService` constructor (IBookDao, IRefreshTokenDao, IFileStorageService, ILogger)
   - Add `TryDeleteAccountAsync` method to `AuthService`

5. **Controller** (5 min)
   - Add `DELETE /auth/account` endpoint to `AuthController`

### Frontend (45 min)
6. **API hook** (15 min)
   - Create `useDeleteAccount.ts`

7. **Settings UI** (30 min)
   - Add Danger Zone card with delete button
   - Add AlertDialog with checkbox confirmation

### Testing (30 min)
8. **Integration tests** (30 min)
   - Add test methods following existing patterns
   - Optionally extend `MongoDbFixture` with helper methods
   - Run tests and verify

**Total estimated time: 3 hours**

---

## Critical Files Reference

Read these files before starting implementation:

1. **`src/Intellishelf.Domain/Books/Services/BookService.cs`**
   - Service layer patterns, file cleanup, dependency injection

2. **`src/Intellishelf.Data/Users/DataAccess/UserDao.cs`**
   - MongoDB query patterns, TryResult usage, error handling

3. **`src/Intellishelf.Domain/Files/Services/FileStorageService.cs`**
   - Azure Blob Storage patterns, path conventions

4. **`tests/Intellishelf.Integration.Tests/BooksTests.cs`**
   - Integration test patterns, fixture usage, assertions

5. **`src/Intellishelf.Api/Controllers/AuthController.cs`**
   - Controller patterns, authentication, cookie management

---

## Testing Checklist

### Integration Tests
- [x] User with books, images, and tokens - all deleted
- [x] User with no books or files - user deleted
- [x] Unauthenticated request - 401 Unauthorized
- [x] User with 20+ books - all deleted

### Manual Testing
- [ ] Delete account via Settings UI
- [ ] Verify redirect to login page
- [ ] Verify cannot log in with deleted credentials
- [ ] Verify MongoDB has no user record
- [ ] Verify MongoDB has no books for user
- [ ] Verify MongoDB has no refresh tokens for user
- [ ] Verify Azure Blob Storage has no files in `userFiles/{userId}/`
- [ ] Test with user having 0 books
- [ ] Test with user having 100+ books
- [ ] Test checkbox behavior (cannot submit without checking)
- [ ] Test Cancel button clears checkbox

---

## Error Handling Strategy

### Best-Effort Deletion Logic
1. **Files** (Azure Blob Storage) - Log warning, continue
2. **Books** (MongoDB) - Log warning, continue
3. **Refresh Tokens** (MongoDB) - Log warning, continue
4. **User Record** (MongoDB) - **MUST SUCCEED** or fail entire operation

### Logging
- **Information**: Successful deletions with counts
- **Warning**: Non-critical failures (files, books, tokens)
- **Error**: Critical failure (user record deletion)

---

## Security Considerations

- ✅ Endpoint requires authentication (`[Authorize]` attribute)
- ✅ User can only delete their own account (`CurrentUserId`)
- ✅ No password confirmation (works for both email and Google OAuth users)
- ✅ Clear authentication cookies after deletion
- ✅ Frontend confirmation with checkbox in red palette
- ✅ GDPR-compliant permanent deletion

---

## Future Enhancements (Out of Scope)

- Soft delete with grace period for recovery
- Email confirmation before deletion
- Export data before deletion
- Admin dashboard for reviewing deletion requests
- Deletion analytics/metrics
