# Requirements Traceability Matrix

| Requirement | Backend | Frontend | Tests | Docs |
|---|---|---|---|---|
| FR-01 Create game | GameService/API | New game UI | CreateGame | API |
| FR-02 Board | Game aggregate | GameBoard | Board tests | Requirements |
| FR-03 Turns | Game aggregate | Status | Turn tests | Requirements |
| FR-04 Validation | Domain/API | Error UI | Invalid move | API |
| FR-05 Win | WinDetector | Highlight | Win tests | Domain |
| FR-06 Draw | Game | Message | Draw tests | Requirements |
| FR-07 History | Game/DTO | History | History tests | API |
| FR-08 Two-player undo | Game | Undo button | Undo tests | ADR |
| FR-09 Computer undo | Game | Undo button | Computer undo | ADR |
| FR-10 Scoreboard | Scoreboard | Scoreboard UI | Score tests | API |
| FR-11 Computer mode | Strategy | Mode UI | Strategy tests | Domain |
| FR-12 Reset game | GameService | Reset UI | Reset tests | Requirements |
| FR-13 Reset scoreboard | ScoreboardService | Reset UI | Score tests | API |
| FR-14 State response | DTO | State model | API tests | API |
| FR-15 REST API | Controllers | API service | Integration | API |
| FR-16 Errors | Error middleware | Error display | Error tests | API |
| FR-17 UI | API integration | Angular | Component/E2E | README |
| FR-18 Backend source of truth | Domain | Render-only state | Integration | Architecture |
| FR-19 Testing | Test project | Frontend tests | Test matrix | Testing |

## Acceptance Checklist

- [ ] Angular application runs locally.
- [ ] .NET API runs locally.
- [ ] REST communication works.
- [ ] New game can be created.
- [ ] Two-player mode works.
- [ ] Computer mode works.
- [ ] Turns alternate correctly.
- [ ] Invalid moves rejected.
- [ ] Win detection works.
- [ ] Draw detection works.
- [ ] Winning cells highlighted.
- [ ] Move history shown.
- [ ] Undo follows selected mode.
- [ ] Scoreboard works.
- [ ] Reset Game works.
- [ ] Reset Scoreboard works.
- [ ] Basic tests included.
- [ ] README explains execution/review.
- [ ] AI workflow documented.
- [ ] Candidate can explain implementation.
