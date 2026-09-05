# Tic-Tac-Toe — Complete System Implementation Guide (Backend & Frontend)

This document serves as the authoritative technical reference for the enterprise **Tic-Tac-Toe** system, detailing both the **.NET 8 Backend** (Domain-Driven Design, Onion Architecture, REST API) and the **Angular 21 Frontend** (Standalone Components, Signals, Reactive Facade, WCAG 2.1 AA Accessibility).

---

## 1. System Architecture Overview

The system is architected as an **Onion / Hexagonal (Ports & Adapters)** monolith with strict inward-pointing dependencies. Business logic, game rules, winning line detection, computer AI strategy, memento undo history, and scoreboard metrics are strictly isolated within the Domain and Application layers. The REST API and Angular SPA serve purely as external delivery adapters.

```text
┌───────────────────────────────────────────────────────────────────┐
│                    Angular 21 Presentation Layer                   │
│   (Standalone Components, Signals, GameFacade, Typed HTTP APIs)   │
└─────────────────────────────────┬─────────────────────────────────┘
                                  │ HTTP / JSON (Port 5000)
                                  │ Canonical cellIndex 0..8
                                  ▼
┌───────────────────────────────────────────────────────────────────┐
│                   TicTacToe.Api (REST Adapter)                    │
│   (Thin Controllers, RFC 7807 Middleware, JSON Serializer, CORS)  │
└─────────────────────────────────┬─────────────────────────────────┘
                                  │
                                  ▼
┌───────────────────────────────────────────────────────────────────┐
│              TicTacToe.Application (Use Case Services)             │
│   (GameService, ScoreboardService, Per-Game Concurrency Locks)    │
└─────────────────────────────────┬─────────────────────────────────┘
                                  │
                                  ▼
┌───────────────────────────────────────────────────────────────────┐
│                 TicTacToe.Domain (Core Enterprise)                │
│   (Game Aggregate, WinDetector, Computer Strategy, Memento Undo)  │
└─────────────────────────────────▲─────────────────────────────────┘
                                  │
┌─────────────────────────────────┴─────────────────────────────────┐
│             TicTacToe.Infrastructure (Persistence)                │
│       (InMemoryGameRepository, InMemoryScoreboardRepository)      │
└───────────────────────────────────────────────────────────────────┘
```

### Architectural Boundaries & Invariants
1. **Domain Isolation**: `TicTacToe.Domain` has zero outer project references and zero external NuGet dependencies (pure .NET 8).
2. **Backend Authority**: Win detection, draw detection, turn alternations, computer moves, undo snapshots, and scoreboard computations are 100% owned by the backend. The Angular client performs zero business logic.
3. **Canonical Cell Addressing**: All layer communications use `cellIndex` (`0..8`):
   ```text
   0 | 1 | 2
   ---------
   3 | 4 | 5
   ---------
   6 | 7 | 8
   ```
4. **RFC 7807 Error Standard**: All API errors adhere to ProblemDetails with standardized type URIs, machine-readable `code` attributes, and `traceId` correlation.
5. **Synchronous Application Orchestration (P012)**: The in-process Domain Event infrastructure was removed in Phase P012. Game completion and scoreboard updates are coordinated synchronously and deterministically within `GameService`.

---

## 2. Backend Implementation (.NET 8)

The backend solution is organized into 5 projects in `backend/TicTacToe.sln`:
- `TicTacToe.Domain`
- `TicTacToe.Application`
- `TicTacToe.Infrastructure`
- `TicTacToe.Api`
- `TicTacToe.Tests`

### 2.1 Domain Layer (`TicTacToe.Domain`)

