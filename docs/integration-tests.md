# Integration tests and Docker

Run from the repository root with the .NET SDK specified in `global.json` and a running Docker daemon:

```bash
dotnet test --project tests/Intellishelf.Integration.Tests/Intellishelf.Integration.Tests.csproj
```

Testcontainers starts disposable MongoDB Atlas Local and Azurite containers. The test fixture
initializes the indexes in its MongoDB container, and the API runs in the test process with test
authentication. The tests do not use the API's configured remote database, and they do not test
the API Docker image or Docker Compose.

The fixture uses `mongodb/mongodb-atlas-local:latest` and `mcr.microsoft.com/azure-storage/azurite:latest`.
Compose uses Atlas Local `:8.0`. The `latest` images can change between runs.

## Troubleshooting

If containers cannot start, check the Docker daemon and available disk space:

```bash
docker info --format 'Driver={{.Driver}} DockerRootDir={{.DockerRootDir}}'
docker system df
docker ps -a
df -h
```

A `no space left on device` error while pulling an image or starting a container indicates a disk
problem. Docker's `vfs` storage driver can consume substantial space because it copies image layers.
`docker system df` may not include partial data left by failed pulls. Inspect the storage before
cleaning up; use Docker's supported cleanup or your environment's recovery mechanism, and remove
only disposable data you understand.

Atlas Search starts separately from MongoDB. The shared setup waits for each search index to be
ready. Indexing new documents is also asynchronous; search tests currently use a fixed delay after
inserting books, so indexing lag can cause intermittent failures.

In one Codex Cloud run, Atlas Local failed to unpack on a 7.9 GiB filesystem using Docker's `vfs`
driver. A separate image download was blocked by a registry redirect. Those were environment
constraints, not evidence of an application memory limit. If image pulls fail, inspect disk and
network errors and use the environment's supported configuration. Do not disable TLS verification.
