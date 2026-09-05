# 04 — DDD and Domain Model

## 1. DDD Position

Use **pragmatic DDD**, not ceremony.

The game rules are the domain. Controllers, Angular components and storage are infrastructure/application concerns.

The core model should remain testable without HTTP, Angular or persistence.

## 2. Bounded Context

For this assignment, one bounded context is sufficient:

```text
TicTacToe Game Management
```

Potential conceptual subdomains:

```text
Game
Scoreboard
Computer Strategy
```

Do not split these into microservices. The assignment is a small local application and explicitly permits in-memory storage.

## 3. Aggregate

### Game Aggregate

`Game` is the aggregate root.

It owns:

- GameId
- Board
- CurrentPlayer
- GameMode
- GameStatus
- Winner
- WinningCells
- MoveHistory
- undo snapshots

All game mutations go through the aggregate/application command boundary.

## 4. Value Objects

Recommended:

- `GameId`
- `CellIndex`
- `Player`
- `GameMode`
- `GameStatus`
- `Move`
- `Scoreboard`

`CellIndex` is especially useful because it centralizes the invariant:

```text
0 <= value <= 8
```

## 5. Domain Invariants

The aggregate must guarantee:

- exactly nine cells
- only X/O/empty values
- moves occur only for current player
- occupied cells cannot be overwritten
- completed games cannot receive moves
- terminal result is immutable until reset
- winning cells correspond to the detected winning line
- move history matches successful moves
- completion occurs once
- `GameCompleted` occurs once

## 6. Memento Pattern — Undo

Undo uses the **Memento pattern**.

Before a successful logical move boundary, capture a snapshot containing all state required to restore the previous valid state:

```text
Board
CurrentPlayer
Status
Winner
WinningCells
MoveHistory
```

Do not implement undo by trying to reverse individual effects heuristically.

### Mode-specific memento boundaries

Two Player:

```text
[X move] → snapshot → [O move] → Undo → restore snapshot
```

Computer:

```text
[snapshot] → X move → O computer move → Undo → restore snapshot
```

The Computer Mode undo snapshot therefore represents the state before the human/computer pair.

## 7. Strategy Pattern — Computer Player

Computer move selection uses the **Strategy pattern**.

Conceptual interface:

```csharp
public interface IComputerMoveStrategy
{
    int SelectMove(GameState state);
}
```

Current implementation:

```text
1. Winning O move
2. Blocking X move
3. Center
4. Corner
5. Any available cell
```

The `Game` aggregate should not contain UI or HTTP logic.

Strategy selection must be deterministic where multiple choices have equal priority. Document the chosen corner/cell ordering.

## 8. Domain Service

A `WinDetector`/`BoardEvaluator` domain service may evaluate:

- row
- column
- diagonal
- draw
- winning cells

Keep the rule deterministic and pure.

## 9. Domain Event — Firm Decision

### `GameCompleted`

This is **not optional**.

The aggregate raises:

```text
GameCompleted
```

when it makes the first transition:

```text
InProgress → Won
```

or:

```text
InProgress → Draw
```

Payload:

```text
GameId
Result
Winner (nullable)
```

The application layer handles the event and updates the session scoreboard.

## 10. Exactly-Once Completion

The aggregate must prevent:

```text
completed game
    ↓
another completion event
    ↓
second scoreboard increment
```

The event can only be created during the state transition that first completes the game.

GET, reset, UI refresh and repeated reads must never generate it.

## 11. Event Handling

For this assessment:

```text
Game aggregate
   |
   | raises GameCompleted
   v
In-process event dispatcher
   |
   v
Scoreboard handler
```

No RabbitMQ, Kafka, SNS/SQS or Service Bus is required.

### Production evolution

If state becomes durable/distributed, use an outbox or equivalent transactional event publication mechanism so completion cannot be lost between persistence and event publication.

## 12. Reset Semantics

Reset Game:

- preserves GameId
- clears board/history/result
- restores X turn
- does not change scoreboard

The aggregate is reset to a fresh logical game state under the same identity.

## 13. Scoreboard Boundary

The scoreboard is session-level state rather than part of the Game aggregate.

This avoids coupling game lifecycle state to a global/session counter.

The `GameCompleted` event is the integration point.

## 14. Suggested Application Flow

```text
Controller
   ↓
Application Command
   ↓
Game Repository
   ↓
Game Aggregate
   ↓
Domain Rules
   ↓
Domain Event: GameCompleted
   ↓
Scoreboard Handler
   ↓
DTO Mapper
   ↓
HTTP Response
```

## 15. Principal-Level Discussion

Why DDD here?

Because the exercise is intentionally small, but the business rules are non-trivial enough to benefit from explicit invariants.

The architecture demonstrates:

- separation of domain rules
- aggregate ownership
- explicit commands
- domain events
- replaceable strategy
- snapshot-based undo
- testable business logic

At the same time, it avoids over-engineering with distributed services, brokers and persistence infrastructure that the assignment does not require.
