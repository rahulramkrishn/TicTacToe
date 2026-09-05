# P004 — Pure Domain Model Foundation

P001, P002, P003 and P003.1 have been completed.

The repository has passed the scaffolding and architecture verification gate.

You are now beginning the first business-domain implementation phase.

## CRITICAL RULE

Implement ONLY the pure Domain Model Foundation.

Do NOT implement the complete Tic Tac Toe game.

Do NOT implement API, Angular, Infrastructure behavior, computer strategy, undo, scoreboard, or domain-event handling.

The Domain project must remain framework-independent.

---

# 1. Read the Complete Specification Before Coding

Read completely:

```text
README.md

docs/01-requirements.md
docs/02-prerequisites-and-environment.md
docs/03-nfr.md
docs/04-ddd-and-domain-model.md
docs/05-architecture.md
docs/06-api-contract.md
docs/07-test-strategy.md
docs/08-ai-development-governance.md
docs/09-traceability-matrix.md
docs/10-adr-template-and-initial-decisions.md
docs/11-implementation-plan.md
docs/12-root-ai-change-log.md
docs/13-assumptions.md
docs/14-panel-review.md
docs/ADR-007-game-completed-domain-event.md
docs/AI-CHANGE-AUDIT.md
docs/AI_PROMPTS.md
docs/ASSUMPTIONS.md
docs/CHANGELOG.md
docs/README.md
```

Also read:

```text
docs/ai/DOCUMENT-INVENTORY.md
docs/ai/REPOSITORY-ANALYSIS.md
docs/ai/REQUIREMENT-TRACEABILITY.md
docs/ai/ARCHITECTURE-TRACEABILITY.md
docs/ai/AI-DECISIONS.md
docs/ai/IMPLEMENTATION-PLAN.md
docs/ai/IMPLEMENTATION-LOG.md
docs/ai/MANUAL-CHANGES.md
docs/ai/reviews/P003.1-review.md
docs/ai/decisions/ADR-008-solution-scaffolding-and-toolchain.md
```

Do not rely on this prompt as a substitute for the specification.

The specification is authoritative.

---

# 2. Establish P004 Scope Before Coding

Before writing code, inspect the specification and produce a short:

## P004 Domain Design Proposal

Identify the exact:

* Aggregate Root
* Entities
* Value Objects
* Enumerations
* Domain concepts
* Domain invariants
* Repository abstractions, if required
* Domain interfaces, if required

For each item provide:

| Concept | Type | Responsibility | Invariant | Specification Source |
| ------- | ---- | -------------- | --------- | -------------------- |

STOP and review your own proposal against `docs/04-ddd-and-domain-model.md` before implementation.

Do not invent concepts merely because they are common DDD patterns.

---

# 3. Frozen Decisions

The following decisions are already authoritative.

## Cell Addressing

The canonical cell representation is:

```text
cellIndex: 0..8
```

Board layout:

```text
0 1 2
3 4 5
6 7 8
```

Do NOT introduce `row` + `column` as an alternative domain/API representation.

If row/column calculations are useful internally, they may be derived from `cellIndex`, but `cellIndex` remains canonical.

---

## Reset

Reset eventually reuses the existing `gameId`.

Do not implement Reset in P004 unless the domain foundation requires a minimal lifecycle representation.

---

## Undo

Undo uses the:

**Memento Pattern**

Specifically snapshot-based restore.

Do NOT implement Memento in P004.

P006 owns undo.

---

## Computer

Computer move selection uses the:

**Strategy Pattern**

Do NOT implement computer strategy in P004.

P007 owns this.

---

## Completion

`GameCompleted` is a mandatory domain event.

Do NOT implement event dispatch/scoreboard behavior in P004.

P008 owns this.

---

# 4. Domain Purity

The `TicTacToe.Domain` project must remain pure.

It must NOT reference:

```text
ASP.NET Core
Entity Framework
Infrastructure
Application
HTTP
Angular
JSON serialization libraries
database libraries
logging frameworks
configuration
```

Avoid external NuGet dependencies unless explicitly required by the specification.

Prefer standard .NET language/runtime types.

---

# 5. Implement the Domain Foundation

Based strictly on `docs/04-ddd-and-domain-model.md`, implement the required foundational domain concepts.

The model should represent the domain rather than HTTP/API concerns.

