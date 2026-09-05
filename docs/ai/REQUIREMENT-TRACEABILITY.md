# Requirement Traceability Matrix

This document maps all Functional Requirements (FR-01 to FR-19) and Non-Functional Requirements (NFR-01 to NFR-16) to architectural design decisions, planned implementation areas, verification tests, and status.

---

## 1. Functional Requirements (FR)

| Requirement ID | Name | Description | Key Invariants / Rules |
|---|---|---|---|
| **FR-01** | Create Game | Initialize a game session with unique `gameId`, empty 3x3 board, X current player, selected mode, InProgress status, empty history, unchanged scoreboard. | Generates new UUID; in-memory store retains game. |
| **FR-02** | Board | 9 cells indexed `0..8` (`0 1 2 / 3 4 5 / 6 7 8`). | Only empty cells are clickable/playable. Occupied cells immutable. |
| **FR-03** | Turns | Players X and O. Valid moves alternate turns. | Current player visible. Invalid moves do not alter turn. |
| **FR-04** | Win Detection | Detect 3 matching symbols across 3 rows, 3 columns, or 2 diagonals. | Expose winner, expose winning cell indices, prevent further moves, trigger `GameCompleted` event exactly once. |
| **FR-05** | Draw Detection | Full board (9 cells filled) with no detected winner. | Status transitions to `Draw`, prevent further moves, trigger `GameCompleted` event exactly once. |
| **FR-06** | Move Validation | Reject `cellIndex < 0` or `> 8`, occupied cells, moves after completion, wrong player. | Backend rejects with HTTP 400 or 409; state is NOT mutated on invalid input. |
| **FR-07** | Move History | Chronological list of moves (`moveNumber`, `player`, `cellIndex`). | UI may derive Row/Col for presentation; API only emits `cellIndex`. |
| **FR-08** | Undo Availability | Disabled when move history is empty. Under Option A, disabled after game completion. | InProgress with >= 1 move enabled; otherwise disabled. |
| **FR-09** | Two Player Undo | Reverts the single most recent move and restores the previous player's turn. | Uses Memento snapshot restore. |
| **FR-10** | Computer Mode Undo | Reverts both the computer's move and the human's preceding move, restoring human X's turn. | Reverts to snapshot captured prior to human move. |
| **FR-11** | Scoreboard | Tracks X Wins, O Wins, Draws at session level. | Updated exclusively via `GameCompleted` domain event; GET requests do not increment. |
| **FR-12** | Reset Game | Clears board, move history, winner, status to InProgress, current player to X. | **Reuses existing `gameId`**. Preserves scoreboard. |
| **FR-13** | Reset Scoreboard | Clears X Wins, O Wins, and Draws to 0. | Does not modify active game board or state. |
| **FR-14** | Computer Mode | Human = X, Computer = O. Computer responds automatically after human move. | Priority: 1. Win move, 2. Block X win, 3. Center, 4. Corner, 5. Any cell. Deterministic. |
| **FR-15** | Backend API | REST endpoints for Game creation, retrieval, moves, undo, reset, and scoreboard. | Resource-oriented REST, ProblemDetails on error. |
| **FR-16** | Backend Ownership | Backend is the authoritative source of truth for rules, state, turns, and outcomes. | Frontend cannot dictate outcomes; purely presents backend state. |
| **FR-17** | Frontend UI | Angular single-page application rendering board, controls, status, history, scoreboard. | Consumes backend API; disables invalid actions client-side while backend validates authoritatively. |
| **FR-18** | Testing | Comprehensive testing covering domain rules, state transitions, API contracts, frontend components. | Minimal 90%+ domain test coverage; integration and E2E scenarios. |
| **FR-19** | AI Governance & Docs | Traceable SDLC linking requirements to prompts, generated code, manual edits, tests, and commits. | Maintained in `docs/ai/` audit directories. |

---

## 2. Non-Functional Requirements (NFR)

