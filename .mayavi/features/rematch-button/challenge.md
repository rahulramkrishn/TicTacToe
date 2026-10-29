# Challenge: rematch-button

Method: LLM-driven spot mutations (no `commands.mutation` configured). Each mutant was run with `npm --prefix frontend test -- --watch=false` and restored with `git restore`. Baseline: 73/73.

## Mutations
| # | file | mutation | result | killing test |
|---|------|----------|--------|--------------|
| 1 | status-banner.html | invert `@if (rematchEnabled)` | KILLED | T-1..T-4 and others |
| 2 | status-banner.html | drop `[disabled]` binding | KILLED | T-13, T-15 |
| 3 | status-banner.html | drop `[attr.aria-disabled]` binding | KILLED | T-13, T-14 |
| 4 | status-banner.html | hardcode `newGame('TwoPlayer')` | KILLED | T-9, T-10 |
| 5 | status-banner.html | InProgress branch never matches | KILLED | T-5, T-28 + 2 existing (wrong direction, see 11) |
| 6 | status-banner.html | remove Rematch from Draw block | KILLED | T-4 |
| 7 | environment.development.ts | `rematchButton` true -> false | SURVIVED, now KILLED | feature-flags.spec.ts: development environment enables rematchButton |
| 8 | environment.ts | `rematchButton` false -> true | SURVIVED, now KILLED | feature-flags.spec.ts: production environment keeps rematchButton dark-launched |
| 9 | feature-flags.ts | token factory returns `true` | SURVIVED, now KILLED | feature-flags.spec.ts: token defaults to environment value; Won/Draw show no button with default token |
| 10 | status-banner.html | invert `[disabled]` | KILLED | T-8, T-9, T-13, T-14 and others |
| 11 | status-banner.html | render Rematch in the InProgress branch | KILLED (added after mutant 5 tested the wrong direction) | T-5, T-7, T-8, T-16, T-30 + new InProgress/Won test |

## Test smells
- Existing specs always override the token, so nothing covered the token default or the environment values. Fixed by `feature-flags.spec.ts` (kills 7, 8, 9).
- T-28 and the `aria-disabled` assertions are tied to exact markup: good regression catchers, brittle against harmless refactors. Accepted.
- Shared mock signals change an earlier fixture's DOM once status changes; the new spec reads each fixture right after rendering.
- The default-token test depends on the compiled environment file: after follow-up F1 it must still pass (it compares to the imported `environment`, and the component test uses `it.skipIf`).

## Flakiness
`mayavi test --repeat 3`: 3 runs, 78/78 each, 11 test files.

Survivors unresolved: 0
