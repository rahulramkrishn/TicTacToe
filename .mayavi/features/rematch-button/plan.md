# Plan: rematch-button

Spec: `.mayavi/features/rematch-button/spec.md`

## Slices
- **S1: Rematch button in `status-banner`, behind a flag** (frontend only: `status-banner` plus the flag token and environment files). Satisfies AC-1 to AC-11. Fits in one day. **Slice in this run: S1.**
  - `taskMaxFiles` is 5, so S1 is done as tasks in this order:
    - **T1 Contracts and flag (3 files):** add `features.rematchButton` to both environment files; create the `REMATCH_BUTTON_ENABLED` token.
    - **T2 Characterization first (1 file):** in `status-banner.spec.ts`, add flag-off tests that pin the current Won (X), Won (Computer/O), Draw and InProgress output. Add `newGame: vi.fn()` to the mock. Provide the token as `false` by default. Run against the unchanged component: must be green.
    - **T3 Component (4 files):** change `status-banner.ts`, `.html` and `.css`; add flag-on unit tests to `status-banner.spec.ts`.
    - **T4 Journey (1 file):** new spec rendering `App` with the real `GameFacade` and mocked API services (AC-5 to AC-10).
- **Follow-ups (not in this run):**
  - **F1 / S2, build config only:** add `fileReplacements` to `build.configurations.development` in `frontend/angular.json` (replace `src/environments/environment.ts` with `environment.development.ts`). Without it every build, including `ng serve`, compiles `environment.ts`, so the flag is off everywhere (safe, but devs cannot see the button). Separate because `angular.json` is shared build config and has uncommitted user changes; settle those first, then make this one hunk. Side effects: `apiUrl` is identical in both files and nothing reads `production`, so the flag is the only behaviour change.
  - **F2:** remove the flag and token once rollout is agreed.

## Files
- **Create**
  - `frontend/src/app/feature-flags.ts`: the `REMATCH_BUTTON_ENABLED` token only.
  - `frontend/src/app/rematch.journey.spec.ts`: journey tests.
- **Modify**
  - `frontend/src/environments/environment.ts`: add `features: { rematchButton: false }`.
  - `frontend/src/environments/environment.development.ts`: add `features: { rematchButton: true }`.
  - `frontend/src/app/components/status-banner/status-banner.ts`: add `readonly rematchEnabled = inject(REMATCH_BUTTON_ENABLED)`.
  - `frontend/src/app/components/status-banner/status-banner.html`:
    - Add the Rematch button as the last child inside the Won block (`.status-content.won`, lines 21-33) and the Draw block (`.status-content.draw`, lines 35-41).
    - Wrap in `@if (rematchEnabled)`; define once in an `<ng-template>` and render with `ngTemplateOutlet` (`CommonModule` already imported).
    - Follow `game-controls.html` conventions: `type="button"`, `id="btn-rematch"`, `class="action-btn rematch-btn"`, `[disabled]="facade.loading()"`, `[attr.aria-disabled]="facade.loading()"`, `(click)="facade.newGame(facade.mode())"`, `title="Start a new game in the same mode (scoreboard kept)"`, `span.btn-icon[aria-hidden=true]` and `span.btn-text` "Rematch".
  - `frontend/src/app/components/status-banner/status-banner.css`: own `.rematch-btn` styles incl. disabled state (`.action-btn` styles are scoped to game-controls). Keep under about 1 kB.
  - `frontend/src/app/components/status-banner/status-banner.spec.ts`: see T2 and T3.
- **Must NOT touch**
  - `backend/**` (all projects, incl. `ScoreboardController`).
  - `frontend/src/app/services/game.facade.ts` (no changes; `newGame(mode)` signature stays).
  - `frontend/src/app/components/game-controls/**`.
  - `frontend/src/app/app.config.ts` (token is `providedIn: 'root'`).
  - `frontend/angular.json` (F1 only, after the uncommitted change is settled).
  - Protected: `**/package-lock.json`, `.github/workflows/**`, `legacy/**`, `**/.env*`.
  - No new npm dependencies; `package.json` unchanged.

## Contracts first
- No API change: `GameApiService.createGame(mode)`, `GameState`, `Scoreboard` unchanged.
- Merged in T1 before anything else:
  - Environment shape in both files: `features: { rematchButton: boolean }`.
  - `export const REMATCH_BUTTON_ENABLED = new InjectionToken<boolean>('REMATCH_BUTTON_ENABLED', { providedIn: 'root', factory: () => environment.features.rematchButton });`
  - Tests always override it with `{ provide: REMATCH_BUTTON_ENABLED, useValue: true|false }` or `TestBed.overrideProvider`; no test may rely on the default (after F1 the test build may compile the development file).

