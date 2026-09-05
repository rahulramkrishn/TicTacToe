# P007 Review — Computer Strategy Pattern (Specification, Design & Implementation)

**Review Date**: 2026-09-05  
**AI Review**: Completed  
**Human Review**: Approved  
**Review Type**: Implementation Review (P007 — Computer Strategy Pattern & Computer Mode Orchestration)  
**Status**: P007 PASS  

---

## 1. Objective

Implement the automated Computer Player (playing as **Player O**) using the **Strategy Pattern** (`GoF`) in `TicTacToe.Domain.Services`, execute candidate evaluations purely using detached cloned boards without mutating aggregate state, reuse `WinDetector.CheckWin` as the single source of truth, and complete the **Computer Mode turn orchestration** on the `Game` aggregate root preserving the P006 Memento snapshot boundary.

---

## 2. Specification Sources

- `docs/01-requirements.md` (FR-10, FR-14)
- `docs/04-ddd-and-domain-model.md` §7 (Strategy Pattern — Computer Player)
- `docs/05-architecture.md` §3, §7
- `docs/10-adr-template-and-initial-decisions.md` (ADR-004, ADR-005)
- `docs/13-assumptions.md` (A-005, A-006)
- `docs/ai/prompts/P007-computer-strategy-pattern-specification,design-and-implementation.md`

---

## 3. Strategy Pattern Architecture & Roles

| Pattern Role | Concrete Type | Location | Responsibilities |
|---|---|---|---|
| **Strategy Interface** | `IComputerMoveStrategy` | `backend/TicTacToe.Domain/Services/` | Declares `CellIndex SelectMove(Board board)`. Strictly pure and deterministic contract. |
| **Concrete Strategy** | `BasicComputerMoveStrategy` | `backend/TicTacToe.Domain/Services/` | Encapsulates the 5-tier priority hierarchy specifically for Player O. Completely stateless with zero mutation side effects. |
| **Context / Aggregate Root** | `Game` | `backend/TicTacToe.Domain/Aggregates/` | Owns game state; accepts `IComputerMoveStrategy` via `PlayComputerMove` and `ExecuteTurn` without retaining strategy as aggregate field state. |

---

## 4. Deterministic Priority Algorithm

`BasicComputerMoveStrategy` evaluates candidate moves in the exact frozen priority order:

1. **Priority 1 — Immediate Win for Player O**:
   - Iterates $c \in \{0..8\}$; simulates Player O on `board.Clone()`.
   - Calls `WinDetector.CheckWin(candidateBoard)`.
   - Returns lowest $c$ if winning line detected.
2. **Priority 2 — Immediate Block for Player X**:
   - Iterates $c \in \{0..8\}$; simulates Player X on `board.Clone()`.
   - Calls `WinDetector.CheckWin(candidateBoard)`.
   - Returns lowest $c$ if threat detected.
   - *Multiple Threats Disclaimer*: If multiple immediate X-winning cells exist, the deterministic strategy selects the lowest-index threatened cell. The strategy does not claim to solve unavoidable fork positions where one block cannot prevent another winning threat.
   - *Precedence*: An immediate winning move for O takes absolute priority over blocking X.
3. **Priority 3 — Center Cell (4)**:
   - Returns `CellIndex(4)` if vacant.
4. **Priority 4 — Corners in Fixed Order `[0, 2, 6, 8]`**:
   - Evaluates corners in deterministic order `[0, 2, 6, 8]`; returns the first vacant corner.
5. **Priority 5 — Any Available Cell in Natural Order `0..8`**:
   - Evaluates all cells in natural order $0..8$; returns the first vacant cell.
6. **No Legal Moves**:
   - Throws `InvalidOperationException` if all 9 cells are occupied.

---

## 5. Candidate Evaluation & Single Source of Truth

- **Pure Simulation**: Uses `board.Clone()`, which creates an independent, detached copy of the 9 cells. Candidate placement never touches `Game.Status`, `Game.CurrentPlayer`, `Game.MoveHistory`, `Game.Winner`, `Game.WinningCells`, or `Game._undoStack`.
- **Zero Algorithmic Duplication**: Candidate boards are evaluated exclusively by calling `WinDetector.CheckWin(simulatedBoard)`. The 8 canonical winning lines remain defined in exactly one place in the domain.
- **Board Immutability**: Verified by automated tests that `board.Cells` and `board.OccupiedCount` are identical before and after `SelectMove`.

---

## 6. Computer Mode Turn Orchestration & Memento Integration

### Orchestration Flow (`Game.ExecuteTurn`)
1. Human plays `Player.X` at `humanMove`:
   - Validates `Status == InProgress`, `CurrentPlayer == Player.X`, and cell vacancy.
   - Pushes Memento snapshot to `_undoStack` (per P006 boundary: `Mode == TwoPlayer || player == Player.X`).
   - Places mark, appends move, and evaluates win/draw.
2. **Terminal Precedence Check**:
   - If Human X move wins or causes a draw, the execution halts immediately. Computer is **not** invoked.
3. **Computer O Response (`Game.PlayComputerMove`)**:
   - If game remains `InProgress`, invokes `strategy.SelectMove(Board)`.
   - Applies move for `Player.O`.
   - Per P006 boundary, `player == Player.O`, so **no second Memento snapshot is pushed**.
   - Evaluates win/draw; if non-terminal, alternates turn back to `Player.X`.

### Explicit Invocation Guards (`PlayComputerMove`)
- `Mode != GameMode.Computer` $\rightarrow$ Throws `InvalidOperationException`.
- `Status != GameStatus.InProgress` $\rightarrow$ Throws `GameAlreadyCompletedException`.
- `CurrentPlayer != Player.O` $\rightarrow$ Throws `InvalidTurnException`.
- `strategy == null` $\rightarrow$ Throws `ArgumentNullException`.

