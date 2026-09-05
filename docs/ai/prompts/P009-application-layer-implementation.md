# P009 — Application Layer Implementation Plan (Corrected & Verified)

## 1. Objective

Establish the **Application Layer (`TicTacToe.Application`)** as the transport-neutral orchestration boundary between external adapters (REST API in P010, Angular UI in P011) and the Domain layer.

This implementation plan incorporates all review findings from **P009.1**, strictly preserving domain encapsulation, aggregate boundaries, the P008.2 domain event lifecycle, and in-memory repository persistence.

---

## 2. Scope

1. **Application Use-Case Contracts & Services**:
   - `IGameService`: Coordinates game lifecycle commands (`CreateGame`, `MakeMove`, `Undo`, `ResetGame`) and queries (`GetGame`).
   - `IScoreboardService`: Coordinates session scoreboard queries (`GetScoreboard`) and administrative reset (`ResetScoreboard`).
2. **Domain Repository Abstraction**:
   - `IGameRepository`: Located in `TicTacToe.Domain.Repositories` (DDD repository interface pattern alongside `IScoreboardRepository`).
3. **Infrastructure In-Memory Repository**:
   - `InMemoryGameRepository`: Located in `TicTacToe.Infrastructure.Repositories`, implementing `IGameRepository` with thread-safe session storage (`ConcurrentDictionary<GameId, Game>`).
4. **Application DTOs & Models**:
   - Canonical application contracts (`GameStateDto`, `MoveDto`, `ScoreboardDto`, `MakeMoveCommand`, `CreateGameCommand`) in `TicTacToe.Application.Models`, completely free of HTTP dependencies and aligned with `docs/06-api-contract.md`.
5. **Domain Event Orchestration**:
   - Application use cases coordinate `game.MakeMove` / `game.ExecuteTurn`, retrieve pending domain events from `game.DomainEvents`, dispatch them via `IDomainEventDispatcher`, and invoke `game.ClearDomainEvents()` strictly upon successful handler completion.
   - If dispatch fails, `ClearDomainEvents()` is not called; events remain on the aggregate for retry.
6. **Application Exception Handling**:
   - `GameNotFoundException` in `TicTacToe.Application.Exceptions` for missing game queries.
   - Domain invariant exceptions propagate through the Application layer without HTTP status code translation.

---

## 3. Explicit Anti-Scope (What P009 Does NOT Implement)

- REST API Controllers (`GamesController`, `ScoreboardController`).
- ASP.NET Core Action Results, HTTP Status Codes, or ProblemDetails serialization.
- Angular UI components, services, or templates.
- EF Core, SQL, databases, or migrations.
- External message brokers (RabbitMQ, Kafka, Azure Service Bus).
- Outbox pattern or distributed transactions.
- Authentication or Authorization.

---

## 4. Repository Dependencies & Layer Boundaries

```text
       [ TicTacToe.Api (P010) ]
                  │
                  ▼
    [ TicTacToe.Application (P009) ]
        │                       │
        ▼                       ▼
[ TicTacToe.Domain ] ◄── [ TicTacToe.Infrastructure (P009) ]
```

- **`TicTacToe.Domain`**: Pure .NET 8 library (0 PackageReferences, 0 ProjectReferences).
  - Owns domain aggregates (`Game`, `Scoreboard`), domain logic, events (`GameCompletedEvent`), strategy (`BasicComputerMoveStrategy`), and repository contracts (`IGameRepository`, `IScoreboardRepository`).
- **`TicTacToe.Application`**: References only `TicTacToe.Domain`.
  - Owns use-case services (`GameService`, `ScoreboardService`), application models/DTOs, event dispatcher contracts, and event handlers.
  - Zero references to ASP.NET Core, HTTP, or Infrastructure.
- **`TicTacToe.Infrastructure`**: References `TicTacToe.Domain`.
  - Owns in-memory storage implementations (`InMemoryGameRepository`, `InMemoryScoreboardRepository`).

---

## 5. Application Use Cases

