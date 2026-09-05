# Tic-Tac-Toe — Complete System Implementation Guide (Backend & Frontend)

This document provides a comprehensive technical reference for the enterprise **Tic-Tac-Toe** system, detailing both the **.NET 8 Backend** (Domain-Driven Design, Onion Architecture, REST API) and the **Angular 21 Frontend** (Standalone Components, Signals, Reactive Facade, WCAG Accessibility).

---

## 1. System Architecture Overview

The system is architected as an **Onion / Hexagonal (Ports & Adapters)** monolith. Business logic, game rules, winning logic, AI strategy, and scoreboard metrics are strictly isolated within the Domain and Application layers. The REST API and the Angular SPA serve purely as external adapters.

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
│   (IGameService, ScoreboardService, Per-Game Concurrency Locks)   │
└─────────────────────────────────┬─────────────────────────────────┘
                                  │
                                  ▼
┌───────────────────────────────────────────────────────────────────┐
│                 TicTacToe.Domain (Core Enterprise)                │
│   (Game Aggregate, WinDetector, Computer Strategy, Memento Undo)  │
└─────────────────────────────────▲─────────────────────────────────┘
                                  │
┌─────────────────────────────────┴─────────────────────────────────┐
│             TicTacToe.Infrastructure (Persistence/Events)          │
│   (InMemoryGameRepository, InMemoryScoreboard, DomainEventBus)    │
└───────────────────────────────────────────────────────────────────┘
```

### Architectural Boundaries & Invariants
1. **Domain Isolation**: `TicTacToe.Domain` has zero outer dependencies (no HTTP, no EF, no ASP.NET Core).
2. **Backend Authority**: Win detection, draw detection, turn alternations, computer moves, undo snapshots, and scoreboard computations are 100% owned by the backend.
3. **Canonical Cell Addressing**: All layer communications use `cellIndex` (`0..8`):
   ```text
   0 | 1 | 2
   ---------
   3 | 4 | 5
   ---------
   6 | 7 | 8
   ```
4. **RFC 7807 Error Standard**: All API errors adhere to ProblemDetails with standardized type URIs, machine-readable codes, and correlation trace IDs.
5. **Dumb UI Adapter**: The Angular application never calculates win states, scores, or AI moves client-side.

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
- **`Player`**: Enum `X` and `O`.
- **`GameMode`**: Enum `TwoPlayer` and `Computer`.
- **`GameStatus`**: Enum `InProgress`, `Won`, and `Draw`.
- **`Move`**: Immutable record `(int MoveNumber, Player Player, CellIndex CellIndex)`.
- **`WinningLine`**: Identifies indices `(int Index1, int Index2, int Index3)` representing the winning combination.

#### Aggregates & Entities
- **`Game` (Aggregate Root)**:
  - Manages `GameId`, `Board`, `CurrentPlayer`, `Mode`, `Status`, `Winner`, `WinningLine`, and `MoveHistory`.
  - Maintains private undo memento stack (`Stack<GameMemento>`).
  - **`MakeMove(Player player, CellIndex cell)`**:
    - Validates status is `InProgress` (`GameAlreadyCompletedException`).
    - Validates `player == CurrentPlayer` (`InvalidTurnException`).
    - Validates cell is empty on `Board` (`CellOccupiedException`).
    - Saves `GameMemento` prior to mutation.
    - Evaluates win or draw via `WinDetector`.
    - If game terminates, transitions status, sets winner/winningLine, and raises `GameCompletedEvent`.
    - Otherwise, alternates `CurrentPlayer`.
  - **`Undo()`**:
    - Enforces Option A rules: Can only undo during `InProgress` status with at least 1 move. Disallowed after game completion (`CannotUndoException`).
    - In `TwoPlayer` mode: pops 1 memento, restoring board, history, and previous player turn.
    - In `Computer` mode: pops both computer and human moves (2 moves), restoring human Player X's turn.
  - **`Reset()`**:
    - Clears board, history, status (`InProgress`), winner (`null`), and currentPlayer (`X`).
    - Strictly **preserves the existing `GameId`** and drains any pending domain events.
- **`Board` Entity**:
  - Encapsulates an array of 9 nullable `Player?` marks.
  - Exposes indexed access `this[CellIndex index]`.
- **`Scoreboard` (Session Aggregate)**:
  - Tracks `XWins`, `OWins`, `Draws`, and `TotalGames`.
  - Incremented exclusively through `RecordWin(Player winner)` and `RecordDraw()`.
  - Can be reset to zeros via `Reset()` without impacting active game sessions.

#### Domain Services & Patterns
- **`WinDetector`**:
  - Evaluates the 8 canonical winning lines (3 horizontal, 3 vertical, 2 diagonal):
    - Rows: `(0,1,2)`, `(3,4,5)`, `(6,7,8)`
    - Columns: `(0,3,6)`, `(1,4,7)`, `(2,5,8)`
    - Diagonals: `(0,4,8)`, `(2,4,6)`
  - Returns `WinResult` indicating whether a line matched and its indices.
- **`IComputerMoveStrategy` & `BasicComputerMoveStrategy`**:
  - Implements a deterministic, rule-based AI strategy for Player O:
    1. **Immediate Win**: Check if Player O can win in one move; if so, play it.
    2. **Block Opponent**: Check if Player X can win on next turn; if so, block that cell.
    3. **Center Cell**: Claim cell 4 if open.
    4. **Opposite / Any Corner**: Claim cell 0, 2, 6, or 8.
    5. **Remaining Open Cells**: First available open cell.
- **`GameMemento`**:
  - Deep-clone snapshot of aggregate state (`Board`, `CurrentPlayer`, `Status`, `MoveHistory`) enabling rollbacks.
- **`GameCompletedEvent`**:
  - Raised when a game transitions to `Won` or `Draw`. Carries `GameId`, `Winner`, and `IsDraw`.

---

### 2.2 Application Layer (`TicTacToe.Application`)

- **`IGameService` / `GameService`**:
  - Orchestrates operations across `Game` aggregates and `IGameRepository`.
  - **Concurrency Isolation**: Manages a thread-safe `ConcurrentDictionary<GameId, SemaphoreSlim>`. All mutations on a specific `GameId` acquire its dedicated semaphore, guaranteeing atomic turn evaluation and serialized concurrent requests.
  - **Atomic Computer Turn**: When `MakeMoveAsync` is called on a Computer-mode game, Player X's move is applied, followed immediately by the AI move evaluation and application under the same lock before returning the resulting `GameStateDto`.
- **`IScoreboardService` / `ScoreboardService`**:
  - Reads and resets session-level scoreboard metrics.
- **`GameCompletedEventHandler`**:
  - Subscribes to `GameCompletedEvent`. Dispatched synchronously to increment `Scoreboard` counts.
- **Data Transfer Objects (DTOs)**:
  - `GameStateDto`: `{ gameId, board, currentPlayer, mode, status, winner, winningCells, moveHistory, canUndo, scoreboard }`
  - `MoveDto`: `{ moveNumber, player, cellIndex }`
  - `ScoreboardDto`: `{ xWins, oWins, draws, totalGames }`

---

### 2.3 Infrastructure Layer (`TicTacToe.Infrastructure`)

- **`InMemoryGameRepository`**: Thread-safe in-memory store utilizing `ConcurrentDictionary<GameId, Game>`.
- **`InMemoryScoreboardRepository`**: Thread-safe singleton repository tracking session scores.
- **`DomainEventDispatcher`**: Dispatches domain events to registered handlers (`GameCompletedEventHandler`).

---

### 2.4 API / Web Host (`TicTacToe.Api`)

#### Endpoints
| HTTP Method | Route | Description | Success Code |
|---|---|---|---|
| `POST` | `/api/games` | Initializes a new game session with mode | `201 Created` (`Location: /api/games/{id}`) |
| `GET` | `/api/games/{id}` | Retrieves authoritative game state | `200 OK` |
| `POST` | `/api/games/{id}/moves` | Submits a move `{ player, cellIndex }` | `200 OK` |
| `POST` | `/api/games/{id}/undo` | Reverts previous move(s) | `200 OK` |
| `POST` | `/api/games/{id}/reset` | Clears board/history, preserves `gameId` | `200 OK` |
| `GET` | `/api/scoreboard` | Retrieves session scoreboard metrics | `200 OK` |
| `POST` | `/api/scoreboard/reset`| Resets scoreboard counts to zero | `200 OK` |
| `GET` | `/health` | System health check | `200 OK` (`"Healthy"`) |

#### Middleware & Policies (`ExceptionHandlingMiddleware`)
- Translates domain exceptions to RFC 7807 `ProblemDetails`:
  - `InvalidCellIndexException` -> `400 INVALID_CELL_INDEX`
  - `JsonException` / Unknown properties -> `400 MALFORMED_REQUEST`
  - `GameNotFoundException` -> `404 GAME_NOT_FOUND`
  - `CellOccupiedException` -> `409 CELL_OCCUPIED`
  - `InvalidTurnException` -> `409 INVALID_TURN`
  - `GameAlreadyCompletedException` -> `409 GAME_ALREADY_COMPLETED`
  - `CannotUndoException` -> `409 CANNOT_UNDO`
  - Unhandled -> `500 INTERNAL_SERVER_ERROR`
- Rejects unmapped JSON properties (`JsonUnmappedMemberHandling.Disallow`).
- Propagates and logs `X-Correlation-Id` as `traceId`.
- Configured CORS: `http://localhost:4200`.

