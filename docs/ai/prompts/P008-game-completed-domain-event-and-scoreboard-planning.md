# P008 — GameCompleted Domain Event & Scoreboard Planning

You are now preparing Phase P008 of the TicTacToe assessment application.

P001–P007 have been completed and closed.

Before writing ANY P008 application code, perform a detailed implementation/design plan only.

DO NOT modify application source code.

DO NOT create the P008 implementation yet.

DO NOT commit anything.

---

# 1. Repository Context

First inspect the repository and read ALL Markdown documentation relevant to P008.

Do not limit the review to a small selected list.

At minimum inspect:

```text
README.md
docs/*.md
docs/ai/*.md
docs/ai/prompts/*.md
docs/ai/reviews/*.md
docs/ai/decisions/*.md
```

Also inspect the current implementation:

```text
backend/TicTacToe.Domain/
backend/TicTacToe.Tests/
backend/TicTacToe.Application/
backend/TicTacToe.Infrastructure/
backend/TicTacToe.Api/
frontend/
```

Use the actual repository state as the source of truth.

Pay particular attention to:

```text
docs/04-ddd-and-domain-model.md
docs/05-architecture.md
docs/06-api-contract.md
docs/07-test-strategy.md
docs/08-ai-development-governance.md
docs/09-traceability-matrix.md
docs/10-adr-template-and-initial-decisions.md
docs/11-implementation-plan.md
docs/13-assumptions.md
docs/14-panel-review.md
docs/ADR-007-game-completed-domain-event.md
docs/AI-CHANGE-AUDIT.md
```

Also inspect the actual P005, P006 and P007 implementation and review artifacts.

---

# 2. P008 Objective

Prepare the implementation design for:

```text
GameCompleted Domain Event
        +
Scoreboard
```

The architectural objective is:

```text
Game Aggregate
      │
      │ terminal transition
      ▼
GameCompleted
Domain Event
      │
      ▼
Application event handling
      │
      ▼
Scoreboard update
```

The event must be emitted as a consequence of the domain transition that completes a game.

---

# 3. Critical Requirement — Exactly Once

Design specifically for:

```text
one game completion
        ↓
one GameCompleted event
        ↓
one scoreboard update
```

The design must prevent:

```text
duplicate event
duplicate scoreboard update
```

from normal application behavior.

Explicitly analyze:

* win transition
* draw transition
* final winning move
* final draw move
* repeated attempts after completion
* Undo interaction
* Computer Mode
* `ExecuteTurn`
* `PlayComputerMove`
* reset
* multiple games
* repeated event dispatch
* accidental duplicate publication

---

# 4. Domain Event Design

Determine and document:

* Event type
* Event ownership
* Event payload
* Event identity if required
* Game identity
* Winner
* Result/status
* Winning cells
* Move count if required
* Timestamp if required
* Whether draw uses `Winner = null`
* Whether the event contains a snapshot of completion state

Do not invent fields merely because they are common in other systems.

Every field must be justified against the existing specification.

---

# 5. Aggregate Responsibility

Analyze exactly where the event is created.

The preferred conceptual boundary is:

```text
Game
 ├── owns game invariants
 ├── owns terminal state transition
 └── raises GameCompleted domain event
```

The Game aggregate must NOT directly update the scoreboard.

Do not create coupling such as:

```text
Game → Scoreboard
```

or:

```text
Game → Application service
```

---

# 6. Domain Event Storage / Collection

Analyze how the existing architecture should expose domain events.

Determine whether the aggregate should have something equivalent to:

```csharp
IReadOnlyCollection<IDomainEvent> DomainEvents
```

and a controlled mechanism such as:

```csharp
AddDomainEvent(...)
ClearDomainEvents()
```

Do not blindly introduce a generic infrastructure abstraction if the existing project structure does not justify it.

Explain the chosen approach.

---

# 7. Event Publication Boundary

Determine where domain events should be dispatched.

The design must preserve:

```text
Domain
    ↓
Application
    ↓
Infrastructure
```

without making the Domain depend on infrastructure.

Explicitly explain:

```text
Who creates the event?
Who stores it?
Who retrieves it?
Who dispatches it?
Who handles it?
When is it cleared?
```

---

# 8. Scoreboard Design

Design the scoreboard according to the existing requirements.

Determine:

* Winner counts
* Draw count
* Whether Player X and Player O have separate scores
* Initial state
* Lifetime
* Scope
* Reset behavior
* GameId relationship
* Whether completed games can be counted twice
* Whether scoreboard is domain state or application/infrastructure state

Do not add persistence unless the existing specification requires it.

If the specification says in-memory, preserve that decision.

---

# 9. Idempotency / Exactly-Once Analysis

This is a Principal-level concern.

Explicitly distinguish:

```text
Exactly once event generation
```

from:

```text
Exactly once event processing
```

and:

```text
Exactly once scoreboard effect
```

Explain what guarantees can be provided by this assessment architecture.

Do not claim distributed exactly-once delivery unless the architecture actually supports it.

If the system is in-memory/in-process, clearly define the exact guarantee we are implementing.

Consider whether the scoreboard handler should guard against duplicate `GameCompleted` events using:

```text
GameId
```

or another event identity.

Do not implement a mechanism unless justified by the specification.

