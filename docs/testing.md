# Test Strategy and Test Matrix

## 1. Testing Pyramid

```text
              E2E
             /   \
        Integration
          /       \
        Domain / Unit
```

Most coverage should be at domain/application level.

## 2. Required Unit Tests

### Move

- Valid first move.
- Occupied cell rejected.
- Invalid row rejected.
- Invalid column rejected.
- Wrong player rejected.
- Move after completion rejected.

### Turn

- X moves -> O turn.
- O moves -> X turn.
- Invalid move leaves turn unchanged.

### Win

- Row 0.
- Row 1.
- Row 2.
- Column 0.
- Column 1.
- Column 2.
- Main diagonal.
- Anti-diagonal.

### Draw

- Full board with no winner -> Draw.
- Full board with a winning final move -> Won, not Draw.

### Reset

- Board cleared.
- History cleared.
- X turn restored.
- Status InProgress.
- Winner cleared.
- Score unchanged.

### Undo

Two-player:

- Remove last move.
- Restore board.
- Restore player.
- Update history.
- Recalculate status.

Computer:

- Remove computer O move.
- Remove preceding human X move.
- Restore X turn.
- Preserve earlier history.

### Scoreboard

- X win increments X exactly once.
- O win increments O exactly once.
- Draw increments Draws exactly once.
- Repeated GET does not increment.
- Reset Game does not alter scoreboard.
- Reset Scoreboard clears counters.

### Computer Strategy

Test each priority:

1. O winning move.
2. X blocking move.
3. Center.
4. Corner.
5. Any available cell.

Also test:

- Computer never selects occupied cell.
- Computer never moves after completion.

## 3. Application Tests

Test:

- Create game.
- Get game.
- Make move.
- Undo.
- Reset.
- Scoreboard.
- Invalid requests.

## 4. API Integration Tests

Recommended:

- API starts with test host.
- Create game returns 201.
- Move returns correct state.
- Invalid move returns expected error.
- Completed game rejects further move.
- Scoreboard updates.
- Reset preserves scoreboard.

## 5. Frontend Tests

Cover:

- Board renders 9 cells.
- Current player displays.
- Mode displays.
- Occupied cell is disabled.
- Winning cells are highlighted.
- Move history renders.
- Undo disabled with no moves.
- Reset invokes API.
- Error response is shown.
- Scoreboard renders.

## 6. E2E Scenarios

Recommended end-to-end scenarios:

### E2E-01 Two-player win

```text
Create game
X move
O move
X move
O move
X winning move
Verify winner
Verify highlight
Verify scoreboard
Verify further move disabled/rejected
```

### E2E-02 Draw

Create a known draw sequence.

Verify:

- Draw message.
- Scoreboard.
- No additional move.

### E2E-03 Computer mode

Verify:

- Human X.
- Computer O.
- Computer responds automatically.
- Computer blocks immediate X win.
- Computer takes winning move when available.

### E2E-04 Undo

Two-player and computer-mode cases.

## 7. Regression Rule

Every defect found during development should result in:

1. A regression test.
2. A changelog entry if meaningful.
3. An AI/manual change record.

## 8. Test Evidence

Keep test execution evidence in:

`docs/testing/test-runs/`

Example:

```text
2026-09-05-local-test-run.md
```

Include:

- Commit.
- Command.
- Result.
- Failed tests if any.
- Resolution.