Potential concepts may include, ONLY if supported by the specification:

```text
Game
Board
Player
Cell
Move
GameStatus
GameMode
GameId
```

Do not automatically implement every concept in this example.

Use the documented domain model as the source of truth.

---

# 6. Aggregate Boundary

Establish the aggregate boundary exactly as specified.

Document:

```text
Aggregate Root
↓
Owned state
↓
Invariants
↓
Public commands/operations
```

The aggregate must protect its own invariants.

Do not expose mutable internal collections or state that allows callers to bypass the aggregate.

Prefer:

```text
encapsulation
private setters
immutable value objects
defensive copying
read-only collections
```

where consistent with the specification.

---

# 7. Value Objects

Identify concepts that are values rather than entities.

Where the specification calls for value objects:

* make them immutable
* validate them at construction
* provide value equality
* prevent invalid instances
* avoid unnecessary identity

Do not create value objects merely for stylistic reasons.

---

# 8. Board Foundation

Implement the canonical board representation specified by the domain model.

The board must:

* represent exactly 9 cells
* use `cellIndex 0..8`
* distinguish empty and occupied cells
* prevent invalid cell indexes
* preserve encapsulation

Do NOT implement winner detection yet.

Do NOT implement draw detection yet.

Do NOT implement winning-cell calculation yet.

Those belong to P005.

---

# 9. Move Foundation

Implement the domain representation of a move if required by the specification.

A move should contain only the information the domain requires.

Verify:

* player
* cell index
* move ordering/number if required
* value equality/identity as specified

Do not implement move history orchestration yet unless the domain model explicitly requires it at this phase.

---

# 10. Player and Game State

Implement the required representation of:

* Player X
* Player O
* game status
* game mode
* current player

only to the extent required by the domain model foundation.

Do not implement computer-player behavior.

Do not implement win/draw transitions.

---

# 11. Domain Invariants

Implement only invariants that belong to the domain foundation and are explicitly supported by the specification.

Examples that may apply:

* valid cell index
* exactly one player per occupied cell
* no mutation of an occupied cell
* valid player representation
* valid game identity
* valid board size

Do not move domain rules into Application or API merely because they are easier to implement there.

However, do not prematurely implement P005 rules such as complete win/draw evaluation.

---

# 12. Exceptions / Error Model

Determine from the specification how invalid domain operations should be represented.

Use domain-level exceptions or result/error types only if the architecture specifies them.

Do NOT introduce ASP.NET `ProblemDetails` into Domain.

Do NOT introduce HTTP status codes into Domain.

The Domain must know nothing about HTTP.

---

# 13. Domain Unit Tests

Create comprehensive unit tests for the P004 domain foundation.

Tests must focus on behavior and invariants, not implementation details.

Cover all domain concepts created in P004.

At minimum verify, where applicable:

### Cell

* valid `0..8`
* invalid negative index
* invalid index greater than 8

### Board

* exactly nine cells
* initial cells empty
* valid cell addressing
* invalid cell addressing
* occupied-cell protection if this behavior belongs to the foundation

### Player

* X
* O
* equality/representation

### Move

* valid construction
* invalid construction
* equality semantics

### Game

* valid creation
* initial state
* game identity
* initial player
* initial status
* mode

Use the exact behavior from the specification rather than inventing behavior.

---

# 14. Test Naming

Use descriptive test names following:

```text
MethodOrBehavior_State_ExpectedResult
```

or the convention already established by the test strategy.

Tests must explain domain behavior.

Avoid:

```text
Test1
TestGame
Works
```

---

# 15. No Infrastructure Implementation

Do not implement repositories yet unless the specification explicitly defines repository abstractions as part of the Domain foundation.

If repository interfaces belong in Domain according to the architecture:

* create only the required abstraction
* do not implement it
* do not add storage behavior

Infrastructure implementation belongs to the later application/storage phase.

---

# 16. No Application Implementation

Do not add:

* commands
* handlers
* application services
* DTO mapping
* use-case orchestration

unless explicitly required as part of the Domain foundation.

P010 will own the Application layer.

---

# 17. No API Implementation

Do not create:

```text
Controllers
API DTOs
ProblemDetails
HTTP endpoints
Swagger/OpenAPI changes
```

P011 owns the API.

---

# 18. No Angular Implementation

