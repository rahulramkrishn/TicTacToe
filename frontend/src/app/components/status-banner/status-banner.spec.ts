import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { describe, it, expect, beforeEach } from 'vitest';
import { StatusBannerComponent } from './status-banner';
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
      loading: signal<boolean>(false)
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
});
