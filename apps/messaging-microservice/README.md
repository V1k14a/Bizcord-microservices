# Messaging microservice

Track A — **Messaging service**: REST API for messages, in-memory persistence, and **MessagePostedEvent** published via RabbitMQ (EasyNetQ).

## Domain

- **Entity:** `Message` (identity, channel, author, content, timestamps)
- **Value object:** `MessageContent` (non-empty text)
- **Integration event (shared contract):** `Shared.Contracts.Events.MessagePostedEvent`

## Run locally

```bash
dotnet run --project src/MessagingMicroservice.csproj
```

Swagger: `http://localhost:5290/swagger`

Set RabbitMQ (optional for local; required for real publish):

```text
RabbitMQ__ConnectionString=host=localhost;username=guest;password=guest
```

## API

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/messages` | List messages |
| GET | `/api/messages/{id}` | Get one |
| POST | `/api/messages` | Create (publishes `MessagePostedEvent`) |
| PUT | `/api/messages/{id}` | Update content |
| DELETE | `/api/messages/{id}` | Delete |

## Tests

```bash
dotnet test tests/MessagingMicroservice.Tests.csproj
```

## Docker (API only)

From repo root (folder containing `apps/` and `packages/`):

```bash
docker build -f apps/messaging-microservice/Dockerfile -t messaging-microservice .
```

## Docker Compose (RabbitMQ + service)

```bash
cd apps/messaging-microservice
docker compose up --build
```

- API: `http://localhost:8080`
- RabbitMQ management UI: `http://localhost:15672` (guest/guest)