### 5.1 Create Game
- **Contract**: `Task<GameStateDto> CreateGameAsync(CreateGameCommand command, CancellationToken ct = default)`
- **Behavior**:
  1. Instantiates `Game.Create(command.Mode)`.
  2. Saves aggregate: `await _gameRepository.SaveAsync(game, ct)`.
  3. Loads session scoreboard: `var scoreboard = await _scoreboardRepository.GetScoreboardAsync(ct)`.
  4. Returns `game.ToDto(scoreboard)`.
- **Invariants**: Starts in `InProgress` status with `Player.X`, empty board, 0 moves, `CanUndo = false`.

### 5.2 Get Game
- **Contract**: `Task<GameStateDto> GetGameAsync(GameId gameId, CancellationToken ct = default)`
- **Behavior**:
  1. Retrieves aggregate: `await _gameRepository.GetByIdAsync(gameId, ct)`.
  2. If null $\rightarrow$ throws `GameNotFoundException(gameId)`.
  3. Loads scoreboard: `var scoreboard = await _scoreboardRepository.GetScoreboardAsync(ct)`.
  4. Returns `game.ToDto(scoreboard)`.
- **Invariants**: Pure query; zero side effects on game or scoreboard state.

### 5.3 Make Move
- **Contract**: `Task<GameStateDto> MakeMoveAsync(MakeMoveCommand command, CancellationToken ct = default)`
- **Behavior**:
  1. Converts primitive index to value object: `var cellIndex = new CellIndex(command.CellIndex)`.
  2. Retrieves aggregate: `await _gameRepository.GetByIdAsync(command.GameId, ct)`.
  3. If null $\rightarrow$ throws `GameNotFoundException(command.GameId)`.
  4. Domain execution:
     - If `game.Mode == GameMode.TwoPlayer`:
       `game.MakeMove(command.Player, cellIndex);`
     - If `game.Mode == GameMode.Computer`:
       `game.ExecuteTurn(cellIndex, _computerStrategy);`
  5. Event Dispatch Lifecycle:
     - If `game.DomainEvents.Count > 0`:
       - `var events = game.DomainEvents.ToList();`
       - `await _eventDispatcher.DispatchAsync(events);`
       - `game.ClearDomainEvents();` // Strictly after successful dispatch
  6. Saves aggregate: `await _gameRepository.SaveAsync(game, ct)`.
  7. Loads scoreboard: `var scoreboard = await _scoreboardRepository.GetScoreboardAsync(ct)`.
  8. Returns `game.ToDto(scoreboard)`.
- **Invariants**:
  - Validates `cellIndex` within `0..8`.
  - If human move in Computer Mode ends game, computer is not invoked.
  - If dispatch throws, `ClearDomainEvents()` is NOT invoked; exception bubbles up and events remain pending for retry.

### 5.4 Undo
- **Contract**: `Task<GameStateDto> UndoAsync(GameId gameId, CancellationToken ct = default)`
- **Behavior**:
  1. Retrieves aggregate: `await _gameRepository.GetByIdAsync(gameId, ct)`.
  2. If null $\rightarrow$ throws `GameNotFoundException(gameId)`.
  3. Invokes aggregate: `game.Undo();`.
  4. Saves aggregate: `await _gameRepository.SaveAsync(game, ct)`.
  5. Loads scoreboard: `var scoreboard = await _scoreboardRepository.GetScoreboardAsync(ct)`.
  6. Returns `game.ToDto(scoreboard)`.
- **Invariants**:
  - TwoPlayer reverts 1 move.
  - Computer Mode reverts human+computer pair.
  - Option A terminal lock enforced by aggregate (`CannotUndoException`).

