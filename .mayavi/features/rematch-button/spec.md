# Spec: rematch-button

## Goal
When a game ends (win or draw), the player sees a "Rematch" button in the game-over banner that starts a new game in the same mode with the scoreboard kept.

## Context
- Game end: `Game.MakeMove` sets Won/Draw (`backend/TicTacToe.Domain/Aggregates/Game.cs:110-122`); `GameService.MakeMoveAsync` records the win/draw on the scoreboard (`GameService.cs:110-121`).
- Frontend: `game.facade.ts:36` `isGameOver`, `:66` `newGame(mode)` (creates a new game, returns early while `loading()`, clears the error first and sets it on failure, leaves state untouched on failure). `facade.mode()` is a computed with a 'TwoPlayer' fallback only when state is null. Game-over banners in `status-banner.html:20-41`; Reset Board and New Game in `game-controls.html:14-36` (New Game passes `facade.mode()`).
- The facade overwrites `state` (including `state.scoreboard`) from the server response on every create.
- Feature flags: none in code today. `frontend/src/environments/environment.ts` and `environment.development.ts` exist and will carry the flag.
- Affected: players in TwoPlayer and Computer modes. Frontend-only change; no API or schema change.

## In scope
- A "Rematch" button (`id="btn-rematch"`) in the game-over banner (`status-banner`), visible only when the game is Won or Draw.
- Clicking it calls `facade.newGame(facade.mode())`.
- Flag `environment.features.rematchButton` (boolean) gating the button.
- Unit tests for the facade and component behaviour.

## Non-goals
- Changing who starts the next game or whether the computer opens in Computer mode (whatever a new game does today).
- Changing or hiding Reset Board or New Game.
- Backend, API or scoreboard changes.
- A general feature-flag framework.

## Acceptance criteria
- **AC-1** Given the flag is on and X has won When the banner renders Then `#btn-rematch` is present with text "Rematch".
- **AC-2** Given the flag is on and O has won (including a computer win in Computer mode) When the banner renders Then `#btn-rematch` is present.
- **AC-3** Given the flag is on and the game ended in a Draw When the banner renders Then `#btn-rematch` is present.
- **AC-4** Given the flag is on and the game is InProgress When the page renders Then `#btn-rematch` is absent.
- **AC-5** Given a finished TwoPlayer game with gameId G1 When the user clicks `#btn-rematch` Then `createGame` is called once with 'TwoPlayer', the facade's gameId is the new id (not G1), the board is empty, the status is InProgress and `#btn-rematch` is absent.
- **AC-6** Given a finished Computer-mode game When the user clicks `#btn-rematch` Then `createGame` is called once with 'Computer'.
- **AC-7** Given the scoreboard before the click is X=2, O=1, Draws=1 and the mocked `createGame` response carries the same scoreboard When the user clicks `#btn-rematch` Then the displayed scoreboard equals the pre-click one (the scoreboard comes from the server response).
- **AC-8** Given a finished game and the facade is `loading()` When the banner renders Then `#btn-rematch` is disabled, and two clicks produce at most one `createGame` call.
- **AC-9** Given a finished game and `createGame` rejects When the user clicks `#btn-rematch` Then the finished game's state is unchanged, the error is displayed and `#btn-rematch` is still present and enabled afterwards.
- **AC-10** Given the flag is on and a finished game When the page renders Then Reset Board and New Game are still present and behave as today.
- **AC-11** (flag off) Given `environment.features.rematchButton` is false and a game is Won or Draw When the banner renders Then `#btn-rematch` is absent and the rest of the banner is unchanged.

## Edge cases and failure modes
- Double click: covered by AC-8.
- `POST /api/games` fails: covered by AC-9.
- Mode switch from the header while a game is over (`header.ts:18` calls `newGame`): existing behaviour applies; Rematch disappears once the new game is InProgress.
- Page reload on a finished game: existing `initGame` behaviour applies.

## Constraints
- Flag: `environment.features.rematchButton`; true in `environment.development.ts`, false in `environment.ts` (production) until rollout is agreed. Tests override it by providing the environment value (or an injected token) per test.
- Do not touch: `backend/` (all projects), `ScoreboardController`, the Reset Board and New Game behaviour, the signature of `newGame`.
- Schema: none.

## Risks
- `game.facade.ts` is a shared hotspot; reuse `newGame` and do not change it.
- Rematch duplicates New Game when the game is over; accepted by the user as the chosen design.
- Small slice; fits in a day.

## Open questions
