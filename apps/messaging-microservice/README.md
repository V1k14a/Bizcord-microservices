# Messaging microservice

Track A — **Messaging service**: REST API for messages, in-memory persistence, **JWT authentication**, and **MessagePostedEvent** published via RabbitMQ (EasyNetQ). Includes an optional **orchestrated saga** for async creates, **HashiCorp Vault** integration for secrets, and **resilience** around broker publishes.

## Domain

- **Entity:** `Message` (identity, channel, author, content, timestamps)
- **Value object:** `MessageContent` (non-empty text)
- **Integration event (shared contract):** `Shared.Contracts.Events.MessagePostedEvent`
- **Saga messages (shared contract):** `Shared.Contracts.Sagas` — `InitiateMessagePost`, `MessagePersisted`, `MessagePostedPublishFailed`, `MessagePostSagaCompleted`

## Authentication

All HTTP endpoints require a **Bearer JWT** signed with the same symmetric key the API gateway uses. Configure one of:

- `Jwt:SigningKey` (preferred in this service), or
- `AuthenticationProviderKey` (alias for the same value)

The token must include a **`role`** claim:

| Role | Use |
|------|-----|
| `User` | `/api/messages` (CRUD + `POST .../async`) |
| `Service` | `/api/internal/messages/channel/{channelId}` (east/west, other microservices) |

**Swagger:** click **Authorize**, then `Bearer <your-jwt>`.

Local development defaults are in `src/appsettings.Development.json`. Docker Compose sets `Jwt__SigningKey` to match the sample API gateway key so tokens from the gateway faucet can work end-to-end if you align environments.

## Run locally

```powershell
dotnet run --project src/MessagingMicroservice.csproj
```

- Swagger: `http://localhost:5290/swagger`
- RabbitMQ (required for real publishes): set `RabbitMQ__ConnectionString`, for example  
  `host=localhost;username=guest;password=guest`

## API

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/messages` | `User` | List messages |
| GET | `/api/messages/{id}` | `User` | Get one |
| POST | `/api/messages` | `User` | Create (publishes `MessagePostedEvent`) |
| POST | `/api/messages/async` | `User` | Start **saga** (202 + `sagaId`; persists then publishes event, compensates on publish failure) |
| PUT | `/api/messages/{id}` | `User` | Update content |
| DELETE | `/api/messages/{id}` | `User` | Delete |
| GET | `/api/internal/messages/channel/{channelId}` | `Service` | List messages for a channel (internal callers) |

## API gateway (Ocelot)

Routes are merged from `apps/api-gateway/src/Configuration/ocelot.messaging-microservice.json`. Upstream paths use the prefix `/messaging-microservice/...` and forward to this service’s `/api/...`. Internal routes require JWT **`role: Service`** on the gateway.

## Reliability and operations

- **Publish path:** `ResilientMessageClient` applies Polly **timeout + retries** around `IMessageClient.PublishAsync`.
- **Repository path:** `MessagesService` uses a **bounded cancellation** timeout on storage calls.
- **Failure analysis:** see [FAILURE_POINTS.md](FAILURE_POINTS.md) (includes saga design notes).

## HashiCorp Vault (optional)

When `Vault:Address` and `Vault:Token` are set, startup reads **KV v2** (default mount `secret`, path `messaging`) and, if present, applies **`rabbitmq_connection_string`** to `RabbitMQ:ConnectionString`. Example after Vault is running:

```text
vault kv put secret/messaging rabbitmq_connection_string="host=rabbitmq;username=guest;password=guest"
```

Reference server config: [vault/config.hcl](vault/config.hcl). Docker Compose includes a **dev-mode** Vault; adjust for production (unseal, policies, etc.).

## Tests

From this directory:

```powershell
dotnet test tests/MessagingMicroservice.Tests.csproj -c Release
```

Integration tests mint JWTs with the same test signing key as the test host (`JwtTestTokens`).

## Docker (image only)

From repo root (folder containing `apps/` and `packages/`):

```powershell
docker build -f apps/messaging-microservice/Dockerfile -t messaging-microservice .
```

## Docker Compose (RabbitMQ + Vault + service)

```powershell
cd apps/messaging-microservice
docker compose up --build
```

- API / Swagger: `http://localhost:8080/swagger`
- RabbitMQ management: `http://localhost:15672` (guest / guest)
- Vault UI (dev): `http://localhost:8200` — root token in Compose: `dev-root-token` (dev only; do not use in production)