### 5.5 Reset Game
- **Contract**: `Task<GameStateDto> ResetGameAsync(GameId gameId, CancellationToken ct = default)`
- **Behavior**:
  1. Retrieves aggregate: `await _gameRepository.GetByIdAsync(gameId, ct)`.
  2. If null $\rightarrow$ throw `GameNotFoundException(gameId)`.
  3. If `game.DomainEvents.Count > 0`:
     - Dispatches pending events before resetting: `await _eventDispatcher.DispatchAsync(game.DomainEvents); game.ClearDomainEvents();`
  4. Invokes aggregate: `game.Reset();`.
  5. Saves aggregate: `await _gameRepository.SaveAsync(game, ct)`.
  6. Loads scoreboard: `var scoreboard = await _scoreboardRepository.GetScoreboardAsync(ct)`.
  7. Returns `game.ToDto(scoreboard)`.
- **Invariants**:
  - Preserves `GameId` (FR-12).
  - Resets board, status to `InProgress`, starting player to `X`, clears history and undo stack.
  - Undelivered events are never silently discarded.

### 5.6 Get Scoreboard
- **Contract**: `Task<ScoreboardDto> GetScoreboardAsync(CancellationToken ct = default)`
- **Behavior**:
  1. Loads session scoreboard: `var scoreboard = await _scoreboardRepository.GetScoreboardAsync(ct)`.
  2. Returns `scoreboard.ToDto()`.

### 5.7 Reset Scoreboard
- **Contract**: `Task<ScoreboardDto> ResetScoreboardAsync(CancellationToken ct = default)`
- **Behavior**:
  1. Loads session scoreboard: `var scoreboard = await _scoreboardRepository.GetScoreboardAsync(ct)`.
  2. Invokes: `scoreboard.Reset();`.
  3. Persists: `await _scoreboardRepository.SaveScoreboardAsync(scoreboard, ct)`.
  4. Returns `scoreboard.ToDto()`.
- **Invariants**: Resets counters to 0 while preserving idempotency event history. Active game aggregate is untouched.

---

## 6. Canonical Application DTOs

Defined in `TicTacToe.Application.Models`:

```csharp
namespace TicTacToe.Application.Models;

using System;
using System.Collections.Generic;
using TicTacToe.Domain.ValueObjects;

public sealed record ScoreboardDto(
    int XWins,
    int OWins,
    int Draws,
    int TotalGames);

public sealed record MoveDto(
    int MoveNumber,
    Player Player,
    int CellIndex);

public sealed record GameStateDto(
    Guid GameId,
    IReadOnlyList<Player?> Board,
    Player CurrentPlayer,
    GameMode Mode,
    GameStatus Status,
    Player? Winner,
    IReadOnlyList<int> WinningCells,
    IReadOnlyList<MoveDto> MoveHistory,
    bool CanUndo,
    ScoreboardDto Scoreboard);

public sealed record MakeMoveCommand(
    Guid GameId,
    Player Player,
    int CellIndex);

public sealed record CreateGameCommand(
    GameMode Mode);
```

---

## 7. Repository Responsibilities & Interfaces

### Domain Interface (`backend/TicTacToe.Domain/Repositories/IGameRepository.cs`):
```csharp
namespace TicTacToe.Domain.Repositories;

using System.Threading;
using System.Threading.Tasks;
using TicTacToe.Domain.Aggregates;
using TicTacToe.Domain.ValueObjects;

public interface IGameRepository
{
    Task<Game?> GetByIdAsync(GameId gameId, CancellationToken cancellationToken = default);
    Task SaveAsync(Game game, CancellationToken cancellationToken = default);
}
```

### Infrastructure Implementation (`backend/TicTacToe.Infrastructure/Repositories/InMemoryGameRepository.cs`):
- Thread-safe storage via `ConcurrentDictionary<GameId, Game>`.
- Stores authoritative aggregate references in memory for the process lifetime.
- `GetByIdAsync`: Returns aggregate instance if found, or `null`.
- `SaveAsync`: Adds or updates entry in concurrent dictionary.

---

## 8. Domain Event Lifecycle & Failure Semantics