---

# 10. Undo Interaction

This is mandatory.

P006 established Memento-based Undo.

Analyze:

```text
Game completes
      ↓
GameCompleted raised
      ↓
Undo?
```

The existing Option A behavior is:

```text
terminal game
    → CanUndo = false
    → Undo rejected
```

Therefore determine how this interacts with the domain event.

The design must ensure a completed game cannot produce inconsistent scoreboard state through Undo.

---

# 11. Computer Mode Interaction

Analyze:

```text
X move
 ↓
Game.ExecuteTurn
 ↓
O strategy
 ↓
O move
 ↓
terminal?
```

Determine exactly when:

```text
GameCompleted
```

is raised.

Ensure that a Computer Mode turn cannot accidentally generate two completion events.

Examples:

```text
X wins
→ one event
→ no O move

O wins
→ one event

Draw
→ one event
```

---

# 12. Reset Interaction

Respect the frozen reset decision:

```text
Reset Game
    → reuse same gameId
```

Analyze whether Reset should:

* clear domain events
* create a new event
* affect scoreboard
* create a new game completion identity

Reset must NOT cause a duplicate `GameCompleted` event for the previous game.

---

# 13. Application Layer

Determine what application abstractions are required.

Possible concerns include:

```text
Game service
Domain event dispatcher
GameCompleted handler
Scoreboard service
```

But do not create abstractions simply for ceremony.

Every proposed application component must have a clear responsibility.

---

# 14. Infrastructure

Determine whether Infrastructure needs to participate.

Current architectural direction is in-memory.

Do not introduce:

```text
database
EF Core
message broker
Azure Service Bus
RabbitMQ
Kafka
```

unless the existing specification explicitly requires them.

If no Infrastructure implementation is required, state why.

---

# 15. Testing Strategy

Provide a detailed test plan.

At minimum consider:

### Domain

* GameCompleted emitted on X win
* GameCompleted emitted on O win
* GameCompleted emitted on draw
* event contains correct GameId
* event contains correct winner/result
* winning cells preserved
* exactly one event per terminal transition
* no event for non-terminal moves
* no event from rejected moves
* no duplicate event after terminal state

### Computer Mode

* X wins → one event
* O wins → one event
* draw → one event
* no double event

### Scoreboard

* X win increments X exactly once
* O win increments O exactly once
* draw increments draw count exactly once
* duplicate event behavior
* multiple completed games
* independent GameIds

### Undo

* terminal game cannot Undo
* scoreboard remains consistent
* no completion event is generated by Undo

### Reset

* same GameId reused
* scoreboard does not change merely because of reset
* previous completion is not counted again

---

# 16. API/UI Boundary

P008 must NOT implement:

```text
REST controllers
DTOs
HTTP endpoints
Angular scoreboard UI
frontend game UI
```

unless the existing specification explicitly places a small prerequisite here.

Those remain future phases.

The P008 implementation should expose the domain/application capability needed by later API/UI phases.

---

# 17. Architecture Review

Explicitly assess:

### DDD

* Aggregate Root
* Domain Event
* Domain Service vs Application Service
* Value Objects
* Aggregate boundary
* invariants

### SOLID

Especially:

* SRP
* DIP
* OCP

### Design Patterns

Explicitly identify:

```text
Memento
Strategy
Domain Event / Observer-style dispatch
```

and explain their boundaries.

Do not force additional patterns.

---

# 18. Documentation Impact

Identify exact Markdown files that will need updates after implementation.

At minimum consider:

```text
docs/ai/REQUIREMENT-TRACEABILITY.md
docs/ai/ARCHITECTURE-TRACEABILITY.md
docs/ai/IMPLEMENTATION-LOG.md
docs/ai/AI-DECISIONS.md
docs/ai/MANUAL-CHANGES.md
docs/ai/reviews/P008-review.md
```

Also identify whether:

```text
docs/ADR-007-game-completed-domain-event.md
```

needs an implementation-status update.

Do not modify documentation during this planning phase.

---

# 19. Traceability

Map the planned implementation to:

```text
FR requirements
NFR requirements
ADR-007
DDD decisions
API contract implications
test strategy
```

Clearly distinguish:

```text
Implemented by P008
```

from:

```text
Deferred to later phase
```

---

# 20. Required Plan Output

Return a detailed P008 implementation plan containing:

1. Context
2. Requirements being implemented
3. Existing code discovered
4. Proposed architecture
5. Domain Event design
6. Game aggregate changes
7. Domain event storage mechanism
8. Event dispatch mechanism
9. Scoreboard design
10. Exactly-once/idempotency strategy
11. Undo interaction
12. Computer Mode interaction
13. Reset interaction
14. Application layer changes
15. Infrastructure changes
16. Test plan
17. Boundary verification
18. Documentation changes
19. Traceability changes
20. Risks
21. Alternatives considered
22. Explicit non-goals
23. File-by-file implementation plan
24. Verification commands
25. Proposed Git commit structure

---

# 21. Important Governance Rule

This is a PLANNING ONLY phase.

Do NOT:

* create application code
* modify existing source files
* modify documentation
* create tests
* commit
* start P009

After producing the plan, STOP.

The human reviewer will review the P008 architecture before implementation begins.
