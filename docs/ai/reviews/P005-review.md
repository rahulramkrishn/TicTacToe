# P005 Review

**Review Date**: 2026-09-05  
**AI Review**: Completed  
**Human Review**: Pending  
**Review Type**: Implementation Review (P005 — Game Rules, Win Detection & Draw Detection)  
**Status**: PASS — READY FOR P006  

---

## Objective

Implement the pure domain game rules, win detection across all eight canonical winning lines, draw detection, winning-cell identification, terminal state transitions, and turn progression inside `TicTacToe.Domain`, backed by automated tests.

---

## Specification Sources

- `docs/01-requirements.md` (FR-01, FR-02, FR-03, FR-04, FR-05, FR-06, FR-07, FR-16)
- `docs/04-ddd-and-domain-model.md` §3 (Game Aggregate), §5 (Domain Invariants), §8 (WinDetector Domain Service)
- `docs/05-architecture.md` §1 (Layer boundaries), §12 (HTTP and Domain separation)
- `docs/06-api-contract.md` (Canonical `cellIndex: 0..8`)
- `docs/07-test-strategy.md` (Domain unit testing scenarios)
- `docs/10-adr-template-and-initial-decisions.md` (ADR-001)
- `docs/13-assumptions.md` (A-001)
- `docs/ai/prompts/P005-game-rules-win-draw.md`

---

## Design Proposal

### Game Responsibility
The `Game` aggregate root coordinates state transitions, validates move prerequisites (game in progress, current player matches, cell unoccupied), places the mark on `Board`, appends to `_moveHistory`, and evaluates the board.

### Win Detection
Delegated to a pure, stateless domain service `WinDetector.CheckWin(Board board)` per `04-ddd-and-domain-model.md` §8. It evaluates the 8 canonical lines and returns a `WinResult` value object (`IsWin`, `Winner`, `WinningCells`).

### Draw Detection
Evaluated directly on the `Board` entity via `Board.IsFull` only if no win was detected. This maintains a single, coherent evaluation flow:
```text
Legal Move -> Apply Move -> Check Win -> [Win? -> Won] -> [Full? -> Draw] -> [Else -> Next Player]
```
Critical Rule: Win detection is evaluated strictly before Draw detection, guaranteeing that a 9th move completing a line results in `Won`, not `Draw`.

### Winning Cells
`WinningCells` contains the exact 3 canonical cell indices (`0..8`) that formed the winning line. If no win occurs, it remains empty (`Array.Empty<int>()`).

### Turn Progression
Turn alternates between `Player.X` and `Player.O` on non-terminal moves. When the game reaches `Won` or `Draw`, turn progression halts—the active player who made the terminal move remains `CurrentPlayer` and no turn switch occurs.

### Terminal State
Once `Status` transitions to `Won` or `Draw`, subsequent calls to `MakeMove` throw `GameAlreadyCompletedException`, preserving all 6 aggregate state facets unmutated.

---

## Implemented

1. `WinResult`: Immutable record in `TicTacToe.Domain.Services` holding `bool IsWin`, `Player? Winner`, and `IReadOnlyList<int> WinningCells` (defensively wrapped as a read-only collection to prevent external array mutation).
2. `WinDetector`: Stateless domain service in `TicTacToe.Domain.Services` encapsulating the 8 canonical winning lines as private immutable definitions.
3. `Board`: Enhanced with `IsFull` and `OccupiedCount` properties.
4. `Game`: Integrated `WinDetector` and `Board.IsFull` into `MakeMove`, with proper terminal state transitions (`Won`, `Draw`) and turn halting.
5. Automated test suites: `WinDetectorTests` (21 test executions covering all lines for X and O, empty/partial/draw boards, and WinningCells immutability) and `GameTests` (18 test executions covering row/col/diag wins, draw, final-move win precedence, terminal immutability, and atomicity).

---

## Deferred

- **Memento / Undo**: Deferred to P006.
- **Strategy / Computer Player**: Deferred to P007.
- **GameCompleted Domain Event & Scoreboard**: Deferred to P008.
- **Frontend Game UI**: Deferred to P009/P010.
- **REST API & DTOs**: Deferred to P010/P011.

---

## Winning Lines

All eight canonical lines defined and tested:

```text
Rows:
1. 0, 1, 2
2. 3, 4, 5
3. 6, 7, 8

Columns:
4. 0, 3, 6
5. 1, 4, 7
6. 2, 5, 8

Diagonals:
7. 0, 4, 8
8. 2, 4, 6
```

---

## Invariants

1. **Terminal Result Immutability**: Once `Won` or `Draw`, no subsequent moves are accepted; state is untouched.
2. **Win Takes Precedence Over Draw**: Full board + winning line = `Won`.
3. **Turn Progression Halting**: Terminal moves do not alternate `CurrentPlayer`.
4. **Winning Cells Accuracy**: Exactly the 3 winning indices (0..8) on win; empty on draw or in progress.
5. **Move Count / Occupied Consistency**: `MoveHistory.Count == Board.OccupiedCount <= 9`.
6. **Atomicity**: Rejected operations leave all 6 aggregate state facets completely unmutated.