```text
Game Action (MakeMove / ExecuteTurn)
       │
       ▼
Terminal Transition? ──── NO ───► Game state updated, 0 events raised
       │ YES
       ▼
Game appends GameCompletedEvent to _domainEvents
       │
       ▼
Use Case retrieves game.DomainEvents
       │
       ▼
DomainEventDispatcher.DispatchAsync(events)
       │
       ├──── Handler Throws Exception ───► Exception bubbles up
       │                                   ClearDomainEvents() NOT called
       │                                   Event remains pending for retry
       ▼ (Dispatch Succeeded)
GameCompletedEventHandler updates Scoreboard
       │
       ▼
Use Case calls game.ClearDomainEvents()
       │
       ▼
Game aggregate saved to IGameRepository
       │
       ▼
Return GameStateDto with updated Scoreboard
```

---

## 9. Error Handling Strategy

1. **Transport-Neutral Exceptions**:
   - `GameNotFoundException` (`TicTacToe.Application.Exceptions`): Thrown when requested `GameId` does not exist.
   - Domain Exceptions (`CellOccupiedException`, `InvalidTurnException`, `GameAlreadyCompletedException`, `CannotUndoException`, `InvalidCellIndexException`): Propagate transparently through the Application layer without HTTP status code mapping.
2. **Zero Framework Coupling**: The Application layer has zero dependencies on `Microsoft.AspNetCore.Mvc` or HTTP status codes. Status code mapping (e.g. 404 for NotFound, 409 for Conflict, 400 for BadRequest) is deferred to P010 API middleware.

---

## 10. Concurrency Model

- **Scoreboard Aggregate**: Protected internally via `lock (_syncLock)` (P008).
- **Game Repository**: Thread-safe storage via `ConcurrentDictionary<GameId, Game>`.
- **Per-Game State**: In-memory reference mutation under single-process execution. Lock-free dictionary operations prevent collection corruption.

---

## 11. Testing Strategy

### Unit Tests (`backend/TicTacToe.Tests/Application/`):
1. **`GameServiceTests.cs`**:
   - `CreateGame_ReturnsInitialGameState_WithRequestedMode`
   - `GetGame_WhenGameExists_ReturnsAuthoritativeState`
   - `GetGame_WhenGameDoesNotExist_ThrowsGameNotFoundException`
   - `MakeMove_ValidMove_UpdatesBoardAndAlternatesTurn`
   - `MakeMove_InvalidCellIndex_ThrowsInvalidCellIndexException`
   - `MakeMove_CellOccupied_ThrowsCellOccupiedException`
   - `MakeMove_WrongPlayer_ThrowsInvalidTurnException`
   - `MakeMove_WhenTerminalWin_DispatchesEventAndUpdatesScoreboard`
   - `MakeMove_WhenTerminalDraw_DispatchesEventAndUpdatesScoreboard`
   - `MakeMove_WhenDispatchThrows_RetainsPendingEventOnGame`
   - `MakeMove_InComputerMode_ExecutesBothHumanAndComputerMoves`
   - `MakeMove_InComputerMode_WhenHumanMoveWins_HaltsWithoutComputerMove`
   - `Undo_WhenAllowed_RevertsGameState`
   - `Undo_WhenCompletedGame_ThrowsCannotUndoException`
   - `ResetGame_PreservesGameIdAndClearsGameState`
   - `ResetGame_WhenEventPending_DispatchesEventBeforeResetting`
2. **`ScoreboardServiceTests.cs`**:
   - `GetScoreboard_ReturnsAuthoritativeCounters`
   - `ResetScoreboard_ResetsCountersToZero`
   - `ResetScoreboard_PreservesEventDeduplicationHistory`

### Infrastructure Tests (`backend/TicTacToe.Tests/Infrastructure/`):
3. **`InMemoryGameRepositoryTests.cs`**:
   - `SaveAsync_And_GetByIdAsync_StoresAndRetrievesGame`
   - `GetByIdAsync_WhenMissing_ReturnsNull`
   - `ConcurrentSaves_DoNotCorruptDictionary`

---

## 12. File-by-File Implementation Plan

