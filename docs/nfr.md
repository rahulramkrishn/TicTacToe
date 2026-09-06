# Non-Functional Requirements

The assignment does not provide numeric NFR targets. The following NFRs are therefore proposed engineering targets for a high-quality Principal Engineer submission. Proposed targets must be labeled as engineering decisions, not as ABB assignment requirements.

## NFR-01 — Correctness

**Priority: Critical**

- Backend is authoritative.
- Every state transition is deterministic.
- No invalid state can be created through the API.
- Scoreboard increments exactly once.
- Undo produces a valid previous state.
- Computer strategy follows the specified priority.

## NFR-02 — Testability

**Priority: Critical**

- Domain rules should be executable without HTTP.
- Computer strategy should be unit-testable independently.
- State transitions should have deterministic tests.
- Controllers should require minimal mocking.

Suggested target:

- 90%+ line/branch coverage for core domain logic where practical.
- 100% coverage of critical game-rule paths is a stronger target.

Coverage percentage alone is not acceptance; meaningful scenario coverage is required.

## NFR-03 — Maintainability

**Priority: High**

- SOLID principles.
- Single responsibility.
- Clear naming.
- Small cohesive services.
- No duplicated rule logic.
- DTO/domain separation.
- Avoid speculative abstractions.

## NFR-04 — Performance

**Priority: Medium**

For a 3x3 game, computational load is trivial.

Expected:

- Normal API calls should return quickly on a local machine.
- Computer move selection should be effectively instantaneous.
- No blocking delays.
- No unnecessary database or network calls.

Production-scale performance would require explicit load testing.

## NFR-05 — Availability and Reliability

**Priority: High**

For this local assignment:

- Backend should fail predictably.
- Invalid requests must not corrupt state.
- Exceptions should be converted to safe API errors.
- State mutations should be atomic from the application's perspective.

Production interpretation:

- Durable/shared state.
- Multiple API instances.
- Distributed locking/concurrency strategy.
- Health checks.
- Graceful shutdown.
- Observability.

## NFR-06 — Security

**Priority: High**

Apply OWASP-aware practices:

- Validate all input server-side.
- Do not trust client-provided turn/state.
- Avoid exposing stack traces.
- Use structured errors.
- Avoid secrets in source control.
- Restrict CORS.
- Validate identifiers and positions.
- Avoid unsafe dynamic SQL if persistence is added.
- Keep dependencies updated.

## NFR-07 — API Consistency

**Priority: High**

All APIs should consistently provide:

- HTTP status.
- JSON response.
- Validation errors.
- Business error codes.
- Trace/correlation identifier where appropriate.

Recommended statuses:

- `200` successful query/action.
- `201` game created.
- `400` malformed/invalid request.
- `404` game not found.
- `409` business conflict such as wrong turn or occupied cell.
- `500` unexpected server error.

## NFR-08 — Observability

**Priority: Medium**

Provide structured logs for:

- Game creation.
- Move submission.
- Invalid move.
- Undo.
- Game completion.
- Scoreboard update.
- Unexpected errors.

Never log secrets.

Recommended correlation:

```text
TraceId
GameId
Operation
Outcome
Duration
```

## NFR-09 — Usability

**Priority: High**

The UI should:

- Clearly show whose turn it is.
- Make disabled/completed cells obvious.
- Highlight winning cells.
- Clearly distinguish game reset from scoreboard reset.
- Clearly show undo availability.
- Give understandable error feedback.
- Work comfortably on a laptop browser.

## NFR-10 — Accessibility

**Priority: Medium**

Recommended:

- Keyboard-accessible cells.
- Semantic buttons.
- Visible focus state.
- Accessible labels such as "Row 1 Column 1".
- Status messages accessible to assistive technologies.
- Do not use color alone to indicate winning cells.

## NFR-11 — Responsive UI

**Priority: Medium**

Minimum target:

- Laptop browser.
- Common desktop widths.
- No horizontal overflow under normal use.

Mobile support is a useful enhancement but is not an explicit acceptance requirement.

## NFR-12 — Deployment Simplicity

**Priority: High**

Panel should be able to:

1. Clone repository.
2. Start backend.
3. Start frontend.
4. Open browser.
5. Play.

The README should provide copy/paste commands.

## NFR-13 — Reproducibility

**Priority: Critical**

Document:

- SDK/runtime versions.
- Node version.
- Angular version.
- Test commands.
- Environment configuration.
- Startup commands.

Prefer version pinning where practical.

## NFR-14 — AI Development Transparency

**Priority: Critical for panel review**

Every significant change must be attributable to:

- AI generated.
- AI generated then modified.
- Human authored.
- Human reviewed/approved.

Each change should link to a commit.

## NFR-15 — Technical Debt

Track known debt explicitly.

Example:

```text
TD-001: In-memory store is not multi-instance safe.
TD-002: No authentication because assignment does not require it.
TD-003: No WebSocket because assignment requires REST.
```

This demonstrates conscious trade-offs rather than accidental omissions.

## NFR-16 — Scalability Discussion

Do not implement production complexity unnecessarily, but document evolution:

```text
Current:
Angular -> REST API -> In-memory state

Production evolution:
Angular -> API Gateway/BFF -> Game service
                         -> distributed state store
                         -> observability
                         -> authentication
```

For this assessment, architectural restraint is preferable to introducing Kubernetes, microservices, or cloud infrastructure solely for appearance.
