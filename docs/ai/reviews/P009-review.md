# P009 Review — Application Layer Implementation

## 1. Executive Summary

Phase **P009** establishes the **Application Layer** for the Tic-Tac-Toe assessment, serving as the transport-neutral orchestration boundary between the pure Domain layer and future delivery mechanisms (P010 REST API controllers and P011 Angular UI).

This implementation strictly adheres to the approved architecture from `P009.1`, `P009.1.1`, and `P009.1.2`:
1. **Transport Neutrality**: The Application layer contains zero ASP.NET Core, HTTP, or UI dependencies. All interactions operate via canonical DTOs and command records.
2. **Domain Purity Preserved**: `TicTacToe.Domain` retains 0 PackageReferences and 0 ProjectReferences. The repository contract `IGameRepository` resides in `TicTacToe.Domain.Repositories`.
3. **Layer Decoupling**: `TicTacToe.Application` references only `TicTacToe.Domain`. It has 0 references to `TicTacToe.Infrastructure`.
4. **Authoritative In-Memory Persistence**: `InMemoryGameRepository` lives in `TicTacToe.Infrastructure`, maintaining live aggregates in a `ConcurrentDictionary<GameId, Game>`.
5. **Per-Game Concurrency**: `GameService` enforces strict command serialization per `GameId` using `ConcurrentDictionary<GameId, SemaphoreSlim>`, eliminating read-modify-write race conditions.
6. **Reconciled Event Lifecycle**: `ResetGameAsync` drains and dispatches any pending domain events before executing `game.Reset()`. Domain events are cleared strictly upon successful dispatch via `IDomainEventDispatcher` and retained on failure.
7. **Computer Mode Orchestration**: Application orchestrates the two-step turn for computer mode (human move followed by computer move) and single-step for two-player mode, utilizing domain invariants and strategy without duplicating rule logic.

All **191 backend tests** and **2 frontend tests** pass with 0 warnings and 0 errors.

---

## 2. Implemented Components & Architecture

### 2.1 Dependency Direction
```text
TicTacToe.Domain  (Pure, 0 PackageReferences, 0 ProjectReferences)
       ▲
       │ references
TicTacToe.Application  (Orchestration, DTOs, no HTTP/ASP.NET Core/Infrastructure)

TicTacToe.Domain
       ▲
       │ references
TicTacToe.Infrastructure  (In-memory repository implementations)
```
- Domain has 0 dependencies.
- Application depends strictly on Domain.
- Application has NO dependency on Infrastructure.
- Domain has NO dependency on Application or Infrastructure.

### 2.2 Domain Layer (`TicTacToe.Domain`)
- **`Repositories/IGameRepository.cs`**:
  - `Task<Game?> GetByIdAsync(GameId gameId, CancellationToken cancellationToken = default);`
  - `Task SaveAsync(Game game, CancellationToken cancellationToken = default);`
  - Placed in `TicTacToe.Domain.Repositories` to allow Domain to define its repository requirements without depending on outer layers.

### 2.3 Infrastructure Layer (`TicTacToe.Infrastructure`)
- **`Repositories/InMemoryGameRepository.cs`**:
  - Thread-safe repository implementing `IGameRepository` using `ConcurrentDictionary<GameId, Game>`.
  - Stores authoritative live aggregate instances in memory without detached cloning.
  - Returns `null` for missing `GameId`.

### 2.4 Application Layer (`TicTacToe.Application`)
- **`Exceptions/GameNotFoundException.cs`**:
  - Transport-neutral exception indicating a missing game aggregate. Contains no HTTP status codes.
- **`Models/ApplicationModels.cs`**:
  - `CreateGameCommand(GameMode Mode)`
  - `MakeMoveCommand(Guid GameId, Player Player, int CellIndex)`
  - `GameStateDto(Guid GameId, IReadOnlyList<Player?> Board, Player CurrentPlayer, GameMode Mode, GameStatus Status, Player? Winner, IReadOnlyList<int> WinningCells, IReadOnlyList<MoveDto> MoveHistory, bool CanUndo, ScoreboardDto Scoreboard)`
  - `MoveDto(int MoveNumber, Player Player, int CellIndex)`
  - `ScoreboardDto(int XWins, int OWins, int Draws, int TotalGames)`
  - All properties strictly match `docs/06-api-contract.md`. Exposes read-only collections and prevents domain entity leakage.
