import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { REMATCH_BUTTON_ENABLED } from './feature-flags';
import { StatusBannerComponent } from './components/status-banner/status-banner';
import { GameFacade } from './services/game.facade';
import { GameStatus, Player } from './models/game.models';
import { environment } from '../environments/environment';
import { environment as devEnvironment } from '../environments/environment.development';

describe('rematchButton feature flag wiring', () => {
  let mockFacade: any;

  beforeEach(() => {
    mockFacade = {
      isInitialized: signal<boolean>(true),
      status: signal<GameStatus>('InProgress'),
      currentPlayer: signal<Player>('X'),
      mode: signal<string>('TwoPlayer'),
      winner: signal<Player | null>(null),
      loading: signal<boolean>(false),
      newGame: vi.fn()
    };
    TestBed.configureTestingModule({
      imports: [StatusBannerComponent],
      providers: [{ provide: GameFacade, useValue: mockFacade }]
    });
  });

  const rematch = (fixture: any): HTMLButtonElement | null =>
    fixture.nativeElement.querySelector('#btn-rematch');

  // M7
  it('development environment enables rematchButton', () => {
    expect(devEnvironment.features.rematchButton).toBe(true);
  });

  // M8
  it('default (production) environment keeps rematchButton dark-launched (false)', () => {
    expect(environment.features.rematchButton).toBe(false);
  });

  // M9
  it('token defaults to environment.features.rematchButton when not overridden', () => {
    expect(TestBed.inject(REMATCH_BUTTON_ENABLED)).toBe(environment.features.rematchButton);
  });

  // M9 component level: only meaningful when the compiled value is false
  it.skipIf(environment.features.rematchButton)(
    'without token override and compiled flag false, Won and Draw show no #btn-rematch',
    () => {
      const results: (HTMLButtonElement | null)[] = [];

      mockFacade.status.set('Won');
      mockFacade.winner.set('X');
      const won = TestBed.createComponent(StatusBannerComponent);
      won.detectChanges();
      results.push(rematch(won));
      const wonText = won.nativeElement.textContent;

      mockFacade.status.set('Draw');
      mockFacade.winner.set(null);
      const draw = TestBed.createComponent(StatusBannerComponent);
      draw.detectChanges();
      results.push(rematch(draw));
      const drawText = draw.nativeElement.textContent;

      expect(wonText).toContain('Player X Wins!');
      expect(drawText).toContain('Game ended in a Draw');
      expect(results).toEqual([null, null]);
    }
  );

  // Unproven direction: show-on-InProgress mutant
  it('with token true, InProgress hides #btn-rematch while Won shows it', () => {
    TestBed.overrideProvider(REMATCH_BUTTON_ENABLED, { useValue: true });

    const inProgress = TestBed.createComponent(StatusBannerComponent);
    inProgress.detectChanges();
    const inProgressBtn = rematch(inProgress);
    const inProgressText = inProgress.nativeElement.textContent;

    mockFacade.status.set('Won');
    mockFacade.winner.set('X');
    const won = TestBed.createComponent(StatusBannerComponent);
    won.detectChanges();
    const wonBtn = rematch(won);

    expect(inProgressText).toContain('Current Turn');
    expect(inProgressBtn).toBeNull();
    expect(wonBtn).not.toBeNull();
  });
});
