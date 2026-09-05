import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HeaderComponent } from './components/header/header';
import { StatusBannerComponent } from './components/status-banner/status-banner';
import { GameBoardComponent } from './components/game-board/game-board';
import { GameControlsComponent } from './components/game-controls/game-controls';
import { ScoreboardComponent } from './components/scoreboard/scoreboard';
import { MoveHistoryComponent } from './components/move-history/move-history';
import { ErrorAlertComponent } from './components/error-alert/error-alert';
import { GameFacade } from './services/game.facade';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    HeaderComponent,
    StatusBannerComponent,
    GameBoardComponent,
    GameControlsComponent,
    ScoreboardComponent,
    MoveHistoryComponent,
    ErrorAlertComponent
  ],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  readonly facade = inject(GameFacade);

  ngOnInit(): void {
    this.facade.initGame('TwoPlayer');
  }
}
