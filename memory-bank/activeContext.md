# Active Context

## Current Work Focus
- Book management system implementation
- Advanced search and filtering capabilities
- Enhanced authentication system with refresh tokens

## Recent Changes
- Implemented Book CRUD operations
- Created data access and service layers
- Developed AI-assisted book parsing
- Added Azure blob storage for images
- Implemented refresh token functionality with token rotation
- Added background service for expired token cleanup
- Shortened access token lifetime for better security

## Next Steps
- Single Sign-On (SSO) integration
- Add AI for chatting to bookshelves
- Add integration with Perplexity search to search book information live
- Develop more robust file storage services
- Add payment and subscriptions

## Active Decisions
- Using JWT for authentication with short-lived access tokens (30 minutes)
- Implementing refresh token rotation for enhanced security
- Storing refresh tokens in MongoDB with automatic cleanup
- Implementing modular architecture
- Leveraging AI for metadata enrichment

## Collaboration and Communication
- Regular code reviews
- Continuous integration practices
- Documentation updates
- Knowledge sharing sessions