| Layer | File Path | Action | Responsibility |
|---|---|---|---|
| **Domain** | `backend/TicTacToe.Domain/Repositories/IGameRepository.cs` | CREATE | Domain contract for game aggregate retrieval and persistence |
| **Application** | `backend/TicTacToe.Application/Exceptions/GameNotFoundException.cs` | CREATE | Transport-neutral exception for missing game aggregate |
| **Application** | `backend/TicTacToe.Application/Models/ScoreboardDto.cs` | CREATE | Canonical DTO for scoreboard counters |
| **Application** | `backend/TicTacToe.Application/Models/MoveDto.cs` | CREATE | Canonical DTO for move history entries |
| **Application** | `backend/TicTacToe.Application/Models/GameStateDto.cs` | CREATE | Canonical DTO for complete authoritative game state |
| **Application** | `backend/TicTacToe.Application/Models/Commands.cs` | CREATE | Input command models (`CreateGameCommand`, `MakeMoveCommand`) |
| **Application** | `backend/TicTacToe.Application/Mappings/DtoMappingExtensions.cs` | CREATE | Pure extension methods mapping `Game` and `Scoreboard` to DTOs |
| **Application** | `backend/TicTacToe.Application/Services/IGameService.cs` | CREATE | Application interface for game orchestration |
| **Application** | `backend/TicTacToe.Application/Services/GameService.cs` | CREATE | Orchestrates use cases, repository access, and event lifecycle |
| **Application** | `backend/TicTacToe.Application/Services/IScoreboardService.cs` | CREATE | Application interface for scoreboard management |
| **Application** | `backend/TicTacToe.Application/Services/ScoreboardService.cs` | CREATE | Orchestrates scoreboard query and reset |
| **Infrastructure**| `backend/TicTacToe.Infrastructure/Repositories/InMemoryGameRepository.cs` | CREATE | In-memory thread-safe `IGameRepository` implementation |
| **Tests** | `backend/TicTacToe.Tests/Application/GameServiceTests.cs` | CREATE | Unit tests for game use cases and event orchestration |
| **Tests** | `backend/TicTacToe.Tests/Application/ScoreboardServiceTests.cs` | CREATE | Unit tests for scoreboard service |
| **Tests** | `backend/TicTacToe.Tests/Infrastructure/InMemoryGameRepositoryTests.cs` | CREATE | Unit tests for in-memory game repository |
| **Docs** | `docs/ai/reviews/P009-review.md` | CREATE | Formal review artifact for Phase P009 |
| **Docs** | `docs/ai/REQUIREMENT-TRACEABILITY.md` | MODIFY | Update FR-01 through FR-14 Application layer status |
| **Docs** | `docs/ai/ARCHITECTURE-TRACEABILITY.md` | MODIFY | Update Application layer component mappings |
| **Docs** | `docs/ai/IMPLEMENTATION-LOG.md` | MODIFY | Record P009 entry and commit hashes |

---

## 13. Verification Commands

```powershell
# 1. Build backend solution (target: 0 warnings, 0 errors)
dotnet build backend\TicTacToe.sln

# 2. Run all backend tests (target: ~180+ tests passing, 0 failed, 0 skipped)
dotnet test backend\TicTacToe.sln

# 3. Run frontend smoke tests (target: 2 passed, 0 regressions)
npm test --prefix frontend -- --watch=false
```

---

## 14. Git Commit Protocol

1. **Commit 1 (Application Layer Implementation)**:
   ```text
   feat(application): implement game and scoreboard application use cases and repository
   ```
2. **Commit 2 (Documentation Closure)**:
   ```text
   docs(ai): record P009 commit hash in implementation log
   ```

---

## 15. Definition of Done

- `IGameService` and `IScoreboardService` fully orchestrate all required functional requirements.
- Domain event lifecycle is strictly preserved (cleared only after successful dispatch).
- In-memory repository safely persists game sessions with `GameId` preservation across resets.
- All unit and integration tests pass with 0 warnings, 0 errors, and 0 regressions.
- Traceability and review artifacts updated.
- Two-commit protocol strictly executed.
