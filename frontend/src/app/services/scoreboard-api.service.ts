import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-config';
import { Scoreboard } from '../models/game.models';

@Injectable({
  providedIn: 'root'
})
export class ScoreboardApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getScoreboard(): Observable<Scoreboard> {
    return this.http.get<Scoreboard>(`${this.baseUrl}/api/scoreboard`);
  }

  resetScoreboard(): Observable<Scoreboard> {
    return this.http.post<Scoreboard>(`${this.baseUrl}/api/scoreboard/reset`, {});
  }
}