- **`Mappings/DtoMappingExtensions.cs`**:
  - Pure, deterministic, side-effect-free mapping extensions:
    - `ToDto(this Game game, Scoreboard scoreboard)`
    - `ToDto(this Scoreboard scoreboard)`
- **`Services/IGameService.cs` & `GameService.cs`**:
  - `CreateGameAsync`: Creates and persists new `Game` aggregate.
  - `GetGameAsync`: Retrieves game state or throws `GameNotFoundException`.
  - `MakeMoveAsync`: Validates, executes human and (if computer mode) computer moves, saves, dispatches domain events, and clears them on success.
  - `UndoAsync`: Restores previous snapshot via memento (reverting 1 move in TwoPlayer, 2 moves in Computer mode).
  - `ResetGameAsync`: Drains pending events, calls `game.Reset()`, and saves.
- **`Services/IScoreboardService.cs` & `ScoreboardService.cs`**:
  - `GetScoreboardAsync`: Retrieves current session scoreboard DTO.
  - `ResetScoreboardAsync`: Resets scoreboard counters while preserving deduplication history.

---

## 3. Concurrency, Event Lifecycle & Architectural Limitations

### 3.1 Per-Game Concurrency Model & Lock Lifecycle
- `GameService` uses `ConcurrentDictionary<GameId, SemaphoreSlim>` to serialize mutating operations on the same game aggregate.
- Concurrent operations on distinct `GameId` instances execute in parallel without contention.
- **Explicit Assessment Limitation (Semaphore Lifecycle)**:
  > *GameService maintains per-GameId locks for the lifetime of the application process. Lock cleanup is intentionally deferred because games are retained in the in-memory repository for the process lifetime. This is an explicit limitation of the in-memory assessment architecture and will be revisited if persistent storage is introduced.*

### 3.2 Transaction Boundary & Persistence Failure Semantics
- In this single-process in-memory architecture, aggregate mutation, event dispatch, event clearing, and repository persistence are treated as one application-level operation.
- **Explicit Assessment Limitation (Failure Semantics & Transaction Boundary)**:
  > *For the current single-process in-memory architecture, aggregate mutation, event dispatch, event clearing, and repository persistence are treated as one application-level operation. `ClearDomainEvents()` occurs only after successful event dispatch, and persistence failure must not be silently swallowed. The design does not claim distributed transactional guarantees.*
- If event dispatch throws:
  - Events are NOT cleared.
  - Exception propagates immediately.
  - In `ResetGameAsync`, the game is NOT reset if dispatch fails.
- If persistence fails after event dispatch:
  - The exception propagates out.
  - No synthetic two-phase commit or fake distributed transaction abstraction is introduced.

### 3.3 Scoreboard Independence
- `ResetGameAsync` resets the game board and turn history under the existing `GameId`, but does NOT reset the `Scoreboard`.
- `ResetScoreboardAsync` resets session score counters (`XWins`, `OWins`, `Draws`), but does NOT modify active games.
- Historical deduplication cache (`_processedEventIds`) is preserved across scoreboard reset.

---

## 4. Verification & Test Suite Summary