#### Value Objects
- **`CellIndex`**: Enforces integer bounds `0..8`. Disallows negative or out-of-range indices with `InvalidCellIndexException`.
- **`GameId`**: Strongly-typed wrapper around `System.Guid` providing type safety.
- **`Player`**: Represents player `X` and `O`. Contains `Opponent` property (`Player.X.Opponent => Player.O`).
- **`GameMode`**: Enum `TwoPlayer` and `Computer`.
- **`GameStatus`**: Enum `InProgress`, `Won`, and `Draw`.
- **`Move`**: Immutable record `(int MoveNumber, Player Player, CellIndex CellIndex)`.
- **`WinResult`**: Immutable tuple returned by `WinDetector` containing `(bool IsTerminal, GameStatus Status, Player? Winner, IReadOnlyList<CellIndex>? WinningCells)`.

#### Aggregates & Entities
- **`Game` (Aggregate Root)**:
  - Manages `GameId`, `Board`, `CurrentPlayer`, `Mode`, `Status`, `Winner`, `WinningCells`, and `MoveHistory`.
  - Maintains private undo memento stack (`Stack<GameMemento>`).
  - **`ExecuteTurn(Player player, CellIndex cellIndex, IComputerMoveStrategy? strategy = null)`**:
    - Validates status is `InProgress` (`GameAlreadyCompletedException`).
    - Validates `player == CurrentPlayer` (`InvalidTurnException`).
    - Validates cell is empty on `Board` (`CellOccupiedException`).
    - Saves `GameMemento` prior to mutation.
    - Stamps cell with player's mark.
    - Evaluates win or draw via `WinDetector`.
    - If terminal, sets `Status`, `Winner`, and `WinningCells`.
    - If mode is `Computer` and game remains `InProgress`, immediately executes AI move using `strategy`.
  - **`Undo()`**:
    - Enforces Option A rules: Can only undo during `InProgress` status with at least 1 move. Disallowed after game completion (`InvalidOperationException`).
    - In `TwoPlayer` mode: pops 1 memento, restoring board, history, and previous player turn.
    - In `Computer` mode: pops both computer and human moves (2 mementos), restoring human Player X's turn.
  - **`Reset()`**:
    - Clears board, history, status (`InProgress`), winner (`null`), winningCells (`null`), and currentPlayer (`X`).
    - Strictly preserves the existing `GameId` and `GameMode`.
- **`Board` Entity**:
  - Encapsulates an internal array of 9 nullable `Player?` marks.
  - Exposes indexed access `this[CellIndex index]`, vacancy checking, and deep snapshotting.
- **`Scoreboard` (Session Aggregate)**:
  - Tracks `PlayerXWins`, `PlayerOWins`, and `Draws`.
  - Incremented exclusively through thread-safe `RecordWin(Player player)` and `RecordDraw()`.
  - Reset to zero via `Reset()` without impacting active game sessions.

#### Domain Services & Heuristics
- **`WinDetector`**:
  - Pure static domain service evaluating the 8 canonical winning lines (3 horizontal, 3 vertical, 2 diagonal):
    - Rows: `(0,1,2)`, `(3,4,5)`, `(6,7,8)`
    - Columns: `(0,3,6)`, `(1,4,7)`, `(2,5,8)`
    - Diagonals: `(0,4,8)`, `(2,4,6)`
  - Evaluates board fill for `Draw` when 9 moves are played with no winning line.
- **`BasicComputerMoveStrategy` (`IComputerMoveStrategy`)**:
  - Deterministic 5-tier rule-based AI for Player O:
    1. **Immediate Win**: Claim winning cell if Player O has two in a row.
    2. **Block Opponent**: Block Player X if human has two in a row.
    3. **Center Control**: Claim center cell (`cellIndex == 4`) if open.
    4. **Corner Selection**: Claim first available corner (`0, 2, 6, 8`).
    5. **Remaining Cells**: Claim first available open cell (`1, 3, 5, 7`).
- **`GameMemento`**:
  - Deep-clone snapshot of aggregate state (`Board`, `CurrentPlayer`, `Status`, `Winner`, `WinningCells`, `MoveHistory`) enabling rollbacks.

---

