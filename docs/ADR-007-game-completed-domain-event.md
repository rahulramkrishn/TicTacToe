# ADR-007 — GameCompleted Domain Event

## Status

Implemented (Phase P008)

## Context

The scoreboard must update when a game completes and must update only once for a completed game.

A direct call from a controller or application service can work, but it makes correctness dependent on every caller remembering the scoreboard operation.

## Decision

The `Game` aggregate raises a **`GameCompleted` domain event** exactly once when the game transitions from `InProgress` to `Won` or `Draw`.

The application layer handles that event and updates the session-level scoreboard.

Event delivery is synchronous and in-process for this assignment.

## Rationale

The event expresses a business fact:

> A game has completed.

The scoreboard reacts to that fact.

This is stronger than procedural discipline because the completion event is tied to the domain transition itself.

## Consequences

### Positive

- Explicit business event.
- Exactly-once completion semantics are easier to enforce.
- Scoreboard is decoupled from the Game aggregate.
- GET requests cannot accidentally update scores.
- Easy unit testing.
- Demonstrates event-driven thinking appropriate to a Principal Engineer role.

### Negative

- Adds a small amount of infrastructure.
- Requires event dispatch/handler testing.
- In a durable distributed implementation, synchronous in-process delivery would not be enough.

## Production Evolution

If game state and scoreboard become persistent/distributed:

```text
Game Transaction
      ↓
Outbox Record
      ↓
Reliable Publisher
      ↓
GameCompleted Consumer
      ↓
Idempotent Scoreboard Update
```

The production version should use transactional outbox/idempotent processing where reliability requires it.

## Alternatives Rejected

### Direct scoreboard call

Rejected as the primary design because the business event is less explicit and completion/scoreboard coupling is stronger.

### External message broker

Rejected for this assignment because it introduces operational complexity without business value for a local in-memory application.
