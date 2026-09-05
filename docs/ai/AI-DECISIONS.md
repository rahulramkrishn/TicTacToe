# Architectural & Design Decisions Register

This register documents all architectural, technical, and engineering decisions identified across the repository specifications, with particular focus on the frozen decisions that govern the implementation.

---

## 1. Frozen Decisions (Authoritative Baseline)

These decisions are committed and must not be reopened by the AI IDE or developers without explicit human ADR revision:

| Decision Key | Topic | Frozen Decision | Source Document | Implementation Impact |
|---|---|---|---|---|
| **FD-01** | Cell Addressing | Canonical integer `cellIndex: 0..8`. Row/Col is derived solely in the frontend for presentation. | `docs/01-requirements.md`, `docs/06-api-contract.md`, `docs/13-assumptions.md` (A-001) | Eliminates dual API schema complexity; single validation invariant (`0 <= index <= 8`). |
| **FD-02** | Reset Game Identity | Reset Game **reuses existing `gameId`**, clearing board and history without reissuing a new identifier. | `docs/01-requirements.md`, `docs/13-assumptions.md` (A-002) | Stable URL / session identity; frontend does not re-navigate or change session handle. |
| **FD-03** | Undo Policy | **Option A**: Undo is disabled once a game reaches a terminal state (`Won` or `Draw`). | `docs/10-adr-template-and-initial-decisions.md` (ADR-004), `docs/13-assumptions.md` (A-003) | Scoreboard values remain final; avoids complex rollback/compensation transactions for scores. |
| **FD-04** | Undo Pattern | **Memento Pattern** using snapshot restore rather than heuristic reversal. | `docs/04-ddd-and-domain-model.md`, `docs/13-assumptions.md` (A-004) | Pure deterministic state restore; 1 snapshot restored for TwoPlayer, human/computer pair for Computer mode. |
| **FD-05** | Computer AI Pattern | **Strategy Pattern** via `IComputerMoveStrategy`. | `docs/04-ddd-and-domain-model.md`, `docs/10-adr-template-and-initial-decisions.md` (ADR-005) | Decouples move selection algorithm from `Game` aggregate; easily mockable and testable. |
| **FD-06** | Scoreboard Trigger | **Mandatory `GameCompleted` domain event**, raised exactly once upon terminal transition. | `docs/ADR-007-game-completed-domain-event.md`, `docs/13-assumptions.md` (A-006) | Scoreboard updates decoupled from Game aggregate; prevents duplicate score increments. |
| **FD-07** | Event Dispatch | **Synchronous in-process event delivery** for domain events. | `docs/ADR-007-game-completed-domain-event.md`, `docs/13-assumptions.md` (A-007) | Zero external broker overhead (no Kafka/RabbitMQ); lightweight and fully testable in-memory. |
| **FD-08** | Storage Mechanism | **In-memory storage** selected for the local assessment application. | `docs/02-prerequisites-and-environment.md`, `docs/10-adr-template-and-initial-decisions.md` (ADR-002) | Zero database prerequisites; fast startup; simplifies panel review while keeping interface extensible. |

---

## 2. Comprehensive Decisions Matrix

| Decision Category | Decision | Source | Status | Must / Should | Implementation Impact |
|---|---|---|---|---|---|
| **Architecture** | Pragmatic DDD layered clean architecture | `docs/04-ddd-and-domain-model.md`, `docs/05-architecture.md`, ADR-003 | Accepted | Must | Domain layer is isolated with zero HTTP/infrastructure dependencies. |
| **Architecture** | Single bounded context; no microservices | `docs/04-ddd-and-domain-model.md`, ADR-006 | Accepted | Must | Single modular monolith avoiding distributed systems complexity. |
| **Domain** | `Game` is the Aggregate Root owning board, turn, history, status | `docs/04-ddd-and-domain-model.md` | Accepted | Must | All game state mutations guarded by aggregate invariants. |
| **Domain** | Pure domain service for win/draw detection (`WinDetector`) | `docs/04-ddd-and-domain-model.md` | Accepted | Should | Eliminates algorithmic clutter from the aggregate root. |
| **Domain** | Value objects for `CellIndex`, `Player`, `GameMode`, `GameStatus` | `docs/04-ddd-and-domain-model.md` | Accepted | Should | Centralizes validation rules and avoids primitive obsession. |
| **API** | Resource-oriented RESTful API returning full `GameStateDto` on mutation | `docs/06-api-contract.md` | Accepted | Must | Keeps frontend synchronized with minimal round trips. |
| **API** | ProblemDetails (RFC 7807) error contract with domain conflict codes | `docs/06-api-contract.md`, `docs/03-nfr.md` | Accepted | Must | Consistent client error handling and HTTP status mappings (400, 404, 409). |
| **Backend** | In-memory thread-safe state store (`ConcurrentDictionary`) | `docs/02-prerequisites-and-environment.md`, ADR-002 | Accepted | Must | Safe for concurrent HTTP requests without database requirements. |
| **Backend** | Backend executes computer move synchronously inside move use case | `docs/05-architecture.md`, `docs/06-api-contract.md` | Accepted | Must | Frontend makes 1 API call; backend performs human move and responds with computer move applied. |
| **Frontend** | Angular component hierarchy with dedicated facade/service layer | `docs/05-architecture.md` | Accepted | Must | Clean separation between presentation components and API HTTP communication. |
| **Frontend** | Lightweight state management (Signals or RxJS service); no NgRx | `docs/05-architecture.md` | Accepted | Should | Avoids boilerplate and over-engineering for a localized state tree. |
| **Frontend** | Derive Row/Col strictly inside presentation templates/formatters | `docs/06-api-contract.md` | Accepted | Must | Never exposes or transmits row/col to backend. |
| **Testing** | Testing pyramid prioritizing domain unit and API integration tests | `docs/07-test-strategy.md` | Accepted | Must | High-value, deterministic test coverage targeting 90%+ domain logic. |
| **Testing** | Deterministic computer strategy corner evaluation `[0, 2, 6, 8]` | `docs/05-architecture.md`, ADR-005 | Accepted | Must | Enables reproducible, deterministic unit tests for AI play. |
| **Security** | Restrict CORS to configured frontend origin (`http://localhost:4200`) | `docs/02-prerequisites-and-environment.md`, `docs/03-nfr.md` | Accepted | Must | Prevents arbitrary origin access; avoids insecure `AllowAnyOrigin()`. |
| **Security** | Server-side validation on all inputs; zero trust in frontend state | `docs/03-nfr.md`, `docs/05-architecture.md` | Accepted | Must | Prevents manipulated moves, turn spoofing, or invalid coordinates. |
| **Observability** | Structured logging with `TraceId`, `GameId`, `Player`, `Operation` | `docs/03-nfr.md`, `docs/05-architecture.md` | Accepted | Should | Facilitates production-grade diagnostic capabilities. |
| **AI Development** | Auditable traceability for every AI-generated/modified artifact | `docs/08-ai-development-governance.md`, `docs/AI-CHANGE-AUDIT.md` | Accepted | Must | Proof of disciplined engineering process during assessment evaluation. |
| **Deployment** | Simple two-command run process (`dotnet run` + `npm start`) | `docs/02-prerequisites-and-environment.md`, `docs/03-nfr.md` | Accepted | Must | Flawless local evaluation experience for the interview panel. |
