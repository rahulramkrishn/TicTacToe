# Phased Implementation Plan

This implementation plan establishes a dependency-aware roadmap for constructing the TicTacToe application. Every phase defines clear objectives, requirement coverage, prerequisites, expected files, test criteria, review gates, and Git commit expectations.

---

## Phase Overview & Dependency Flow

```text
Phase 0: Baseline & Tooling (Current / P001 & P002)
   ↓
Phase 1: Pure Domain Model Foundation (Entities, VOs, Invariants)
   ↓
Phase 2: Core Game Rules & Win/Draw Detection
   ↓
Phase 3: Undo Mechanism (Memento Pattern)
   ↓
Phase 4: Computer Move Strategy (Strategy Pattern)
   ↓
Phase 5: Domain Events & Scoreboard Aggregate
   ↓
Phase 6: Application Layer & In-Memory Infrastructure
   ↓
Phase 7: ASP.NET Core REST API & ProblemDetails Middleware
   ↓
Phase 8: Backend API Contract & Integration Tests
   ↓
Phase 9: Angular Application Setup & Core Components
   ↓
Phase 10: Frontend API Integration & State Facade
   ↓
Phase 11: End-to-End Verification & Edge Cases
   ↓
Phase 12: Security, CORS & NFR Hardening
   ↓
Phase 13: AI-Assisted Governance Audit & Traceability Reconciliation
   ↓
Phase 14: Panel Defense Readiness & Demonstration Script
```

---

## Detailed Phase Breakdown

### Phase 0 — Repository Baseline & Engineering Governance
- **Objective**: Establish documentation inventory, requirements traceability, `.gitignore`, and Git baseline without generating application code.
- **Requirements Covered**: FR-19, NFR-13, NFR-14.
- **Prerequisites**: Repository clone with Git initialized.
- **Expected Files**:
  - `docs/ai/*.md` (Analysis, Inventory, Traceability, Decisions, Plan, Logs)
  - `.gitignore` (Configured for .NET, Angular/Node, IDEs)
- **Tests**: Verify markdown links, document inventory completeness.
- **Review Gate**: P001 Analysis report accepted; git status confirmed clean.
- **Git Commit**: `docs: establish assessment specification and engineering baseline`

---

### Phase 1 — Pure Domain Model Foundation
- **Objective**: Implement core domain concepts without external or HTTP dependencies.
- **Requirements Covered**: FR-01, FR-02, FR-03, FR-06, NFR-01, NFR-02, NFR-03.
- **Prerequisites**: Phase 0 baseline committed.
- **Expected Files**:
  - `backend/src/TicTacToe.Domain/ValueObjects/CellIndex.cs`
  - `backend/src/TicTacToe.Domain/ValueObjects/Player.cs`
  - `backend/src/TicTacToe.Domain/ValueObjects/GameMode.cs`
  - `backend/src/TicTacToe.Domain/ValueObjects/GameStatus.cs`
  - `backend/src/TicTacToe.Domain/Entities/Board.cs`
  - `backend/src/TicTacToe.Domain/Entities/Move.cs`
  - `backend/src/TicTacToe.Domain/Aggregates/Game.cs`
- **Tests**: Unit tests for `CellIndex` validation (`0..8`), initial game creation, player alternation.
- **Review Gate**: 100% domain compilation; zero references to ASP.NET Core or System.Web.
- **Git Commit**: `feat(domain): establish core domain model and value objects`

---

### Phase 2 — Game Rules & Win/Draw Detection
- **Objective**: Implement deterministic rule evaluation (rows, columns, diagonals, draw) and transition to terminal states.
- **Requirements Covered**: FR-04, FR-05, FR-06, NFR-01.
- **Prerequisites**: Phase 1 domain foundation.
- **Expected Files**:
  - `backend/src/TicTacToe.Domain/Services/WinDetector.cs`
  - `backend/src/TicTacToe.Domain/Exceptions/InvalidMoveException.cs`
- **Tests**: Unit tests for all 3 rows, 3 columns, 2 diagonals, draw on move 9, winning move on move 9 (win precedence over draw), rejection of moves on completed games.
- **Review Gate**: All 8 winning line combinations verified by unit tests.
- **Git Commit**: `feat(domain): implement win detection and move validation rules`

---

### Phase 3 — Undo Mechanism via Memento Pattern
- **Objective**: Implement snapshot-based state restore for TwoPlayer and Computer modes under Option A.
- **Requirements Covered**: FR-08, FR-09, FR-10, NFR-01.
- **Prerequisites**: Phase 2 game rules.
- **Expected Files**:
  - `backend/src/TicTacToe.Domain/Mementos/GameMemento.cs`
  - Update `backend/src/TicTacToe.Domain/Aggregates/Game.cs` with `CreateMemento()`, `Restore()`, `Undo()`