---

## 3. Frontend Implementation (Angular 21)

The frontend is a standalone Angular single-page application located in `frontend/`.

### 3.1 Reactive State Coordination (`GameFacade`)

State is coordinated through a single reactive facade (`frontend/src/app/services/game.facade.ts`):
- **Signals**:
  - State signals: `gameState()`, `scoreboard()`, `loading()`, `error()`
  - Computed signals: `gameId()`, `board()`, `currentPlayer()`, `mode()`, `status()`, `winner()`, `winningCells()`, `moveHistory()`, `canUndo()`, `isGameOver()`, `isInitialized()`
- **Guards & Concurrency**:
  - All mutating actions (`initGame`, `newGame`, `playMove`, `undo`, `resetGame`, `resetScoreboard`) enforce in-flight guarding:
    ```typescript
    if (this._loading()) return;
    ```
  - RxJS `finalize(() => this._loading.set(false))` guarantees that loading is reset on both success and error.
- **ProblemDetails Fidelity**:
  - Retains all 7 RFC 7807 properties (`type`, `title`, `status`, `code`, `detail`, `instance`, `traceId`) to surface informative error dialogs.

---

### 3.2 UI Components

```text
frontend/src/app/components/
├── header/              -> Mode selector toggle & application title
├── status-banner/       -> aria-live polite region for turn & victory announcements
├── game-board/          -> 3x3 semantic button grid with pulsating win lines
├── game-controls/       -> Undo, Reset Board, and New Game action toolbar
├── scoreboard/          -> X Wins, Draws, and O/Computer Wins metrics & Reset
├── move-history/        -> Chronological move audit log (with derived Row/Col)
└── error-alert/         -> Dismissible RFC 7807 error banner with retry trigger
```

