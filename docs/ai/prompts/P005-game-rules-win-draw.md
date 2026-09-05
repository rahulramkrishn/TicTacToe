# P005 — Game Rules, Win Detection & Draw Detection

## 1. Context

P001, P002, P003, P003.1, P004 and P004.1 have been completed.

The repository currently contains:

* the complete assessment specification
* architecture and NFR documentation
* AI governance and audit structure
* solution scaffolding
* a pure Domain model foundation
* validated aggregate boundaries
* validated domain invariants
* domain unit tests
* clean Git history

P004.1 has explicitly approved the repository for P005.

The current implementation must remain the baseline.

---

# 2. Objective

Implement the **Tic-Tac-Toe game rules** inside the Domain layer.

P005 is responsible for:

1. legal move processing using the P004 domain foundation
2. win detection
3. draw detection
4. winning-cell identification
5. game terminal-state transitions
6. correct turn progression
7. domain tests for all game-rule behavior

P005 must NOT implement future phases.

---

# 3. Mandatory Specification Reading

Before modifying any code, read the following files completely:

```text
README.md

docs/01-requirements.md
docs/02-prerequisites-and-environment.md
docs/03-nfr.md
docs/04-ddd-and-domain-model.md
docs/05-architecture.md
docs/06-api-contract.md
docs/07-test-strategy.md
docs/08-ai-development-governance.md
docs/09-traceability-matrix.md
docs/10-adr-template-and-initial-decisions.md
docs/11-implementation-plan.md
docs/12-root-ai-change-log.md
docs/13-assumptions.md
docs/14-panel-review.md
docs/ADR-007-game-completed-domain-event.md
docs/AI-CHANGE-AUDIT.md
docs/AI_PROMPTS.md
docs/ASSUMPTIONS.md
docs/CHANGELOG.md
docs/README.md
```

Also read the current AI governance artifacts:

```text
docs/ai/DOCUMENT-INVENTORY.md
docs/ai/REPOSITORY-ANALYSIS.md
docs/ai/REQUIREMENT-TRACEABILITY.md
docs/ai/ARCHITECTURE-TRACEABILITY.md
docs/ai/AI-DECISIONS.md
docs/ai/IMPLEMENTATION-PLAN.md
docs/ai/IMPLEMENTATION-LOG.md
docs/ai/MANUAL-CHANGES.md
docs/ai/reviews/P004-review.md
docs/ai/reviews/P004.1-domain-review.md
```

The specification is authoritative.

Do not infer requirements that are not supported by the specification.

---

# 4. P005 Boundary

P005 owns:

```text
Move rules
Win detection
Draw detection
Winning cells
Terminal game state
Turn progression
```

P005 does NOT own:

```text
Undo
Memento
Computer Strategy
Computer move selection
GameCompleted domain event
Scoreboard
REST API
API DTOs
Angular UI
Persistence
Authentication
Logging infrastructure
```

These are intentionally deferred.

---

# 5. Frozen Decisions

The following decisions are already fixed.

## Cell Addressing

The canonical representation is:

```text
cellIndex: 0..8
```

Board:

```text
0 1 2
3 4 5
6 7 8
```

Do NOT introduce an alternative public `row/column` representation.

If row/column calculations are useful internally, derive them from `cellIndex`.

The canonical domain and API representation remains `cellIndex`.

---

## Reset

Reset will eventually reuse the same `gameId`.

Do not implement Reset in P005 unless it is already part of the existing Domain API and P005 must preserve its behavior.

Do not create a new game identity during reset.

---

## Undo

Undo uses the:

**Memento Pattern**

P006 owns the implementation.

Do not implement undo in P005.

Do not introduce undo-specific application services.

---

## Computer Player

Computer move selection uses the:

**Strategy Pattern**

P007 owns this.

Do not implement computer behavior in P005.

---

## Game Completion

`GameCompleted` is a mandatory domain event.

P008 owns the event implementation and scoreboard integration.

Do not publish, dispatch, or handle `GameCompleted` in P005.

---

# 6. Before Coding — Produce a P005 Design Proposal

Before changing code, inspect the existing P004 model.

Produce a short design proposal containing:

```text
Game rule responsibilities
Win detection responsibility
Draw detection responsibility
Winning-cell responsibility
Game state transition responsibility
Turn progression responsibility
```