| NFR ID | Category | Target / Requirement | Verification Method |
|---|---|---|---|
| **NFR-01** | Correctness | Deterministic state transitions, backend authority, atomic mutations, exactly-once score update. | Automated unit & integration tests covering all state transitions. |
| **NFR-02** | Testability | Pure domain isolation (no HTTP/DB dependencies), deterministic computer strategy, >90% domain coverage. | xUnit / NUnit test suites, code coverage reports. |
| **NFR-03** | Maintainability | SOLID design, clean DDD layers, separation of DTOs and Domain models, no duplicated logic. | Architectural static analysis & peer review. |
| **NFR-04** | Performance | Sub-50ms API response locally; computer move evaluation is instantaneous; zero blocking calls. | Local API benchmark / stopwatch integration test. |
| **NFR-05** | Reliability | Graceful failure handling; malformed/conflict input converted to structured error without corrupting state. | Negative testing (boundary values, concurrent invalid calls). |
| **NFR-06** | Security | Server-side validation, CORS restricted to frontend origin, no stack trace exposure, OWASP compliance. | Security review checklist, CORS integration test. |
| **NFR-07** | API Consistency | Standard HTTP codes (200, 201, 400, 404, 409, 500) and ProblemDetails RFC 7807 error format. | Contract testing against OpenAPI specification. |
| **NFR-08** | Observability | Structured logging with `TraceId`, `GameId`, `Operation`, `Outcome`, `DurationMs`. | Log inspection during integration tests. |
| **NFR-09** | Usability | Intuitive UI, clear turn/winner banners, winning cell highlight, distinct game/score reset buttons. | Manual UX inspection on desktop browser. |
| **NFR-10** | Accessibility | Keyboard navigation, ARIA labels for grid cells ("Row 1 Column 1"), visible focus rings, high-contrast states. | Accessibility audit (Lighthouse / manual screen reader checks). |
| **NFR-11** | Responsive UI | Clean presentation on desktop/laptop resolutions without horizontal scrollbars or layout breakage. | Browser viewport testing (1024px to 1920px). |
| **NFR-12** | Deployment Simplicity | Simple setup: clone, `dotnet run`, `npm start`, play. Complete commands documented in README. | Clean environment reproduction test. |
| **NFR-13** | Reproducibility | Documented and pinned runtime/SDK versions (.NET SDK, Node.js, Angular CLI). | Pre-flight validation script. |
| **NFR-14** | AI Transparency | Auditable chain of custody: Requirement → ADR → Prompt → Code → Test → Review → Commit. | Verification of `docs/ai/` changelogs and commit history. |
| **NFR-15** | Technical Debt Tracking | Explicit documentation of intentional architectural trade-offs (e.g. in-memory storage vs distributed DB). | Review of `docs/13-assumptions.md` and ADRs. |
| **NFR-16** | Scalability Discussion | Documented evolutionary roadmap from single-node in-memory to distributed cluster. | Architectural documentation review in `docs/05-architecture.md`. |

---

## 3. Comprehensive Traceability Matrix