### 2.2 Game State Machine

```text
                  ┌──────────────────────┐
                  │       Created        │
                  │ (X Turn, Board Empty)│
                  └──────────┬───────────┘
                             │ ExecuteTurn(X, cell)
                             ▼
         ┌────────────────────────────────────────────────┐
         │                   InProgress                   │
         │  ◄── ExecuteTurn(O) ────── ExecuteTurn(X) ──►  │
         │  ◄── Undo() [Human/Pair]                       │
         └───────────┬────────────────────────┬───────────┘
                     │                        │
  WinDetector finds  │                        │ WinDetector finds
  3-in-a-row         │                        │ 9 occupied cells & no win
                     ▼                        ▼
         ┌──────────────────────┐ ┌───────────────────────┐
         │         Won          │ │         Draw          │
         │ (Terminal / Locked)  │ │  (Terminal / Locked)  │
         └───────────┬──────────┘ └───────────┬───────────┘
                     │                        │
                     └───────────┬────────────┘
                                 │ Reset()
                                 ▼
                     ┌──────────────────────┐
                     │      InProgress      │
                     │ (Clean Board, X Turn)│
                     └──────────────────────┘
```

| Current State | Operation | Condition | Next State | Action / Side Effect | Invariant Owner |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Uninitialized** | `Game.Create(mode)` | Valid `GameMode` | `InProgress` | Allocates empty `Board`, sets `CurrentPlayer = X` | `Game` Factory |
| **InProgress** | `ExecuteTurn(P, idx)` | `P == CurrentPlayer`, cell free, lines < 3 | `InProgress` | Records move, creates Memento, toggles `CurrentPlayer` | `Game` & `Board` |
| **InProgress** | `ExecuteTurn(P, idx)` | `P == CurrentPlayer`, cell free, forms line | `Won` | `Winner = P`, sets `WinningCells`, locks game | `WinDetector` & `Game` |
| **InProgress** | `ExecuteTurn(P, idx)` | `P == CurrentPlayer`, cell free, 9th move | `Draw` | `Winner = null`, `WinningCells = null`, locks game | `WinDetector` & `Game` |
| **InProgress** | `ExecuteTurn(P, idx)` | `P != CurrentPlayer` | *No change* | Throws `InvalidTurnException` | `Game` |
| **InProgress** | `ExecuteTurn(P, idx)` | Cell already occupied | *No change* | Throws `CellOccupiedException` | `Board` |
| **InProgress** | `Undo()` | TwoPlayer mode, history >= 1 | `InProgress` | Pops 1 Memento, restores previous board & player | `Game` & `GameMemento` |
| **InProgress** | `Undo()` | Computer mode, history >= 2 | `InProgress` | Pops 2 Mementos (reverses O and X), returns to X | `Game` & `GameMemento` |
| **InProgress** | `Undo()` | History empty | *No change* | Throws `InvalidOperationException` | `Game` |
| **Won / Draw** | `ExecuteTurn(...)` | Game already completed | *No change* | Throws `GameAlreadyCompletedException` | `Game` |
| **Won / Draw** | `Undo()` | Game completed | *No change* | Throws `InvalidOperationException` (Option A) | `Game` |
| **Any State** | `Reset()` | Any active game | `InProgress` | Clears board, clears mementos, resets to `Player.X` | `Game` |

---

### 2.3 Application Layer (`TicTacToe.Application`)

- **`GameService` (`IGameService`)**:
  - Registered as a `Singleton` in DI.
  - **Concurrency Isolation**: Manages a thread-safe `ConcurrentDictionary<GameId, SemaphoreSlim>`. All operations on a specific `GameId` acquire its dedicated semaphore, guaranteeing atomic turn evaluation and serialized concurrent requests.
  - **Synchronous Scoreboard Coordination (P012)**:
    When a turn completes, `GameService` inspects the transition edge (`previousStatus == GameStatus.InProgress && (game.Status == Won || Draw)`). If terminal, it directly calls `Scoreboard.RecordWin` or `RecordDraw` and persists both aggregates synchronously.
  - **Atomic Computer Turn**: When `MakeMoveAsync` is executed in `Computer` mode, human Player X's move and the computer's deterministic AI response execute within a single domain call under the same lock, returning the updated state in a single HTTP response.