### Undo Integration (FR-10)
- In active Computer Mode games, exactly one Memento snapshot exists per completed logical turn (the pre-human-move state).
- Invoking `game.Undo()` restores the board, `CurrentPlayer = Player.X`, move history, and `InProgress` status immediately prior to the human move.
- If the game concludes (either by human win/draw or computer win/draw), Option A (`ADR-004`) disables undo: `CanUndo` evaluates to `false` and `Undo()` throws `CannotUndoException`.

---

## 7. Test Verification

### Backend Tests (`dotnet test backend/TicTacToe.sln`):
- **Total Tests**: 114 passed, 0 failed, 0 skipped.
- **P007 Computer Strategy Suite (`ComputerStrategyTests.cs` - 17 tests)**:
  - `SelectMove_WhenImmediateWinAvailableForO_SelectsWinningCell`
  - `SelectMove_WhenImmediateColumnWinAvailableForO_SelectsWinningCell`
  - `SelectMove_WhenImmediateDiagonalWinAvailableForO_SelectsWinningCell`
  - `SelectMove_WhenMultipleWinsAvailableForO_SelectsLowestIndexDeterministically`
  - `SelectMove_WhenBothWinForOAndBlockForXAvailable_PrioritizesWinOverBlock`
  - `SelectMove_WhenHumanThreatensRowWin_BlocksThreat`
  - `SelectMove_WhenHumanThreatensColWin_BlocksThreat`
  - `SelectMove_WhenHumanThreatensDiagonalWin_BlocksThreat`
  - `SelectMove_WhenMultipleThreatsExist_BlocksDeterministicallyByLowestIndex`
  - `SelectMove_WhenNoWinOrBlockAndCenterAvailable_SelectsCenterCell4`
  - `SelectMove_WhenCenterOccupied_SelectsFirstAvailableCornerInOrder0_2_6_8`
  - `SelectMove_WhenCenterAndCorner0Occupied_SelectsCorner2`
  - `SelectMove_WhenCenterAndCorners0And2Occupied_SelectsCorner6`
  - `SelectMove_WhenCenterAndCorners0_2_6Occupied_SelectsCorner8`
  - `SelectMove_WhenCenterAndAllCornersOccupied_SelectsFirstAvailableEdgeInNaturalOrder`
  - `SelectMove_NeverReturnsOccupiedCell`
  - `SelectMove_ReturnsValidCellIndex_InRange0To8`
  - `SelectMove_DoesNotMutateSuppliedBoard`
  - `SelectMove_WhenBoardFull_ThrowsInvalidOperationException`
  - `SelectMove_WhenNullBoard_ThrowsArgumentNullException`
  - `SelectMove_Across100RepetitionsOnSameBoard_ProducesIdenticalCell`
- **P007 Computer Mode Integration Suite (`ComputerModeIntegrationTests.cs` - 14 tests)**:
  - `ExecuteTurn_HumanMoveThenComputerMove_CompletesTwoMovesAndReturnsTurnToHuman`
  - `ExecuteTurn_VerifiesBoardHistoryAndAlternation`
  - `ExecuteTurn_WhenHumanMoveWins_HaltsImmediatelyAndComputerDoesNotMove`
  - `ExecuteTurn_WhenHumanMoveCreatesDraw_HaltsImmediatelyAndComputerDoesNotMove`
  - `ExecuteTurn_WhenComputerMoveWins_TransitionsToWonWithWinnerO`
  - `ExecuteTurn_WhenComputerMoveFillsBoard_TransitionsToDraw`
  - `Undo_AfterCompletedComputerTurn_RevertsBothHumanAndComputerMovesRestoringPlayerX`
  - `Undo_AfterMultipleComputerTurns_RevertsOneLogicalTurnPairPerUndo`
  - `Undo_AfterComputerWins_ThrowsCannotUndoExceptionPreservingOptionA`
  - `Undo_AfterHumanWins_ThrowsCannotUndoExceptionPreservingOptionA`
  - `PlayComputerMove_InTwoPlayerMode_ThrowsInvalidOperationException`
  - `PlayComputerMove_WhenGameAlreadyWon_ThrowsGameAlreadyCompletedException`
  - `PlayComputerMove_WhenCurrentPlayerIsX_ThrowsInvalidTurnException`
  - `PlayComputerMove_WhenStrategyNull_ThrowsArgumentNullException`

### Frontend Smoke Tests (`npm test --prefix frontend -- --watch=false`):
- **Total Tests**: 2 passed, 0 failed. Zero regressions.

---

## 8. Domain Architecture Purity

`TicTacToe.Domain.csproj` verification:
- `PackageReferences`: 0
- `ProjectReferences`: 0
- Zero dependencies on ASP.NET Core, HTTP, EF Core, databases, Angular, or external AI/LLM libraries.

---

## 9. Deferred Requirements Boundary Verification

The following boundaries were strictly preserved and not implemented:
- [x] No `GameCompleted` domain event or `Scoreboard` implementation (deferred to P008).
- [x] No Application layer use case handlers or DTOs (deferred to P010).
- [x] No REST API controllers or HTTP endpoints (deferred to P010/P011).
- [x] No Angular UI game board or component interactions (deferred to P009/P010).

---

## 10. Traceability Status

- **FR-10 (Computer Undo)**: Domain portion: Implemented and Verified end-to-end via automated integration tests; Overall requirement: Partially Implemented (Deferred integration: API/UI).
- **FR-14 (Computer Strategy)**: Domain portion: Implemented (Deterministic 5-tier hierarchy via `BasicComputerMoveStrategy`); Overall requirement: Partially Implemented (Deferred integration: API/UI).

---

## 11. Final Status

P007 PASS — READY FOR P008
