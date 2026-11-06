---
name: run-intellishelf-api
description: Start, drive, and stop the Intellishelf API locally to confirm a change works against the running app. Use in ADLC verify or when asked to run the API and exercise endpoints.
---

# Run Intellishelf API

Start the API, drive it over HTTP, and stop exactly what you started.

## Start

Configuration comes from `dotnet user-secrets` (database, storage, auth, AI keys). Check that the
keys exist with `dotnet user-secrets list --project src/Intellishelf.Api | cut -d= -f1`; never
print or copy secret values.

```bash
dotnet run --project src/Intellishelf.Api/Intellishelf.Api.csproj --launch-profile http \
  > "${TMPDIR:-/tmp}/intellishelf-api.log" 2>&1 &
echo $! > "${TMPDIR:-/tmp}/intellishelf-api.pid"
```

Wait until it is healthy (the app uses path base `/api`):

```bash
for i in $(seq 1 60); do
  curl -fs http://localhost:8080/api/health && break
  sleep 2
done
```

If it never becomes healthy, read the log and report the startup error. If port 8080 is already in
use, report it instead of killing a process you did not start.

**Fully local stack:** `docker compose up -d --build` runs the API with MongoDB and Azurite on the
same port. Use it when user-secrets are unavailable; stop it with `docker compose down`.

## Drive

- Health: `GET http://localhost:8080/api/health`
- OpenAPI document: `GET http://localhost:8080/api/openapi/v1.json`
- Scalar UI: `http://localhost:8080/api/scalar`
- Endpoints require auth (`/api/books` returns 401 without it). Register or log in via
  `POST /api/auth/register` / `POST /api/auth/login`, then send `Authorization: Bearer <token>`.
  user-secrets may point at a shared database, so use throwaway test accounts and data only.

Record the requests made and the responses observed as verification evidence.

## Stop

```bash
kill "$(cat "${TMPDIR:-/tmp}/intellishelf-api.pid")" && rm -f "${TMPDIR:-/tmp}/intellishelf-api.pid"
```

Confirm `curl -fs http://localhost:8080/api/health` no longer responds.