- **`ScoreboardService` (`IScoreboardService`)**:
  - Reads and resets session-level scoreboard metrics independently from active games.
- **Data Transfer Objects (DTOs)**:
  - `GameDto`: `{ id, mode, status, currentPlayer, winner, winningCells, board, moveHistory, canUndo }`
  - `CellDto`: `{ index, player }`
  - `MoveDto`: `{ moveNumber, player, cellIndex }`
  - `ScoreboardDto`: `{ playerXWins, playerOWins, draws }`

---

### 2.4 Infrastructure Layer (`TicTacToe.Infrastructure`)

- **`InMemoryGameRepository` (`IGameRepository`)**:
  - Thread-safe storage utilizing `ConcurrentDictionary<GameId, Game>`.
  - Operations: `GetByIdAsync`, `SaveAsync`.
- **`InMemoryScoreboardRepository` (`IScoreboardRepository`)**:
  - Thread-safe singleton repository holding the application-wide authoritative `Scoreboard` instance.
  - Operations: `GetAsync`, `SaveAsync`.

---

### 2.5 API / Web Host (`TicTacToe.Api`)

#### Endpoints

| HTTP Method | Route | Description | Request Body | Success Code | Error Codes |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/games` | Initializes a new game session | `CreateGameRequestDto` (`{ "mode": "TwoPlayer" \| "Computer" }`) | `201 Created` (`Location: /api/games/{id}`) | `400` |
| `GET` | `/api/games/{id}` | Retrieves authoritative game state | *None* | `200 OK` (`GameDto`) | `404` |
| `POST` | `/api/games/{id}/moves` | Submits a move | `MakeMoveRequestDto` (`{ "player": "X", "cellIndex": 4 }`) | `200 OK` (`GameDto`) | `400, 404, 409` |
| `POST` | `/api/games/{id}/undo` | Reverts previous move(s) | *None* | `200 OK` (`GameDto`) | `400, 404` |
| `POST` | `/api/games/{id}/reset` | Clears board/history, preserves `gameId` | *None* | `200 OK` (`GameDto`) | `404` |
| `GET` | `/api/scoreboard` | Retrieves session scoreboard metrics | *None* | `200 OK` (`ScoreboardDto`) | `500` |
| `POST` | `/api/scoreboard/reset`| Resets scoreboard counts to zero | *None* | `200 OK` (`ScoreboardDto`) | `500` |
| `GET` | `/health` | System health check | *None* | `200 OK` (`"Healthy"`) | `503` |

#### Middleware & Policies (`ExceptionHandlingMiddleware`)
- Translates domain exceptions to RFC 7807 `ProblemDetails`:
  - `InvalidCellIndexException` -> `400 INVALID_CELL_INDEX`
  - `InvalidTurnException` -> `400 INVALID_TURN`
  - `GameAlreadyCompletedException` -> `400 GAME_ALREADY_COMPLETED`
  - `InvalidOperationException` (Undo) -> `400 INVALID_UNDO`
  - `KeyNotFoundException` -> `404 NOT_FOUND`
  - `CellOccupiedException` -> `409 CELL_OCCUPIED`
  - Unhandled exceptions -> `500 INTERNAL_SERVER_ERROR`
- Rejects unmapped JSON properties (`JsonUnmappedMemberHandling.Disallow`).
- Propagates and logs `X-Correlation-Id` as `traceId`.
- String enum serialization via `JsonStringEnumConverter` in `Program.cs`.
- Configured CORS: `http://localhost:4200` with exposed `Location` header.

---