Explicitly decide whether win detection should be:

```text
inside Game
```

or implemented as a:

```text
stateless domain service / domain policy
```

Use the architecture and DDD documentation as evidence.

Do not create abstractions merely because they are theoretically possible.

Prefer the simplest design that:

* preserves aggregate invariants
* is easy to test
* keeps the Game aggregate responsible for state transitions
* avoids duplicating rule logic
* remains framework-independent

If the existing specification explicitly names a `WinDetector`, follow it.

---

# 7. Winning Combinations

The board contains exactly nine cells.

The complete set of winning lines is:

```text
0,1,2
3,4,5
6,7,8

0,3,6
1,4,7
2,5,8

0,4,8
2,4,6
```

There are exactly:

```text
8 winning combinations
```

Do not hard-code behavior for only selected rows, columns, or diagonals.

All eight combinations must be tested.

---

# 8. Win Detection

After a legal move, determine whether the active player has completed a winning line.

A win occurs when the same player's mark occupies all three cells of one winning combination.

When a win occurs:

```text
GameStatus = Won
Winner = active player
WinningCells = exact three cell indexes
```

The winning cells must use canonical:

```text
cellIndex 0..8
```

Do not return row/column coordinates.

---

# 9. Winning Cells

`WinningCells` must contain the actual cells that caused the winning condition.

Examples:

```text
0,1,2
```

or:

```text
0,3,6
```

or:

```text
0,4,8
```

Do not return all occupied cells.

Do not return all possible winning lines.

Return the winning line associated with the completed game.

If the specification defines a deterministic rule for multiple simultaneous winning lines, follow it.

Do not invent a new rule if the specification is silent.

If the existing game model makes simultaneous multiple-line wins possible on one move, inspect the specification and current model before choosing behavior.

---

# 10. Draw Detection

A draw occurs when:

```text
Board is full
AND
No player has won
```

Therefore evaluation order must be:

```text
1. Validate move
2. Apply move
3. Check win
4. If no win, check draw
5. If neither, continue game
```

Do NOT classify a full board as a draw before checking whether the move produced a win.

A final move that fills the board and creates a winning line is a:

```text
Win
```

not a draw.

---

# 11. Turn Progression

For a non-terminal legal move:

```text
CurrentPlayer
    ↓
Move applied
    ↓
No win
    ↓
No draw
    ↓
Switch player
```

When the game becomes:

```text
Won
```

or:

```text
Draw
```

do NOT switch to another player.

The terminal game state must remain stable.

---

# 12. Completed Game Protection

After:

```text
Won
```

or:

```text
Draw
```

another move must not be accepted.

The existing P004 domain exception for completed games should be used if that is the established design.

Do not introduce HTTP status codes or API error types.

---

# 13. Atomicity

Every legal move must produce one coherent domain transition.

Every rejected move must leave the aggregate state unchanged.

For rejected operations verify, where applicable:

```text
Board unchanged
MoveHistory unchanged
CurrentPlayer unchanged
Status unchanged
Winner unchanged
WinningCells unchanged
```

Examples:

### Wrong player

```text
MakeMove(O, cell)
```

when X is expected.

State must remain unchanged.

### Occupied cell

Attempt to place a mark in an occupied cell.

State must remain unchanged.

### Invalid cell

Attempt to use:

```text
-1
9
```

State must remain unchanged.

### Completed game

Attempt a move after:

```text
Won
```

or:

```text
Draw
```

State must remain unchanged.

Do not rely on exceptions alone.

Tests must verify state preservation.

---

# 14. Win Detection Design

Avoid unnecessary duplication such as eight separate blocks of procedural logic inside `Game`.

Prefer a maintainable representation of the eight winning combinations.

For example, conceptually:

```text
WinningLines
    ↓
Evaluate board
    ↓
Matching line
    ↓
Winning player
```

The exact implementation is up to the existing architecture and specification.

Do not introduce a generic rules engine.

Do not introduce reflection.

Do not introduce configuration files.

Do not introduce a database table for winning combinations.

Keep the implementation simple and deterministic.

---

# 15. Domain Purity

The implementation must remain entirely inside the pure Domain boundary.

No references to:

```text
ASP.NET Core
Infrastructure
Entity Framework
HTTP
Controllers
ProblemDetails
Angular
JSON serialization
database
logging frameworks
configuration
```

