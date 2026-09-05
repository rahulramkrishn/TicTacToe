import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GameFacade } from '../../services/game.facade';

@Component({
  selector: 'app-status-banner',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './status-banner.html',
  styleUrl: './status-banner.css'
})
export class StatusBannerComponent {
  readonly facade = inject(GameFacade);
}
