P007 — Computer Strategy Pattern: Specification, Design & Implementation

Status

Phase: P007
Objective: Implement the deterministic Computer Player using the Strategy Pattern.
Prerequisite: P006 — Memento / Undo completed and committed.
Implementation state: Planning / awaiting approval.

1. Purpose

P007 implements the Computer Player for the Tic-Tac-Toe assessment.

The computer plays as Player O and must select a legal move using the frozen deterministic priority:

Winning move for O

Block winning move for X

Center

Corner

Any available cell

The implementation must use the Strategy Pattern, remain deterministic, be independently testable, and preserve the domain boundaries established in earlier phases.

P007 also completes the Computer Mode orchestration prepared by P006, including the logical human+computer turn boundary for Undo.

2. Required Specification Review

Before writing code, inspect the complete contents of:

README.md
docs/**/*.md

Also inspect:

backend/
frontend/

Do not restrict the review to selected Markdown files.

Pay particular attention to:

docs/01-requirements.md
docs/04-ddd-and-domain-model.md
docs/05-architecture.md
docs/06-api-contract.md
docs/07-test-strategy.md
docs/08-ai-development-governance.md
docs/09-traceability-matrix.md
docs/10-adr-template-and-initial-decisions.md
docs/11-implementation-plan.md
docs/13-assumptions.md
docs/14-panel-review.md
docs/ADR-007-game-completed-domain-event.md
docs/AI-CHANGE-AUDIT.md
docs/ai/REQUIREMENT-TRACEABILITY.md
docs/ai/ARCHITECTURE-TRACEABILITY.md
docs/ai/IMPLEMENTATION-LOG.md
docs/ai/AI-DECISIONS.md
docs/ai/MANUAL-CHANGES.md

The complete Markdown set is the authoritative project specification.

Do not silently change frozen requirements or assumptions.

3. P007 Architectural Goal

Explicitly use and document the Strategy Pattern.

Conceptually:

                    ┌──────────────────────────┐
                    │ IComputerMoveStrategy    │
                    │                          │
                    │ SelectMove(...)           │
                    └────────────┬─────────────┘
                                 │
                    ┌────────────▼─────────────┐
                    │ BasicComputerMoveStrategy│
                    │                          │
                    │ deterministic priority   │
                    └──────────────────────────┘

Strategy responsibilities:

inspect the current legal board/game state

select the next computer cell

return a legal CellIndex

remain deterministic

perform no game mutation

The strategy must not:

mutate Game

mutate the live Board

update MoveHistory

modify Undo/Memento state

publish domain events

update Scoreboard

call HTTP

access persistence

access Angular

call external AI/LLM services

4. Strategy Location

Determine the correct location for the Strategy based on the existing architecture and DDD documents.

Preferred direction, subject to verification:

backend/TicTacToe.Domain/
    Services/
        IComputerMoveStrategy.cs
        BasicComputerMoveStrategy.cs

If the existing architecture requires a different location, document the reason before implementation.

Do not introduce unnecessary abstraction.

5. Computer Player Rules

The computer is always:

Player X = Human
Player O = Computer

The strategy must use the following exact priority.

Priority 1 — Winning Move

If O can win immediately, choose the winning cell.

Example:

O | O | .
X | X | .
. | . | .

Select:

cell 2

Winning takes priority over blocking.

Priority 2 — Block X

If X can win on the next move, O must block X.

Example:

X | X | .
O | . | .
. | . | .

Select:

cell 2

If O has an immediate winning move at the same time, O's winning move has priority.

Priority 3 — Center

If there is no winning or blocking move:

center = cell 4

Select cell 4 if available.

Priority 4 — Corner

If center is unavailable, use deterministic corner ordering.

Use:

0, 2, 6, 8

Select the first available corner.

Do not use randomness.

Priority 5 — Any Available Cell

If no winning move, blocking move, center, or corner is available, select the first available cell using:

0, 1, 2, 3, 4, 5, 6, 7, 8

This is deliberately deterministic.

6. Determinism

Given exactly the same board state and player state, the strategy must always return the same cell.

Do not use:

Random

GUIDs

timestamps

external state

network calls

unordered iteration where ordering affects the result

Determinism provides:

reproducible tests

reproducible demonstrations

easier debugging

predictable behavior

simpler panel explanation

7. Cell Addressing

The project-wide cell representation is:

cellIndex = 0..8

Use the existing CellIndex value object where appropriate.

Do not introduce {row, column} or another addressing model.

Do not introduce a second cell-index representation.

8. Win Detection Reuse

Reuse the existing domain win-detection capability.

The project already has the canonical eight winning lines.

Do not duplicate the eight winning-line definitions in the Computer Strategy.

Maintain a single source of truth for Tic-Tac-Toe winning combinations.