### New Test Suites Created (31 New Tests Added; Total Suite = 191 Tests)
1. **`GameServiceTests.cs` (21 tests)**:
   - `CreateGame_ReturnsInitialGameState_WithRequestedMode`
   - `GetGame_WhenGameExists_ReturnsAuthoritativeState`
   - `GetGame_WhenGameNotFound_ThrowsGameNotFoundException`
   - `MakeMove_ValidMove_UpdatesBoardAndAlternatesTurn`
   - `MakeMove_InvalidCellIndex_ThrowsInvalidCellIndexException`
   - `MakeMove_OccupiedCell_ThrowsCellOccupiedException`
   - `MakeMove_InvalidTurn_ThrowsInvalidTurnException`
   - `MakeMove_WhenGameAlreadyCompleted_ThrowsGameAlreadyCompletedException`
   - `MakeMove_WhenTerminalWin_DispatchesEventAndClearsPendingEvents`
   - `MakeMove_WhenTerminalDraw_DispatchesEventAndClearsPendingEvents`
   - `MakeMove_WhenDispatchThrows_RetainsPendingEvent`
   - `MakeMove_InComputerMode_ExecutesBothMoves`
   - `MakeMove_InComputerMode_WhenHumanWins_DoesNotInvokeComputer`
   - `Undo_WhenAllowed_RevertsGameState`
   - `Undo_WhenCompletedGame_ThrowsCannotUndoException`
   - `ResetGame_PreservesGameId`
   - `ResetGame_ClearsGameState`
   - `ResetGame_PreservesScoreboard`
   - `ResetGame_WhenEventPending_DispatchesBeforeReset`
   - `ResetGame_WhenDispatchFails_DoesNotReset`
   - `ResetGame_WhenDispatchFails_RetainsEvent`
2. **`GameConcurrencyTests.cs` (4 tests)**:
   - Serialized execution of concurrent moves on the same game.
   - Non-blocking concurrent execution across distinct games.
   - Concurrent undo operations serialization.
   - Concurrent moves and reset serialization.
3. **`ScoreboardServiceTests.cs` (3 tests)**:
   - Scoreboard retrieval and DTO mapping.
   - Scoreboard counter reset.
   - Independence of scoreboard operations from active games.
4. **`InMemoryGameRepositoryTests.cs` (3 tests)**:
   - Save and retrieve game aggregate.
   - Returns null for non-existent game ID.
   - Concurrent saves for distinct game aggregates.

### Test Execution Results
- `dotnet build backend/TicTacToe.sln`: **0 Warnings, 0 Errors**
- `dotnet test backend/TicTacToe.sln`: **191 Passed, 0 Failed, 0 Skipped (100% Success)**
- `npm test --prefix frontend -- --watch=false`: **2 Passed, 0 Failed (100% Success)**

---

## 5. Architectural & Governance Compliance Checklist

| Rule / Invariant | Status | Verification Evidence |
|---|---|---|
| Domain purity (0 external references) | Verified | `TicTacToe.Domain.csproj` has 0 PackageReferences and 0 ProjectReferences |
| Application independence | Verified | `TicTacToe.Application.csproj` references only `TicTacToe.Domain` |
| No HTTP/ASP.NET Core in Application | Verified | Application contains no controller, HTTP status, or web dependency |
| Cell addressing `0..8` | Verified | Enforced across DTOs, domain models, and service interfaces |
| `Game.Reset()` preserves `GameId` | Verified | Tested in `GameServiceTests.ResetGame_PreservesGameId` |
| `ResetGameAsync` drains pending events | Verified | Tested in `GameServiceTests.ResetGame_WhenEventPending_DispatchesBeforeReset` |
| Successful dispatch clears events | Verified | Tested in `GameServiceTests.MakeMove_WhenTerminalWin_DispatchesEventAndClearsPendingEvents` |
| Failed dispatch retains events | Verified | Tested in `GameServiceTests.MakeMove_WhenDispatchThrows_RetainsPendingEvent` |
| Command serialization per `GameId` | Verified | Tested in `GameConcurrencyTests.MakeMoveAsync_ConcurrentMovesOnSameGame_AreSerialized` |
| Scoreboard updated via domain event | Verified | Tested in `GameCompletedEventHandlerTests` & `ScoreboardServiceTests` |
| Computer mode turn executed via domain | Verified | `game.ExecuteTurn(cellIndex, _computerStrategy)` invoked in `GameService.MakeMoveAsync` |
| No REST controllers or Angular UI in P009 | Verified | Deferred to P010 and P011 |

---

## 6. Final Gate Decision

```text
P009 PASS — CLOSED
P010 READY
```
All requirements of prompt P009.1.2 are met, verified, and documented.
