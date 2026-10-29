import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { StatusBannerComponent } from './status-banner';
import { REMATCH_BUTTON_ENABLED } from '../../feature-flags';
import { GameFacade } from '../../services/game.facade';
import { GameStatus, Player } from '../../models/game.models';

describe('StatusBannerComponent', () => {
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

  it('should have role="status" and aria-live="polite" for screen reader announcements', () => {
    const fixture = TestBed.createComponent(StatusBannerComponent);
    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('.status-banner');
    expect(banner.getAttribute('role')).toBe('status');
    expect(banner.getAttribute('aria-live')).toBe('polite');
  });

  it('should display current turn during InProgress status', () => {
    mockFacade.currentPlayer.set('O');
    const fixture = TestBed.createComponent(StatusBannerComponent);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Player O');
  });

  it('should display winner announcement during Won status', () => {
    mockFacade.status.set('Won');
    mockFacade.winner.set('X');
    const fixture = TestBed.createComponent(StatusBannerComponent);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Player X Wins!');
  });

  it('should display draw announcement during Draw status', () => {
    mockFacade.status.set('Draw');
    const fixture = TestBed.createComponent(StatusBannerComponent);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Game ended in a Draw');
  });

  it('should display authoritative Player X turn in Computer Mode without misleading Thinking text', () => {
    mockFacade.mode.set('Computer');
    mockFacade.currentPlayer.set('X');
    mockFacade.status.set('InProgress');
    const fixture = TestBed.createComponent(StatusBannerComponent);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Player X');
    expect(text).not.toContain('Thinking');
  });

  // ---- rematch-button feature (T-n / AC-n from test-plan.md) ----

  const render = (flag: boolean) => {
    TestBed.configureTestingModule({
      providers: [{ provide: REMATCH_BUTTON_ENABLED, useValue: flag }]
    });
    const fixture = TestBed.createComponent(StatusBannerComponent);
    fixture.detectChanges();
    return fixture;
  };
  const rematch = (fixture: any): HTMLButtonElement | null =>
    fixture.nativeElement.querySelector('#btn-rematch');
  const norm = (s: string | null) => (s ?? '').replace(/\s+/g, ' ').trim();

  const setWonX = () => { mockFacade.status.set('Won'); mockFacade.winner.set('X'); };
  const setWonComputerO = () => {
    mockFacade.mode.set('Computer');
    mockFacade.status.set('Won');
    mockFacade.winner.set('O');
  };

  describe('flag on', () => {
    it('T-1 (AC-1): Won by X shows #btn-rematch with text "Rematch"', () => {
      setWonX();
      const btn = rematch(render(true));
      expect(btn).not.toBeNull();
      expect(btn?.querySelector('.btn-text')?.textContent?.trim()).toBe('Rematch');
    });

    it('T-2 (AC-2): Won by O in TwoPlayer shows #btn-rematch', () => {
      mockFacade.status.set('Won');
      mockFacade.winner.set('O');
      expect(rematch(render(true))).not.toBeNull();
    });

    it('T-3 (AC-2): Won by O in Computer mode shows #btn-rematch', () => {
      setWonComputerO();
      const fixture = render(true);
      expect(fixture.nativeElement.textContent).toContain('Computer Wins!');
      expect(rematch(fixture)).not.toBeNull();
    });

    it('T-4 (AC-3): Draw shows #btn-rematch', () => {
      mockFacade.status.set('Draw');
      expect(rematch(render(true))).not.toBeNull();
    });

    it('T-5 (AC-4): InProgress has no #btn-rematch', () => {
      const fixture = render(true);
      expect(fixture.nativeElement.textContent).toContain('Current Turn');
      expect(rematch(fixture)).toBeNull();
    });

    it('T-6 (AC-4): not initialized has no #btn-rematch', () => {
      mockFacade.isInitialized.set(false);
      const fixture = render(true);
      expect(fixture.nativeElement.textContent).toContain('Connecting to game server');
      expect(rematch(fixture)).toBeNull();
    });

    it('T-9 (AC-5): click calls facade.newGame once with facade.mode() (Computer)', () => {
      setWonComputerO();
      const btn = rematch(render(true));
      expect(btn).not.toBeNull();
      btn?.click();
      expect(mockFacade.newGame).toHaveBeenCalledTimes(1);
      expect(mockFacade.newGame).toHaveBeenCalledWith('Computer');
    });

    it('T-13 (AC-8): loading true disables the button and sets aria-disabled="true"', () => {
      setWonX();
      mockFacade.loading.set(true);
      const btn = rematch(render(true));
      expect(btn).not.toBeNull();
      expect(btn?.disabled).toBe(true);
      expect(btn?.getAttribute('aria-disabled')).toBe('true');
    });

    it('T-14 (AC-8): loading false leaves the button enabled with aria-disabled="false"', () => {
      setWonX();
      const btn = rematch(render(true));
      expect(btn).not.toBeNull();
      expect(btn?.disabled).toBe(false);
      expect(btn?.getAttribute('aria-disabled')).toBe('false');
    });
  });

  describe('flag off', () => {
    it('T-22 (AC-11): Won (X) has no #btn-rematch and no button element', () => {
      setWonX();
      const fixture = render(false);
      expect(fixture.nativeElement.textContent).toContain('Player X Wins!');
      expect(rematch(fixture)).toBeNull();
      expect(fixture.nativeElement.querySelectorAll('button').length).toBe(0);
    });

    it('T-23 (AC-11): Won (Computer/O) has no #btn-rematch and no button element', () => {
      setWonComputerO();
      const fixture = render(false);
      expect(fixture.nativeElement.textContent).toContain('Computer Wins!');
      expect(rematch(fixture)).toBeNull();
      expect(fixture.nativeElement.querySelectorAll('button').length).toBe(0);
    });

    it('T-24 (AC-11): Draw has no #btn-rematch and no button element', () => {
      mockFacade.status.set('Draw');
      const fixture = render(false);
      expect(fixture.nativeElement.textContent).toContain('Game ended in a Draw');
      expect(rematch(fixture)).toBeNull();
      expect(fixture.nativeElement.querySelectorAll('button').length).toBe(0);
    });

    // Characterization: pins today's banner output with the flag off.
    it('T-25 (AC-11, characterization): Won (X) banner is unchanged', () => {
      setWonX();
      const el = render(false).nativeElement as HTMLElement;
      const content = el.querySelector('.status-content') as HTMLElement;
      expect(norm(el.textContent)).toBe('🏆Victory! Player X Wins!');
      expect(Array.from(content.classList).sort()).toEqual(['status-content', 'winner-x', 'won']);
      expect(content.children.length).toBe(2);
    });

    it('T-26 (AC-11, characterization): Won (Computer/O) banner is unchanged', () => {
      setWonComputerO();
      const el = render(false).nativeElement as HTMLElement;
      const content = el.querySelector('.status-content') as HTMLElement;
      expect(norm(el.textContent)).toBe('🏆Victory! Computer Wins!');
      expect(Array.from(content.classList).sort()).toEqual(['status-content', 'winner-o', 'won']);
      expect(content.children.length).toBe(2);
    });

    it('T-27 (AC-11, characterization): Draw banner is unchanged', () => {
      mockFacade.status.set('Draw');
      const el = render(false).nativeElement as HTMLElement;
      const content = el.querySelector('.status-content') as HTMLElement;
      expect(norm(el.textContent)).toBe('🤝StalemateGame ended in a Draw');
      expect(Array.from(content.classList).sort()).toEqual(['draw', 'status-content']);
      expect(content.children.length).toBe(2);
    });

    it('T-28 (AC-11, characterization): InProgress banner is unchanged', () => {
      const el = render(false).nativeElement as HTMLElement;
      const content = el.querySelector('.status-content') as HTMLElement;
      expect(norm(el.textContent)).toBe('XCurrent TurnPlayer X');
      expect(Array.from(content.classList).sort()).toEqual(['in-progress', 'status-content', 'turn-x']);
      expect(content.children.length).toBe(2);
    });
  });
});
