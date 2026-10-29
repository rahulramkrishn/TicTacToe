import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GameFacade } from '../../services/game.facade';
import { REMATCH_BUTTON_ENABLED } from '../../feature-flags';

@Component({
  selector: 'app-status-banner',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './status-banner.html',
  styleUrl: './status-banner.css'
})
export class StatusBannerComponent {
  readonly facade = inject(GameFacade);
  readonly rematchEnabled = inject(REMATCH_BUTTON_ENABLED);
}
