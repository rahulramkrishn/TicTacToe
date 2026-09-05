# Tic Tac Toe — ABB Principal Software Engineer Assessment

## Documentation First

This repository is designed to be implemented with an AI-assisted engineering workflow while keeping all significant decisions and changes auditable.

## Documents

- `docs/01-requirements.md` — detailed functional and engineering requirements.
- `docs/02-prerequisites-and-environment.md` — prerequisites and setup.
- `docs/03-nfr.md` — proposed non-functional requirements.
- `docs/04-ddd-and-domain-model.md` — DDD/domain design.
- `docs/05-architecture.md` — architecture and design.
- `docs/06-api-contract.md` — API contract.
- `docs/07-test-strategy.md` — test strategy.
- `docs/08-ai-development-governance.md` — AI development audit process.
- `docs/09-traceability-matrix.md` — requirement-to-code/test traceability.
- `docs/10-adr-template-and-initial-decisions.md` — architectural decisions.
- `docs/11-implementation-plan.md` — AI IDE implementation plan.
- `docs/12-root-ai-change-log.md` — AI/manual change log.
- `docs/13-assumptions.md` — assumptions and decisions.
- `docs/14-panel-review.md` — panel preparation.

## Implementation Status

This documentation baseline is complete. Application implementation should proceed incrementally and update the audit trail after every meaningful change.

## Core Design

```text
Angular
   |
   | REST
   v
ASP.NET Core API
   |
Application
   |
Domain / Game Aggregate
   |
In-memory repository
```

## Key Decisions

- Backend is source of truth.
- In-memory storage.
- Pragmatic DDD.
- No microservices for this small exercise.
- Deterministic computer strategy.
- Undo disabled after completion.
- AI-assisted changes are traceable.

## Required Final README Sections

Before submission, this README must additionally contain:

1. Project overview.
2. Final tech stack and exact versions.
3. Features implemented.
4. Backend setup.
5. Frontend setup.
6. API endpoint summary.
7. Test commands.
8. AI tools and prompt summary.
9. Design decisions.
10. Clarifications and assumptions.
11. Known limitations.
12. Future improvements.

## Final Submission Checklist

- [ ] Angular source.
- [ ] .NET source.
- [ ] Tests.
- [ ] README.
- [ ] API documentation.
- [ ] Setup instructions.
- [ ] AI prompt summary.
- [ ] AI change log.
- [ ] Manual change log.
- [ ] ADRs.
- [ ] Traceability matrix.
- [ ] Known limitations.
- [ ] Test evidence.
- [ ] Git history is clean and understandable.