The Domain must remain framework-independent.

---

# 16. Domain Events — Explicit Boundary

Do NOT implement:

```text
GameCompleted
```

in P005.

However, the design must leave the aggregate in a state where P008 can reliably determine that the game has transitioned to a completed state.

Do not add:

```text
event dispatcher
event handler
scoreboard update
```

in P005.

P008 owns that behavior.

---

# 17. Memento — Explicit Boundary

Do NOT implement:

```text
Memento
Undo
UndoManager
Snapshot history
```

in P005.

If P004's `Board.Clone()` remains, do not expand it into a Memento implementation.

P006 will make the Memento design explicit.

---

# 18. Strategy — Explicit Boundary

Do NOT implement:

```text
ComputerPlayer
ComputerMoveStrategy
RandomStrategy
OptimalStrategy
Minimax
```

in P005.

P007 owns computer move selection.

P005 only implements the game rules that apply regardless of whether the player is human or computer-controlled.

---

# 19. Test Requirements

Expand the Domain test suite substantially.

## Win Tests

Test all eight winning lines.

### Rows

```text
0,1,2
3,4,5
6,7,8
```

### Columns

```text
0,3,6
1,4,7
2,5,8
```

### Diagonals

```text
0,4,8
2,4,6
```

For every winning line verify:

```text
Status == Won
Winner == correct player
WinningCells == expected three cell indexes
```

---

# 20. Both Players Must Be Tested

Do not test only X wins.

Verify equivalent winning scenarios for:

```text
X
O
```

where practical.

The rules are symmetric with respect to player identity.

---

# 21. Draw Tests

Create tests for:

### Standard draw

A full board with no winning line.

Verify:

```text
Status == Draw
Winner == none/null
WinningCells == empty
```

### Final-move win

A full board where the final move creates a winning line.

Verify:

```text
Status == Won
```

and NOT:

```text
Status == Draw
```

This test is mandatory.

---

# 22. Turn Tests

Verify:

```text
Initial player
    ↓
legal move
    ↓
next player
```

Then:

```text
winning move
    ↓
no next-player transition
```

and:

```text
draw-producing move
    ↓
no next-player transition
```

---

# 23. Terminal State Tests

Verify that after:

```text
Won
```

another move is rejected.

Verify that after:

```text
Draw
```

another move is rejected.

Verify state preservation after the rejected operation.

---

# 24. Move History Tests

Because P004 established move history, verify that P005 rule processing preserves correct history.

For each legal move:

```text
MoveHistory.Count increases by exactly 1
```

For rejected moves:

```text
MoveHistory.Count does not change
```

Do not implement undo.

---

# 25. Property / Invariant Tests

Where practical, verify:

```text
Number of moves == number of occupied cells
```

for valid game progression.

Verify that:

```text
Occupied cells <= 9
```

and:

```text
No cell contains both players
```

Do not introduce a property-testing framework unless the project already uses one.

---

# 26. Regression Tests

Run the complete existing suite.

P005 must not break:

```text
P003/P003.1 tests
P004 domain tests
frontend smoke tests
```

No existing passing test should be removed merely to accommodate implementation.

---

# 27. Test Quality

Tests must verify behavior rather than implementation details.

Avoid tests that merely assert:

```text
class exists
method exists
private field exists
```

Prefer:

```text
Given board state
When legal move is made
Then domain state transitions correctly
```

Use clear test names.

---

# 28. Requirement Traceability

Update:

```text
docs/ai/REQUIREMENT-TRACEABILITY.md
```

Do not mark entire requirements as implemented if they contain functionality belonging to future phases.

Explicitly distinguish:

```text
P004 foundation
+
P005 game rules
```

from:

```text
P006 undo
P007 strategy
P008 completion event/scoreboard
P009/P010 frontend
P011 API
```

---

# 29. Architecture Traceability

Update:

```text
docs/ai/ARCHITECTURE-TRACEABILITY.md
```

where required.

Document:

```text
Game aggregate
Win detection
Draw detection
Terminal state transitions
```

and their relationship to the architecture.

---

# 30. AI Decision Record

If P005 introduces a genuine architectural decision not already covered by the existing specification, update:

```text
docs/ai/AI-DECISIONS.md
```

If the decision materially changes architecture, create an ADR.

