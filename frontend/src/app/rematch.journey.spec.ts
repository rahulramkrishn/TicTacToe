import { TestBed, ComponentFixture } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { Observable, Subject, of, throwError } from 'rxjs';
import { App } from './app';
import { GameFacade } from './services/game.facade';
import { GameApiService } from './services/game-api.service';
import { ScoreboardApiService } from './services/scoreboard-api.service';
import { REMATCH_BUTTON_ENABLED } from './feature-flags';
import { GameMode, GameState, Player, Scoreboard } from './models/game.models';

const SCORE: Scoreboard = { xWins: 2, oWins: 1, draws: 1, totalGames: 4 };
const EMPTY: Array<Player | null> = Array(9).fill(null);

function wonGame(id: string, mode: GameMode, winner: Player, scoreboard: Scoreboard = SCORE): GameState {
  const board: Array<Player | null> = [winner, winner, winner, null, null, null, null, null, null];
  if (winner === 'X') { board[3] = 'O'; board[4] = 'O'; } else { board[3] = 'X'; board[4] = 'X'; board[8] = 'X'; }
  return {
    gameId: id, board, currentPlayer: winner, mode, status: 'Won', winner,
    winningCells: [0, 1, 2], moveHistory: [], canUndo: false, scoreboard
  };
}

function freshGame(id: string, mode: GameMode = 'TwoPlayer', scoreboard: Scoreboard = SCORE): GameState {
  return {
    gameId: id, board: [...EMPTY], currentPlayer: 'X', mode, status: 'InProgress', winner: null,
    winningCells: [], moveHistory: [], canUndo: false, scoreboard
  };
}