| Requirement | Design Decision | Implementation Area | Verification Test | Evidence | Current Status |
|---|---|---|---|---|---|
| **FR-01** (Create Game) | ADR-001, ADR-002 | `Game` Aggregate, `CreateGameHandler`, `GamesController` | `Create_InitializesGameInStandardInitialState` | Unit / Integration Test | Partially Implemented (Domain Aggregate Initialized; Application/API/UI Deferred) |
| **FR-02** (Board 0..8) | A-001, ADR-001 | `Board` Entity, `CellIndex` Value Object | `BoardTests`, `CellIndexTests` | Unit Test | Partially Implemented (Domain Model Complete; API DTO/UI Rendering Deferred) |
| **FR-03** (Turns) | ADR-001, ADR-003 | `Game.MakeMove()`, `CurrentPlayer` transition | `MakeMove_ValidMove_PlacesMarkAppendsHistoryAndAlternatesTurn` | Unit Test | Partially Implemented (Domain Turn Rules Complete; API/UI Interaction Deferred) |
| **FR-04** (Win Detection) | ADR-001, ADR-007 | `WinDetector` Domain Service, `Game.MakeMove` | `WinDetectorTests`, `MakeMove_PlayerXWinsRow_TransitionsToWonSetsWinnerAndHaltsTurnProgression` | Unit Test | Domain portion: Implemented; Overall requirement: Partially Implemented (Deferred integration: P008 GameCompleted event, P010/P011 API/UI) |
| **FR-05** (Draw Detection) | ADR-001, ADR-007 | `WinDetector` Domain Service, `Game.MakeMove` | `MakeMove_StandardDraw_TransitionsToDrawWithNullWinnerAndEmptyWinningCells`, `MakeMove_FinalMoveCompletesWinningLine_TakesPrecedenceOverDraw` | Unit Test | Domain portion: Implemented; Overall requirement: Partially Implemented (Deferred integration: P008 GameCompleted event, P010/P011 API/UI) |
| **FR-06** (Move Validation) | ADR-001, ADR-003 | `Game.MakeMove()` invariants, Domain Exceptions | `MakeMove_OccupiedCell_ThrowsCellOccupiedExceptionAndDoesNotMutateState` | Unit / API Test | Partially Implemented (Domain Invariants Complete; HTTP/UI Validation Deferred) |
| **FR-07** (Move History) | A-001, ADR-001 | `Move` Value Object, `Game.MoveHistory` | `MakeMove_SequentialValidMoves_AlternatesPlayerCorrectly` | Unit Test | Partially Implemented (Domain History Tracking Complete; API/UI Presentation Deferred) |
| **FR-08** (Undo Availability)| ADR-004 (Option A) | `Game.CanUndo()`, `GameStatus` check | `CanUndo_DisabledWhenEmptyOrCompleted` | Unit Test | Intentionally Deferred to P006 |
| **FR-09** (Two Player Undo) | ADR-004, Memento | `GameMemento`, `Game.Undo()` | `Undo_TwoPlayer_RevertsSingleMove_And_RestoresPlayer` | Unit Test | Baseline Planned |
| **FR-10** (Computer Undo) | ADR-004, Memento | `GameMemento`, `Game.Undo()` | `Undo_ComputerMode_RevertsMovePair_And_RestoresHuman` | Unit Test | Baseline Planned |
| **FR-11** (Scoreboard) | ADR-007 | `Scoreboard` Entity, `GameCompletedHandler` | `Scoreboard_UpdatesExactlyOnce_OnGameCompleted` | Unit / Integration Test | Baseline Planned |
| **FR-12** (Reset Game) | A-002 | `Game.Reset()` (reuses `GameId`) | `ResetGame_PreservesGameId_And_ClearsState` | Unit / API Test | Baseline Planned |
| **FR-13** (Reset Scoreboard)| ADR-007 | `Scoreboard.Reset()`, `ScoreboardController` | `ResetScoreboard_ClearsCounters_WithoutAlteringGame` | Unit / API Test | Baseline Planned |
| **FR-14** (Computer Strategy)| ADR-005, Strategy | `IComputerMoveStrategy`, `RuleBasedComputerStrategy` | `ComputerStrategy_FollowsPriority_WinBlockCenterCorner` | Unit Test | Baseline Planned |
| **FR-15** (REST API) | ADR-001, REST DTOs | `GamesController`, `ScoreboardController` | API Integration Tests (`WebApplicationFactory`) | Integration Test | Host Scaffolded (/health verified) |
| **FR-16** (Backend Ownership)| ADR-001 | ASP.NET Core API Backend | End-to-End verification of state authority | E2E Test | Baseline Planned |
| **FR-17** (Frontend UI) | Layered Arch | Angular App (`GameBoard`, `Scoreboard`, etc.) | Jasmine/Karma / Vitest Component Tests | Unit / E2E Test | App Scaffolded (Build & Test OK) |
| **FR-18** (Testing) | Test Strategy | `TicTacToe.Tests`, Frontend specs | Test runner output, coverage reports | CI / Test Execution Log | Test Harness Active (xUnit & Vitest OK) |
| **FR-19** (AI Governance) | ADR-008, Governance | `docs/ai/` audit files, commit history | Audit ledger & Git log inspection | Documentation | Active (P001 & P002 Committed, P003 Ready) |

