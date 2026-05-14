# Bizcord microservices

A small .NET monorepo used for system integration exercises: several ASP.NET Core apps, shared NuGet-style projects, RabbitMQ messaging via **EasyNetQ**, and Docker.

## Repository layout

| Path | Purpose |
|------|---------|
| `apps/api-gateway` | Ocelot-based API gateway |
| `apps/messaging-microservice` | REST API for messages; JWT (`User` / `Service` roles), saga async create, optional Vault for RabbitMQ connection string, resilient publishes; see its README |
| `apps/sample-microservice` | Sample service (ping/pong messaging) |
| `apps/pong-microservice` | Consumer-style sample |
| `packages/MessageClient` | `IMessageClient` + EasyNetQ adapter, DI extensions |
| `packages/Shared.Contracts` | DTOs, integration events, and saga message contracts shared between services |
| `tests/E2E` | End-to-end tests |

`global.json` pins the solution to the **.NET 8** SDK line. On Windows, if `dotnet --version` shows 6.x but you have 8.x installed under `%LOCALAPPDATA%\Microsoft\dotnet`, run `.\build.ps1` from this folder or put that directory first on your `PATH`.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (optional, for RabbitMQ + containerized runs)

## Build

From this directory:

```powershell
dotnet build apps\messaging-microservice\messaging-microservice.sln -c Release
```

Or:

```powershell
.\build.ps1
```

Restore assumes a working NuGet configuration. This repo includes a root `nuget.config` that clears broken Visual Studio fallback package paths when needed.

## Messaging service (quick start)

Run with **Docker Compose** (RabbitMQ, optional **Vault** in dev mode, API — Swagger on port 8080):

```powershell
cd apps\messaging-microservice
docker compose up --build
```

- Swagger: [http://localhost:8080/swagger](http://localhost:8080/swagger) — use **Authorize** with a JWT whose `role` claim is `User` or `Service` as required by each endpoint (see service README).
- RabbitMQ management UI: [http://localhost:15672](http://localhost:15672) (guest / guest)
- Vault (dev): [http://localhost:8200](http://localhost:8200) — token set in that `docker-compose.yml` for local use only

Gateway routes for this service live under **`/messaging-microservice/...`** (see `apps/api-gateway/src/Configuration/ocelot.messaging-microservice.json`).

Run the API only on the host (Swagger on 5290). You need a reachable RabbitMQ for publishes to succeed (`RabbitMQ:ConnectionString` or `RabbitMQ__ConnectionString`), and a signing key for JWT (`Jwt:SigningKey` in `appsettings.Development.json` or environment):

```powershell
dotnet run --project apps\messaging-microservice\src\MessagingMicroservice.csproj
```

More detail: [apps/messaging-microservice/README.md](apps/messaging-microservice/README.md).

## Tests

```powershell
dotnet test apps\messaging-microservice\tests\MessagingMicroservice.Tests.csproj -c Release
```

Other apps may have their own solutions and READMEs under `apps/<name>/`.
