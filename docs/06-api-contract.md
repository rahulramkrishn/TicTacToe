# 06 — API Contract

## 1. Canonical Cell Addressing

**Decision: use `cellIndex` only.**

Valid range:

```text
0..8
```

Canonical board:

```text
0 1 2
3 4 5
6 7 8
```

Row/column is a presentation concern.

Frontend derivation:

```typescript
row = Math.floor(cellIndex / 3);
column = cellIndex % 3;
```

The backend must not accept both representations.

## 2. Create Game

### Request

```http
POST /api/games
Content-Type: application/json
```

```json
{
  "mode": "TwoPlayer"
}
```

### Response

```json
{
  "gameId": "guid",
  "board": [null, null, null, null, null, null, null, null, null],
  "currentPlayer": "X",
  "mode": "TwoPlayer",
  "status": "InProgress",
  "winner": null,
  "winningCells": [],
  "moveHistory": [],
  "scoreboard": {
    "xWins": 0,
    "oWins": 0,
    "draws": 0
  }
}
```

## 3. Get Game

```http
GET /api/games/{gameId}
```

Returns authoritative current state.

GET must not mutate state or scoreboard.

## 4. Make Move

```http
POST /api/games/{gameId}/moves
Content-Type: application/json
```

Canonical request:

```json
{
  "player": "X",
  "cellIndex": 4
}
```

Do **not** accept:

```json
{
  "row": 1,
  "column": 1
}
```

and do not provide a second alternative request DTO.

### Backend Validation

Reject:

- unknown game
- index outside 0..8
- occupied cell
- wrong player
- completed game

### Successful Response

Return the complete authoritative `GameState`.

## 5. Computer Move

The preferred API design is for the same move application service to execute the computer move after a valid X move in Computer Mode.

The exact HTTP orchestration can remain an implementation detail, but the backend must remain authoritative and must ensure O's move is valid and does not occur after completion.

## 6. Undo

```http
POST /api/games/{gameId}/undo
```

Behavior:

- Two Player: restore the state before the most recent move.
- Computer Mode: restore the state before the latest human/computer pair.
- No moves: reject with a stable business error.
- Completed game: reject because Option A disables undo.

## 7. Reset Game

```http
POST /api/games/{gameId}/reset
```

**Important: the same `gameId` is returned.**

Example:

```text
Before reset:
gameId = 7d...

After reset:
gameId = 7d...
board = empty
currentPlayer = X
status = InProgress
history = []
scoreboard = unchanged
```

Reset is a state reset, not a new session identity.

## 8. Scoreboard

```http
GET /api/scoreboard
POST /api/scoreboard/reset
```

Scoreboard updates are triggered by `GameCompleted`, not by GET operations.

## 9. Canonical DTOs

### MakeMoveRequest

```csharp
public sealed record MakeMoveRequest(
    Player Player,
    int CellIndex);
```

### MoveDto

```csharp
public sealed record MoveDto(
    int MoveNumber,
    Player Player,
    int CellIndex);
```

### GameStateDto

```csharp
public sealed record GameStateDto(
    Guid GameId,
    IReadOnlyList<Player?> Board,
    Player CurrentPlayer,
    GameMode Mode,
    GameStatus Status,
    Player? Winner,
    IReadOnlyList<int> WinningCells,
    IReadOnlyList<MoveDto> MoveHistory,
    ScoreboardDto Scoreboard);
```

### TypeScript

```typescript
export interface MakeMoveRequest {
  player: Player;
  cellIndex: number;
}

export interface MoveDto {
  moveNumber: number;
  player: Player;
  cellIndex: number;
}

export interface GameState {
  gameId: string;
  board: Array<Player | null>;
  currentPlayer: Player;
  mode: GameMode;
  status: GameStatus;
  winner: Player | null;
  winningCells: number[];
  moveHistory: MoveDto[];
  scoreboard: Scoreboard;
}
```

## 10. Error Contract

Use a consistent ProblemDetails-style response.

Example:

```json
{
  "type": "https://example/errors/cell-occupied",
  "title": "Invalid move",
  "status": 409,
  "detail": "Cell 4 is already occupied.",
  "code": "CELL_OCCUPIED",
  "traceId": "..."
}
```

The exact status mapping should be documented in the implementation.

## 11. API Invariants

1. Client cannot supply authoritative board state.
2. Client cannot force current player.
3. Client cannot force winner/status.
4. Client cannot mutate scoreboard through game commands.
5. GET has no side effects.
6. `cellIndex` is the sole cell-addressing contract.
7. Every successful command returns sufficient state for UI rendering.