## 3. Frontend Implementation (Angular 21)

The frontend is a standalone Angular single-page application located in `frontend/`.

### 3.1 Reactive State Coordination (`GameFacade`)

State is coordinated through an enterprise reactive facade (`frontend/src/app/services/game.facade.ts`):
- **Signals**:
  - Core signals: `gameState()`, `scoreboard()`, `loading()`, `error()`
  - Computed signals: `isGameOver()`, `canUndo()`, `currentPlayer()`, `status()`, `winner()`, `winningCells()`
- **Guards & Concurrency**:
  - In-flight mutation guarding prevents duplicate requests:
    ```typescript
    if (this._loading()) return;
    ```
  - RxJS `finalize(() => this._loading.set(false))` guarantees reset on both success and error.
- **ProblemDetails Fidelity**:
  - Surfaces structured RFC 7807 error messages (`code`, `title`, `detail`, `traceId`) to user-facing alerts.

---

### 3.2 UI Components

```text
frontend/src/app/components/
├── header/              -> Mode indicator and application title
├── status-banner/       -> aria-live polite region for turn & victory announcements
├── game-board/          -> 3x3 semantic accessible button grid with win highlights
├── game-controls/       -> Mode selector, Undo, Reset Board, Reset Scores toolbar
├── scoreboard/          -> X Wins, Draws, and O/Computer Wins live metrics
├── move-history/        -> Chronological move audit log (with derived Row/Col)
└── error-alert/         -> Dismissible RFC 7807 error banner with correlation trace
```

#### 1. Header (`app-header`)
- Displays title, subtitle, and active `GameMode` badge.

#### 2. Status Banner (`app-status-banner`)
- Configured with `role="status"`, `aria-live="polite"`, and `aria-atomic="true"`.
- Announces turn progression:
  - In Progress: `Player X's turn` or `Player O's turn`.
  - Won: `Player X Wins!` or `Player O Wins!`.
  - Draw: `Game Over - It's a Draw!`.

#### 3. Game Board (`app-game-board`)
- **Semantic Structure**: 9 native `<button>` elements with `type="button"` and `role="gridcell"`.
- **Dynamic Accessible Names**: ARIA labels report `Cell {N}, empty` or `Cell {N}, Player {X|O}` plus winning indicator.
- **Interaction Guarding**: Native `[disabled]` attribute bound when cell is occupied, when game is over, or when a request is in-flight.
- **Keyboard Accessible**: Arrow key navigation (`ArrowUp`, `ArrowDown`, `ArrowLeft`, `ArrowRight`, `Home`, `End`). High-contrast focus rings (`#6366f1`).

#### 4. Game Controls (`app-game-controls`)
- **Mode Dropdown**: Switches between `TwoPlayer` and `Computer` modes.
- **Undo Move**: Bound to native `[disabled]="!canUndo()"` (Option A enforcement).
- **Reset Game**: Calls `POST /api/games/{id}/reset` (clears board, retains `gameId` and scoreboard).
- **Reset Scores**: Calls `POST /api/scoreboard/reset` (zeroes scores independently).

#### 5. Scoreboard (`app-scoreboard`)
- Cards displaying Player X Wins, Player O Wins, and Draws.
- Strictly bound to server `ScoreboardDto`; zero client-side arithmetic.

#### 6. Move History (`app-move-history`)
- Chronological list of completed moves. Derives 1-based coordinates:
  ```typescript
  Row = Math.floor(cellIndex / 3) + 1
  Col = (cellIndex % 3) + 1
  ```

#### 7. Error Alert (`app-error-alert`)
- Dismissible notification banner that activates if an API request fails with `role="alert"`.

---

### 3.3 Design System & Aesthetics