Do not modify Angular functionality.

P009/P010 own the frontend.

---

# 19. Architecture Verification

After implementation inspect:

```text
backend/TicTacToe.Domain/TicTacToe.Domain.csproj
```

Confirm:

```text
ProjectReferences = none
```

and no forbidden infrastructure/API dependencies were introduced.

Verify that:

```text
Domain
```

can be tested independently.

---

# 20. Test Execution

Run:

```text
dotnet build backend/TicTacToe.sln
dotnet test backend/TicTacToe.sln
```

Also run the complete existing test suite to ensure P003/P003.1 behavior has not regressed.

Do not accept:

```text
warnings treated as errors
failing tests
skipped tests created merely to bypass failures
```

If an existing test becomes invalid because of a specification-backed domain change, document the reason.

Do not delete tests merely to make the suite green.

---

# 21. Requirement Traceability

Update:

```text
docs/ai/REQUIREMENT-TRACEABILITY.md
```

Map every P004 implementation to its actual requirement(s).

Do not mark requirements complete merely because a related class exists.

Use:

```text
Implemented
Partially implemented
Not yet implemented
```

where appropriate.

---

# 22. AI Audit Trail

Update:

```text
docs/ai/IMPLEMENTATION-LOG.md
docs/ai/MANUAL-CHANGES.md
```

Record:

```text
Prompt ID: P004

Objective:
Pure Domain Model Foundation

Specification documents used:
<list>

AI-generated changes:
<list>

Human changes:
<list>

Tests:
<commands + results>

Requirements addressed:
<IDs>

Requirements intentionally deferred:
<IDs>

Architectural decisions:
<list>

Review status:
Pending human review
```

Do not claim human review occurred unless a human actually reviewed the changes.

---

# 23. Create P004 Review Artifact

Create:

```text
docs/ai/reviews/P004-review.md
```

Include:

```markdown
# P004 Review

## Objective

Pure Domain Model Foundation.

## Specification Sources

[List]

## Domain Model

### Aggregate Root

### Entities

### Value Objects

### Enumerations

### Domain Invariants

### Repository Abstractions

## Design Patterns

Confirm only patterns actually applicable at this phase.

## Implemented

[List]

## Deferred

[List]

## Tests

[List and results]

## Architecture Verification

### Domain Dependencies

### Forbidden Dependencies

## Requirement Traceability

[List]

## AI Changes

[List]

## Human Changes

[List]

## Known Risks

[List]

## Final Status

PASS / BLOCKED
```

---

# 24. Git Discipline

Before committing:

```text
git status
git diff
git diff --cached
```

Review every change.

Do not commit generated build artifacts.

Do not commit:

```text
bin/
obj/
node_modules/
dist/
coverage/
```

Create one logical implementation commit:

```text
feat(domain): establish pure domain model foundation
```

After committing, update the implementation log with the actual commit hash.

If updating the implementation log after the code commit creates a second commit, use:

```text
docs(ai): record P004 commit hash in implementation log
```

Do not invent the hash.

---

# 25. Final P004 Report

Return a concise but evidence-based report containing:

## 1. Domain Model

Show the final domain model.

## 2. Aggregate Boundary

Explain the aggregate root and owned state.

## 3. Value Objects

List them and explain why they are value objects.

## 4. Entities

List them and explain identity.

## 5. Invariants

List every invariant implemented.

## 6. Deferred Rules

Explicitly confirm that the following were NOT implemented:

```text
Win detection
Draw detection
Undo / Memento
Computer Strategy
GameCompleted dispatch
Scoreboard
API
Angular game behavior
```

## 7. Tests

Report exact:

```text
Total
Passed
Failed
Skipped
```

and relevant test categories.

## 8. Build

Report exact build result.

## 9. Architecture

Confirm Domain has no forbidden dependencies.

## 10. Requirements

List requirements implemented and intentionally deferred.

## 11. Audit

Report:

* P004 review artifact
* implementation log
* requirement traceability
* manual change log
* Git commit hash

## 12. Git

Report:

```text
HEAD:
Working tree:
Latest commit:
```

## 13. Final Gate

If all requirements for this phase pass:

```text
P004 PASS — READY FOR P005
```

Otherwise:

```text
P004 BLOCKED
```

Do not proceed automatically to P005.

STOP.
