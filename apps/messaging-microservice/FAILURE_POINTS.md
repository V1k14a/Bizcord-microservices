# Failure points — Messaging microservice

This document lists operational and design failure modes for the messaging service, plus mitigations that were implemented or recommended.

## Service dependencies

- **RabbitMQ (message broker)**  
  If RabbitMQ is down or unreachable, `PublishAsync` throws after retries/timeouts (see `ResilientMessageClient`). Symptom: HTTP requests that must emit `MessagePostedEvent` fail; saga async flow can end in `MessagePostedPublishFailed` and compensate by deleting the persisted message.  
  **Mitigation implemented:** Polly retry + pessimistic timeout around publish.  
  **Residual risk:** Prolonged outages still surface as errors to clients; consider dead-letter queues and outbox pattern for stronger guarantees.

- **Other microservices**  
  This service does not yet call HTTP peers for every write path; when you add channel-service validation, a slow or hung HTTP call can block ASP.NET request threads.  
  **Mitigation:** Use `HttpClient` with explicit timeouts, cancellation tokens, and Polly policies (retry only for idempotent calls).  
  **Failure point:** Missing timeouts on future HTTP clients → thread pool starvation.

## Data stores

- **In-memory repository**  
  Process restart loses all messages; not suitable for production.  
  **Failure point:** Single-instance memory; no durability.  
  **Mitigation (future):** Move to a real database; add connection resilience (retry on transient SQL errors, circuit breaker on repeated failures).

- **Slow repository**  
  Repository calls are bounded by a linked `CancellationTokenSource` timeout in `MessagesService` so a pathological slow store fails fast instead of hanging indefinitely.

## External configuration and secrets

- **HashiCorp Vault**  
  If Vault is sealed, misconfigured, or the KV path is missing, startup continues with `appsettings` / environment values (`VaultBootstrap` catches and logs).  
  **Failure point:** Secrets never refreshed at runtime (read only at startup).  
  **Mitigation (future):** Sidecar or periodic reload with versioning.

  **KV example (dev Vault):** after the server is up and unsealed (or when using `-dev`), write the broker connection string:

  `vault kv put secret/messaging rabbitmq_connection_string="host=rabbitmq;username=guest;password=guest"`

## Saga coordination

- **Duplicate or out-of-order messages**  
  The orchestrator uses minimal idempotency checks (`EventPublished`, `IsFailed`). Duplicate `MessagePersisted` could theoretically double-publish integration events if the check regresses.  
  **Mitigation (future):** Persist idempotency keys per saga step.

- **Compensation after partial publish**  
  If `MessagePostedEvent` is delivered to consumers but the broker ACK fails, compensation might delete the message while some subscribers already acted. This is inherent to at-least-once delivery without an outbox.

---

## Saga pattern: orchestration vs choreography (assignment)

**Choice: orchestration** (single `MessagePostSagaOrchestrator` handling `InitiateMessagePost`, `MessagePersisted`, and `MessagePostedPublishFailed`).

**Why:** The happy path and compensation rules are short but tightly coupled to local persistence order and a single integration event. Keeping decisions in one place makes it obvious when to delete a message after a failed publish and avoids scattering “if publish fails then delete” logic across multiple services.

**What would be harder with choreography:** Each service would need shared knowledge of saga state transitions; a failed publish would require a dedicated subscriber elsewhere to infer that compensation is required, which is harder to reason about for a two-step local workflow.

**Where saga state lives:** `InMemorySagaStateRepository` in the messaging microservice process, keyed by `SagaId`. For production this should be replaced with durable storage so handlers can recover after restarts.