describe('Rematch journey', () => {
  let api: { createGame: ReturnType<typeof vi.fn>; makeMove: ReturnType<typeof vi.fn>;
             resetGame: ReturnType<typeof vi.fn>; undo: ReturnType<typeof vi.fn>; getGame: ReturnType<typeof vi.fn> };
  let facade: GameFacade;
  let fixture: ComponentFixture<App>;
  let el: HTMLElement;

  /** Renders App; the first createGame seeds g1, then the mock is cleared. */
  async function start(g1: GameState, flag = true) {
    api = {
      createGame: vi.fn().mockReturnValue(of(g1)),
      makeMove: vi.fn(), resetGame: vi.fn(), undo: vi.fn(), getGame: vi.fn()
    };
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        { provide: GameApiService, useValue: api },
        { provide: ScoreboardApiService, useValue: { getScoreboard: vi.fn(), resetScoreboard: vi.fn() } },
        { provide: REMATCH_BUTTON_ENABLED, useValue: flag }
      ]
    }).compileComponents();
    fixture = TestBed.createComponent(App);
    facade = TestBed.inject(GameFacade);
    fixture.detectChanges();
    fixture.detectChanges();
    el = fixture.nativeElement as HTMLElement;
    api.createGame.mockClear();
  }

  const q = <T extends HTMLElement = HTMLButtonElement>(sel: string) => el.querySelector(sel) as T | null;
  const click = (sel: string) => { q(sel)?.click(); fixture.detectChanges(); };
  const text = (sel: string) => (q(sel)?.textContent ?? '').trim();
  const next = (obs: Observable<GameState>) => api.createGame.mockReturnValue(obs);

  describe('flag on', () => {
    it('T-7 (AC-4): no #btn-rematch before or after a move in a fresh game', async () => {
      await start(freshGame('G1'));
      expect(facade.status()).toBe('InProgress');
      expect(q('#btn-rematch')).toBeNull();

      const moved: GameState = { ...freshGame('G1'), board: [...EMPTY.slice(0, 4), 'X', ...EMPTY.slice(5)], currentPlayer: 'O' };
      api.makeMove.mockReturnValue(of(moved));
      click('#cell-4');

      expect(api.makeMove).toHaveBeenCalledWith('G1', 'X', 4);
      expect(facade.board()[4]).toBe('X');
      expect(q('#btn-rematch')).toBeNull();
    });

    it('T-8 (AC-5): rematch of a won TwoPlayer game starts a new empty game G2', async () => {
      await start(wonGame('G1', 'TwoPlayer', 'X'));
      next(of(freshGame('G2')));
      expect(q('#btn-rematch')).not.toBeNull();
      click('#btn-rematch');

      expect(api.createGame).toHaveBeenCalledTimes(1);
      expect(api.createGame).toHaveBeenCalledWith('TwoPlayer');
      expect(facade.gameId()).toBe('G2');
      expect(facade.board().every(c => c === null)).toBe(true);
      expect(facade.status()).toBe('InProgress');
      expect(q('#btn-rematch')).toBeNull();
    });

    it('T-10 (AC-6): rematch of a Computer-mode game requests Computer mode', async () => {
      await start(wonGame('G1', 'Computer', 'O'));
      next(of(freshGame('G2', 'Computer')));
      expect(q('#btn-rematch')).not.toBeNull();
      click('#btn-rematch');

      expect(api.createGame).toHaveBeenCalledTimes(1);
      expect(api.createGame).toHaveBeenCalledWith('Computer');
      expect(facade.mode()).toBe('Computer');
      expect(facade.status()).toBe('InProgress');
    });

    it('T-11 (AC-7): scoreboard is unchanged when G2 carries the same scoreboard', async () => {
      await start(wonGame('G1', 'TwoPlayer', 'X'));
      expect([text('#score-x-wins'), text('#score-o-wins'), text('#score-draws')]).toEqual(['2', '1', '1']);
      next(of(freshGame('G2')));
      expect(q('#btn-rematch')).not.toBeNull();
      click('#btn-rematch');

      expect(facade.gameId()).toBe('G2');
      expect([text('#score-x-wins'), text('#score-o-wins'), text('#score-draws')]).toEqual(['2', '1', '1']);
    });

    it('T-12 (AC-7): scoreboard comes from the server response of the rematch', async () => {
      await start(wonGame('G1', 'TwoPlayer', 'X'));
      next(of(freshGame('G2', 'TwoPlayer', { xWins: 3, oWins: 1, draws: 1, totalGames: 5 })));
      expect(q('#btn-rematch')).not.toBeNull();
      click('#btn-rematch');

      expect(text('#score-x-wins')).toBe('3');
    });

    it('T-15 (AC-8): two clicks while pending produce one createGame call and a disabled button', async () => {
      await start(wonGame('G1', 'TwoPlayer', 'X'));
      next(new Subject<GameState>());
      expect(q('#btn-rematch')).not.toBeNull();
      click('#btn-rematch');
      click('#btn-rematch');

      expect(api.createGame).toHaveBeenCalledTimes(1);
      expect(q('#btn-rematch')?.disabled).toBe(true);
    });

    it('T-16 (AC-8): when the pending request emits G2, loading ends and Rematch disappears', async () => {
      await start(wonGame('G1', 'TwoPlayer', 'X'));
      const pending = new Subject<GameState>();
      next(pending);
      expect(q('#btn-rematch')).not.toBeNull();
      click('#btn-rematch');

      pending.next(freshGame('G2'));
      pending.complete();
      fixture.detectChanges();

      expect(facade.loading()).toBe(false);
      expect(facade.gameId()).toBe('G2');
      expect(facade.status()).toBe('InProgress');
      expect(q('#btn-rematch')).toBeNull();
    });

    it('T-17 (AC-9): failed rematch keeps G1, shows the error and keeps an enabled button', async () => {
      const g1 = wonGame('G1', 'TwoPlayer', 'X');
      await start(g1);
      next(throwError(() => ({ error: { title: 'Boom', code: 'E_BOOM', status: 500, detail: 'bad', type: 't', instance: '', traceId: '' } })));
      expect(q('#btn-rematch')).not.toBeNull();
      click('#btn-rematch');

      expect(facade.gameState()).toEqual(g1);
      const alert = q('.error-banner[role=alert]');
      expect(alert).not.toBeNull();
      expect(alert?.textContent).toContain('Boom');
      expect(alert?.textContent).toContain('E_BOOM');
      expect(q('#btn-rematch')).not.toBeNull();
      expect(q('#btn-rematch')?.disabled).toBe(false);
    });

    it('T-18 (AC-9): network error without body keeps G1 and an enabled button', async () => {
      const g1 = wonGame('G1', 'TwoPlayer', 'X');
      await start(g1);
      next(throwError(() => new Error('network down')));
      expect(q('#btn-rematch')).not.toBeNull();
      expect(() => click('#btn-rematch')).not.toThrow();

      expect(facade.gameState()).toEqual(g1);
      expect(q('#btn-rematch')).not.toBeNull();
      expect(q('#btn-rematch')?.disabled).toBe(false);
    });

    it('T-19 (AC-9): retry after a failed rematch starts the new game and clears the error', async () => {
      await start(wonGame('G1', 'TwoPlayer', 'X'));
      api.createGame
        .mockReturnValueOnce(throwError(() => ({ error: { title: 'Boom', code: 'E_BOOM' } })))
        .mockReturnValueOnce(of(freshGame('G2')));
      expect(q('#btn-rematch')).not.toBeNull();
      click('#btn-rematch');
      expect(q('.error-banner')).not.toBeNull();

      click('#btn-rematch');

      expect(facade.gameId()).toBe('G2');
      expect(q('.error-banner')).toBeNull();
    });

    it('T-20 (AC-10): Reset Board and New Game are present alongside Rematch', async () => {
      await start(wonGame('G1', 'TwoPlayer', 'X'));
      expect(q('#btn-rematch')).not.toBeNull();
      expect(q('#btn-reset-game')).not.toBeNull();
      expect(q('#btn-new-game')).not.toBeNull();
    });

    it('T-21 (AC-10): Reset Board calls resetGame(G1) and New Game calls createGame(mode)', async () => {
      const g1 = wonGame('G1', 'TwoPlayer', 'X');
      await start(g1);
      expect(q('#btn-rematch')).not.toBeNull();
      api.resetGame.mockReturnValue(of(g1));
      click('#btn-reset-game');
      expect(api.resetGame).toHaveBeenCalledTimes(1);
      expect(api.resetGame).toHaveBeenCalledWith('G1');

      next(of(freshGame('G2')));
      click('#btn-new-game');
      expect(api.createGame).toHaveBeenCalledTimes(1);
      expect(api.createGame).toHaveBeenCalledWith('TwoPlayer');
    });

    it('T-30 (edge): switching mode from the header leaves no #btn-rematch once the new game is InProgress', async () => {
      await start(wonGame('G1', 'TwoPlayer', 'X'));
      expect(q('#btn-rematch')).not.toBeNull();
      next(of(freshGame('G2', 'Computer')));
      const modeButtons = el.querySelectorAll<HTMLButtonElement>('.mode-btn');
      modeButtons[1].click();
      fixture.detectChanges();

      expect(api.createGame).toHaveBeenCalledWith('Computer');
      expect(facade.status()).toBe('InProgress');
      expect(q('#btn-rematch')).toBeNull();
    });
  });

  describe('flag off', () => {
    it('T-29 (AC-11): no #btn-rematch while New Game and Reset Board still work', async () => {
      const g1 = wonGame('G1', 'TwoPlayer', 'X');
      await start(g1, false);
      expect(facade.status()).toBe('Won');
      expect(q('#btn-rematch')).toBeNull();

      api.resetGame.mockReturnValue(of(g1));
      click('#btn-reset-game');
      expect(api.resetGame).toHaveBeenCalledWith('G1');

      next(of(freshGame('G2')));
      click('#btn-new-game');
      expect(api.createGame).toHaveBeenCalledWith('TwoPlayer');
      expect(q('#btn-rematch')).toBeNull();
    });
  });
});
