# Architecture and Design

## 1. Recommended Architecture

Use a pragmatic layered architecture with DDD-inspired domain isolation.

```text
┌───────────────────────────────────────┐
│             Angular UI                │
│ Board | History | Score | Controls    │
└───────────────────┬───────────────────┘
                    │ REST/JSON
┌───────────────────▼───────────────────┐
│          ASP.NET Core API             │
│ Controllers | Validation | Errors     │
└───────────────────┬───────────────────┘
                    │
┌───────────────────▼───────────────────┐
│          Application Layer            │
│ Use cases | orchestration | DTO map   │
└───────────────────┬───────────────────┘
                    │
┌───────────────────▼───────────────────┐
│              Domain                   │
│ Game | Rules | Undo | Strategy        │
└───────────────────┬───────────────────┘
                    │
┌───────────────────▼───────────────────┐
│          Infrastructure               │
│ In-memory repositories                │
└───────────────────────────────────────┘
```

## 2. Why This Architecture

It demonstrates principal-level engineering judgment without overengineering.

It gives clear answers to:

- Where do rules live?
- Where does state live?
- How is persistence isolated?
- How are APIs kept thin?
- How is computer logic tested?
- How could storage evolve?

## 3. Frontend Architecture

Suggested:

```text
AppShell
 ├── GamePage
 │    ├── GameBoard
 │    ├── GameStatus
 │    ├── MoveHistory
 │    └── GameControls
 └── Scoreboard
```

Services:

```text
GameApiService
ScoreboardApiService
GameFacade / GameStateService
```

The facade coordinates:

- Loading game.
- Sending moves.
- Undo.
- Reset.
- Mode selection.
- Mapping API state into UI state.

## 4. State Management

Do not introduce NgRx solely because the job description mentions state management.

For a small application, Angular signals or a lightweight service/facade is enough.

Use a stronger state-management library only if there is a demonstrated need.

## 5. Computer Move Flow

Recommended sequence:

```text
Human clicks cell
      |
      v
POST /games/{id}/moves
      |
      v
Backend validates X move
      |
      +--> X wins? complete
      |
      +--> Draw? complete
      |
      v
Computer strategy selects O move
      |
      v
Backend validates O move
      |
      +--> O wins? complete
      |
      +--> Draw? complete
      |
      v
Return final GameState
```

A key design point:

**The frontend should not independently calculate the computer move.**

The backend owns game behavior.

## 6. Computer Strategy Algorithm

Pseudo-logic:

```text
available = empty cells

if O has immediate winning move:
    return it

if X has immediate winning move:
    return it

if center empty:
    return center

if any corner empty:
    return first deterministic corner

return first available cell
```

Make corner ordering deterministic, for example:

```text
0, 2, 6, 8
```

This improves reproducible tests and panel demonstrations.

## 7. Undo Design

Recommended choice:

**Option A — disable Undo after completion.**

State transition:

```text
Before:
X O X
. O .
. . .

Undo
```

Restore the exact prior snapshot logically by removing the required move(s).

Do not mutate scoreboard on undo because undo is disabled after completion.

## 8. Concurrency

The assignment is local and does not explicitly require concurrency.

Still, define the production concern:

Two requests could theoretically attempt moves simultaneously.

Production design:

- Load aggregate.
- Validate and mutate atomically.
- Persist with optimistic concurrency/version.
- Reject stale writes.

Do not implement distributed locking for the assessment unless needed.

## 9. Persistence Evolution

Current:

```text
In-memory repository
```

Possible future:

```text
SQLite
```

Production:

```text
PostgreSQL / SQL Server / distributed data store
```

The repository interface protects the domain/application layers from this decision.

## 10. API Design

Prefer resource-oriented REST:

```text
POST /api/games
GET  /api/games/{gameId}
POST /api/games/{gameId}/moves
POST /api/games/{gameId}/undo
POST /api/games/{gameId}/reset

GET  /api/scoreboard
POST /api/scoreboard/reset
```

OpenAPI should document:

- Requests.
- Responses.
- Errors.
- Examples.

## 11. API Idempotency

Read operations are naturally idempotent.

Command endpoints are state transitions.

For this assignment, duplicate move submissions should be rejected by business validation rather than silently applied twice.

## 12. HTTP and Domain Separation

HTTP:

```text
409 Conflict
```

is an API representation of a domain conflict such as:

```text
Cell occupied
Wrong turn
Game completed
```

The domain should not depend on HTTP status codes.

## 13. Error Handling

Use centralized exception/problem handling.

Map domain errors:

```text
GameNotFound
InvalidMove
WrongTurn
GameCompleted
NothingToUndo
```

to consistent HTTP responses.

## 14. Observability

Use structured logging.

Example event:

```text
GameMoveAccepted
GameId=...
Player=X
Position=4
DurationMs=...
```

Avoid logging every frontend rendering event.

## 15. Security Architecture

Even though local:

```text
Browser
   |
   | restricted CORS
   v
API
   |
   | validate
   v
Domain
```

Never allow the browser to dictate:

- Winner.
- Scoreboard.
- Current player.
- Game status.

## 16. Job Description Alignment

| Job expectation | Assignment demonstration |
|---|---|
| Angular/React | Angular UI |
| .NET/C# | ASP.NET Core API |
| REST | Game/scoreboard endpoints |
| SOLID | Layered/domain design |
| High performance | Lightweight deterministic rules |
| Testing | Domain + API + frontend tests |
| Security/OWASP | Server validation, error handling, CORS |
| AI-assisted SDLC | Prompt/change audit |
| Architecture | ADRs and DDD model |
| Reliability | Controlled state transitions |
| Cloud/DevOps | Document production evolution without unnecessary deployment complexity |
| Industrial systems | Discuss backend-as-source-of-truth and deterministic state transitions as foundational patterns |
