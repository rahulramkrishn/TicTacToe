# P001 — Complete Specification Discovery and Repository Analysis

You are the engineering AI assistant for the TicTacToe assessment application.

## CRITICAL RULE

**Do NOT write application implementation code yet.**

This task is ONLY for repository discovery, specification analysis, requirement consolidation, architecture validation, and implementation planning.

Do not create Angular components, .NET projects, controllers, services, domain classes, tests, configuration or other application implementation.

---

# 1. Discover the Entire Specification

Start from the repository root.

Inspect:

```text
README.md
docs/
```

Find **EVERY Markdown file (`*.md`)** under the repository.

Do not assume that only a subset of files is relevant.

Read every Markdown document completely.

This includes, but is not limited to:

```text
README.md

docs/01-requirements.md
docs/02-*.md
docs/03-*.md
docs/04-ddd-and-domain-model.md
docs/05-*.md
docs/06-api-contract.md
docs/07-*.md
docs/08-*.md
docs/09-*.md
docs/10-*.md
docs/11-*.md
docs/12-*.md
docs/13-assumptions.md
docs/AI-CHANGE-AUDIT.md
docs/ADR-*.md
```

If additional Markdown files exist, read those too.

**Do not skip files because their names appear unrelated.**

The complete repository documentation is the source of truth.

---

# 2. Build a Documentation Inventory

Create an inventory of every Markdown file.

Use:

| File | Purpose | Key Topics | Requirements/Decisions Introduced |
| ---- | ------- | ---------- | --------------------------------- |

For each file, briefly explain why it matters to implementation.

Do not modify the existing documentation during this step.

---

# 3. Extract ALL Requirements

From the complete documentation set, identify every requirement.

Separate them into:

## Functional Requirements

Examples may include:

* game creation
* board
* turns
* move validation
* win detection
* draw detection
* move history
* undo
* computer mode
* scoreboard
* reset
* API
* UI

Use the requirement IDs already defined by the documentation.

Do not invent replacement IDs.

## Non-Functional Requirements

Identify every documented NFR, including where applicable:

* performance
* scalability
* reliability
* maintainability
* testability
* security
* observability
* accessibility
* usability
* API quality
* error handling
* logging
* deployment/runability
* code quality
* AI-assisted development traceability

Do not assume an NFR exists unless the documentation supports it.

---

# 4. Extract Architectural Requirements

Identify everything the documentation says about:

* Angular
* .NET
* REST
* layered architecture
* DDD
* domain model
* aggregates
* entities
* value objects
* domain services
* application services
* repositories
* controllers
* DTOs
* dependency injection
* validation
* error handling
* testing
* frontend/backend responsibility
* state ownership
* persistence
* infrastructure

Create:

```text
Architecture Requirement → Source Document → Planned Architectural Location
```

---

# 5. Extract ALL Design Decisions

Find every explicit or implied architectural/design decision in the documentation.

Categorize them:

```text
Architecture
Domain
API
Frontend
Backend
Testing
Persistence
Security
Observability
AI Development
Deployment
```

For each decision record:

| Decision | Source | Status | Must/Should | Implementation Impact |
| -------- | ------ | ------ | ----------- | --------------------- |

Distinguish clearly between:

* explicit requirement
* accepted decision
* assumption
* recommendation
* optional enhancement

Do not convert an optional recommendation into a mandatory requirement.

---

# 6. Apply These FROZEN Decisions

The following decisions have already been explicitly resolved in the documentation and must be treated as authoritative.

## Cell Addressing

Use:

```text
cellIndex: 0..8
```

Canonical board:

```text
0 1 2
3 4 5
6 7 8
```

`cellIndex` is the ONLY API representation.

Do not implement both:

```text
row + column
```

and:

```text
cellIndex
```

Row/column may only be derived by the frontend for presentation.

---

## Reset Game

Reset Game must:

```text
reuse the existing gameId
```

It creates a fresh logical game state but does NOT create a new Game ID.