Defined in `frontend/src/styles.css`:
- **Theme**: Dark-mode glassmorphism (`--bg-dark: #0f172a`, `--card-bg: rgba(30, 41, 59, 0.8)`).
- **Typography**: Google Fonts `Outfit` (display/headings) and `Inter` (body/data).
- **Neon Accent Colors**:
  - Cyan (`#38bdf8`) for Player X.
  - Rose (`#f43f5e`) for Player O.
  - Emerald (`#10b981`) for Winning lines.
- **Responsive Layout**: Fluid CSS grid adapting from mobile to desktop viewports.

---

## 4. Test Suite Summary

The system is guarded by **238 automated tests** (195 Backend + 43 Frontend) with a **100% pass rate**:

| Component | Test Framework | Test Files | Tests | Result |
|---|---|:---:|:---:|:---:|
| **Backend Unit & Integration** | xUnit, WebApplicationFactory | 25 fixtures | **195** | **PASS (100%)** |
| **Frontend Components & Facade** | Vitest, Angular Testbed | 9 spec files | **43** | **PASS (100%)** |
| **Total Automated Tests** | — | **34 files** | **238** | **PASS (100%)** |

### Verified Scenarios
- **Domain rules**: Win detection across all 8 lines, draw conditions, turn sequencing, out-of-turn play rejection, cell occupancy validation.
- **AI strategy**: Immediate win execution, opponent blocking, center control, corner and remaining cell priority.
- **Memento undo**: Single-move reversal (Two-Player), pair reversal (Computer), Option A terminal state locking.
- **Reset mechanics**: Game reset preserving `GameId`, scoreboard reset independence.
- **API validation**: RFC 7807 ProblemDetails mapping, camelCase serialization, string enum conversion, trace correlation.
- **Concurrency**: Per-game `SemaphoreSlim` serialization, concurrent games execution, thread-safe scoreboard increments.
- **Accessibility**: 9 semantic buttons, dynamic ARIA labels, focus-visible rings, keyboard arrow navigation, live region announcements.

---

## 5. Running the Application Locally

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js (v20+) & npm](https://nodejs.org/)

### Step 1: Start the Backend REST API
From the repository root:
```bash
dotnet run --project backend/TicTacToe.Api --launch-profile http
```
The API starts listening on:
- Base URL: `http://localhost:5000`
- Health check: `http://localhost:5000/health`
- Swagger documentation: `http://localhost:5000/swagger`

### Step 2: Start the Angular Frontend
In a separate terminal, from the repository root:
```bash
npm start --prefix frontend
```
The Angular application starts listening on:
- URL: `http://localhost:4200` (bound to `0.0.0.0` for local access).

### Step 3: Run Automated Test Suites
- **Backend Tests**:
  ```bash
  dotnet test backend/TicTacToe.sln
  ```
- **Frontend Tests**:
  ```bash
  npm test --prefix frontend -- --watch=false
  ```
- **Production Builds**:
  ```bash
  dotnet build backend/TicTacToe.sln --warnaserror
  npm run build --prefix frontend
  ```

---

## 6. Architecture Evolution & Governance (P001 – P012)

The implementation followed a rigorous 12-phase AI governance protocol:
- **P001 – P004**: Repository bootstrap, solution scaffolding, Onion architecture boundaries, and DDD foundations.
- **P005 – P007**: Game rules, win/draw detection, Memento pattern undo (Option A), and 5-tier AI strategy.
- **P008**: Initial Scoreboard aggregate tracking.
- **P009 – P010**: Application layer orchestration, per-game concurrency semaphores, REST API, and RFC 7807 error handling.
- **P011**: Angular 21 presentation layer, Signals-based `GameFacade`, and WCAG 2.1 AA accessibility.
- **P012 (Architecture Simplification)**: Eliminated speculative in-process domain event infrastructure (`IDomainEvent`, `DomainEventDispatcher`, `GameCompletedEvent`, `GameCompletedEventHandler`) in favor of direct, deterministic synchronous coordination in `GameService`. Resulted in a ~40% reduction in write-path indirection while preserving 100% of functional requirements and test coverage.