If candidate simulation requires a helper, keep it local and pure without creating a competing win-rule implementation.

9. Candidate Evaluation

The strategy must evaluate candidate moves without permanently mutating the live Game.

Do not implement simulation by:

Game.MakeMove(...)
Undo(...)

Do not use the P006 Memento mechanism for strategy simulation.

Conceptually:

Current Board
    |
    +-- simulate O at candidate cell
    |       |
    |       +-- evaluate win
    |
    +-- next candidate

Simulation must not alter:

Game.Status
Game.CurrentPlayer
Game.MoveHistory
Game.Winner
Game.WinningCells
Undo stack

Use an appropriate pure board-state evaluation approach consistent with the existing domain model.

10. Legal Move Guarantee

The strategy must never return:

an occupied cell

a cell outside 0..8

an invalid CellIndex

When a legal move exists, the strategy must return one.

If no legal move exists, follow the existing domain convention for terminal/full-board state.

Do not invent a new game state.

11. Computer Mode Orchestration

P007 must implement the appropriate orchestration so that Computer Mode behaves as:

Human X move
      |
      v
Game accepts X move
      |
      +---- X wins? ----> STOP
      |
      +---- Draw? ------> STOP
      |
      v
Computer Strategy
      |
      v
Select O move
      |
      v
Game accepts O move
      |
      +---- O wins? ----> terminal
      |
      +---- Draw? ------> terminal
      |
      v
Return authoritative game state

The computer must not move after a terminal human move.

12. Memento Integration

P006 established the Memento mechanism and Computer Mode logical boundary.

Preserve the P006 behavior:

Computer Mode:

Human X move
    |
    +--> snapshot boundary
    |
Computer O move
    |
    +--> no second user-visible Undo boundary

After:

X human move
O computer move

one Undo must restore the state before the logical human+computer turn:

prior Board
prior CurrentPlayer
prior MoveHistory
prior Status
prior Winner
prior WinningCells

Do not redesign Memento merely to implement Strategy.

13. Terminal Precedence

X Wins

Human X move
    |
    v
X wins
    |
    v
NO computer move
    |
    v
Status = Won
Winner = X

Human Creates Draw

Human X move
    |
    v
Board becomes full
    |
    v
NO computer move
    |
    v
Status = Draw

O Wins

Human X move
    |
    v
No terminal state
    |
    v
Computer Strategy
    |
    v
O winning move
    |
    v
Status = Won
Winner = O

O Creates Draw

Human X move
    |
    v
No terminal state
    |
    v
Computer O move fills final cell
    |
    v
Status = Draw

14. Test Requirements

Create comprehensive strategy tests.

14.1 Winning Tests

Test:

O has one winning move.

O has multiple winning moves.

deterministic selection when multiple winning moves exist.

O winning move takes precedence over blocking X.

14.2 Blocking Tests

Test:

X has one immediate winning move.

X must be blocked.

multiple threats behave according to the documented deterministic algorithm.

14.3 Center Tests

Test:

no winning move

no blocking move

center available

strategy selects cell 4.

14.4 Corner Tests

Test:

center unavailable

corner available

strategy selects first available corner according to 0,2,6,8.

Test multiple corner availability and deterministic ordering.

14.5 Any-Available Tests

Test:

center unavailable

all corners unavailable

strategy selects first available cell according to 0..8.

14.6 Legality Tests

Verify:

occupied cells are never returned

returned cell is always 0..8

returned CellIndex is valid

strategy returns a move whenever a legal move exists.

14.7 Terminal Tests

Verify the chosen existing domain behavior when strategy is asked to select from:

Won game

Draw game

Full board

15. Computer Mode Integration Tests

Add appropriate integration/application/domain tests.

Basic Automatic Move

Verify:

Human X move
    |
    v
Computer O move

and:

two moves are present when the game remains active

X remains the human

O is the computer

turn returns to X

move history is correct

board state is correct.

X Win

Verify:

X wins

and:

Computer does not move

X Draw

Verify:

X produces draw

and:

Computer does not move

O Win

Verify computer's winning move produces:

Status = Won
Winner = O

O Draw

Verify computer's final move produces:

Status = Draw

Computer Undo

Verify one Undo after a completed human+computer logical turn restores:

state before human X move
CurrentPlayer = X
MoveHistory = prior history
Status = InProgress

16. Determinism Test

Run the same state through the strategy repeatedly.

For identical input:

same board
same player
same strategy

the result must always be the same CellIndex.

17. Domain Purity

Verify:

TicTacToe.Domain

has:

0 PackageReferences
0 ProjectReferences

and does not reference:

ASP.NET Core

HTTP

Entity Framework

database

Angular

JSON infrastructure

external AI/LLM services

18. Complexity

Document complexity accurately.

Maximum candidate cells:

9

Winning lines:

8

The board is fixed at nine cells.

Therefore:

Winning candidate evaluation:
    bounded by 9 candidates × 8 winning lines

Blocking candidate evaluation:
    bounded by 9 candidates × 8 winning lines

For fixed-size Tic-Tac-Toe, execution is effectively constant time with respect to board size.

Do not claim generic O(1) without explaining the fixed board-size assumption.

19. Traceability

Update:

docs/ai/REQUIREMENT-TRACEABILITY.md
docs/ai/ARCHITECTURE-TRACEABILITY.md
docs/ai/IMPLEMENTATION-LOG.md

Ensure the status accurately distinguishes:

Computer Strategy
    Implemented

Computer Mode
    Implemented at the appropriate domain/application boundary

FR-10 Computer Mode Undo
    Integrated and verified

API
    Deferred

UI
    Deferred

GameCompleted / Scoreboard
    Deferred to P008

Do not mark unrelated requirements as complete.

20. P007 Review Artifact

Create:

docs/ai/reviews/P007-review.md

The review must document:

Strategy Pattern roles

Strategy selection rationale

deterministic priority

winning move logic

blocking logic

center/corner/fallback logic

legal move guarantees

pure candidate evaluation

reuse of WinDetector

Computer Mode orchestration

Memento integration

terminal-state behavior

complexity

test evidence

domain boundary verification

remaining deferred requirements

design risks and mitigations

21. AI Audit Requirements

Record all AI-generated implementation work through the existing governance mechanism.

Update the appropriate files under:

docs/ai/

including:

AI-DECISIONS.md
IMPLEMENTATION-LOG.md
REQUIREMENT-TRACEABILITY.md
ARCHITECTURE-TRACEABILITY.md
MANUAL-CHANGES.md

Record:

prompt used

files inspected

files created

files modified

implementation decisions

test results

review findings

corrections, if any

final commit hash

Do not overwrite previous phase history.

22. No Unrelated Work

P007 must NOT implement:

GameCompleted domain event

Scoreboard

REST controllers

API endpoints

DTOs

persistence

database

Angular game UI

Angular state management

authentication

external AI/LLM calls

Those remain deferred to later phases.

23. Required Planning Output

Before writing application code, produce a P007 implementation plan containing:

A. Current Architecture

Where Strategy will live and why.

B. Strategy Design

Show the interface and concrete strategy responsibilities.

C. Orchestration Design

Show the exact human-X → terminal check → computer-O → terminal check flow.

D. Memento Integration

Explain how the P006 logical snapshot boundary remains intact.

E. Candidate Simulation

Explain how strategy evaluates candidate moves without mutating the aggregate.

F. Win Detection Reuse

Explain how the existing WinDetector is reused without duplicating winning lines.

G. Test Matrix

List all planned tests.

H. Files to Create

Provide exact paths.

I. Files to Modify

Provide exact paths.

J. Requirement Mapping

Map each change to the relevant FR/NFR/ADR.

K. Risks

Identify implementation and architectural risks.

L. Boundary Confirmation

Explicitly list what remains deferred.

24. Implementation Gate

Do not write application code during the first planning response.

Return the implementation plan only.

Do not commit anything during planning.

Wait for explicit approval before implementing P007.

25. Git Governance

After implementation is explicitly approved:

Run the complete relevant build.

Run all backend tests.

Run all frontend tests.

Inspect git status.

Inspect git diff.

Review all changed files.

Update audit and traceability artifacts.

Commit implementation changes.

Record the implementation commit hash in IMPLEMENTATION-LOG.md.

Create the documentation closure commit if required by established governance.

Verify the working tree is clean.

Do not create duplicate commits.

26. Final P007 Closure Criteria

P007 can be marked PASS only when:

[ ] Strategy Pattern explicitly implemented
[ ] Computer plays as O
[ ] Winning move priority implemented
[ ] Blocking priority implemented
[ ] Center priority implemented
[ ] Deterministic corner priority implemented
[ ] Deterministic fallback implemented
[ ] Strategy never returns occupied cell
[ ] Strategy does not mutate Game
[ ] Existing WinDetector reused
[ ] Computer does not move after X terminal state
[ ] O win handled correctly
[ ] O draw handled correctly
[ ] Computer Mode Undo restores one logical human+computer turn
[ ] P006 Memento behavior preserved
[ ] Domain remains dependency-free
[ ] Tests pass
[ ] Build passes
[ ] Traceability updated
[ ] P007 review created
[ ] Implementation log updated
[ ] Git history recorded
[ ] Working tree clean

Final report must end with:

P007 PASS — READY FOR P008

Do not implement P008.

Do not implement Scoreboard.

Do not implement REST API.

Do not implement Angular UI.

STOP after P007 closure.