#### 1. Header (`app-header`)
- Allows toggling between `TwoPlayer` and `vs Computer` modes.
- Mode change triggers `facade.newGame(selectedMode)`.

#### 2. Status Banner (`app-status-banner`)
- Configured with `role="status"` and `aria-live="polite"`.
- Displays authoritative state:
  - In Progress: `CURRENT TURN: Player X` or `Player O`.
  - Won: `🏆 VICTORY! Player {X|O} Wins!`.
  - Draw: `🤝 STALEMATE: Game ended in a Draw`.
- Zero artificial client delays or misleading "Thinking..." status.

#### 3. Game Board (`app-game-board`)
- **Semantic Structure**: 9 native `<button>` elements with `type="button"` and `role="gridcell"`.
- **Dynamic Accessible Names**: ARIA labels report `Cell {N}, row {R} column {C}, empty` or `occupied by Player {X|O}`.
- **Interaction Guarding**: Native `[disabled]` attribute bound when cell is occupied, when game is over, or when a request is in-flight.
- **Visual Feedback**:
  - Player X rendered with radiant cyan neon (`#38bdf8`).
  - Player O rendered with glowing rose neon (`#f43f5e`).
  - Winning cells pulse with an emerald glow (`#10b981`).
- **Keyboard Accessible**: Focus-visible ring with 2px cyan border and 3px offset.

