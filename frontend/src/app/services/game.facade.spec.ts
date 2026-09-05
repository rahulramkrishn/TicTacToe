import { TestBed } from '@angular/core/testing';
import { of, throwError, Subject } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { GameFacade } from './game.facade';
import { GameApiService } from './game-api.service';
import { ScoreboardApiService } from './scoreboard-api.service';
import { GameState, Scoreboard } from '../models/game.models';

describe('GameFacade', () => {
  let facade: GameFacade;
  let gameApiMock: any;
  let scoreboardApiMock: any;

  const mockInitialGameState: GameState = {
    gameId: '11111111-1111-1111-1111-111111111111',
    board: Array(9).fill(null),
    currentPlayer: 'X',
    mode: 'TwoPlayer',
    status: 'InProgress',
    winner: null,
    winningCells: [],
    moveHistory: [],
    canUndo: false,
    scoreboard: { xWins: 0, oWins: 0, draws: 0, totalGames: 0 }
  };

  const mockScoreboard: Scoreboard = {
    xWins: 2,
    oWins: 1,
    draws: 0,
    totalGames: 3
  };

  beforeEach(() => {
    gameApiMock = {
      createGame: vi.fn().mockReturnValue(of(mockInitialGameState)),
      getGame: vi.fn(),
      makeMove: vi.fn(),
      undo: vi.fn(),
      resetGame: vi.fn()
    };

    scoreboardApiMock = {
      getScoreboard: vi.fn().mockReturnValue(of(mockScoreboard)),
      resetScoreboard: vi.fn().mockReturnValue(of({ xWins: 0, oWins: 0, draws: 0, totalGames: 0 }))
    };

    TestBed.configureTestingModule({
      providers: [
        GameFacade,
        { provide: GameApiService, useValue: gameApiMock },
        { provide: ScoreboardApiService, useValue: scoreboardApiMock }
      ]
    });

    facade = TestBed.inject(GameFacade);
  });

  it('initGame should call createGame and hydrate gameState and scoreboard', () => {
    facade.initGame('TwoPlayer');

    expect(gameApiMock.createGame).toHaveBeenCalledWith('TwoPlayer');
    expect(facade.gameState()).toEqual(mockInitialGameState);
    expect(facade.scoreboard()).toEqual(mockInitialGameState.scoreboard);
    expect(facade.isInitialized()).toBe(true);
    expect(facade.loading()).toBe(false);
    expect(facade.error()).toBeNull();
  });

  it('initGame should handle failure without rendering a fake game', () => {
    const errorResponse = {
      error: {
        type: 'https://api.tictactoe.com/errors/internal-server-error',
        title: 'Internal Server Error',
        status: 500,
        code: 'INTERNAL_SERVER_ERROR',
        detail: 'Database unreachable',
        instance: '/api/games',
        traceId: 'trace-abc-123'
      }
    };
    gameApiMock.createGame.mockReturnValue(throwError(() => errorResponse));

    facade.initGame('TwoPlayer');

    expect(facade.gameState()).toBeNull();
    expect(facade.isInitialized()).toBe(false);
    expect(facade.loading()).toBe(false);
    expect(facade.error()).not.toBeNull();
    expect(facade.error()?.code).toBe('INTERNAL_SERVER_ERROR');
    expect(facade.error()?.traceId).toBe('trace-abc-123');
  });

  it('newGame should call createGame with requested mode and update state', () => {
    const computerGame: GameState = {
      ...mockInitialGameState,
      gameId: '22222222-2222-2222-2222-222222222222',
      mode: 'Computer'
    };
    gameApiMock.createGame.mockReturnValue(of(computerGame));

    facade.newGame('Computer');

    expect(gameApiMock.createGame).toHaveBeenCalledWith('Computer');
    expect(facade.gameId()).toBe(computerGame.gameId);
    expect(facade.mode()).toBe('Computer');
  });

  it('playMove in Computer Mode makes exactly ONE HTTP call and applies atomic turn pair', () => {
    facade.initGame('Computer');

    const stateAfterTurnPair: GameState = {
      ...mockInitialGameState,
      board: ['X', null, null, null, 'O', null, null, null, null],
      currentPlayer: 'X',
      canUndo: true,
      moveHistory: [
        { moveNumber: 1, player: 'X', cellIndex: 0 },
        { moveNumber: 2, player: 'O', cellIndex: 4 }
      ]
    };
    gameApiMock.makeMove.mockReturnValue(of(stateAfterTurnPair));

    facade.playMove(0);

    // Assert exactly ONE call was made for move 0
    expect(gameApiMock.makeMove).toHaveBeenCalledTimes(1);
    expect(gameApiMock.makeMove).toHaveBeenCalledWith(mockInitialGameState.gameId, 'X', 0);
    expect(facade.board()[0]).toBe('X');
    expect(facade.board()[4]).toBe('O');
    expect(facade.moveHistory().length).toBe(2);
    expect(facade.currentPlayer()).toBe('X');
  });

  it('playMove drops duplicate clicks when already loading', () => {
    facade.initGame('TwoPlayer');

    const moveSubject = new Subject<GameState>();
    gameApiMock.makeMove.mockReturnValue(moveSubject);

    facade.playMove(0);
    expect(facade.loading()).toBe(true);

    // While in-flight, rapid second click is dropped
    facade.playMove(1);
    expect(gameApiMock.makeMove).toHaveBeenCalledTimes(1);

    // Complete the first move
    moveSubject.next(mockInitialGameState);
    moveSubject.complete();
    expect(facade.loading()).toBe(false);
  });

  it('playMove preserves complete RFC 7807 ProblemDetails with all 7 fields on error', () => {
    facade.initGame('TwoPlayer');

    const conflictError = {
      error: {
        type: 'https://api.tictactoe.com/errors/cell-occupied',
        title: 'Cell Occupied',
        status: 409,
        code: 'CELL_OCCUPIED',
        detail: 'Cell 4 is already occupied.',
        instance: '/api/games/11111111-1111-1111-1111-111111111111/moves',
        traceId: 'trace-409-uuid'
      }
    };
    gameApiMock.makeMove.mockReturnValue(throwError(() => conflictError));

    facade.playMove(4);

    const error = facade.error();
    expect(error).not.toBeNull();
    expect(error?.type).toBe('https://api.tictactoe.com/errors/cell-occupied');
    expect(error?.title).toBe('Cell Occupied');
    expect(error?.status).toBe(409);
    expect(error?.code).toBe('CELL_OCCUPIED');
    expect(error?.detail).toBe('Cell 4 is already occupied.');
    expect(error?.instance).toBe('/api/games/11111111-1111-1111-1111-111111111111/moves');
    expect(error?.traceId).toBe('trace-409-uuid');
    expect(facade.loading()).toBe(false);
  });

  it('finalize() resets loading to false on both success and error completion', () => {
    facade.initGame('TwoPlayer');

    // Test success completion via Subject
    const successSubject = new Subject<GameState>();
    gameApiMock.makeMove.mockReturnValue(successSubject);
    facade.playMove(0);
    expect(facade.loading()).toBe(true);
    successSubject.next(mockInitialGameState);
    successSubject.complete();
    expect(facade.loading()).toBe(false);

    // Test error completion via Subject
    const errorSubject = new Subject<GameState>();
    gameApiMock.makeMove.mockReturnValue(errorSubject);
    facade.playMove(1);
    expect(facade.loading()).toBe(true);
    errorSubject.error({ error: { code: 'INVALID_MOVE', status: 400 } });
    expect(facade.loading()).toBe(false);
  });

  it('scoreboard is hydrated strictly from backend response and never modified client-side', () => {
    facade.initGame('TwoPlayer');
    expect(facade.scoreboard()).toEqual({ xWins: 0, oWins: 0, draws: 0, totalGames: 0 });

    const winningState: GameState = {
      ...mockInitialGameState,
      status: 'Won',
      winner: 'X',
      winningCells: [0, 1, 2],
      scoreboard: { xWins: 1, oWins: 0, draws: 0, totalGames: 1 }
    };
    gameApiMock.makeMove.mockReturnValue(of(winningState));

    facade.playMove(2);

    // Scoreboard must match backend response exactly
    expect(facade.scoreboard()).toEqual({ xWins: 1, oWins: 0, draws: 0, totalGames: 1 });
    expect(facade.gameState()?.scoreboard.xWins).toBe(1);
  });

  it('resetGame calls reset API and preserves existing gameId', () => {
    facade.initGame('TwoPlayer');

    const resetState: GameState = {
      ...mockInitialGameState,
      board: Array(9).fill(null),
      status: 'InProgress',
      canUndo: false,
      moveHistory: []
    };
    gameApiMock.resetGame.mockReturnValue(of(resetState));

    facade.resetGame();

    expect(gameApiMock.resetGame).toHaveBeenCalledWith(mockInitialGameState.gameId);
    expect(facade.gameId()).toBe(mockInitialGameState.gameId);
    expect(facade.board().every(c => c === null)).toBe(true);
  });

  it('newGame allocates a distinct gameId from previous session', () => {
    facade.initGame('TwoPlayer');
    const initialId = facade.gameId();

    const secondGame: GameState = {
      ...mockInitialGameState,
      gameId: '99999999-9999-9999-9999-999999999999',
      mode: 'TwoPlayer'
    };
    gameApiMock.createGame.mockReturnValue(of(secondGame));

    facade.newGame('TwoPlayer');

    expect(facade.gameId()).toBe('99999999-9999-9999-9999-999999999999');
    expect(facade.gameId()).not.toBe(initialId);
  });

  it('undo reverts move using backend response and updates scoreboard and canUndo', () => {
    facade.initGame('TwoPlayer');

    const undoneState: GameState = {
      ...mockInitialGameState,
      board: Array(9).fill(null),
      canUndo: false,
      moveHistory: []
    };
    gameApiMock.undo.mockReturnValue(of(undoneState));

    // Simulate canUndo=true in active state
    facade['_gameState'].set({ ...mockInitialGameState, canUndo: true });
    facade.undo();

    expect(gameApiMock.undo).toHaveBeenCalledWith(mockInitialGameState.gameId);
    expect(facade.canUndo()).toBe(false);
  });

  it('resetScoreboard calls scoreboard API and updates score counters', () => {
    facade.initGame('TwoPlayer');

    const zeroScores: Scoreboard = { xWins: 0, oWins: 0, draws: 0, totalGames: 0 };
    scoreboardApiMock.resetScoreboard.mockReturnValue(of(zeroScores));

    facade.resetScoreboard();

    expect(scoreboardApiMock.resetScoreboard).toHaveBeenCalled();
    expect(facade.scoreboard()).toEqual(zeroScores);
    expect(facade.gameState()?.scoreboard).toEqual(zeroScores);
  });
});
