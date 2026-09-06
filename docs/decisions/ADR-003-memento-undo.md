# ADR-003 — Memento Pattern for Undo

## Status
Accepted

## Context
The application must support an "Undo" feature for moves, allowing the board to revert to its state before the previous turn.

## Decision
Use the Memento Pattern to snapshot the Game aggregate's state before a move is executed. When undoing, pop the Memento from history and restore the aggregate's internal state.

## Why
- Undo requires precise restoration of the previous aggregate state (Board, Status, Winner, CurrentPlayer).
- Trying to algorithmically reverse a move is fragile and error-prone, especially with win/draw evaluations.
- Memento encapsulates the state snapshot securely without breaking aggregate invariants.
