import { Injectable, computed, inject, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { GameApiService } from './game-api.service';
import { ScoreboardApiService } from './scoreboard-api.service';
import { GameMode, GameState, Player, ProblemDetails, Scoreboard } from '../models/game.models';

@Injectable({
  providedIn: 'root'
})
export class GameFacade {
  private readonly gameApi = inject(GameApiService);
  private readonly scoreboardApi = inject(ScoreboardApiService);

  // Private reactive signals
  private readonly _gameState = signal<GameState | null>(null);
  private readonly _scoreboard = signal<Scoreboard | null>(null);
  private readonly _loading = signal<boolean>(false);
  private readonly _error = signal<ProblemDetails | null>(null);

  // Public readonly state signals
  readonly gameState = this._gameState.asReadonly();
  readonly scoreboard = this._scoreboard.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  // Public derived computed signals
  readonly gameId = computed(() => this._gameState()?.gameId ?? '');
  readonly board = computed(() => this._gameState()?.board ?? Array<Player | null>(9).fill(null));
  readonly currentPlayer = computed(() => this._gameState()?.currentPlayer ?? 'X');
  readonly mode = computed(() => this._gameState()?.mode ?? 'TwoPlayer');
  readonly status = computed(() => this._gameState()?.status ?? 'InProgress');
  readonly winner = computed(() => this._gameState()?.winner ?? null);
  readonly winningCells = computed(() => this._gameState()?.winningCells ?? []);
  readonly moveHistory = computed(() => this._gameState()?.moveHistory ?? []);
  readonly canUndo = computed(() => (this._gameState()?.canUndo ?? false) && !this._loading());
  readonly isGameOver = computed(() => this.status() !== 'InProgress');
  readonly isInitialized = computed(() => this._gameState() !== null);

  /**
   * Initializes the game session and synchronizes session scoreboard on app boot.
   */
  initGame(mode: GameMode = 'TwoPlayer'): void {
    if (this._loading()) {
      return;
    }

    this._loading.set(true);
    this._error.set(null);

    this.gameApi.createGame(mode).pipe(
      finalize(() => this._loading.set(false))
    ).subscribe({
      next: (state) => {
        this._gameState.set(state);
        this._scoreboard.set(state.scoreboard);
      },
      error: (err) => {
        this._error.set(this.extractProblemDetails(err));
      }
    });
  }

  /**
   * Creates a brand new game session with a distinct gameId.
   */
  newGame(mode: GameMode): void {
    if (this._loading()) {
      return;
    }

    this._loading.set(true);
    this._error.set(null);

    this.gameApi.createGame(mode).pipe(
      finalize(() => this._loading.set(false))
    ).subscribe({
      next: (state) => {
        this._gameState.set(state);
        this._scoreboard.set(state.scoreboard);
      },
      error: (err) => {
        this._error.set(this.extractProblemDetails(err));
      }
    });
  }

  /**
   * Submits a move for the current player at the selected cell index.
   * In Computer Mode, this single call triggers both the human move and the computer response atomically.
   */
  playMove(cellIndex: number): void {
    if (this._loading()) {
      return;
    }

    const state = this._gameState();
    if (!state || state.status !== 'InProgress') {
      return;
    }

    if (cellIndex < 0 || cellIndex > 8 || state.board[cellIndex] !== null) {
      return;
    }

    this._loading.set(true);
    this._error.set(null);

    this.gameApi.makeMove(state.gameId, state.currentPlayer, cellIndex).pipe(
      finalize(() => this._loading.set(false))
    ).subscribe({
      next: (updatedState) => {
        this._gameState.set(updatedState);
        this._scoreboard.set(updatedState.scoreboard);
      },
      error: (err) => {
        this._error.set(this.extractProblemDetails(err));
      }
    });
  }

  /**
   * Reverts the previous move (1 move in TwoPlayer, 2 moves in Computer Mode).
   */
  undo(): void {
    if (this._loading() || !this.canUndo()) {
      return;
    }

    const state = this._gameState();
    if (!state) {
      return;
    }

    this._loading.set(true);
    this._error.set(null);

    this.gameApi.undo(state.gameId).pipe(
      finalize(() => this._loading.set(false))
    ).subscribe({
      next: (updatedState) => {
        this._gameState.set(updatedState);
        this._scoreboard.set(updatedState.scoreboard);
      },
      error: (err) => {
        this._error.set(this.extractProblemDetails(err));
      }
    });
  }

  /**
   * Resets the current game board and history while strictly PRESERVING the existing gameId.
   */
  resetGame(): void {
    if (this._loading()) {
      return;
    }

    const state = this._gameState();
    if (!state) {
      return;
    }

    this._loading.set(true);
    this._error.set(null);

    this.gameApi.resetGame(state.gameId).pipe(
      finalize(() => this._loading.set(false))
    ).subscribe({
      next: (updatedState) => {
        this._gameState.set(updatedState);
        this._scoreboard.set(updatedState.scoreboard);
      },
      error: (err) => {
        this._error.set(this.extractProblemDetails(err));
      }
    });
  }

  /**
   * Resets the session-level scoreboard without modifying the current game board.
   */
  resetScoreboard(): void {
    if (this._loading()) {
      return;
    }

    this._loading.set(true);
    this._error.set(null);

    this.scoreboardApi.resetScoreboard().pipe(
      finalize(() => this._loading.set(false))
    ).subscribe({
      next: (score) => {
        this._scoreboard.set(score);
        const current = this._gameState();
        if (current) {
          this._gameState.set({
            ...current,
            scoreboard: score
          });
        }
      },
      error: (err) => {
        this._error.set(this.extractProblemDetails(err));
      }
    });
  }

  /**
   * Dismisses the active error notification.
   */
  dismissError(): void {
    this._error.set(null);
  }

  private extractProblemDetails(err: unknown): ProblemDetails {
    if (err && typeof err === 'object' && 'error' in err) {
      const httpErr = (err as { error?: unknown }).error;
      if (httpErr && typeof httpErr === 'object' && 'code' in httpErr) {
        const p = httpErr as Partial<ProblemDetails>;
        return {
          type: p.type || 'https://api.tictactoe.com/errors/internal-server-error',
          title: p.title || 'Error',
          status: p.status || 500,
          code: p.code || 'UNKNOWN_ERROR',
          detail: p.detail || 'An unexpected error occurred.',
          instance: p.instance || '',
          traceId: p.traceId || ''
        };
      }
    }
    return {
      type: 'https://api.tictactoe.com/errors/network-error',
      title: 'Communication Failure',
      status: 0,
      code: 'NETWORK_ERROR',
      detail: 'Unable to reach the game server. Please ensure the backend is running at http://localhost:5000.',
      instance: '',
      traceId: ''
    };
  }
}
