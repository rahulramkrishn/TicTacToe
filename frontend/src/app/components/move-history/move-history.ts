import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GameFacade } from '../../services/game.facade';

@Component({
  selector: 'app-move-history',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './move-history.html',
  styleUrl: './move-history.css'
})
export class MoveHistoryComponent {
  readonly facade = inject(GameFacade);

  getRow(cellIndex: number): number {
    return Math.floor(cellIndex / 3) + 1;
  }

  getColumn(cellIndex: number): number {
    return (cellIndex % 3) + 1;
  }
}
