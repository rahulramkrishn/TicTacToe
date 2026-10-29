<!-- mayavi:begin -->
## Mayavi workflow (agentic, test-first)
- Lanes: /mayavi:spec (normal), /mayavi:task (exact small change), /mayavi:hotfix (production incident), /mayavi:brainstorm (approach unknown). Then red > green > challenge > review > pr. Lost? `/mayavi:where`.
- Branches: `<feat|fix|hotfix>/<git user>/<ticket>_<description>`. Commits: Conventional Commits `type(scope): subject` plus `Refs: <ticket>`. `mayavi start <slug> --ticket <ID>` creates the branch.
- Tests come first and are locked after red. Never edit a locked test to go green; stop and ask for /mayavi:rewind.
- Behaviour change = feature flag + tests. Schema change = new migration only. No edits to protected paths (see .mayavi/config.json).
- Run tests through `mayavi test` (compact output, records evidence). One task per session; /clear between phases.
- Every Mayavi command is safe to re-run. Park work with /mayavi:pause, give up with /mayavi:abandon, continue with /mayavi:resume.
- Hard rules are enforced by hooks and CI, not by this file.
<!-- mayavi:end -->
