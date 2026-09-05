# 13 — Assumptions and Frozen Decisions

## 1. Assignment-Explicit Requirements

The source assignment explicitly allows in-memory storage and states that the exact endpoint names may vary if the API contract is documented. It also requires the candidate to state whether Option A or Option B is used for undo after completion.

This implementation selects **Option A**.

## 2. Frozen Decisions

### A-001 — Cell Addressing

**Decision:** `cellIndex 0..8`.

Reason:

- one primitive value
- simple validation
- simple DTOs
- simple history serialization
- easy snapshot serialization
- avoids inconsistent row/column combinations

Mapping:

```text
0 1 2
3 4 5
6 7 8
```

Row/column is derived only for display.

### A-002 — Reset Game Identity

**Decision:** Reset Game reuses the existing `gameId`.

Reason:

- simpler API traces
- easier panel walkthrough
- same logical session identity
- no client reassignment after reset
- scoreboard remains independent

A reset is therefore:

```text
same identity + new logical game state
```

not:

```text
new identity
```

### A-003 — Undo

**Decision:** Option A.

Undo is disabled after completion.

Reason:

- scoreboard remains final
- no reverse-score transaction required
- simpler consistency model
- explicitly permitted by assignment

### A-004 — Undo Pattern

**Decision:** Memento pattern.

Use snapshot restore rather than attempting to reverse moves by calculation.

### A-005 — Computer Pattern

**Decision:** Strategy pattern.

Use a dedicated computer-move strategy with the required priority ordering.

### A-006 — Scoreboard Completion

**Decision:** `GameCompleted` domain event is mandatory.

The event is the structural trigger for the scoreboard update.

### A-007 — Event Delivery

**Decision:** synchronous, in-process event handling.

No external broker is justified for this local assignment.

### A-008 — Storage

**Decision:** in-memory storage.

This is permitted by the assignment and keeps the solution easy to run locally.

## 3. Important Non-Requirements

Do not introduce unless justified:

- authentication
- authorization
- database migrations
- distributed cache
- Kubernetes
- service mesh
- external event broker
- cloud deployment
- microservices

These may be documented as production evolution options, but should not obscure the assignment.

## 4. AI IDE Guardrail

The AI IDE must treat this file and the ADRs as frozen design decisions.

It must not silently change:

- `cellIndex`
- reset identity semantics
- undo option
- Memento approach
- Strategy approach
- `GameCompleted` event decision

Any requested change must first update the relevant ADR and traceability entry.
