# Active Context - January 2025

## Current Status
Taking a break from active development to refresh and organize the project state.

## Next Planned Work
1. **ISBN Search Integration** - Add ability to search books by ISBN via external API
2. **Integration Tests** - Add test coverage for API endpoints  
3. **Deployment Consideration** - Eventually move from local development to hosted environment

## Recent Context
- Core features are working locally
- No major bugs or issues currently
- Project is in a stable state for next development phase

## Technical Notes
- All core systems operational in local development
- File upload and storage working
- Authentication system stable
- AI parsing functional

## Active Decisions & Patterns
- Using JWT for authentication with short-lived access tokens (30 minutes)
- Implementing refresh token rotation for enhanced security
- Storing refresh tokens in MongoDB with automatic cleanup
- Implementing modular architecture with clean separation
- Leveraging AI for metadata enrichment
- Using TryResult pattern for consistent error handling
- Implementing pagination with MongoDB Skip/Limit for efficient data retrieval
- Supporting multiple sorting options for book collections
