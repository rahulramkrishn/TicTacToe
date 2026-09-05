# Architecture Traceability

This document traces architectural requirements extracted from the repository specifications to their planned architectural implementation locations within the solution layers.

---

## 1. Architectural Requirement Mapping

| Architectural Requirement | Source Document | Planned Architectural Location | Description / Responsibility |
|---|---|---|---|
| Pragmatic DDD Layering | `docs/04-ddd-and-domain-model.md`, `docs/05-architecture.md` | `backend/src/` (.Domain, .Application, .Infrastructure, .Api) | Strict layer separation with dependencies pointing inward toward the Domain. |
| Backend State Authority | `docs/01-requirements.md` (FR-16), `docs/10-adr-template-and-initial-decisions.md` (ADR-001) | `TicTacToe.Domain` & `TicTacToe.Api` | All game rules, turn alternations, and win detections are computed and enforced server-side. |
| Win and Draw Detection | `docs/01-requirements.md` (FR-04, FR-05), `docs/04-ddd-and-domain-model.md` §8 | `TicTacToe.Domain/Services/WinDetector.cs`, `Game.cs` | Pure domain service evaluating 8 canonical lines and full-board draw with win precedence. |
| Canonical Cell Addressing (`0..8`) | `docs/01-requirements.md` (FR-02), `docs/06-api-contract.md`, `docs/13-assumptions.md` (A-001) | `TicTacToe.Domain/ValueObjects/CellIndex.cs`, DTOs | Single integer `0..8` used across domain, DTOs, and API requests. Row/Col derived in frontend. |
| Snapshot Memento for Undo | `docs/04-ddd-and-domain-model.md`, `docs/10-adr-template-and-initial-decisions.md` (ADR-004) | `TicTacToe.Domain/Mementos/GameMemento.cs` | Snapshot captured prior to move execution. Undo restores previous snapshot without recalculating reversals. |
| Replaceable Computer Move Strategy | `docs/04-ddd-and-domain-model.md`, `docs/10-adr-template-and-initial-decisions.md` (ADR-005) | `TicTacToe.Domain/Strategies/IComputerMoveStrategy.cs` | Strategy pattern isolating AI decision-making from the `Game` aggregate. |
| Mandatory `GameCompleted` Domain Event | `docs/04-ddd-and-domain-model.md`, `docs/ADR-007-game-completed-domain-event.md` | `TicTacToe.Domain/Events/GameCompletedEvent.cs` | Domain event raised exactly once upon game transition to `Won` or `Draw`. |
| Synchronous In-Process Event Dispatching | `docs/ADR-007-game-completed-domain-event.md`, `docs/13-assumptions.md` (A-007) | `TicTacToe.Application/Events/IDomainEventDispatcher.cs` | In-memory dispatcher routing domain events to application handlers synchronously. |
| Session-Level Scoreboard Isolation | `docs/04-ddd-and-domain-model.md`, `docs/06-api-contract.md` | `TicTacToe.Domain/Entities/Scoreboard.cs`, `ScoreboardHandler` | Scoreboard is maintained outside the `Game` aggregate lifecycle, updated via domain event. |
| Reusable Identity on Game Reset | `docs/01-requirements.md` (FR-12), `docs/13-assumptions.md` (A-002) | `TicTacToe.Domain/Aggregates/Game.cs` (`Reset()` method) | Game state resets while retaining the original `GameId`. |
| In-Memory Repository Storage | `docs/02-prerequisites-and-environment.md`, `docs/10-adr-template-and-initial-decisions.md` (ADR-002) | `TicTacToe.Infrastructure/Repositories/InMemoryGameRepository.cs` | Thread-safe in-memory store (`ConcurrentDictionary`) for sessions and scoreboard. |
| RESTful Resource Model & ProblemDetails | `docs/06-api-contract.md`, `docs/03-nfr.md` (NFR-07) | `TicTacToe.Api/Controllers/`, Middleware | Thin controllers mapping HTTP verbs to use cases; standard RFC 7807 problem responses on errors. |
| Restricted CORS Policy | `docs/02-prerequisites-and-environment.md`, `docs/03-nfr.md` (NFR-06) | `TicTacToe.Api/Program.cs` | Restricted to frontend origin (`http://localhost:4200`); no wildcard `AllowAnyOrigin()`. |
| Structured Logging & Observability | `docs/03-nfr.md` (NFR-08), `docs/05-architecture.md` | ASP.NET Core ILogger in Application/Api layers | Structured context with `TraceId`, `GameId`, `Player`, `Operation`, `Outcome`. |
| Dumb Frontend Presentation Layer | `docs/05-architecture.md`, `docs/01-requirements.md` (FR-17) | `frontend/src/app/` (Components & Services) | Angular components strictly render backend state; facade services handle HTTP communication. |

