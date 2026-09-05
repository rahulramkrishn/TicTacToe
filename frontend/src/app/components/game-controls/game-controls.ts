import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GameFacade } from '../../services/game.facade';

@Component({
  selector: 'app-game-controls',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './game-controls.html',
  styleUrl: './game-controls.css'
})
export class GameControlsComponent {
  readonly facade = inject(GameFacade);
}
