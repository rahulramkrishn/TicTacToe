import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { GameApiService } from './game-api.service';
import { API_BASE_URL } from './api-config';
import { GameState } from '../models/game.models';

describe('GameApiService', () => {
  let service: GameApiService;
  let httpMock: HttpTestingController;
  const baseUrl = 'http://localhost:5000';

  const mockGameState: GameState = {
    gameId: '123e4567-e89b-12d3-a456-426614174000',
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

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        GameApiService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: baseUrl }
      ]
    });

    service = TestBed.inject(GameApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('createGame should send POST to /api/games with mode', () => {
    service.createGame('TwoPlayer').subscribe((res) => {
      expect(res).toEqual(mockGameState);
    });

    const req = httpMock.expectOne(`${baseUrl}/api/games`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ mode: 'TwoPlayer' });
    req.flush(mockGameState);
  });

  it('getGame should send GET to /api/games/{id}', () => {
    service.getGame(mockGameState.gameId).subscribe((res) => {
      expect(res).toEqual(mockGameState);
    });

    const req = httpMock.expectOne(`${baseUrl}/api/games/${mockGameState.gameId}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockGameState);
  });

  it('makeMove should send POST to /api/games/{id}/moves with player and cellIndex', () => {
    service.makeMove(mockGameState.gameId, 'X', 4).subscribe((res) => {
      expect(res).toEqual(mockGameState);
    });

    const req = httpMock.expectOne(`${baseUrl}/api/games/${mockGameState.gameId}/moves`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ player: 'X', cellIndex: 4 });
    req.flush(mockGameState);
  });

  it('undo should send POST to /api/games/{id}/undo', () => {
    service.undo(mockGameState.gameId).subscribe((res) => {
      expect(res).toEqual(mockGameState);
    });

    const req = httpMock.expectOne(`${baseUrl}/api/games/${mockGameState.gameId}/undo`);
    expect(req.request.method).toBe('POST');
    req.flush(mockGameState);
  });

  it('resetGame should send POST to /api/games/{id}/reset', () => {
    service.resetGame(mockGameState.gameId).subscribe((res) => {
      expect(res).toEqual(mockGameState);
    });

    const req = httpMock.expectOne(`${baseUrl}/api/games/${mockGameState.gameId}/reset`);
    expect(req.request.method).toBe('POST');
    req.flush(mockGameState);
  });
});
