# Implementation Plan for an AI IDE

## Phase 0 — Prepare

1. Initialize Git.
2. Create frontend/backend.
3. Add docs.
4. Create requirement IDs.
5. Create traceability matrix.
6. Create AI audit files.
7. Create initial ADRs.
8. Verify build/test commands.

## Phase 1 — Backend Domain

Prompt AI to implement only:

- Player.
- Position.
- GameMode.
- GameStatus.
- Move.
- Board.
- Game aggregate.

Then manually review.

### Exit criteria

- Domain compiles.
- No HTTP dependencies.
- Unit tests pass.

## Phase 2 — Game Rules

Implement:

- Valid move.
- Turn switching.
- Win detection.
- Draw detection.
- Completion lock.

Add tests before moving forward.

## Phase 3 — Undo

Implement:

- Two-player one-move undo.
- Computer pair undo.
- Recalculation.
- No undo after completion.

Test all state transitions.

## Phase 4 — Computer Strategy

Implement strategy as isolated policy.

Test all five priority rules.

## Phase 5 — Scoreboard

Implement:

- Increment X.
- Increment O.
- Increment Draw.
- Exactly-once completion.
- Reset.

## Phase 6 — API

Implement:

- Create.
- Get.
- Move.
- Undo.
- Reset.
- Scoreboard.
- Scoreboard reset.

Add integration tests.

## Phase 7 — Angular

Implement:

- Board.
- Turn.
- Mode selector.
- Winner/draw.
- Winning cells.
- History.
- Scoreboard.
- Controls.
- Error handling.

Keep UI dumb about game rules.

## Phase 8 — Integration

Verify:

```text
Angular -> REST -> Backend -> Domain
```

Do not allow frontend-only state transitions.

## Phase 9 — UX and Quality

Review:

- Accessibility.
- Keyboard use.
- Error messages.
- Loading states.
- Disabled controls.
- Responsive laptop layout.

## Phase 10 — Security Review

Check:

- Server validation.
- CORS.
- Error leakage.
- Input validation.
- Dependencies.
- Secrets.

## Phase 11 — Test Hardening

Run:

- Unit.
- Integration.
- Frontend.
- E2E if included.

Fix every defect with a regression test.

## Phase 12 — AI Audit

Ensure:

- AI prompts recorded.
- AI changes recorded.
- Manual changes recorded.
- Commits are meaningful.
- Requirement traceability complete.

## Phase 13 — Panel Readiness

Prepare a 10-minute architecture walkthrough:

1. Requirements.
2. Architecture.
3. Domain model.
4. API.
5. Computer strategy.
6. Undo.
7. Testing.
8. AI workflow.
9. Trade-offs.
10. Production evolution.

## AI IDE Operating Rule

Do not ask the AI to build the entire system in one prompt.

Use incremental prompts tied to requirements.

Each prompt should produce:

- Code.
- Tests.
- Explanation.
- Files changed.
- Assumptions.

Then commit.

## Suggested Prompt Sequence

### Prompt 1 — Architecture

Read `docs/01-requirements.md`, `docs/04-ddd-and-domain-model.md`, and `docs/05-architecture.md`.

Propose the solution structure. Do not write implementation code yet. Identify risks and ambiguities.

### Prompt 2 — Domain

Implement FR-02 through FR-06 in the domain layer only. Add comprehensive unit tests.

### Prompt 3 — Undo

Implement FR-08 and FR-09. Do not alter scoreboard behavior. Add tests for every specified scenario.

### Prompt 4 — Computer

Implement FR-11 as a deterministic strategy. Add tests for each priority.

### Prompt 5 — Scoreboard

Implement FR-10 and FR-13. Prove exactly-once update behavior with tests.

### Prompt 6 — API

Implement FR-01, FR-14, FR-15, FR-16. Keep controllers thin.

### Prompt 7 — Angular

Implement FR-17. The UI must render backend state and must not implement authoritative game rules.

### Prompt 8 — Review

Review the entire implementation against `docs/09-traceability-matrix.md`. Identify missing requirements and tests. Do not modify code yet.

### Prompt 9 — Fix

Implement only the missing requirements identified in the previous review and add regression tests.

### Prompt 10 — Security

Perform an OWASP-aware review of the application. Identify issues first; fix only after review.

### Prompt 11 — Panel Simulation

Act as an ABB Principal Software Engineer interviewer. Ask architecture, DDD, API, testing, AI-assisted development, concurrency, security and scalability questions based on this repository.
