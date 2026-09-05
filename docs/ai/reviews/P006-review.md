# P006 Review — Memento Pattern / Undo

**Review Date**: 2026-09-05  
**AI Review**: Completed  
**Human Review**: Pending  
**Review Type**: Implementation Review (P006 — Memento Pattern / Undo)  
**Status**: PASS — READY FOR P007  

---

## 1. Objective

Implement snapshot-based **Undo** for the `Game` aggregate root using the **Memento Pattern** (`GoF`) in `TicTacToe.Domain`, ensuring defensive snapshot encapsulation, logical turn boundary support (Two Player vs Computer Mode), Option A terminal state handling, and 100% invariant preservation.

---

## 2. Specification Sources

- `docs/01-requirements.md` (FR-08, FR-09, FR-10)
- `docs/04-ddd-and-domain-model.md` §3 (Game Aggregate), §6 (Memento Pattern — Undo)
- `docs/05-architecture.md` §7 (Undo Design — Option A)
- `docs/10-adr-template-and-initial-decisions.md` (ADR-004: Disable Undo After Completion)
- `docs/13-assumptions.md` (A-003: Option A, A-004: Memento Pattern)
- `docs/ai/prompts/P006-memento-pattern-undo.md`

---

## 3. Pattern Implementation & Architecture

### GoF Pattern Roles:
- **`Game` = Memento Originator**:
  - Owns all private aggregate state.
  - Captures internal state into an immutable `GameMemento` (`CreateMemento()`).
  - Restores its own state from a Memento (`RestoreFromMemento(memento)`).
- **`GameMemento` = Memento**:
  - Internal record in `TicTacToe.Domain.Mementos`.
  - Holds immutable/defensive copies of `Board`, `CurrentPlayer`, `Status`, `Winner`, `WinningCells`, and `MoveHistory`.
  - Completely opaque to external callers.
- **`_undoStack` = Internal Caretaker**:
  - Private `Stack<GameMemento>` owned directly by `Game`.
  - External callers cannot manipulate, forge, or leak arbitrary snapshots.

### Snapshot Timing & Atomicity:
- Snapshot is captured **after validation, immediately before aggregate mutation** in `MakeMove`.
- Rejected moves (e.g. out of bounds, occupied cell, wrong turn) never push snapshots.

### Logical Move Boundaries:
- **Two Player Mode**: Every valid human move captures a snapshot and pushes to `_undoStack`. Calling `Undo()` reverts 1 move.
- **Computer Mode**: The human X move captures a snapshot at the start of the pair; the computer O response does not push a second snapshot. Calling `Undo()` reverts the entire human/computer pair.
- **P006 vs P007 Boundary**:
  > P006 establishes the Memento and logical snapshot-boundary mechanism. P007 owns the Computer Strategy and the application/domain orchestration that guarantees the human X move and computer O move form one logical turn before the response is returned.
  P007 must perform the final end-to-end FR-10 Computer Mode Undo verification.
- **Computer Mode Intermediate State**:
  Do not add unnecessary complexity to P006 to support an intermediate "X has moved but O has not yet moved" state. The eventual Computer Mode application flow will be:
  ```text
  Human X move
        ↓
  Memento already exists
        ↓
  Computer Strategy
        ↓
  Computer O move
        ↓
  authoritative GameState returned
  ```
  Therefore the user-facing/API-visible Computer Mode state will represent the completed logical pair. This is documented as a P007 integration responsibility.

### Option A Terminal State Handling:
- Pre-mutation snapshot is captured on terminal moves (`Won` or `Draw`) and preserved in `_undoStack`.
- `CanUndo` explicitly checks `Status == GameStatus.InProgress && _undoStack.Count > 0`.
- Calling `Undo()` on a completed game throws `CannotUndoException`, preserving scoreboard finality.

### Restoration Computational Complexity:
```text
Memento selection/pop = O(1)
Board restoration = O(9), effectively constant for this domain
MoveHistory restoration = O(n)
Overall restoration cost = proportional to snapshot size
```

---

## 4. Implemented Artifacts

1. [`GameMemento.cs`](file:///c:/Labs/TicTacToe/backend/TicTacToe.Domain/Mementos/GameMemento.cs): Internal record holding defensive snapshots.
2. [`CannotUndoException.cs`](file:///c:/Labs/TicTacToe/backend/TicTacToe.Domain/Exceptions/CannotUndoException.cs): Domain exception deriving from `DomainException`.
3. [`Game.cs`](file:///c:/Labs/TicTacToe/backend/TicTacToe.Domain/Aggregates/Game.cs): Enhanced with `CanUndo`, `Undo()`, private snapshot capture/restoration, and logical turn boundary handling.
4. [`GameUndoTests.cs`](file:///c:/Labs/TicTacToe/backend/TicTacToe.Tests/Domain/GameUndoTests.cs): 12 unit tests covering all Memento snapshot, restoration, exhaustion, atomicity, defensive isolation, and invariant preservation scenarios.

---

## 5. Deferred Functionality

- **Computer Move Strategy**: Priority heuristics and automated selection deferred to P007.
- **Scoreboard & GameCompleted Domain Event**: Event emission and score counters deferred to P008.
- **Frontend Game UI**: Board controls and Undo button interaction deferred to P009/P010.
- **REST API Endpoints & DTOs**: `POST /api/games/{id}/undo` endpoint deferred to P010/P011.

---

## 6. Test Verification

### Backend Tests (`dotnet test backend/TicTacToe.sln`):
- **Total Tests**: 79 passed, 0 failed, 0 skipped.
- **P006 Specific Scenarios**:
  - `CanUndo_OnNewGame_ReturnsFalse`
  - `Undo_OnNewGame_ThrowsCannotUndoExceptionAndPreservesState`
  - `Undo_AfterSingleMove_RestoresInitialEmptyBoardAndPlayerX`
  - `Undo_AfterTwoMoves_RestoresFirstMoveAndPlayerOTurn`
  - `Undo_MultipleSequentialUndos_RestoresPriorStatesSequentiallyUntilExhaustion`
  - `Undo_FollowedByNewMove_EstablishesNewBranchCorrectly`
  - `MakeMove_WhenRejected_DoesNotCreateMementoOrAlterUndoStack`
  - `Undo_AfterGameWon_ThrowsCannotUndoExceptionAndPreservesTerminalState` (Option A)
  - `Undo_AfterGameDraw_ThrowsCannotUndoExceptionAndPreservesTerminalState` (Option A)
  - `Undo_MaintainsMoveHistoryCountEqualsBoardOccupiedCountInvariant`
  - `Memento_DefensivelyIsolatesStateFromExternalMutations`
  - `Undo_InComputerMode_RevertsHumanAndComputerPair` (Logical turn pair boundary)

### Frontend Smoke Tests (`npm test --prefix frontend -- --watch=false`):
- **Total Tests**: 2 passed, 0 failed. Zero regressions.

---

## 7. Requirement Traceability

- **FR-08 (Undo Availability)**: Domain portion: Implemented; Overall requirement: Partially Implemented (Deferred integration: API/UI).
- **FR-09 (Two Player Undo)**: Domain portion: Implemented; Overall requirement: Partially Implemented (Deferred integration: API/UI).
- **FR-10 (Computer Undo)**: Memento logical-boundary mechanism established in P006; full Computer Mode pair-level integration deferred to P007.

---

## 8. Final Status

PASS — READY FOR P007