- **Tests**: TwoPlayer undo (reverts 1 move), Computer undo (reverts 2 moves), undo disabled when history empty, undo disabled after game completed (Option A).
- **Review Gate**: Zero scoreboard mutation on undo; snapshot state verified clean.
- **Git Commit**: `feat(domain): implement snapshot memento undo mechanism`

---

### Phase 4 — Computer Strategy Pattern
- **Objective**: Implement deterministic AI move selection following the 5-tier priority rule.
- **Requirements Covered**: FR-14, NFR-01, NFR-02.
- **Prerequisites**: Phase 2 game rules.
- **Expected Files**:
  - `backend/src/TicTacToe.Domain/Strategies/IComputerMoveStrategy.cs`
  - `backend/src/TicTacToe.Domain/Strategies/RuleBasedComputerStrategy.cs`
- **Tests**: Unit tests isolating each priority:
  1. Winning O move
  2. Blocking X move
  3. Center cell (`4`)
  4. Corner cells (`[0, 2, 6, 8]`)
  5. Any remaining cell
- **Review Gate**: Deterministic test passes consistently without random selection.
- **Git Commit**: `feat(domain): implement deterministic computer move strategy`

---

### Phase 5 — Domain Events & Scoreboard State
- **Objective**: Decouple scoreboard from Game aggregate via `GameCompleted` domain event raised exactly once.
- **Requirements Covered**: FR-11, FR-13, NFR-01, ADR-007.
- **Prerequisites**: Phase 2 game rules.
- **Expected Files**:
  - `backend/src/TicTacToe.Domain/Events/GameCompletedEvent.cs`
  - `backend/src/TicTacToe.Domain/Entities/Scoreboard.cs`
  - `backend/src/TicTacToe.Application/Events/IDomainEventHandler.cs`
  - `backend/src/TicTacToe.Application/Handlers/ScoreboardGameCompletedHandler.cs`
- **Tests**: Event emitted exactly once on win; event emitted exactly once on draw; no event emitted on InProgress move; scoreboard resets independently without altering board.
- **Review Gate**: Verified that repeated queries or resets do not trigger scoreboard increments.
- **Git Commit**: `feat(domain): introduce GameCompleted event and Scoreboard aggregate`

---

### Phase 6 — Application Layer & In-Memory Infrastructure
- **Objective**: Orchestrate use cases, DTO mapping, and in-memory thread-safe repository storage.
- **Requirements Covered**: FR-01, FR-07, FR-12, FR-15, NFR-03, NFR-05.
- **Prerequisites**: Phases 1 through 5.
- **Expected Files**:
  - `backend/src/TicTacToe.Application/DTOs/GameStateDto.cs`
  - `backend/src/TicTacToe.Application/DTOs/MakeMoveRequest.cs`
  - `backend/src/TicTacToe.Application/UseCases/*.cs` (Create, Move, Undo, Reset)
  - `backend/src/TicTacToe.Infrastructure/Repositories/InMemoryGameRepository.cs`
  - `backend/src/TicTacToe.Infrastructure/Repositories/InMemoryScoreboardRepository.cs`
- **Tests**: Application use case tests mocking/using in-memory repositories; verify reset preserves `gameId`.
- **Review Gate**: Clean architecture layer boundaries enforced.
- **Git Commit**: `feat(application): implement use case orchestration and in-memory persistence`

---

### Phase 7 — ASP.NET Core REST API & Error Handling
- **Objective**: Expose HTTP endpoints with RFC 7807 ProblemDetails error handling, Swagger documentation, and CORS configuration.
- **Requirements Covered**: FR-15, NFR-06, NFR-07, NFR-08.
- **Prerequisites**: Phase 6 application layer.
- **Expected Files**:
  - `backend/src/TicTacToe.Api/Controllers/GamesController.cs`
  - `backend/src/TicTacToe.Api/Controllers/ScoreboardController.cs`
  - `backend/src/TicTacToe.Api/Middleware/ProblemDetailsExceptionMiddleware.cs`
  - `backend/src/TicTacToe.Api/Program.cs`
- **Tests**: API startup test, Swagger JSON verification, status code mapping (200, 201, 400, 404, 409).
- **Review Gate**: OpenAPI schema complies strictly with `docs/06-api-contract.md`.
- **Git Commit**: `feat(api): expose REST endpoints with ProblemDetails error handling`

---

### Phase 8 — Backend Contract & Integration Tests
- **Objective**: Validate end-to-end backend workflows with in-memory test host (`WebApplicationFactory`).
- **Requirements Covered**: FR-18, NFR-02, NFR-04.
- **Prerequisites**: Phase 7 API.
- **Expected Files**:
  - `backend/tests/TicTacToe.IntegrationTests/GameFlowTests.cs`
  - `backend/tests/TicTacToe.IntegrationTests/ScoreboardTests.cs`
