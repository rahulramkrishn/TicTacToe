# P012 Simplification Review — Architecture Simplification & In-Process Domain Event Infrastructure Removal

## 1. What Was Removed
- **Domain Event Marker & Event Types**: `IDomainEvent` interface and `GameCompletedEvent` record.
- **Aggregate Event Queuing**: `Game._domainEvents`, `Game.DomainEvents`, and `Game.ClearDomainEvents()`.
- **Event Dispatcher & Handlers**: `IDomainEventDispatcher`, `DomainEventDispatcher`, `IDomainEventHandler<T>`, and `GameCompletedEventHandler`.
- **Event-Based Scoreboard Mutation**: `Scoreboard._processedEventIds` (`HashSet<Guid>`) and `Scoreboard.RecordGameCompleted(GameCompletedEvent)`.
- **DI Registrations**: Obsolete singleton registration of `IDomainEventDispatcher` and handler composition in `Program.cs`.
- **Event-Specific Tests**: 28 obsolete unit and integration tests exclusively verifying event queue retention, event payloads, and dispatcher routing.

## 2. What Was Retained
- **Pure Domain Aggregate**: `Game` remains the sole authority for move validation, turn alternation, win/draw detection, winner identification, winning cells calculation, and memento-based undo.
- **Domain Strategy Pattern**: `IComputerMoveStrategy` and `BasicComputerMoveStrategy` with deterministic 5-tier priority hierarchy.
- **Domain Memento Pattern**: `GameMemento` snapshots providing undo capability without heuristic recalculation.
- **Scoreboard Aggregate**: `Scoreboard` aggregate tracking session-level outcomes (`XWins`, `OWins`, `Draws`, `TotalGames`) with thread-safe `_syncLock`.
- **Repository Abstractions**: `IGameRepository`, `IScoreboardRepository`, and `InMemoryGameRepository`, `InMemoryScoreboardRepository`.
- **Per-Game Concurrency Protection**: `ConcurrentDictionary<GameId, SemaphoreSlim>` in singleton `GameService`.
- **Atomic Computer Turns**: Single endpoint call executes human move + computer response atomically under the per-game lock.
- **REST API Contract**: Complete adherence to `docs/06-api-contract.md` (all 7 endpoints, DTOs, HTTP verbs, and RFC 7807 ProblemDetails).
- **Angular Presentation Layer**: Zero code changes required; continues functioning as a pure presentation adapter.

## 3. Why Each Decision Was Made
- **Removal of In-Process Events**: In a single-process application with in-memory persistence and no independent asynchronous consumers, event streams, or microservices, in-process domain events added indirection and test fragility without business value.
- **Synchronous Application Orchestration**: Moving the consequence coordination to `GameService` keeps the domain pure (the `Game` aggregate decides when a game is Won/Draw; `GameService` coordinates the consequence of recording the outcome in the `Scoreboard`).
- **Explicit Terminal Transition Detection**: Guarding the scoreboard update by `previousStatus == GameStatus.InProgress && game.Status == Won/Draw` guarantees that exactly one transition causes exactly one scoreboard update, shifting idempotency from event GUID tracking to command orchestration.
- **Retaining Scoreboard `_syncLock`**: While game mutations are locked per `GameId`, the session scoreboard is shared across parallel games; maintaining internal synchronization ensures thread-safe counter increments under concurrent cross-game load.

## 4. Before Architecture
```text
Game.MakeMove()
   │ (emits GameCompletedEvent into _domainEvents)
   ▼
GameService.MakeMoveAsync()
   │ (drains game.DomainEvents)
   ▼
IDomainEventDispatcher.DispatchAsync()
   ▼
GameCompletedEventHandler.HandleAsync()
   ▼
Scoreboard.RecordGameCompleted(evt)
   ▼
IScoreboardRepository.SaveScoreboardAsync()
```

## 5. After Architecture
```text
GameService.MakeMoveAsync() [under per-GameId SemaphoreSlim lock]
   │
   ├── Game.MakeMove() or Game.ExecuteTurn()
   │     (Domain aggregate validates move and evaluates win/draw)
   │
   ├── if (previousStatus == InProgress && game.Status == Won/Draw):
   │     Scoreboard = await ScoreboardRepository.GetScoreboardAsync()
   │     Scoreboard.RecordWin(winner) OR Scoreboard.RecordDraw()
   │     await ScoreboardRepository.SaveScoreboardAsync(Scoreboard)
   │
   ├── await GameRepository.SaveAsync(game)
   │
   └── return game.ToDto(Scoreboard)
```