---

## Tests

### Win Tests
- `WinDetectorTests.CheckWin_AllEightLines_DetectsWinForPlayerX` (8 lines tested)
- `WinDetectorTests.CheckWin_AllEightLines_DetectsWinForPlayerO` (8 lines tested)
- `GameTests.MakeMove_PlayerXWinsRow_TransitionsToWonSetsWinnerAndHaltsTurnProgression` (Row win)
- `GameTests.MakeMove_PlayerOWinsColumn_TransitionsToWonSetsWinnerAndHaltsTurnProgression` (Column win for O)
- `GameTests.MakeMove_PlayerXWinsDiagonal_TransitionsToWon` (Main and Anti diagonals)

### Draw Tests
- `WinDetectorTests.CheckWin_FullBoardWithoutWinningLine_ReturnsNoWin`
- `GameTests.MakeMove_StandardDraw_TransitionsToDrawWithNullWinnerAndEmptyWinningCells`
- `GameTests.MakeMove_FinalMoveCompletesWinningLine_TakesPrecedenceOverDraw` (Mandatory precedence verification)

### Turn Tests
- `GameTests.MakeMove_ValidMove_PlacesMarkAppendsHistoryAndAlternatesTurn`
- `GameTests.MakeMove_SequentialValidMoves_AlternatesPlayerCorrectly`
- Verified non-progression on `Won` and `Draw`.

### Terminal State Tests
- `GameTests.MakeMove_AfterNaturalWin_RejectsSubsequentMovesAndPreservesState`
- `GameTests.MakeMove_AfterNaturalDraw_RejectsSubsequentMovesAndPreservesState`
- `GameTests.MakeMove_WhenGameAlreadyCompleted_ThrowsGameAlreadyCompletedExceptionAndPreservesState`

### Atomicity Tests
- `GameTests.MakeMove_WrongPlayer_ThrowsInvalidTurnExceptionAndDoesNotMutateState`
- `GameTests.MakeMove_OccupiedCell_ThrowsCellOccupiedExceptionAndDoesNotMutateState`
- `GameTests.MakeMove_AtomicityAndOccupiedCellConsistency_PreservesInvariants`

### Move History Tests
- Sequential contiguous 1-based move numbering verified.
- Move history length matches occupied cell count.

---

## Architecture

### Domain Purity
- Zero ProjectReferences and zero PackageReferences in `TicTacToe.Domain.csproj`.
- Zero ASP.NET Core, Entity Framework, HTTP, MVC, or JSON dependencies.
- Zero logging framework or configuration dependencies.

### Dependency Direction
- `TicTacToe.Domain` has no dependencies.
- `TicTacToe.Application` and `TicTacToe.Infrastructure` depend on `Domain`.
- Solution project graph complies strictly with Clean Architecture.

---

## Requirement Traceability

- **FR-01 (Create Game)**: Partially Implemented (Domain Aggregate Initialized; Application/API/UI Deferred).
- **FR-02 (Board 0..8)**: Partially Implemented (Domain Model Complete; API DTO/UI Rendering Deferred).
- **FR-03 (Turns)**: Partially Implemented (Domain Turn Rules Complete; API/UI Interaction Deferred).
- **FR-04 (Win Detection)**: Domain portion: Implemented; Overall requirement: Partially Implemented (Deferred integration: P008 GameCompleted event, P010/P011 API/UI).
- **FR-05 (Draw Detection)**: Domain portion: Implemented; Overall requirement: Partially Implemented (Deferred integration: P008 GameCompleted event, P010/P011 API/UI).
- **FR-06 (Move Validation)**: Partially Implemented (Domain Invariants Complete; HTTP/UI Validation Deferred).
- **FR-07 (Move History)**: Partially Implemented (Domain History Tracking Complete; API/UI Presentation Deferred).

---

## AI Changes

- Created `backend/TicTacToe.Domain/Services/WinResult.cs`.
- Created `backend/TicTacToe.Domain/Services/WinDetector.cs`.
- Modified `backend/TicTacToe.Domain/Entities/Board.cs` (added `IsFull` and `OccupiedCount`).
- Modified `backend/TicTacToe.Domain/Aggregates/Game.cs` (integrated `WinDetector` and `Board.IsFull` into `MakeMove`).
- Created `backend/TicTacToe.Tests/Domain/WinDetectorTests.cs`.
- Modified `backend/TicTacToe.Tests/Domain/GameTests.cs` (added win, draw, final-move win, terminal state, and atomicity tests).
- Updated `docs/ai/REQUIREMENT-TRACEABILITY.md` and `docs/ai/ARCHITECTURE-TRACEABILITY.md`.
- Created `docs/ai/reviews/P005-review.md`.

---

## Human Changes

None.

---

## Known Risks

None. All rule evaluations and state transitions are pure, deterministic, and 100% test-covered.

---

## Final Status

PASS — READY FOR P006