---

## 2. Bounded Context & Domain Model Elements

```text
====================================================================================
                        BOUNDED CONTEXT: TicTacToe Game Management
====================================================================================

  [ Aggregates & Entities ]
  ├── Game (Aggregate Root)
  │     ├── GameId (Identity)
  │     ├── Board (Value Object: 9 cells)
  │     ├── CurrentPlayer (Value Object: Player enum)
  │     ├── GameMode (Value Object: GameMode enum)
  │     ├── GameStatus (Value Object: GameStatus enum)
  │     ├── Winner (Value Object: Player? nullable)
  │     ├── WinningCells (Value Object: IReadOnlyList<CellIndex>)
  │     ├── MoveHistory (Entity / Value Object collection: List<Move>)
  │     └── UndoStack (Memento Collection: Stack<GameMemento>)
  │
  └── Scoreboard (Session State Entity)
        ├── XWins (int)
        ├── OWins (int)
        └── Draws (int)

  [ Value Objects ]
  ├── CellIndex (invariant: 0 <= value <= 8)
  ├── Player (X, O)
  ├── GameMode (TwoPlayer, Computer)
  ├── GameStatus (InProgress, Won, Draw)
  └── Move (MoveNumber, Player, CellIndex, Timestamp)

  [ Domain Services ]
  └── WinDetector (Pure rule evaluation: 3 rows, 3 columns, 2 diagonals, draw)

  [ Strategy Pattern ]
  ├── IComputerMoveStrategy (interface: SelectMove(Board, Player))
  └── RuleBasedComputerStrategy (Priority: 1. Win, 2. Block, 3. Center, 4. Corner, 5. Any)

  [ Domain Events ]
  └── GameCompletedEvent (GameId, ResultStatus, Winner)

  [ Repository Interfaces (Domain/Application) ]
  ├── IGameRepository (GetById, Save, Delete)
  └── IScoreboardRepository (Get, Save, Reset)

  [ Application Services & Use Cases ]
  ├── CreateGameUseCase
  ├── MakeMoveUseCase (Orchestrates human move + computer move if mode is Computer)
  ├── UndoMoveUseCase
  ├── ResetGameUseCase
  ├── GetGameStateUseCase
  ├── GetScoreboardUseCase
  ├── ResetScoreboardUseCase
  └── GameCompletedEventHandler (Handles GameCompletedEvent and increments Scoreboard)
====================================================================================
```

---

## 3. Frontend-to-Backend Responsibility Matrix

| Concern | Frontend Responsibility | Backend Responsibility |
|---|---|---|
| Board State | Displays cells based on backend response | Authoritative state storage, mutation, and validation |
| Move Validation | Disables occupied cells in UI for usability | Authoritative rejection of occupied/out-of-bounds/wrong-player moves |
| Turn Alternation | Displays current player returned by backend | Enforces alternating turns and updates `CurrentPlayer` |
| Winning Line Detection | Highlights cells identified in `winningCells` list | Evaluates board lines and computes `winner` and `winningCells` |
| Draw Detection | Displays draw banner when `status === 'Draw'` | Checks cell exhaustion and transitions status to `Draw` |
| Computer Move | Receives updated board containing computer move | Selects move via `IComputerMoveStrategy` and applies to board |
| Undo Operation | Provides Undo button (disabled when invalid) | Restores previous Memento snapshot (1 or 2 moves) and returns state |
| Reset Operation | Triggers Reset button | Reinitializes board/moves under existing `gameId` |
| Scoreboard | Displays session scores | Tracks wins/draws triggered by `GameCompleted` domain event |
| Error Presentation | Shows user-friendly message from ProblemDetails | Generates RFC 7807 ProblemDetails with HTTP status and error code |
