# Tic Tac Toe — Engineering Specification Update

This package freezes four previously unresolved decisions before implementation begins.

## Decisions

| Area | Final Decision |
|---|---|
| Cell addressing | `cellIndex` 0..8 |
| Reset Game | Reuse existing `gameId` |
| Undo | Memento pattern |
| Computer | Strategy pattern |
| Completion/scoreboard | `GameCompleted` domain event |
| Event delivery | In-process synchronous |
| Undo after completion | Disabled — Option A |

## Mandatory AI IDE Rule

Do not start implementation from an ambiguous specification.

The AI IDE should first read:

1. `docs/01-requirements.md`
2. `docs/04-ddd-and-domain-model.md`
3. `docs/06-api-contract.md`
4. `docs/13-assumptions.md`
5. `docs/ADR-007-game-completed-domain-event.md`
6. `docs/AI-CHANGE-AUDIT.md`

Only then should implementation prompts be issued.

## Repository Audit Model

```text
Requirement
    ↓
ADR / Design Decision
    ↓
AI Prompt OR Manual Change
    ↓
Code
    ↓
Test
    ↓
Human Review
    ↓
Git Commit
```

The objective is not merely to build Tic Tac Toe. It is to demonstrate disciplined AI-assisted engineering and the ability to explain every significant design decision.