#### 4. Game Controls (`app-game-controls`)
- **Undo Move**: Bound to native `[disabled]="!facade.canUndo()"`.
- **Reset Board**: Calls `POST /api/games/{id}/reset` (clears board, retains `gameId` and scoreboard).
- **New Game**: Calls `POST /api/games` (allocates brand new `gameId`).

#### 5. Scoreboard (`app-scoreboard`)
- Responsive metric cards displaying Player X Wins, Draws, and Player O/Computer Wins.
- Score values are hydrated strictly from server responses; zero client-side arithmetic.
- Independent **Reset Scores** button calling `POST /api/scoreboard/reset`.

#### 6. Move History (`app-move-history`)
- Chronological list of completed moves.
- Derives 1-based Row and Column coordinates from canonical `cellIndex`:
  ```typescript
  Row = Math.floor(cellIndex / 3) + 1
  Col = (cellIndex % 3) + 1
  ```

#### 7. Error Alert (`app-error-alert`)
- Dismissible notification banner that activates if an API request fails.
- Displays `title`, `code`, `detail`, and `traceId`, with a "Retry Connection" action if initialization failed.

---

### 3.3 Design System & Aesthetics

Defined in `frontend/src/styles.css`:
- **Theme**: Dark-mode glassmorphism (`--bg-main: #0b0f19`, `--surface-card: rgba(30, 41, 59, 0.7)`).
- **Typography**: Google Fonts `Outfit` (display/headings) and `Inter` (body/data).
- **Neon Accent Colors**:
  - Cyan (`#38bdf8`) for Player X.
  - Rose (`#f43f5e`) for Player O.
  - Emerald (`#10b981`) for Winning lines.
- **Responsive Layout**: Fluid CSS grid adjusting between desktop, tablet, and mobile viewports without horizontal scrolling.

---

## 4. Test Suite Summary

The system is guarded by a comprehensive, automated test suite across backend and frontend:

| Component | Test Framework | Test Files | Tests | Result |
|---|---|---|---|---|
| **Backend Unit & Integration** | xUnit, WebApplicationFactory | 11 fixtures | **233** | **PASS (100%)** |
| **Frontend Components & Facade** | Vitest, Angular Testbed | 9 spec files | **43** | **PASS (100%)** |
| **Total Automated Tests** | — | **20 files** | **276** | **PASS (100%)** |

### Verified Scenarios
- Domain rules: Win detection (all 8 lines), draw conditions, turn sequencing, illegal moves.
- AI strategy: Center control, win execution, opponent blocking, corner selection.
- Memento undo: Single-move reversal (Two-Player), pair reversal (Computer), terminal state locking.
- Reset mechanics: Game reset preserving `GameId`, scoreboard reset independence.
- API validation: Unknown JSON property rejection, malformed inputs, out-of-bounds indices, trace correlation.
- Concurrency: Thread-safe parallel requests per game under semaphore locks.
- Accessibility: 9 semantic buttons, dynamic ARIA labels, native disabled states, live region announcements.

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
- URL: `http://localhost:4200` (bound to `0.0.0.0` for dual IPv4/IPv6 support).

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
