import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GameFacade } from '../../services/game.facade';

@Component({
  selector: 'app-scoreboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './scoreboard.html',
  styleUrl: './scoreboard.css'
})
export class ScoreboardComponent {
  readonly facade = inject(GameFacade);
}
