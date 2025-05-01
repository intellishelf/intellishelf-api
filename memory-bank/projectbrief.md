# Intellishelf Project Brief

## Project Overview
Intellishelf is a comprehensive digital bookshelf management application powered with AI and built with a .NET.

## Core Objectives
- Provide a robust bookshelf management system
- Support user authentication and authorization
- Enable book-related operations (add, update, delete)
- Implement AI-assisted capabilites: book parsing, chatting to a bookshelf

## Key Components
- intellishelf-api (this project)
    - Intellishelf.Api: Web API layer
    - Intellishelf.Domain: Business logic and services
    - Intellishelf.Data: Data access and persistence
    - Intellishelf.Common: Shared utilities and error handling
- intellishelf-mobile (MAUI mobile app - different project)
- intellishelf-web (Web frontend on React - different project)

## Primary Features
- User Registration and Authentication
- Book CRUD Operations
- ISBN search via some API
- AI Book Parsing from Text
- AI chat with access to user books via MCP protocol, live search via Perplexity
- File Storage Management for book covers, user profile
- Subscriptions: free and pro version

## Technical Stack
- .NET Core
- C#
- JWT Authentication
