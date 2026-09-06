# ADR-005 — Domain Events Removed

## Status
Superseded

## Original Decision
Use `GameCompleted` domain event (ADR-007) and an in-process dispatcher to asynchronously update the scoreboard when a game transitions to Won or Draw.

## P012 Decision
Remove the in-process domain events infrastructure entirely.

## Reason
- There is only one synchronous, in-process consumer (Scoreboard) for the `GameCompleted` event.
- There is no independent architectural boundary requiring asynchronous decoupling.
- The Domain Event dispatcher added unnecessary indirection and boilerplate, obscuring the primary write-path and making the architecture harder to follow for a small local assessment application.
- The `GameService` application service is already responsible for orchestrating the lifecycle and persistence of the game; it can cleanly coordinate the scoreboard update.

## Current Design
`GameService` explicitly orchestrates the scoreboard update in a deterministic, synchronous flow after executing a turn if the game status becomes terminal.

## Reconsider when:
- Multiple independent consumers exist for game events.
- An external message broker or distributed messaging (e.g., Kafka, RabbitMQ) is introduced.
- External integrations (e.g., analytics, email notifications) need to subscribe to game completion.
- Distributed deployment requires reliable outbox-pattern event delivery.
- Audit or Event Sourcing becomes a hard business requirement.
