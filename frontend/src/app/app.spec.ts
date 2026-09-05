import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { App } from './app';
import { GameFacade } from './services/game.facade';
import { Player } from './models/game.models';

describe('App', () => {
  let mockFacade: any;

  beforeEach(async () => {
    mockFacade = {
      isInitialized: signal<boolean>(true),
      loading: signal<boolean>(false),
      error: signal<null>(null),
      mode: signal<string>('TwoPlayer'),
      status: signal<string>('InProgress'),
      currentPlayer: signal<Player>('X'),
      winner: signal<null>(null),
      winningCells: signal<number[]>([]),
      board: signal<Array<Player | null>>(Array(9).fill(null)),
      canUndo: signal<boolean>(false),
      isGameOver: signal<boolean>(false),
      scoreboard: signal<any>({ xWins: 0, oWins: 0, draws: 0, totalGames: 0 }),
      moveHistory: signal<any[]>([]),
      initGame: vi.fn(),
      newGame: vi.fn(),
      playMove: vi.fn(),
      undo: vi.fn(),
      resetGame: vi.fn(),
      resetScoreboard: vi.fn(),
      dismissError: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [{ provide: GameFacade, useValue: mockFacade }]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should call initGame on initialization', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    expect(mockFacade.initGame).toHaveBeenCalledWith('TwoPlayer');
  });

  it('should render main title "Tic-Tac-Toe" in header', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Tic-Tac-Toe');
  });
});