## Data
- None. No schema, migration or API change.

## Flag
Flag: environment.features.rematchButton

- Compile-time, read through `REMATCH_BUTTON_ENABLED`.
- `false` in `environment.ts` (production and, until F1, every build); `true` in `environment.development.ts` (effective only after F1).
- Flag off: neither block renders the button and the banner DOM is unchanged (T2 and AC-11).

## Test strategy
All tests: vitest via `ng test` (`npm --prefix frontend test -- --watch=false`).

**Unit, `status-banner.spec.ts`** (mock facade with `newGame: vi.fn()`, token provided per test):

| AC | Assertion |
|---|---|
| AC-1 | Flag true, Won X: `#btn-rematch` exists, `.btn-text` is "Rematch". |
| AC-2 | Flag true, Won O in TwoPlayer and Computer mode ("Computer Wins!"): `#btn-rematch` exists. |
| AC-3 | Flag true, Draw: `#btn-rematch` exists. |
| AC-4 | Flag true, InProgress and not initialized: `#btn-rematch` absent. |
| AC-8 | Flag true, Won, `loading` true: button has `disabled` and `aria-disabled="true"`. |
| AC-11 | Flag false, Won X / Won Computer O / Draw: `#btn-rematch` absent, no `button` elements in banner; T2 tests (text, `.status-content` classes, child element counts) pass unchanged; all 5 existing tests stay green. Compare elements, not raw `innerHTML` (`@if` adds comment nodes). |

**Journey, `rematch.journey.spec.ts`** (real `GameFacade`, mocked `GameApiService` and `ScoreboardApiService`, token `true`; `App.ngOnInit` seeds finished game G1, then `createGame.mockClear()` and set the G2 response; synchronous `of(...)`, a never-emitting `Subject` for AC-8):

| AC | Assertion |
|---|---|
| AC-5 | TwoPlayer Won G1, click: `createGame` called once with `'TwoPlayer'`; `facade.gameId()` is G2; board cells empty; status InProgress; `#btn-rematch` absent. |
| AC-6 | Computer-mode Won (O), click: `createGame` called once with `'Computer'`. |
| AC-7 | Pre-click `#score-x-wins`=2, `#score-o-wins`=1, `#score-draws`=1; G2 response carries the same scoreboard; DOM values identical after the click. |
| AC-8 | `createGame` returns a pending `Subject`; click twice: at most one call; button disabled while loading. |
| AC-9 | `createGame` returns `throwError` with a ProblemDetails body: `gameState()` still equals G1; `.error-banner[role=alert]` shows title and code; `#btn-rematch` present and enabled. |
| AC-10 | `#btn-reset-game` and `#btn-new-game` present next to Rematch; New Game calls `createGame(mode)`, Reset Board calls `resetGame(G1)`. `game-controls.spec.ts` stays green and unchanged. |

**Regression:** `app.spec.ts` and `game.facade.spec.ts` need no edits and must stay green.

## Risks & owners
- Shared hotspots: `game.facade.ts` reused, not edited; `angular.json` deferred to F1; environment files get a new shape (nothing imports them today).
- Flag not visible in dev until F1: S1 alone leaves the flag effectively off in `ng serve`; behaviour is proven by tests only. State this in the PR.
- Accessibility: the button sits inside the `role="status" aria-live="polite"` region, so screen readers may announce "Rematch" with the result. Accepted; check with a screen reader.
- Style budget: `anyComponentStyle` warns at 4 kB, fails at 8 kB; `status-banner.css` is about 3 kB now.
- Duplicate action: Rematch equals New Game once the game is over (accepted in the spec).
- Owners: no CODEOWNERS file. Reviewer: rahul (frontend). No backend or CI owners needed.

## Rollback
- **Switch off (no code revert):** set `features.rematchButton: false` in `environment.development.ts` (`environment.ts` is already false), rebuild and redeploy (`npm --prefix frontend run build`). The flag is compile-time, so a rebuild is required. With the flag off the button is never rendered and behaviour matches baseline (T2 and AC-11 tests).
- **Full removal:** revert the S1 commit(s). No data, migration or API change to undo. F1, if merged, reverts separately; reverting it only stops dev builds from using `environment.development.ts`.
