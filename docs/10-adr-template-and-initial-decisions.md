# Architecture Decision Records

## ADR-001 — Backend Owns Game State

### Status
Accepted

### Context
The assignment explicitly requires the backend to be the source of truth.

### Decision
All authoritative game rules and state transitions live in the backend.

### Consequences
Positive:
- Prevents client-side rule divergence.
- Makes business logic testable.
- Enables future multiple clients.

Negative:
- Frontend must call API for every command.

---

## ADR-002 — In-Memory Storage

### Status
Accepted

### Context
The assignment permits in-memory storage and requires local ease of execution.

### Decision
Use in-memory repositories.

### Consequences
Positive:
- Zero database setup.
- Fast local execution.
- Simple panel experience.

Negative:
- State disappears on restart.
- Not suitable for multiple backend instances.
- Not production-grade persistence.

---

## ADR-003 — Pragmatic DDD Layering

### Status
Accepted

### Context
The domain is small but contains meaningful invariants.

### Decision
Use a DDD-inspired layered architecture without splitting into microservices.

### Consequences
- Rules remain isolated.
- Tests remain fast.
- Architecture remains understandable.
- Avoids unnecessary operational complexity.

---

## ADR-004 — Disable Undo After Completion

### Status
Accepted

### Context
The assignment allows either disabling undo after completion or adjusting the scoreboard when undo reverses a result.

### Decision
Choose Option A: disable Undo after completion.

### Rationale
- Simpler state model.
- Scoreboard remains final.
- Lower risk of double increment/decrement bugs.
- Explicitly permitted.

### Consequences
Users cannot undo a completed game.

---

## ADR-005 — Deterministic Computer Strategy

### Status
Accepted

### Decision
Implement the exact required priority and deterministic ordering for equivalent choices.

### Consequences
- Reproducible UI behavior.
- Stable tests.
- Easier panel demonstration.

---

## ADR-006 — No Microservices

### Status
Accepted

### Context
The job description values microservices, but the assignment is a small local game.

### Decision
Keep one backend application with clean boundaries.

### Rationale
Demonstrates architectural judgment by avoiding distributed-system complexity where it has no business value.

### Production Evolution
The domain/application boundary could later become a service behind an API gateway if scale or organizational boundaries require it.
