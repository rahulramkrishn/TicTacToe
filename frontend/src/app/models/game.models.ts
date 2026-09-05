export type Player = 'X' | 'O';
export type GameMode = 'TwoPlayer' | 'Computer';
export type GameStatus = 'InProgress' | 'Won' | 'Draw';

export interface MoveDto {
  moveNumber: number;
  player: Player;
  cellIndex: number;
}

export interface Scoreboard {
  xWins: number;
  oWins: number;
  draws: number;
  totalGames: number;
}

export interface GameState {
  gameId: string;
  board: Array<Player | null>;
  currentPlayer: Player;
  mode: GameMode;
  status: GameStatus;
  winner: Player | null;
  winningCells: number[];
  moveHistory: MoveDto[];
  canUndo: boolean;
  scoreboard: Scoreboard;
}

export interface CreateGameRequest {
  mode: GameMode;
}

export interface MakeMoveRequest {
  player: Player;
  cellIndex: number;
}

export interface ProblemDetails {
  type: string;
  title: string;
  status: number;
  code: string;
  detail: string;
  instance: string;
  traceId: string;
}