Example:

```text
Before:
gameId = ABC
board = partially played

Reset

After:
gameId = ABC
board = empty
currentPlayer = X
history = empty
scoreboard = unchanged
```

---

## Undo

Undo uses:

**Memento Pattern**

It is:

```text
snapshot-based restore
```

not:

```text
derived reversal of individual operations
```

The documentation's selected undo option must be respected.

---

## Computer Move Selection

Computer move selection uses:

**Strategy Pattern**

The strategy must remain replaceable independently of the Game aggregate.

---

## GameCompleted

`GameCompleted` is a **mandatory domain event**, not optional.

It is raised exactly once when the game transitions from:

```text
InProgress → Won
```

or:

```text
InProgress → Draw
```

The scoreboard reacts to this domain event.

For this assessment, event processing is synchronous/in-process unless the existing documentation specifies otherwise.

Do not introduce an external broker simply to implement this.

---

# 7. Cross-Document Consistency Analysis

Compare all documents against each other.

Look specifically for contradictions involving:

* requirements
* API
* DTOs
* cell addressing
* reset behavior
* undo behavior
* computer behavior
* scoreboard
* domain events
* architecture
* testing
* NFRs
* frontend/backend responsibilities

Create:

| Conflict | Documents | Impact | Recommended Resolution | Requires Human Decision? |
| -------- | --------- | ------ | ---------------------- | ------------------------ |

Do NOT silently change the source documents.

If an older document conflicts with one of the newly frozen decisions, report the conflict and identify which document should be updated.

---

# 8. Build the Complete Traceability Model

Create:

```text
Requirement
    ↓
Architecture / Design
    ↓
Implementation Area
    ↓
Test
    ↓
Evidence
    ↓
Git Commit
```

Prepare a table:

| Requirement | Design Decision | Implementation Area | Test | Evidence | Status |
| ----------- | --------------- | ------------------- | ---- | -------- | ------ |

Every functional and important non-functional requirement should eventually be traceable.

---

# 9. Identify Bounded Context / Domain Model

Based ONLY on the documentation, identify:

* bounded context
* aggregate roots
* entities
* value objects
* domain services
* domain events
* repositories
* application services
* domain invariants

Pay particular attention to:

```text
Game
Scoreboard
Computer Strategy
Undo
GameCompleted
```

Do not invent unnecessary DDD artifacts.

---

# 10. Identify API Surface

From the complete documentation determine:

* endpoints
* HTTP methods
* request DTOs
* response DTOs
* error contracts
* validation
* status codes
* state ownership
* idempotency expectations
* API invariants

Verify that all API definitions agree with:

```text
cellIndex 0..8
```

and:

```text
Reset reuses gameId
```

---

# 11. Identify Test Strategy

Extract every required test category.

Create a test matrix:

| Area | Scenario | Requirement | Test Type |
| ---- | -------- | ----------- | --------- |

Include, where documented:

* domain unit tests
* application tests
* API/integration tests
* frontend tests
* E2E tests
* invalid input
* terminal state
* undo
* computer strategy
* scoreboard
* reset
* error handling

Do not generate tests yet.

---

# 12. Identify NFR Verification Strategy

For every documented NFR determine:

```text
NFR
→ How it will be verified
→ Tool/test/evidence
```

For example:

```text
Maintainability
→ architecture review + code structure

Performance
→ measured API response time / appropriate test

Security
→ validation + dependency/configuration review

Observability
→ structured logging + correlation/trace information
```

Only include NFRs actually supported by the specification.

---

# 13. AI-Assisted Development Governance

The repository must make AI-assisted development auditable.

The implementation workflow must record:

```text
Requirement
↓
Prompt
↓
AI-generated implementation
↓
Human review
↓
Tests
↓
Manual changes
↓
Git commit
```

Determine from the existing documentation how this should be recorded.

If the required folders/files do not exist, prepare the documentation structure needed for the audit.

Recommended structure:

