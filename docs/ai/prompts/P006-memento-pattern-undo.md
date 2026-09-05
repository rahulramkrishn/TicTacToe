# P006 — Memento Pattern / Undo

## 1. Context

P001, P002, P003, P003.1, P004, P004.1, P005, and P005.1 have been completed.

The repository currently contains:
- Architecture and NFR documentation
- AI governance and audit ledger
- Scaffolding and clean dependency boundaries
- Pure domain model foundation (`Game` aggregate root, `Board` entity, value objects, domain exceptions)
- Game rules, win detection across 8 canonical lines, draw detection, and winning-cell identification
- Encapsulated, defensive domain state with 67 passing backend unit tests and 2 passing frontend tests
- Clean Git history

---

## 2. Objective

Implement snapshot-based **Undo** for the `Game` aggregate root using the **Memento Pattern** (`GoF`) in the pure `TicTacToe.Domain` layer.

P006 is responsible for:
1. `GameMemento`: Immutable value object capturing aggregate state snapshots.
2. Snapshot capture mechanism immediately before successful logical move boundaries.
3. Logical boundary capability supporting Two Player (per-move) and Computer Mode (pair-level).
4. Snapshot restoration restoring the exact pre-move aggregate state without heuristic recalculation.
5. Multiple sequential Undo operations back to initial state.
6. `CanUndo` availability rule and `CannotUndoException` domain exception.
7. Option A terminal state handling (pre-mutation snapshot captured, but `CanUndo` disabled upon `Won` or `Draw`).
8. Comprehensive domain unit tests for all Memento creation, restoration, atomicity, and invariant scenarios.

---

## 3. Strict P006 Boundary (Deferred Functionality)

P006 does NOT implement:
- Computer Strategy (`IComputerMoveStrategy`, AI priorities) — deferred to P007.
- Automatic computer move execution — deferred to P007.
- `GameCompleted` domain event or Scoreboard — deferred to P008.
- REST API controllers or DTOs — deferred to P010/P011.
- Angular game UI — deferred to P009/P010.
- Persistence / Database — deferred to Infrastructure.

---

## 4. Separation of P006 and P007 Responsibilities

```text
P006: Memento Infrastructure
    ├── GameMemento value object (immutable snapshot)
    ├── Originator creation & restoration (Game)
    ├── Internal Caretaker (_undoStack)
    ├── Logical snapshot boundary mechanism
    └── CanUndo & CannotUndoException

P007: Computer Player & Strategy Pattern
    ├── IComputerMoveStrategy & RuleBasedComputerStrategy
    ├── Human move + computer move execution
    ├── Pair-level logical turn boundary
    └── Pair-level Undo automated tests
```

Do NOT move P007 implementation into P006.

---

## 5. Frozen Architectural Decisions

1. **Pattern Requirement**: Must explicitly use the **Memento Pattern**.
   - Use snapshot-based restoration (`O(snapshot size)`).
   - Do NOT implement heuristic move-reversal logic (e.g. clearing cells manually or subtracting turn counters).
2. **Undo Policy (Option A)**:
   - Frozen per `ADR-004`, `A-003`, and `FR-08`.
   - Undo is **disabled** when the game reaches `Won` or `Draw`.
   - `CanUndo => Status == GameStatus.InProgress && _undoStack.Count > 0`.
   - Attempting `Undo()` on a completed game throws `CannotUndoException`.
3. **Terminal Move Snapshot**:
   - A valid move that completes a game (`Won` or `Draw`) **captures** its pre-mutation Memento.
   - The Memento remains in `_undoStack`.
   - Availability is governed by `CanUndo`.
4. **Logical Boundaries**:
   - Two Player: One undo = one player move.
   - Computer: One undo = the human X move + computer O move pair.
5. **Redo**: Explicitly out of scope.

---

## 6. Snapshot Contents & Defensive Isolation

| State Property | Capture in Memento? | Reason |
|---|---|---|
| **`Board`** | **YES** | Mutable aggregate state. Cloned via `Board.Clone()`. |
| **`CurrentPlayer`** | **YES** | Required to restore player turn. |
| **`Status`** | **YES** | Required to restore lifecycle state (`InProgress`). |
| **`Winner`** | **YES** | Required to clear/restore winner. |
| **`WinningCells`** | **YES** | Required to restore presentation highlight state. Defensively copied. |
| **`MoveHistory`** | **YES** | Required for historical consistency. Defensively cloned list. |
| **`Id`** | **NO** | Immutable aggregate session identity. |
| **`Mode`** | **NO** | Immutable game configuration. |

---

## 7. Snapshot Timing & Atomicity

The snapshot timing must be:
```text
1. Validate move prerequisites (Status, Turn, CellIndex, Cell vacancy)
2. Capture Memento snapshot (CreateMemento())
3. Push to _undoStack (if logical turn start)
4. Mutate aggregate (Board, MoveHistory, WinDetector evaluation)
```
- Rejected moves (e.g. occupied cell, wrong turn) MUST NOT create a Memento or alter `_undoStack`.

---

## 8. DDD & Pattern Language

- `Game`: **Memento Originator**.
- `GameMemento`: **Memento** (immutable domain value object).
- `_undoStack`: **Caretaker** responsibility owned internally by `Game`.
- Keeping caretaker responsibility inside the aggregate prevents external callers from constructing or mutating arbitrary snapshots.

---

## 9. Computational Complexity

- **Memento Selection / Pop**: `O(1)`.
- **Board Restoration**: `O(9)` (effectively constant).
- **MoveHistory Restoration**: `O(n)` where `n <= 9`.
- **Overall Restoration Cost**: `O(snapshot size)`.
- Eliminates replay or recalculation of previous moves.

---

## 10. Required Test Scenarios

Create `backend/TicTacToe.Tests/Domain/GameUndoTests.cs`:
1. **Basic Undo**: Move 1 played -> Undo -> initial empty board, player X, `CanUndo == false`.
2. **Turn Restoration**: Move 1 (X at 0), Move 2 (O at 1) -> Undo -> O's move removed, Player O turn restored.
3. **Multiple Undo**: 3 moves -> 3 sequential undos restore move 2, then move 1, then initial state.
4. **Undo Exhaustion**: Calling `Undo()` on initial game throws `CannotUndoException`.
5. **Rejected Move Atomicity**: Invalid move attempts do not push Mementos or alter `CanUndo`.
6. **Branching History**: Move 1, Move 2, Undo, Move 2' (new cell) -> new branch cleanly established.
7. **Terminal State Option A**: Naturally won or drawn game -> `CanUndo == false`. Calling `Undo()` throws `CannotUndoException` and preserves state.
8. **Invariant Preservation**: `MoveHistory.Count == Board.OccupiedCount` maintained at every step.
9. **Defensive Isolation**: External modifications to returned board or history references cannot corrupt snapshots.

---

## 11. Audit Artifacts

- Update `docs/ai/REQUIREMENT-TRACEABILITY.md` (FR-08, FR-09).
- Update `docs/ai/ARCHITECTURE-TRACEABILITY.md` (Memento pattern).
- Create `docs/ai/reviews/P006-review.md`.
- Update `docs/ai/IMPLEMENTATION-LOG.md`.

---

## 12. Build and Test Gate

```powershell
dotnet build backend/TicTacToe.sln
dotnet test backend/TicTacToe.sln
npm test --prefix frontend -- --watch=false
```

Expected: 0 warnings, 0 errors, all tests passing.

---

## 13. Final Gate

Return:
```text
P006 PASS — READY FOR P007
```
or
```text
P006 BLOCKED
```

Do not start P007 automatically.