## 6. Files Deleted (12 Files)
1. `backend/TicTacToe.Domain/Events/IDomainEvent.cs`
2. `backend/TicTacToe.Domain/Events/GameCompletedEvent.cs`
3. `backend/TicTacToe.Application/Events/IDomainEventDispatcher.cs`
4. `backend/TicTacToe.Application/Events/DomainEventDispatcher.cs`
5. `backend/TicTacToe.Application/Events/IDomainEventHandler.cs`
6. `backend/TicTacToe.Application/EventHandlers/GameCompletedEventHandler.cs`
7. `backend/TicTacToe.Tests/Domain/GameCompletedEventTests.cs`
8. `backend/TicTacToe.Tests/Domain/GameEventEmissionTests.cs`
9. `backend/TicTacToe.Tests/Domain/EventLifecycleTests.cs`
10. `backend/TicTacToe.Tests/Application/DomainEventDispatcherTests.cs`
11. `backend/TicTacToe.Tests/Application/GameCompletedEventHandlerTests.cs`
12. `backend/TicTacToe.Tests/Application/ComputerModeEventIntegrationTests.cs`

## 7. Files Modified (11 Files)
1. `backend/TicTacToe.Domain/Entities/Scoreboard.cs`: Added `RecordWin(Player)` and `RecordDraw()`; removed `_processedEventIds` and `RecordGameCompleted`.
2. `backend/TicTacToe.Domain/Aggregates/Game.cs`: Removed `_domainEvents`, `DomainEvents`, `ClearDomainEvents()`, and event emission in `MakeMove()`.
3. `backend/TicTacToe.Application/Services/GameService.cs`: Injected only repositories and strategy; implemented synchronous scoreboard orchestration on terminal transitions; simplified reset.
4. `backend/TicTacToe.Api/Program.cs`: Removed `IDomainEventDispatcher` and handler registrations.
5. `backend/TicTacToe.Tests/Domain/ScoreboardTests.cs`: Updated to test direct `RecordWin`, `RecordDraw`, and concurrency under `_syncLock`.
6. `backend/TicTacToe.Tests/Domain/ResetIndependenceTests.cs`: Updated to call `RecordWin` directly.
7. `backend/TicTacToe.Tests/Domain/GameResetTests.cs`: Removed obsolete event collection assertions.
8. `backend/TicTacToe.Tests/Application/GameServiceTests.cs`: Removed dispatcher tests; updated constructor; added `MakeMove_OnAlreadyCompletedGame_ThrowsAndDoesNotDoubleCountScoreboard`.
9. `backend/TicTacToe.Tests/Application/GameConcurrencyTests.cs`: Removed dispatcher from test setup.
10. `backend/TicTacToe.Tests/Api/DiLifetimeTests.cs`: Removed dispatcher check.
11. `backend/TicTacToe.Tests/Api/GamesApiTests.cs`: Renamed test to `ResetGame_WhenTerminalMoveCompleted_PreservesScoreboardWinAfterReset`.

## 8. Tests Removed (33 Tests in Total)
- 7 tests in `GameCompletedEventTests` (event properties, validation, invariants).
- 7 tests in `GameEventEmissionTests` (event emission counts, clear events).
- 4 tests in `EventLifecycleTests` (event retention on reset, dispatch success/failure).
- 3 tests in `DomainEventDispatcherTests` (registration, handler invocation, unhandled events).
- 3 tests in `GameCompletedEventHandlerTests` (handler execution, idempotency, null guards).
- 3 tests in `ComputerModeEventIntegrationTests` (event count assertions during computer turns).
- 2 tests in `GameResetTests` (`Reset_DoesNotEmitDomainEvents`, `Reset_WhenCompletedAgain_ProducesNewEventIdWithSameGameId`).
- 1 test in `ResetIndependenceTests` (`GameReset_DoesNotEmitDomainEvents`).
- 3 tests in `GameServiceTests` (`MakeMove_WhenDispatchThrows_RetainsPendingEvent`, `ResetGame_WhenDispatchFails_DoesNotReset`, `ResetGame_WhenDispatchFails_RetainsEvent`).

