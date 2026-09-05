import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GameFacade } from '../../services/game.facade';
import { GameMode } from '../../models/game.models';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './header.html',
  styleUrl: './header.css'
})
export class HeaderComponent {
  readonly facade = inject(GameFacade);

  switchMode(newMode: GameMode): void {
    if (this.facade.mode() !== newMode) {
      this.facade.newGame(newMode);
    }
  }
}
