# 01 — Requirements

## 1. Source of Truth

The assignment requires a browser-based Tic Tac Toe application with an Angular frontend and local .NET REST backend. The backend owns game session state, move history, game status and scoreboard. The application must support Two Player and Computer modes, move history, undo, scoreboard, reset operations, and documented APIs. Tests are expected for core game logic and state transitions.

## 2. Frozen Architecture Decisions

These decisions are now **committed** and must not be reopened by an AI IDE unless a human explicitly changes an ADR:

1. **Cell addressing:** `cellIndex`, integer `0..8`, everywhere in the API and DTOs.
2. **Reset Game:** reuse the current `gameId`; reset the logical game state without creating a new identifier.
3. **Undo:** Option A — disable undo after a game is Won or Drawn.
4. **Undo implementation:** Memento pattern using snapshot-based restore, not derived reversal.
5. **Computer move selection:** Strategy pattern.
6. **Scoreboard completion:** `GameCompleted` domain event, raised exactly once per game completion.
7. **Event processing:** synchronous/in-process for this assessment; no broker required.
8. **Storage:** in-memory is acceptable and selected for this assessment.

## 3. Functional Requirements

### FR-01 — Create Game

Create a game session with:

- unique `gameId`
- empty 3x3 board
- X as current player
- selected mode
- `InProgress` status
- empty move history
- unchanged scoreboard

### FR-02 — Board

Nine cells are represented as:

```text
0 1 2
3 4 5
6 7 8
```

Only empty cells are clickable/playable.

A selected cell remains occupied for the current game state.

### FR-03 — Turns

- Players are X and O.
- Current player must be visible.
- Valid moves alternate turns.
- Invalid moves do not change the turn.

### FR-04 — Win Detection

Detect:

- any row
- any column
- either diagonal

On win:

- expose winner
- expose winning cell indices
- prevent further moves
- update scoreboard exactly once

### FR-05 — Draw Detection

If all nine cells are filled without a winner:

- status becomes `Draw`
- show draw message
- prevent further moves
- update scoreboard exactly once

### FR-06 — Move Validation

Backend rejects:

- `cellIndex < 0`
- `cellIndex > 8`
- occupied cell
- move after completion
- wrong player

Invalid commands must not mutate game state.

### FR-07 — Move History

Each move contains:

- move number
- player
- `cellIndex`

Example:

```json
{
  "moveNumber": 1,
  "player": "X",
  "cellIndex": 0
}
```

The UI may display `Row 1, Column 1`, derived from index 0. Row/column is **not** an alternative API representation.

### FR-08 — Undo Availability

Undo is disabled when there are no moves.

Under Option A, undo is also disabled after completion.

### FR-09 — Two Player Undo

For:

```text
X plays
O plays
Undo
```

remove only O's move and restore O as the current player.

### FR-10 — Computer Mode Undo

For:

```text
X plays
O computer plays
Undo
```

remove both moves and restore X's turn.

### FR-11 — Scoreboard

Track:

- X wins
- O wins
- draws

Scoreboard is session-level and served by backend.

It must update only once for a completed game.

### FR-12 — Reset Game

Reset:

- board
- move history
- winner
- draw status
- current player to X

It must:

- **reuse the existing `gameId`**
- preserve scoreboard

### FR-13 — Reset Scoreboard

Separate operation resets all scoreboard counters without changing the current game's board/state.

### FR-14 — Computer Mode

Modes:

1. Two Player
2. Play Against Computer

In Computer Mode:

- human = X
- computer = O
- computer moves automatically after valid human move
- computer makes only valid moves
- computer does not move after completion

Priority:

1. O winning move
2. block X winning move
3. center
4. corner
5. any available cell

### FR-15 — Backend API

REST API must expose operations equivalent to:

```text
POST /api/games
GET  /api/games/{id}
POST /api/games/{id}/moves
POST /api/games/{id}/undo
POST /api/games/{id}/reset
GET  /api/scoreboard
POST /api/scoreboard/reset
```

Exact endpoint names may vary if documented.

### FR-16 — Backend Ownership

Backend is authoritative for:

- game state
- rules
- move validation
- turn
- status
- history
- scoreboard

Frontend does not independently decide game outcomes.

### FR-17 — Frontend

UI must show:

- board
- current player
- selected mode
- winner/draw
- winning cells
- history
- scoreboard
- Reset Game
- Undo Last Move
- Reset Scoreboard

Frontend renders the latest authoritative state returned by backend.

### FR-18 — Testing

At minimum:

- valid move
- invalid move
- turn switching
- row win
- column win
- diagonal win
- draw
- reset
- two-player undo
- computer-mode undo
- scoreboard
- computer strategy
- move after completion

Backend tests are preferred for rules/state transitions.

### FR-19 — Documentation / AI Governance

Repository must explain:

- project overview
- stack
- features
- backend run instructions
- frontend run instructions
- API summary
- test instructions
- AI tools/prompts
- design decisions
- assumptions
- limitations
- future improvements

AI-assisted development must be explainable: prompts, generated output, human changes, review points, assumptions and trade-offs must be recorded.

## 4. Acceptance Gate

Do not consider implementation complete until every FR has:

```text
Requirement → implementation → test → evidence → commit
```