## 9. Tests Added / Modified
- **Added**: `GameServiceTests.MakeMove_OnAlreadyCompletedGame_ThrowsAndDoesNotDoubleCountScoreboard` (asserts attempts to move on finished game throw `GameAlreadyCompletedException` and do not increment scoreboard).
- **Added**: `ScoreboardTests.RecordDraw_ConcurrentIncrements_AreThreadSafe` (verifies 20 parallel threads calling `RecordDraw` update count accurately).
- **Modified**: All remaining test suites updated to test actual business invariants rather than event infrastructure.

## 10. API Regression Results
- All 7 REST API endpoints tested via integration tests (`GamesApiTests`, `ScoreboardApiTests`, `ApiValidationTests`, `ApiConcurrencyTests`, `ApiSerializationTests`):
  - `POST /api/games` -> 201 Created (Verified)
  - `GET /api/games/{id}` -> 200 OK / 404 NotFound (Verified)
  - `POST /api/games/{id}/moves` -> 200 OK / 400 BadRequest / 409 Conflict (Verified)
  - `POST /api/games/{id}/undo` -> 200 OK / 409 Conflict (Verified)
  - `POST /api/games/{id}/reset` -> 200 OK (Verified)
  - `GET /api/scoreboard` -> 200 OK (Verified)
  - `POST /api/scoreboard/reset` -> 200 OK (Verified)
  - `GET /health` -> 200 OK (Verified)

## 11. Backend Test Results
```text
dotnet test backend\TicTacToe.sln
Passed!  - Failed: 0, Passed: 195, Skipped: 0, Total: 195, Duration: 373 ms
```
- Compilation with `--warnaserror`: **0 Warning(s), 0 Error(s)**.

## 12. Frontend Test Results
```text
npm test --prefix frontend -- --watch=false
Test Files  9 passed (9)
     Tests  43 passed (43)
```
- Frontend production build: **Succeeded** (main: 173.33 kB, styles: 13.36 kB).

## 13. Architecture Boundary Results
- `ArchitectureBoundaryTests.cs` (4 passed):
  - `DomainAssembly_HasNoDependencyOnOuterLayers` -> PASS
  - `ApplicationAssembly_DependsOnlyOnDomain_NotOnInfrastructureOrApi` -> PASS
  - `InfrastructureAssembly_DoesNotDependOnApi` -> PASS
  - `Controllers_AreThin_AndOnlyDependOnApplicationServiceInterfaces` -> PASS

## 14. Git Status
- Clean working directory ready for pre-commit review.
- No uncommitted source modifications outside of the intended simplification.
- `git diff --check` passed cleanly with 0 trailing whitespace or formatting errors.

## 15. Multi-Aggregate Persistence Failure Semantics
- **Architectural Limitation Declared**: The application currently uses independent in-memory repositories (`IGameRepository` and `IScoreboardRepository`) and therefore does not provide an atomic transaction across `Game` and `Scoreboard` aggregates.
- **Decision**: P012 intentionally accepts this limitation. Introducing Unit of Work, Domain Events, Outbox, or distributed transaction infrastructure is explicitly deferred until durable multi-aggregate database persistence becomes a requirement.

## 16. Remaining Complexity
- `GameService` retains thread-safe per-`GameId` `SemaphoreSlim` instances in a `ConcurrentDictionary`. This is necessary for in-memory serializing of concurrent requests to the same game session.
- `Scoreboard` retains internal `_syncLock` object synchronization. This is necessary because the session scoreboard is shared across parallel games.

## 17. Future Recommendations
- If persistent storage (e.g. EF Core with SQL or SQLite) is introduced, the per-game `SemaphoreSlim` in `GameService` can be replaced with optimistic concurrency (`rowversion` / `xmin`) on the `Game` aggregate.
- If distributed consumers (analytics, external services, event streaming) are introduced, Domain Events or Integration Events can be reintroduced using an explicit message broker or outbox pattern.

---

```text
P012 PASS — READY FOR PRE-COMMIT REVIEW
```
