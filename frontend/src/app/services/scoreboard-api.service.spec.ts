import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { ScoreboardApiService } from './scoreboard-api.service';
import { API_BASE_URL } from './api-config';
import { Scoreboard } from '../models/game.models';

describe('ScoreboardApiService', () => {
  let service: ScoreboardApiService;
  let httpMock: HttpTestingController;
  const baseUrl = 'http://localhost:5000';

  const mockScoreboard: Scoreboard = {
    xWins: 3,
    oWins: 2,
    draws: 1,
    totalGames: 6
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ScoreboardApiService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: baseUrl }
      ]
    });

    service = TestBed.inject(ScoreboardApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getScoreboard should send GET to /api/scoreboard', () => {
    service.getScoreboard().subscribe((res) => {
      expect(res).toEqual(mockScoreboard);
    });

    const req = httpMock.expectOne(`${baseUrl}/api/scoreboard`);
    expect(req.request.method).toBe('GET');
    req.flush(mockScoreboard);
  });

  it('resetScoreboard should send POST to /api/scoreboard/reset', () => {
    const zeroScoreboard: Scoreboard = { xWins: 0, oWins: 0, draws: 0, totalGames: 0 };

    service.resetScoreboard().subscribe((res) => {
      expect(res).toEqual(zeroScoreboard);
    });

    const req = httpMock.expectOne(`${baseUrl}/api/scoreboard/reset`);
    expect(req.request.method).toBe('POST');
    req.flush(zeroScoreboard);
  });
});
