# Intellishelf.Unit.Tests

This project contains unit tests for the Intellishelf application.

## Framework
- **xUnit** - Primary testing framework
- **Target Framework**: .NET 9.0

## Test Structure
- `Books/` - Tests for Book-related functionality
  - `BookServiceTests.cs` - Unit tests for the BookService (main flows)

## Dependencies
- References to `Intellishelf.Domain` and `Intellishelf.Common` projects
- xUnit test framework with Visual Studio test runner support
- Moq for mocking dependencies
- Code coverage with Coverlet

## Running Tests

### Command Line
```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run unit tests specifically
dotnet test Tests/Intellishelf.Unit.Tests/

# Run specific test class
dotnet test --filter "BookServiceTests"
```

### Visual Studio Code
- Use the Test Explorer or run tests directly from the editor

## Test Coverage
The tests cover:
- **BookService**: Main flows for TryGetBooksAsync (success/error) and TryAddBookAsync (success)

## Future Improvements
- Add more service tests
- Add controller tests
- Add performance tests for critical paths
- Set up automated test reporting
