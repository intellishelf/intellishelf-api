# Integration tests and Docker

From the repository root, with the .NET 10 SDK and Docker available:

```bash
dotnet test --project tests/Intellishelf.Integration.Tests/Intellishelf.Integration.Tests.csproj
```

Testcontainers starts disposable Atlas Local and Azurite containers. The API is hosted by
`TestWebApplicationFactory` inside the test process; this suite does **not** test the API
Docker image or the Compose stack. The Mongo fixture creates an `intellishelf-test` database
in its container and applies the shared indexes. It does not use the API's configured remote
database. Tests use a test authentication handler. The fixture currently uses
`mongodb/mongodb-atlas-local:latest`, while Compose uses `:8.0`; `latest` can change.

## Troubleshooting in a constrained environment

Start by checking the daemon, the filesystem holding Docker's data, and existing containers:

```bash
docker info --format 'Driver={{.Driver}} DockerRootDir={{.DockerRootDir}}'
docker system df
docker ps -a
df -h
```

`docker system df` may not account for partial data left by failed image operations. A
`no space left on device` error while pulling or creating a container is a disk error, not
evidence that the application has exhausted RAM. Docker's `vfs` driver copies layers and can
consume much more space than the compressed image. Inspect what is using space before retrying;
repeated failed pulls may leave additional partial data. Remove only disposable build outputs
or caches whose ownership and contents are understood. Do not delete Docker's data directory
or its layer files as a general cleanup procedure: Docker metadata and layers must remain
consistent. If standard Docker cleanup does not recover space, use the environment's supported
recovery mechanism or a different Docker host.

A listening MongoDB port does not guarantee that Atlas Search is ready. Atlas Local may restart
`mongod` during initial setup, and Search indexes become queryable asynchronously. The shared
index setup waits for the search index and handles the observed startup restart. Atlas can add
default index settings and reorder field mappings when returning a definition, so a raw JSON
comparison may report a false difference. Search tests still use a fixed delay after inserting
books; a failure caused by indexing lag may need investigation rather than an immediate retry.

For a registry pull failure, check the actual error and redirects. In restricted environments,
permitting the registry hostname alone may not permit its image download hosts. Use the
platform's supported network configuration; keep TLS verification enabled. Avoid printing
registry credentials or connection strings in diagnostics.

## One observed Codex Cloud run

On 8 November 2026, an environment with a 7.9 GiB filesystem and Docker's `vfs` driver ran
out of space unpacking Atlas Local. Failed pulls left partial layer directories that were not
shown by `docker system df`. After identifying and removing the specific unreferenced remnants,
a temporary flattened image was assembled from the official Atlas Local image (MongoDB 8.3.13).
Its uncompressed imported image was approximately 1.27 GB. A temporary Azurite image was built
from the official npm package (3.37.0) because the Microsoft registry redirected to a host not
permitted by that environment's network policy. These temporary images were tagged with the
names expected by the fixtures. With them, all 32 integration tests passed; the API ran in the
test host, not in Docker.

This was a diagnostic workaround, not a supported or reproducible setup script. Flattening and
rebuilding images can change image metadata and behavior; do not assume they are equivalent to
the official images. If reused, record the image source, version, digest, and modifications, and
prefer official pinned images on a Docker host with adequate space. Image tags alone are not
proof of image provenance; inspect the image ID and repository digests with `docker image inspect`.
The run does not establish a minimum disk or RAM requirement for the app.
