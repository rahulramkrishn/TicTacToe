# Review: rematch-button

Diff: `git diff 2574cde...HEAD` (frontend). Full suite run before review: 78/78 pass (`mayavi test --full`). The reviewer read the diff and did not run tests itself.

## BLOCKING
None.

## MAJOR
None.

## MINOR
1. `status-banner.html` (`<ng-template #rematch>`): the button sits inside the `role="status" aria-live="polite"` region, so a screen reader may announce "Rematch" with the result. Accepted in the plan; record a manual screen-reader check in the PR.
2. `status-banner.html` (Draw/Won blocks): the button is an extra flex child of `.status-content`; banners may widen or wrap on narrow viewports. No visual check exists; do a pass at mobile width.
3. `status-banner.html`: class `action-btn` has no styles in status-banner (harmless dead class, follows the plan).
4. Spec comments: `status-banner.spec.ts` refers to `test-plan.md`, and `feature-flags.spec.ts` has `// M7`..`// M9` mutation jargon; reword to reference the spec/behaviour.
5. `.gitignore` carries Mayavi/Claude ignore entries (tooling, outside feature scope; separate commit `3467cf2`).
6. `feature-flags.spec.ts`: the `it.skipIf(environment.features.rematchButton)` test skips once F1 makes the test build compile the development environment; the token-default test still runs.
7. The flag is effectively off in `ng serve` until follow-up F1 (`fileReplacements` in `angular.json`); say so in the PR description.

## Spec and plan checks
- AC-1..4, AC-8 (render, disabled): T-1..T-6, T-13, T-14. AC-5..10: journey tests T-8, T-10..T-12, T-15..T-21. AC-11: T-22..T-29. Extra: T-30 and `feature-flags.spec.ts`.
- Must-NOT-touch respected: no changes to `backend/`, `game.facade.ts`, `game-controls/**`, `app.config.ts`, `angular.json`, package or lock files.
- Environment files: flag false in `environment.ts`, true in `environment.development.ts`.
- Flag off equals baseline: only comment nodes added; element/class/children assertions pin the banner.
- CSS about 3.2 kB (+0.7 kB), under the 4 kB warning budget.
- No new dependencies; `newGame(mode())` reused unchanged.

Verdict: PASS
