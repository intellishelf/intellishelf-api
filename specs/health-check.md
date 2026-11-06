# Intellishelf API Health Check Test Plan

## Application Overview

Scope: ONLY the health check endpoint, `GET http://localhost:8080/api/health`, plus the unprefixed `GET http://localhost:8080/health`. Auth, books and UI flows are out of scope. Verified live in the planner browser (Playwright MCP, fresh context with no cookies, against the running API at localhost:8080). Observed for both URLs: HTTP 200, Content-Type `text/plain` (no charset parameter), body text exactly `Healthy`, no authentication or redirect. Observed response headers on both: `cache-control: no-store, no-cache`, `pragma: no-cache`, `expires: Thu, 01 Jan 1970 00:00:00 GMT`, `server: Kestrel`, `transfer-encoding: chunked`. The browser console showed one unrelated error, a 404 for /favicon.ico, which is expected because the endpoint serves no HTML. Code basis (from earlier repo review, not re-verified in this session): Program.cs registers `AddHealthChecks()` and `MapHealthChecks("/health")` under `UsePathBase("/api")`. UsePathBase does not enforce the prefix, so the endpoint is reachable with or without `/api`. planner_setup_page with seed.spec.ts succeeded with no errors, so the earlier test.describe() failure is resolved. Starting state for every scenario: API running locally on port 8080, no cookies, no Authorization header.

## Test Scenarios

### 1. Health Check Endpoint

**Seed:** `seed.spec.ts`

#### 1.1. Health endpoint returns 200 with body Healthy

**File:** `tests/health-check/status-and-body.spec.ts`

**Steps:**
  1. Send an unauthenticated GET request to http://localhost:8080/api/health (e.g. with the Playwright `request` fixture).
    - expect: Response status is 200
    - expect: Response body text is exactly `Healthy`

#### 1.2. Health endpoint returns a plain text content type

**File:** `tests/health-check/content-type.spec.ts`

**Steps:**
  1. Send a GET request to http://localhost:8080/api/health and read the response headers.
    - expect: Content-Type header starts with `text/plain` (observed value is exactly `text/plain`, so do not assert a charset)
    - expect: Cache-Control header contains `no-store` (observed `no-store, no-cache`)

#### 1.3. Health endpoint requires no authentication

**File:** `tests/health-check/no-auth.spec.ts`

**Steps:**
  1. Send a GET request to http://localhost:8080/api/health with no cookies and no Authorization header.
    - expect: Response status is 200, not 401, 403, or a redirect to a login or Google OAuth page
    - expect: Body is `Healthy`

#### 1.4. Unprefixed /health also returns 200 Healthy (path base is optional)

**File:** `tests/health-check/path-base.spec.ts`

**Steps:**
  1. Send an unauthenticated GET request to http://localhost:8080/health (without the /api prefix).
    - expect: Response status is 200
    - expect: Response body text is exactly `Healthy`
    - expect: Content-Type header starts with `text/plain`
