import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GameFacade } from '../../services/game.facade';
import { Player } from '../../models/game.models';

@Component({
  selector: 'app-game-board',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './game-board.html',
  styleUrl: './game-board.css'
})
export class GameBoardComponent {
  readonly facade = inject(GameFacade);

  getCellAriaLabel(index: number, value: Player | null): string {
    const row = Math.floor(index / 3) + 1;
    const col = (index % 3) + 1;
    if (value === null) {
      return `Cell ${index + 1}, row ${row} column ${col}, empty`;
    }
    return `Cell ${index + 1}, row ${row} column ${col}, occupied by Player ${value}`;
  }

  isCellDisabled(value: Player | null): boolean {
    return (
      this.facade.loading() ||
      !this.facade.isInitialized() ||
      this.facade.isGameOver() ||
      value !== null
    );
  }

  isWinningCell(index: number): boolean {
    return this.facade.winningCells().includes(index);
  }

  onCellClick(index: number): void {
    this.facade.playMove(index);
  }
}
