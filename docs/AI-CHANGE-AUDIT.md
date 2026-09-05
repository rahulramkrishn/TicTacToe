# AI-Assisted Engineering Audit

## Purpose

The assignment explicitly expects the candidate to explain:

- how requirements became a specification
- prompts used
- AI-generated work
- manual changes
- reviewed areas
- assumptions
- trade-offs

Therefore AI usage is treated as an auditable engineering workflow.

## Required Traceability

Every material change should have:

```text
Requirement ID
→ Design/ADR
→ Prompt ID or Manual Change ID
→ Files changed
→ Tests
→ Git commit
→ Verification result
```

## Prompt Register

Maintain:

```text
docs/ai/prompts/
```

Naming:

```text
P001-repository-bootstrap.md
P002-domain-model.md
P003-memento-undo.md
P004-computer-strategy.md
P005-domain-events-scoreboard.md
P006-api-contract.md
P007-angular-ui.md
P008-tests.md
P009-review-and-hardening.md
```

Each prompt records:

- objective
- context supplied
- exact prompt
- expected files
- constraints
- output summary
- human review
- follow-up prompt
- resulting commit

## Change Log

Maintain:

```text
docs/ai/CHANGELOG.md
```

Suggested format:

```markdown
## 2026-09-05 — P005

Type: AI-assisted

Requirement: FR-11, FR-18

Prompt:
P005-domain-events-scoreboard.md

Changed:
- GameCompleted event
- scoreboard handler
- unit tests

Human review:
- verified event emitted once
- verified GET does not increment
- verified reset does not increment

Commit:
abc1234

Status:
Accepted
```

## Manual Changes

Record human changes separately:

```text
docs/ai/manual-changes.md
```

Include:

- date
- reason
- files
- requirement/ADR
- what changed
- why AI output was insufficient
- tests executed
- commit

## AI Review Checklist

Before accepting generated code:

- Does it satisfy the requirement?
- Does it respect the frozen ADRs?
- Does it put business rules in the domain?
- Does it validate `cellIndex`?
- Does it prevent invalid state mutation?
- Does it use Memento for undo?
- Does it use Strategy for computer moves?
- Does it emit `GameCompleted` exactly once?
- Does Reset reuse `gameId`?
- Are tests meaningful rather than generated only for coverage?
- Are API DTOs consistent?
- Is frontend state derived from backend response?

## Git Discipline

Prefer small commits:

```text
docs: freeze architecture decisions
feat(domain): implement game aggregate
feat(domain): add memento undo
feat(domain): add computer strategy
feat(scoreboard): handle GameCompleted
feat(api): expose game endpoints
feat(ui): implement board
test(domain): add rule coverage
test(api): add contract tests
docs(ai): record implementation audit
```

Do not squash away the evidence needed for the panel if the company asks how AI-assisted development progressed.

## Golden Rule

AI is an implementation accelerator, not the source of engineering judgment.

The candidate remains responsible for:

- requirements
- architecture
- correctness
- security
- testing
- trade-offs
- final code
