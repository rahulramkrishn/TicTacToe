## Summary
When a game ends, players had to reach for Reset Board or New Game. This adds a "Rematch" button to the Won/Draw banner that starts a new game in the same mode and keeps the scoreboard.
Frontend only, behind the compile-time flag `environment.features.rematchButton` (false in `environment.ts`; true in `environment.development.ts`). No backend, API or schema change.
Known limitation: the flag stays off in `ng serve` until a follow-up (TTT-2) adds `fileReplacements` to `angular.json`. Still to check manually: screen reader announcement (button is inside the aria-live status region) and the banner at mobile width.

## Ticket
TTT-1

## Agent-assisted
yes (Mayavi, test-first flow)

## Plan link
.mayavi/features/rematch-button/plan.md (spec: spec.md, review: review.md)

## Tests run
- Acceptance criteria covered: AC-1, AC-2, AC-3, AC-4, AC-5, AC-6, AC-7, AC-8, AC-9, AC-10, AC-11
- Red (before implementation): dotnet test backend/TicTacToe.sln --filter "Category!=Integration" && npm --prefix frontend test -- --watch=false -> FAIL
- Full suite on final commit: dotnet test backend/TicTacToe.sln && npm --prefix frontend test -- --watch=false -> PASS
- Locked test files unchanged since red: 3
- Mutation challenge: see .mayavi/features/rematch-button/challenge.md

## Flag
environment.features.rematchButton

## Rollback
- **Switch off (no code revert):** set `features.rematchButton: false` in `environment.development.ts` (`environment.ts` is already false), rebuild and redeploy (`npm --prefix frontend run build`). The flag is compile-time, so a rebuild is required. With the flag off the button is never rendered and behaviour matches baseline (T2 and AC-11 tests).

## Reviewer checklist
- [ ] I read every line of the diff
- [ ] Flag off behaves like baseline
- [ ] No protected/legacy paths touched without a ticket
