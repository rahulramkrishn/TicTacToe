# Test plan: rematch-button

All tests: vitest via `ng test`. Token `REMATCH_BUTTON_ENABLED` is always provided explicitly per test (no test relies on the default).
Unit file: `frontend/src/app/components/status-banner/status-banner.spec.ts` (mock facade incl. `newGame: vi.fn()`; "not initialized" = mock `gameState()` returns null and `status()` undefined).
Journey file: `frontend/src/app/rematch.journey.spec.ts` (real `GameFacade`, mocked `GameApiService`/`ScoreboardApiService`, token `true`; `App.ngOnInit` seeds G1 from the first `createGame`, then `createGame.mockClear()`; real DOM clicks; the real state sets `mode()`, no fallback).

| T-n | AC-n | level | Given/When/Then | planned file |
|---|---|---|---|---|
| T-1 | AC-1 | unit | Given flag true, Won, winner X, When banner renders, Then `#btn-rematch` exists and its `.btn-text` is "Rematch" | status-banner.spec.ts |
| T-2 | AC-2 | unit | Given flag true, Won, winner O in TwoPlayer, When banner renders, Then `#btn-rematch` exists | status-banner.spec.ts |
| T-3 | AC-2 | unit | Given flag true, Won, winner O in Computer mode (banner "Computer Wins!"), When banner renders, Then `#btn-rematch` exists | status-banner.spec.ts |
| T-4 | AC-3 | unit | Given flag true, Draw, When banner renders, Then `#btn-rematch` exists | status-banner.spec.ts |
| T-5 | AC-4 | unit | Given flag true, InProgress, When banner renders, Then `#btn-rematch` is absent | status-banner.spec.ts |
| T-6 | AC-4 | unit | Given flag true and not initialized (state null, status undefined), When banner renders, Then `#btn-rematch` is absent | status-banner.spec.ts |
| T-7 | AC-4 | journey | Given a fresh InProgress game, When the user plays one move, Then `#btn-rematch` is absent before and after the move | rematch.journey.spec.ts |
| T-8 | AC-5 | journey | Given TwoPlayer game G1 Won, When user clicks `#btn-rematch` and `createGame` returns G2, Then `createGame` called once with 'TwoPlayer', `facade.gameId()` is G2 (not G1), board cells empty, status InProgress, `#btn-rematch` absent | rematch.journey.spec.ts |
| T-9 | AC-5 | unit | Given flag true, Won, mock `mode()` returns 'Computer', When `#btn-rematch` is clicked, Then `facade.newGame` called once with 'Computer' (a hardcoded 'TwoPlayer' fails) | status-banner.spec.ts |
| T-10 | AC-6 | journey | Given Computer-mode game Won by O (state mode 'Computer'), When user clicks `#btn-rematch` and `createGame` returns G2 in Computer mode, Then `createGame` called once with 'Computer', the new game's mode is 'Computer' and status InProgress (who opens is not asserted) | rematch.journey.spec.ts |
| T-11 | AC-7 | journey | Given `#score-x-wins`=2, `#score-o-wins`=1, `#score-draws`=1 and G2 carries the same scoreboard, When user clicks `#btn-rematch`, Then the DOM still shows 2, 1, 1 | rematch.journey.spec.ts |
| T-12 | AC-7 | journey | Given the same pre-click scoreboard but G2's response carries X=3, When user clicks `#btn-rematch`, Then `#score-x-wins` shows 3 (the scoreboard comes from the server response) | rematch.journey.spec.ts |
| T-13 | AC-8 | unit | Given flag true, Won, `loading()` true, When banner renders, Then `#btn-rematch` has `disabled` and `aria-disabled="true"` | status-banner.spec.ts |
| T-14 | AC-8 | unit | Given flag true, Won, `loading()` false, When banner renders, Then `#btn-rematch` is enabled and `aria-disabled="false"` | status-banner.spec.ts |
| T-15 | AC-8 | journey | Given `createGame` returns a never-emitting Subject, When user clicks `#btn-rematch` twice (real DOM clicks), Then `createGame` called exactly once and the button is disabled | rematch.journey.spec.ts |
| T-16 | AC-8 | journey | Given T-15's Subject, When it emits G2, Then loading is false, the game is G2 InProgress and `#btn-rematch` is gone | rematch.journey.spec.ts |
| T-17 | AC-9 | journey | Given Won G1 and `createGame` errors with ProblemDetails (title, code), When user clicks `#btn-rematch`, Then `gameState()` equals G1 (Won, same board), `.error-banner[role=alert]` shows title and code, `#btn-rematch` present and not disabled | rematch.journey.spec.ts |
| T-18 | AC-9 | journey | Given Won G1 and `createGame` errors with a network error with no body, When user clicks `#btn-rematch`, Then no exception escapes, `gameState()` equals G1 and `#btn-rematch` is present and enabled | rematch.journey.spec.ts |
| T-19 | AC-9 | journey | Given a failed rematch (T-17) and then `createGame` returns G2, When user clicks `#btn-rematch` again, Then the new game starts and the error banner is gone | rematch.journey.spec.ts |
| T-20 | AC-10 | journey | Given flag true and Won, When the page renders, Then `#btn-reset-game` and `#btn-new-game` are present alongside `#btn-rematch` | rematch.journey.spec.ts |
| T-21 | AC-10 | journey | Given flag true and Won G1, When user clicks New Game, Then `createGame(mode)` is called; and (separate case) When user clicks Reset Board, Then `resetGame(G1)` is called. `game-controls.spec.ts` stays unchanged and green | rematch.journey.spec.ts |
| T-22 | AC-11 | unit | Given flag false, Won (X), When banner renders, Then `#btn-rematch` absent and banner contains no `button` element | status-banner.spec.ts |
| T-23 | AC-11 | unit | Given flag false, Won (Computer/O), When banner renders, Then `#btn-rematch` absent and no `button` element | status-banner.spec.ts |
| T-24 | AC-11 | unit | Given flag false, Draw, When banner renders, Then `#btn-rematch` absent and no `button` element | status-banner.spec.ts |
| T-25 | AC-11 | characterization | Given flag false and Won (X), Then banner text, `.status-content` class list and element child count equal today's (compare elements via `children`/`textContent`, not `innerHTML`); written first, green before the component change | status-banner.spec.ts |
| T-26 | AC-11 | characterization | Same as T-25 for Won (Computer/O) | status-banner.spec.ts |
| T-27 | AC-11 | characterization | Same as T-25 for Draw | status-banner.spec.ts |
| T-28 | AC-11 | characterization | Same as T-25 for InProgress | status-banner.spec.ts |
| T-29 | AC-11 | journey | Given token false and a Won game rendered in `App`, Then `#btn-rematch` is absent while `#btn-new-game` and `#btn-reset-game` still call `createGame(mode)` / `resetGame(G1)` | rematch.journey.spec.ts |
| T-30 | edge: header mode switch | journey | Given a Won game and the flag on, When the user switches mode from the header (`header.ts:18` calls `newGame`) and the new game is InProgress, Then `#btn-rematch` is absent | rematch.journey.spec.ts |

Regression (existing, unchanged, must stay green): the 5 existing `status-banner.spec.ts` tests, `game-controls.spec.ts`, `app.spec.ts`, `game.facade.spec.ts`.

Deliberately not tested:
- The default value of `REMATCH_BUTTON_ENABLED` (it depends on which environment file the test build compiles; changes with follow-up F1).
- The a11y concern (button inside the `role="status" aria-live="polite"` region): accepted in `plan.md`; manual screen-reader check at PR time.
- Page reload on a finished game: existing `initGame` behaviour, unchanged.
