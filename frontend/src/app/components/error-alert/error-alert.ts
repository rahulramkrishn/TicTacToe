import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GameFacade } from '../../services/game.facade';

@Component({
  selector: 'app-error-alert',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './error-alert.html',
  styleUrl: './error-alert.css'
})
export class ErrorAlertComponent {
  readonly facade = inject(GameFacade);
}