Do NOT create ADRs for ordinary implementation choices.

---

# 31. P005 Review Artifact

Create:

```text
docs/ai/reviews/P005-review.md
```

Use:

```markdown
# P005 Review

## Objective

Game Rules, Win Detection and Draw Detection.

## Specification Sources

[List]

## Design Proposal

### Game Responsibility

### Win Detection

### Draw Detection

### Winning Cells

### Turn Progression

### Terminal State

## Implemented

[List]

## Deferred

[List]

## Winning Lines

[List all eight]

## Invariants

[List]

## Tests

### Win Tests

### Draw Tests

### Turn Tests

### Terminal State Tests

### Atomicity Tests

### Move History Tests

## Architecture

### Domain Purity

### Dependency Direction

## Requirement Traceability

[List]

## AI Changes

[List]

## Human Changes

[List]

## Known Risks

[List]

## Final Status

PASS / BLOCKED
```

Do not claim human review unless a human has actually reviewed the implementation.

---

# 32. AI Change Audit

Update:

```text
docs/ai/IMPLEMENTATION-LOG.md
docs/ai/AI-DECISIONS.md
docs/ai/MANUAL-CHANGES.md
```

Record:

```text
Prompt ID: P005

Objective:
Implement Tic-Tac-Toe game rules.

AI-assisted changes:
<exact files/classes>

Human changes:
<only actual human changes>

Tests:
<exact commands and results>

Requirements:
<implemented / partially implemented / deferred>

Architecture decisions:
<list>

Review:
AI review completed
Human review pending
```

Do not fabricate human changes.

---

# 33. Git Discipline

Before commit:

```text
git status
git diff
git diff --cached
```

Review all modified files.

Do not commit:

```text
bin/
obj/
node_modules/
dist/
coverage/
```

Create one logical implementation commit:

```text
feat(domain): implement game rules and win detection
```

If the implementation log is updated afterward with the commit hash, create:

```text
docs(ai): record P005 commit hash in implementation log
```

Only if required.

Do not invent commit hashes.

---

# 34. Build and Test Gate

Run:

```text
dotnet restore backend/TicTacToe.sln
dotnet build backend/TicTacToe.sln
dotnet test backend/TicTacToe.sln
```

Also run the frontend verification command established by P003.1.

Report exact:

```text
Tests discovered:
Passed:
Failed:
Skipped:
Warnings:
Errors:
```

The expected result is:

```text
0 failed
0 unexpected skipped
0 build errors
```

Do not hide failures.

---

# 35. Final Architecture Verification

Before reporting success, verify that P005 did NOT introduce:

```text
Undo
Memento
Strategy
Computer player
GameCompleted
Scoreboard
API
Controllers
DTOs
Angular business logic
Persistence
```

Search the repository if necessary.

---

# 36. Final P005 Report

Return an evidence-based report with the following sections.

## 1. Executive Summary

State whether P005 passed.

## 2. Design Decision

Explain:

```text
How Game owns the state transition
How win detection is implemented
How draw detection is implemented
How winning cells are determined
```

## 3. Domain Changes

List every changed/created class.

## 4. Win Detection

List all eight winning combinations and confirm tests exist for them.

## 5. Draw Detection

Explain the evaluation order:

```text
Win before Draw
```

## 6. Turn Management

Explain normal and terminal transitions.

## 7. Atomicity

Explain how rejected moves preserve state.

## 8. Tests

Provide exact:

```text
Total
Passed
Failed
Skipped
```

## 9. Build

Exact result.

## 10. Architecture

Confirm Domain purity and dependency direction.

## 11. Requirements

List:

```text
Implemented
Partially Implemented
Deferred
```

## 12. Deferred Functionality

Explicitly confirm:

```text
Memento / Undo → P006
Strategy / Computer → P007
GameCompleted / Scoreboard → P008
Frontend → later phase
API → later phase
```

## 13. Audit

Confirm:

```text
P005 review artifact
Implementation log
Requirement traceability
Architecture traceability
AI decisions
Manual change log
```

## 14. Git

Report:

```text
Current branch:
HEAD:
Latest commit:
Working tree:
```

## 15. Final Gate

If all P005 requirements pass:

```text
P005 PASS — READY FOR P006
```

Otherwise:

```text
P005 BLOCKED
```

Do not automatically begin P006.

STOP.