- **Tests**: TwoPlayer win flow, Computer mode automated response, Undo flow, Reset preserving gameId, Scoreboard reset.
- **Review Gate**: All backend unit and integration tests pass green with >90% coverage on domain logic.
- **Git Commit**: `test(api): add comprehensive integration test suite`

---

### Phase 9 — Angular Frontend Setup & Core Presentation
- **Objective**: Initialize Angular application with modern component architecture, responsive styling, and accessibility foundations.
- **Requirements Covered**: FR-02, FR-17, NFR-09, NFR-10, NFR-11.
- **Prerequisites**: Node LTS, Angular CLI.
- **Expected Files**:
  - `frontend/src/app/components/game-board/`
  - `frontend/src/app/components/game-status/`
  - `frontend/src/app/components/move-history/`
  - `frontend/src/app/components/scoreboard/`
  - `frontend/src/app/components/game-controls/`
- **Tests**: Component creation tests, template rendering tests.
- **Review Gate**: Clean responsive layout, high visual polish, visible focus indicators, no horizontal overflow.
- **Git Commit**: `feat(ui): create presentation components and layout shell`

---

### Phase 10 — Frontend State Facade & API Integration
- **Objective**: Wire frontend components to backend REST API via typed Angular services and a state facade.
- **Requirements Covered**: FR-16, FR-17, NFR-01.
- **Prerequisites**: Phase 7 API and Phase 9 UI components.
- **Expected Files**:
  - `frontend/src/app/core/services/game-api.service.ts`
  - `frontend/src/app/core/services/scoreboard-api.service.ts`
  - `frontend/src/app/core/services/game-state.service.ts`
  - `frontend/src/app/core/models/game.models.ts`
- **Tests**: Mocked HTTP service unit tests, state facade transition tests.
- **Review Gate**: Zero game rule duplication in Angular; UI strictly renders backend responses.
- **Git Commit**: `feat(ui): integrate state facade with backend REST API`

---

### Phase 11 — End-to-End Verification & Edge Cases
- **Objective**: Verify end-to-end user workflows in a real browser session.
- **Requirements Covered**: FR-01 through FR-18, NFR-01, NFR-05.
- **Prerequisites**: Both frontend and backend running locally.
- **Expected Files**:
  - `docs/testing/test-runs/e2e-verification-report.md`
- **Tests**: E2E-01 TwoPlayer Win, E2E-02 Draw, E2E-03 Computer Mode, E2E-04 Undo scenarios.
- **Review Gate**: All acceptance criteria satisfied across interactive play.
- **Git Commit**: `test(e2e): verify end-to-end workflows and edge cases`

---

### Phase 12 — Security, CORS & NFR Hardening
- **Objective**: Harden security posture (CORS origin validation, request body limits, error masking) and performance verification.
- **Requirements Covered**: NFR-04, NFR-05, NFR-06, NFR-08.
- **Prerequisites**: Functional application.
- **Expected Files**:
  - Configuration hardening in `appsettings.json` and `Program.cs`
- **Tests**: Invalid payload fuzzing, unallowed origin CORS rejection, latency benchmarking (<50ms).
- **Review Gate**: Security checklist fully signed off.
- **Git Commit**: `security: harden CORS, input validation, and error shielding`

---

### Phase 13 — AI Development Audit & Traceability Reconciliation
- **Objective**: Ensure complete audit ledger linking all requirements, prompts, generated outputs, manual edits, and Git commits.
- **Requirements Covered**: FR-19, NFR-14, NFR-15.
- **Prerequisites**: Codebase feature-complete.
- **Expected Files**:
  - `docs/ai/REQUIREMENT-TRACEABILITY.md` (Updated to 100% Complete)
  - `docs/ai/IMPLEMENTATION-LOG.md` (All prompt entries closed)
  - `docs/ai/MANUAL-CHANGES.md` (All human changes logged)
  - `docs/README.md` (Final sections populated)
- **Tests**: Verify all commit hashes and prompt IDs match audit documents.
- **Review Gate**: Full traceability audit verified with zero dangling requirements.
- **Git Commit**: `docs(ai): complete traceability matrix and implementation audit`

---

### Phase 14 — Panel Defense Readiness & Architecture Presentation
- **Objective**: Rehearse architecture defense presentation and prepare walkthrough notes for the interview panel.
- **Requirements Covered**: NFR-16, Panel readiness.
- **Prerequisites**: Documentation and code complete.
- **Expected Files**:
  - `docs/ai/PANEL-DEFENSE-NOTES.md`
- **Tests**: Dry-run of the 5-minute architecture story, DDD rationale, and scalability questions.
- **Review Gate**: Candidate prepared to defend all architectural choices.
- **Git Commit**: `docs: finalize panel defense notes and architectural walkthrough`
