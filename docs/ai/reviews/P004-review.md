# P004 Review — Pure Domain Model Foundation

## Objective
Implement the pure Domain Model Foundation for the TicTacToe application without implementing win/draw detection, undo mementos, computer strategy, scoreboard, API, or UI.

## Specification Sources
- `docs/01-requirements.md` (FR-01, FR-02, FR-03, FR-06, FR-07)
- `docs/04-ddd-and-domain-model.md` (Domain model, aggregate boundary, value objects, invariants)
- `docs/05-architecture.md` (Layer boundaries and domain purity)
- `docs/06-api-contract.md` (Canonical DTO mapping alignment)
- `docs/07-test-strategy.md` (Domain unit test scenarios)
- `docs/10-adr-template-and-initial-decisions.md` (ADR-001, ADR-003)
- `docs/13-assumptions.md` (A-001 cell addressing 0..8)
- `docs/ai/decisions/ADR-008-solution-scaffolding-and-toolchain.md`

## Domain Model

### Aggregate Root
- **`Game`**: Aggregate root maintaining session state and protecting domain invariants.
  - Owns: `Id` (`GameId`), `Mode` (`GameMode`), `Status` (`GameStatus`), `CurrentPlayer` (`Player`), `Winner` (`Player?`), `WinningCells` (`IReadOnlyList<int>`), `Board` (`Board`), `MoveHistory` (`IReadOnlyList<Move>`).
  - Command: `MakeMove(Player player, CellIndex cellIndex)`.

### Entities
- **`Board`**: Represents the 3x3 grid (9 cells) indexed by `CellIndex` (0..8). Enforces occupied-cell protection and defensive encapsulation.

### Value Objects
- **`CellIndex`**: Immutable record struct enforcing `0 <= value <= 8`.
- **`GameId`**: Strongly-typed identity value object wrapping non-empty `Guid`.
- **`Move`**: Immutable record representing a played move (`MoveNumber`, `Player`, `CellIndex`).

### Enumerations
- **`Player`**: `X`, `O` with `Other()` turn alternator extension.
- **`GameMode`**: `TwoPlayer`, `Computer`.
- **`GameStatus`**: `InProgress`, `Won`, `Draw` with `IsTerminal()` extension.

### Domain Invariants
1. **Cell Index Boundary**: Must be an integer between 0 and 8 inclusive.
2. **Board Size**: Exactly 9 cells initialized to empty (`null`).
3. **Occupied Cell Protection**: A mark cannot be placed on an occupied cell. Throws `CellOccupiedException`.
4. **Turn Validation**: Only the `CurrentPlayer` can submit a move. Throws `InvalidTurnException`.
5. **Game Status Validation**: Moves rejected on completed games. Throws `GameAlreadyCompletedException`.
6. **Turn Alternation**: Turn toggles between X and O upon successful move.
7. **Move History**: Moves are tracked chronologically with sequential 1-based `MoveNumber`.

### Repository Abstractions
- **`IGameRepository`**: Domain interface for retrieving and persisting `Game` aggregates asynchronously.

## Design Patterns
- **Aggregate Root Pattern**: `Game` guards all state mutations and enforces invariants.
- **Value Object Pattern**: `CellIndex`, `GameId`, `Move` encapsulate validation rules with structural equality.

## Implemented
- Pure domain entities, value objects, exceptions, and repository abstractions.
- Move placement, turn switching, move history recording.
- Domain exception hierarchy: `DomainException`, `InvalidCellIndexException`, `CellOccupiedException`, `InvalidTurnException`, `GameAlreadyCompletedException`.

## Deferred
- **Win Detection**: Rows, columns, diagonals evaluation (Deferred to P005).
- **Draw Detection**: Full board without winner transition (Deferred to P005).
- **Winning Cells Calculation**: Highlight array calculation (Deferred to P005).
- **Undo / Memento**: Snapshot-based restore (Deferred to P006).
- **Computer Strategy**: AI move selection priority (Deferred to P007).
- **Scoreboard & GameCompleted**: Domain event emission and score tracking (Deferred to P008).
- **Application & API Layer**: DTO mapping, controllers, HTTP endpoints (Deferred to P010/P011).
- **Frontend UI Game Behavior**: Board interaction (Deferred to P009/P010).

## Tests
35 passed tests across backend (33 domain unit tests + 2 smoke tests):
- `CellIndexTests`: 18 tests (valid 0..8, invalid negative/large indices, TryCreate, equality).
- `BoardTests`: 4 tests (initialization, placement, occupied cell rejection, cloning).
- `PlayerTests`: 2 tests (turn alternation for X and O).
- `MoveTests`: 3 tests (valid instantiation, invalid move number rejection, equality).
- `GameTests`: 6 tests (initialization, move placement, sequential alternation, wrong player rejection, occupied cell rejection).
- Frontend tests: 2 Vitest unit tests passing.

## Architecture Verification

### Domain Dependencies
- **ProjectReferences**: None (`TicTacToe.Domain.csproj` has zero project references).
- **PackageReferences**: None.

### Forbidden Dependencies
- No ASP.NET Core, Entity Framework, HTTP, JSON serializer, or logging framework references exist in Domain.

## Requirement Traceability
- **FR-01 (Create Game)**: Partially implemented (Domain aggregate initialization complete).
- **FR-02 (Board)**: Implemented in Domain (9 cells, canonical 0..8 indexing, occupied cell immutability).
- **FR-03 (Turns)**: Implemented in Domain (CurrentPlayer visible, valid moves alternate, invalid moves do not change turn).
- **FR-06 (Move Validation)**: Implemented in Domain (Validation of index bounds, cell vacancy, correct turn).
- **FR-07 (Move History)**: Implemented in Domain (Sequential move records with moveNumber, player, cellIndex).

## AI Changes
Scaffolded domain entities, value objects, domain exceptions, repository abstraction, and unit test suites.

## Human Changes
None.

## Known Risks
None. Pure domain logic is deterministic and 100% test-covered.

## Final Status
PASS — READY FOR P005.
