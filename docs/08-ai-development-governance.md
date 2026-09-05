# AI-Assisted Development Governance

## 1. Objective

The assignment explicitly allows AI-assisted development and expects the candidate to explain:

- How requirements became a specification.
- Prompts used.
- AI-generated work.
- Manual changes.
- Review performed.
- Assumptions.
- Trade-offs.

Therefore AI usage must be treated as an auditable engineering workflow.

## 2. Golden Rule

**No important implementation change should exist without a traceable record.**

Every meaningful change should be traceable:

```text
Requirement
   ↓
Design/ADR
   ↓
Prompt or manual decision
   ↓
Code change
   ↓
Tests
   ↓
Review
   ↓
Git commit
```

## 3. Required Files

Maintain:

```text
AI_CHANGELOG.md
AI_PROMPTS.md
CHANGELOG.md
ASSUMPTIONS.md
docs/adr/
docs/testing/
```

## 4. AI Change Log

Use this format:

```markdown
## AI-CHANGE-001

- Date: 2026-09-05
- Commit: abc1234
- Tool: Codex / Antigravity / Claude / Copilot
- Requirement IDs: FR-01, FR-14
- Change type: AI-generated / AI-assisted / Human
- Prompt reference: AI-PROMPT-001
- Files changed:
  - backend/...
  - frontend/...
- What was generated:
  - ...
- What I changed manually:
  - ...
- Review performed:
  - ...
- Tests added:
  - ...
- Test result:
  - PASS
- Engineering decision:
  - Accepted / Modified / Rejected
- Reason:
  - ...
```

## 5. Prompt Log

Do not rely on chat history being available during panel review.

Record prompts that materially influenced the implementation.

Example:

```markdown
## AI-PROMPT-001

### Goal
Design the backend domain model.

### Prompt
You are a principal .NET architect. Based on docs/requirements/01-requirements.md,
design a DDD-inspired domain model for Tic Tac Toe. Keep it pragmatic and avoid
microservices. Identify aggregates, entities, value objects, invariants and
application services.

### Output
See commit abc1234.

### My assessment
Accepted with changes:
- ...
```

## 6. Human Change Record

Human-only work must also be recorded.

Example:

```markdown
## HUMAN-CHANGE-004

- Date: ...
- Requirement: FR-09
- Decision: Changed computer-mode undo to remove two moves.
- Reason: Assignment explicitly requires move pair removal.
- Tests: ComputerUndoRestoresHumanTurn
- Commit: ...
```

## 7. AI Generated Code Review Checklist

Before accepting generated code:

- Does it satisfy the requirement?
- Does it violate any invariant?
- Is validation server-side?
- Does it duplicate business logic?
- Is it testable?
- Are edge cases handled?
- Does it introduce unnecessary dependencies?
- Does it leak internal implementation?
- Is error handling correct?
- Does it create concurrency problems?
- Does it match existing architecture?
- Is it understandable enough to explain?

## 8. AI Use Cases

Good uses:

- Requirement decomposition.
- Test-case generation.
- Boilerplate.
- DTO scaffolding.
- Unit-test scaffolding.
- API documentation.
- Code review.
- Refactoring suggestions.
- Architecture alternatives.
- Edge-case analysis.

Human responsibility:

- Final architecture.
- Business rule interpretation.
- Acceptance of code.
- Security review.
- Correctness.
- Trade-offs.
- Final explanation.

## 9. Prompt Quality

Use prompts that include:

- Context.
- Requirements.
- Constraints.
- Existing architecture.
- Expected output.
- Non-goals.
- Tests required.

Avoid:

```text
Build the whole app.
```

Prefer:

```text
Implement FR-05 win detection inside the domain.
Do not change API contracts.
Add unit tests for all rows, columns and diagonals.
Return winning cell indices.
Do not add third-party dependencies.
```

## 10. Git Discipline

Use small commits.

Recommended format:

```text
feat(domain): implement game move rules
test(domain): add win detection scenarios
feat(api): expose game move endpoint
feat(ui): render board and turn
test(api): add move integration tests
docs: add architecture and ADRs
```

Avoid giant:

```text
AI generated entire project
```

commit.

## 11. Branch Strategy

For a small assessment:

```text
main
feature/...
```

Keep the final main branch clean.

If using AI extensively, feature branches make the audit trail easier.

## 12. Commit Message Traceability

Include requirement IDs where useful:

```text
feat(game): implement undo behavior [FR-08, FR-09]
```

## 13. Review Ledger

Maintain:

```text
docs/ai/review-ledger.md
```

For every AI-generated PR/change:

| Change | AI | Human reviewed | Tests | Accepted |
|---|---|---|---|---|
| Game aggregate | Yes | Yes | Yes | Yes |
| Win detector | Yes | Yes | Yes | Yes |
| Computer strategy | Yes | Yes | Yes | Yes |

## 14. What to Say in the Panel

A strong answer is:

> I used AI as an engineering accelerator, not as the decision maker. I first converted the problem statement into explicit requirements and acceptance tests. I then used targeted prompts for domain modeling, implementation scaffolding and test generation. Every significant AI-assisted change is traceable to a requirement and Git commit. I reviewed generated code for correctness, security, maintainability and edge cases, and manually changed areas where the generated implementation did not match the requirements.

