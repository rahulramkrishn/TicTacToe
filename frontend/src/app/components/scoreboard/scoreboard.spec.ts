import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { ScoreboardComponent } from './scoreboard';
import { GameFacade } from '../../services/game.facade';
import { Scoreboard } from '../../models/game.models';

describe('ScoreboardComponent', () => {
  let mockFacade: any;

  beforeEach(() => {
    mockFacade = {
      scoreboard: signal<Scoreboard | null>({
        xWins: 4,
        oWins: 2,
        draws: 1,
        totalGames: 7
      }),
      mode: signal<string>('TwoPlayer'),
      loading: signal<boolean>(false),
      resetScoreboard: vi.fn()
    };

    TestBed.configureTestingModule({
      imports: [ScoreboardComponent],
      providers: [{ provide: GameFacade, useValue: mockFacade }]
    });
  });

  it('should render score counters for X wins, draws, and O wins', () => {
    const fixture = TestBed.createComponent(ScoreboardComponent);
    fixture.detectChanges();

    const xCount = fixture.nativeElement.querySelector('#score-x-wins').textContent.trim();
    const drawsCount = fixture.nativeElement.querySelector('#score-draws').textContent.trim();
    const oCount = fixture.nativeElement.querySelector('#score-o-wins').textContent.trim();

    expect(xCount).toBe('4');
    expect(drawsCount).toBe('1');
    expect(oCount).toBe('2');
  });

  it('should call resetScoreboard() when Reset Scores is clicked', () => {
    const fixture = TestBed.createComponent(ScoreboardComponent);
    fixture.detectChanges();

    const resetScoresBtn = fixture.nativeElement.querySelector('#btn-reset-scoreboard') as HTMLButtonElement;
    resetScoresBtn.click();

    expect(mockFacade.resetScoreboard).toHaveBeenCalled();
  });
});
