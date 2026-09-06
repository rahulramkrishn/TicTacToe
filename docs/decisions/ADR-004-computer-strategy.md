# ADR-004 — Strategy Pattern for Computer Player

## Status
Accepted

## Context
The "Computer vs Player" mode requires the system to calculate AI moves automatically.

## Decision
Use the Strategy Pattern (`IComputerMoveStrategy`) to inject the computer move algorithm into the Game aggregate.

## Why
- Keeps the computer algorithm replaceable and testable independent of the aggregate.
- The Game aggregate's rules (turns, wins, empty cells) remain decoupled from the specific AI intelligence.
- Enables extending the game with difficulty levels (e.g., Random vs Minimax) in the future without modifying core logic.