```text
docs/
└── ai/
    ├── prompts/
    ├── reviews/
    ├── decisions/
    ├── REQUIREMENT-TRACEABILITY.md
    ├── IMPLEMENTATION-LOG.md
    ├── MANUAL-CHANGES.md
    └── AI-DECISIONS.md
```

Do not create implementation code.

---

# 14. Implementation Phases

Based on the complete specification, propose a dependency-aware implementation sequence.

For example:

```text
Phase 0 — Repository / tooling
Phase 1 — Domain model
Phase 2 — Game rules
Phase 3 — Undo / Memento
Phase 4 — Computer / Strategy
Phase 5 — GameCompleted / Scoreboard
Phase 6 — Application layer
Phase 7 — REST API
Phase 8 — Backend tests
Phase 9 — Angular UI
Phase 10 — Frontend/API integration
Phase 11 — E2E
Phase 12 — NFR hardening
Phase 13 — Final architecture review
Phase 14 — AI/human audit
```

Do not blindly use this sequence. Derive the final sequence from the actual documentation.

For every phase specify:

* objective
* requirements covered
* prerequisites
* expected files
* tests
* review gate
* Git commit

---

# 15. AI IDE Implementation Guardrails

Before future implementation prompts are executed, the following rules must apply:

### Rule 1

Do not change requirements to make implementation easier.

### Rule 2

Do not silently change an ADR.

### Rule 3

Do not introduce architecture that is not justified by the specification.

### Rule 4

Do not move business rules into Angular merely because it is convenient.

### Rule 5

Backend remains authoritative for game rules/state.

### Rule 6

Do not introduce microservices, brokers, databases or cloud infrastructure unless explicitly required or later approved.

### Rule 7

Every significant implementation must map to a requirement, ADR or explicitly documented engineering decision.

### Rule 8

Every AI-generated change must be reviewed and tested.

### Rule 9

Every material manual change must be recorded.

### Rule 10

Never overwrite existing documentation without explaining why.

---

# 16. Create Analysis Artifacts

After reading the complete documentation, create/update ONLY documentation artifacts required for this analysis:

```text
docs/ai/REPOSITORY-ANALYSIS.md
docs/ai/DOCUMENT-INVENTORY.md
docs/ai/REQUIREMENT-TRACEABILITY.md
docs/ai/ARCHITECTURE-TRACEABILITY.md
docs/ai/IMPLEMENTATION-PLAN.md
docs/ai/AI-DECISIONS.md
docs/ai/IMPLEMENTATION-LOG.md
docs/ai/MANUAL-CHANGES.md
```

Do not modify the original specification files unless a contradiction is discovered that requires an explicit documentation correction.

If a contradiction exists, report it first instead of silently fixing it.

---

# 17. Final P001 Output

At the end provide a concise but complete report containing:

## A. Repository Structure

What files/folders currently exist.

## B. Documentation Inventory

Every Markdown file discovered.

## C. Functional Requirements

All requirements with IDs.

## D. NFRs

All documented NFRs.

## E. Architecture

Complete architectural interpretation.

## F. DDD Model

Aggregate, entities, value objects, services and events.

## G. API

Endpoints and DTO model.

## H. Frozen Decisions

Explicitly confirm:

```text
cellIndex 0..8
Reset reuses gameId
Memento for undo
Strategy for computer
GameCompleted domain event
```

## I. Contradictions

Anything that needs resolution.

## J. Implementation Plan

Ordered implementation phases.

## K. AI Development Process

How AI-generated and manually created changes will be recorded.

## L. Next Step

Recommend the exact next implementation prompt.

---

# FINAL INSTRUCTION

**STOP AFTER THIS ANALYSIS.**

Do not create Angular application code.

Do not create .NET application code.

Do not create controllers.

Do not create services.

Do not create domain classes.

Do not create tests.

Do not install packages unless required solely for repository inspection.

The goal of P001 is to establish a complete understanding of the **entire existing specification**, not to begin implementation.
