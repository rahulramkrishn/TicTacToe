# Repository Analysis — TicTacToe

## 1. Executive Summary

This repository represents the engineering specification and governance framework for the **TicTacToe Assessment Application** (designed for an ABB Principal Software Engineer evaluation). The application comprises an **Angular** single-page frontend and a **.NET REST API** backend.

The engineering mandate places highest priority on:
1. Architectural clarity and pragmatic Domain-Driven Design (DDD).
2. Backend authority as the sole source of truth for game rules and state.
3. Explicit separation of concerns across presentation, orchestration, domain, and infrastructure.
4. Auditable AI-assisted software engineering lifecycle (traceable from requirement to commit).

---

## 2. Current Repository State

As of discovery (Phase 0 / Prompt P001):
- **Source Code**: No application code exists yet (neither .NET nor Angular).
- **Documentation**: 22 documentation files across root, `docs/`, and `docs/ai/prompts/` provide a comprehensive, fully specified engineering baseline.
- **Version Control**: Git repository is initialized (`.git` exists at the root).
- **Implementation Code**: Fully inhibited during P001 per explicit guardrail.

```text
c:\Labs\TicTacToe
├── .git/
├── README.md
├── docs/
│   ├── 01-requirements.md
│   ├── 02-prerequisites-and-environment.md
│   ├── 03-nfr.md
│   ├── 04-ddd-and-domain-model.md
│   ├── 05-architecture.md
│   ├── 06-api-contract.md
│   ├── 07-test-strategy.md
│   ├── 08-ai-development-governance.md
│   ├── 09-traceability-matrix.md
│   ├── 10-adr-template-and-initial-decisions.md
│   ├── 11-implementation-plan.md
│   ├── 12-root-ai-change-log.md
│   ├── 13-assumptions.md
│   ├── 14-panel-review.md
│   ├── ADR-007-game-completed-domain-event.md
│   ├── AI-CHANGE-AUDIT.md
│   ├── AI_PROMPTS.md
│   ├── ASSUMPTIONS.md
│   ├── CHANGELOG.md
│   ├── README.md
│   └── ai/
│       ├── prompts/
│       │   ├── P001-repository-bootstrap.md
│       │   └── P002-initialize-git-and-create-baseline.md
│       └── DOCUMENT-INVENTORY.md
```

---

## 3. Technology Stack & Planned Architecture

### Backend:
- **Framework**: .NET 8 / ASP.NET Core Web API
- **Language**: C# 12
- **Architecture**: Pragmatic Layered Clean/DDD Architecture:
  - `TicTacToe.Domain`: Pure C# domain model (Entities, Aggregates, Value Objects, Domain Services, Domain Events, Strategy interface). Zero external or HTTP dependencies.
  - `TicTacToe.Application`: Orchestration, use case handlers, DTO mapping, in-process event handlers (`GameCompleted` -> `ScoreboardHandler`).
  - `TicTacToe.Infrastructure`: In-memory thread-safe state store (`ConcurrentDictionary`), event dispatch mechanism.
  - `TicTacToe.Api`: ASP.NET Core Controllers, ProblemDetails error handling middleware, CORS policy, Swagger/OpenAPI.
  - `TicTacToe.Tests`: Unit tests (Domain rules, Computer strategy, Memento undo), Integration tests (`WebApplicationFactory`).

### Frontend:
- **Framework**: Angular (modern LTS release)
- **Language**: TypeScript, HTML5, Vanilla/Clean CSS
- **Architecture**: Component-based presentation layer:
  - `GameBoard`: 9-cell responsive grid.
  - `GameStatus`: Current turn indicator, winner banner, draw banner.
  - `MoveHistory`: Chronological move log with derived row/column display.
  - `Scoreboard`: X wins, O wins, Draws.
  - `GameControls`: Reset Game, Undo Move, Reset Scoreboard buttons.
  - `GameFacade` / `GameApiService`: Thin HTTP client consuming authoritative backend endpoints. No independent rule execution.

---

## 4. Bounded Context & Domain Invariants

A single Bounded Context exists: **TicTacToe Game Management**.

### Key Aggregate & Entities:
- **`Game` (Aggregate Root)**: Owns `GameId`, `Board` (9 cells), `CurrentPlayer` (X/O), `GameMode` (TwoPlayer/Computer), `GameStatus` (InProgress/Won/Draw), `Winner`, `WinningCells`, `MoveHistory`, and Memento history stack.
- **`Scoreboard` (Session State Aggregate/Entity)**: Owns counters (`XWins`, `OWins`, `Draws`). Decoupled from `Game` lifecycle.

### Domain Invariants:
1. **Grid**: Exactly 9 cells indexed `0..8`. Valid values: `null`, `Player.X`, `Player.O`.
2. **Move Legality**: Moves allowed only if status is `InProgress`, target cell is empty (`null`), and player matches `CurrentPlayer`.
3. **Turn Alternation**: Successful move in TwoPlayer mode toggles player (`X -> O`, `O -> X`).
4. **Terminal State**: Win detected across 3 rows, 3 columns, or 2 diagonals; Draw detected when all 9 cells filled without winner. Status transitions to `Won` or `Draw`.
5. **Completion Lock**: Once status is `Won` or `Draw`, no further moves can be accepted (`409 Conflict`).
6. **Exactly-Once Completion**: Exactly one `GameCompleted` domain event is raised on transition to terminal state.
7. **Scoreboard Integrity**: Scoreboard updates solely upon handling `GameCompleted`. Resetting a game does not reset the scoreboard.
8. **Undo Legality**: Undo disabled when move history is empty or game status is terminal (Option A). TwoPlayer undo restores 1 move; Computer mode undo restores 2 moves (the human/computer pair).
9. **Reset Identity**: Resetting a game resets the logical board and status while retaining the original `GameId`.

---

## 5. Architectural Guardrails & Principles

1. **Backend Authoritative**: The frontend never calculates winning lines, computer moves, or turn validity.
2. **Canonical Addressing**: `cellIndex: 0..8` everywhere in API contracts and DTOs. Row/column is strictly derived in the presentation layer.
3. **No Speculative Microservices**: Distributed brokers, database migrations, and microservice meshes are explicitly avoided.
4. **Pragmatic Testing**: Focus on high-value deterministic unit and integration tests (targeting 90-100% domain coverage).
5. **Strict AI Traceability**: Every generated code change must map back to a requirement ID, prompt, and human review.

---

## 6. Risk and Ambiguity Assessment

| Risk / Area | Description | Mitigation Strategy |
|---|---|---|
| In-Memory Concurrency | Multiple simultaneous HTTP requests to the same `gameId` could cause race conditions. | Use atomic operations / thread-safe state storage (`ConcurrentDictionary` and locks inside aggregate mutation). |
| File Naming Inconsistency | `docs/ai/prompts/P002 — Initialize Git and Create Baseline` lacks `.md` extension. | Rename file to `P002-initialize-git-and-create-baseline.md` for Unix/Windows portability and convention consistency. |
| Traceability Matrix ID Variations | `09-traceability-matrix.md` has slight naming/ID variations compared to `01-requirements.md`. | Reconcile in `docs/ai/REQUIREMENT-TRACEABILITY.md` using `01-requirements.md` as the authoritative source of truth. |
| Computer Strategy Ties | Multiple cells may have identical priority under strategy rules (e.g. 4 corners). | Define deterministic cell evaluation order: corners `[0, 2, 6, 8]`, remaining cells in ascending index order. |
