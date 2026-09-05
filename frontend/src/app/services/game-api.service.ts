import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-config';
import { CreateGameRequest, GameMode, GameState, MakeMoveRequest, Player } from '../models/game.models';

@Injectable({
  providedIn: 'root'
})
export class GameApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  createGame(mode: GameMode = 'TwoPlayer'): Observable<GameState> {
    const payload: CreateGameRequest = { mode };
    return this.http.post<GameState>(`${this.baseUrl}/api/games`, payload);
  }

  getGame(gameId: string): Observable<GameState> {
    return this.http.get<GameState>(`${this.baseUrl}/api/games/${gameId}`);
  }

  makeMove(gameId: string, player: Player, cellIndex: number): Observable<GameState> {
    const payload: MakeMoveRequest = { player, cellIndex };
    return this.http.post<GameState>(`${this.baseUrl}/api/games/${gameId}/moves`, payload);
  }

  undo(gameId: string): Observable<GameState> {
    return this.http.post<GameState>(`${this.baseUrl}/api/games/${gameId}/undo`, {});
  }

  resetGame(gameId: string): Observable<GameState> {
    return this.http.post<GameState>(`${this.baseUrl}/api/games/${gameId}/reset`, {});
  }
}
