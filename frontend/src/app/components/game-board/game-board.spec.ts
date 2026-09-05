import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { GameBoardComponent } from './game-board';
import { GameFacade } from '../../services/game.facade';
import { Player } from '../../models/game.models';

describe('GameBoardComponent', () => {
  let mockFacade: any;

  beforeEach(() => {
    mockFacade = {
      board: signal<Array<Player | null>>(Array(9).fill(null)),
      currentPlayer: signal<Player>('X'),
      loading: signal<boolean>(false),
      isInitialized: signal<boolean>(true),
      isGameOver: signal<boolean>(false),
      winningCells: signal<number[]>([]),
      playMove: vi.fn()
    };

    TestBed.configureTestingModule({
      imports: [GameBoardComponent],
      providers: [{ provide: GameFacade, useValue: mockFacade }]
    });
  });

  it('should render 9 semantic button elements for the 3x3 grid', () => {
    const fixture = TestBed.createComponent(GameBoardComponent);
    fixture.detectChanges();

    const buttons = fixture.nativeElement.querySelectorAll('button.grid-cell');
    expect(buttons.length).toBe(9);
  });

  it('should generate accessible ARIA labels for empty and occupied cells', () => {
    mockFacade.board.set(['X', 'O', null, null, null, null, null, null, null]);
    const fixture = TestBed.createComponent(GameBoardComponent);
    fixture.detectChanges();

    const buttons = fixture.nativeElement.querySelectorAll('button.grid-cell');
    expect(buttons[0].getAttribute('aria-label')).toBe('Cell 1, row 1 column 1, occupied by Player X');
    expect(buttons[1].getAttribute('aria-label')).toBe('Cell 2, row 1 column 2, occupied by Player O');
    expect(buttons[2].getAttribute('aria-label')).toBe('Cell 3, row 1 column 3, empty');
  });

  it('should disable occupied cells and prevent clicks', () => {
    mockFacade.board.set(['X', null, null, null, null, null, null, null, null]);
    const fixture = TestBed.createComponent(GameBoardComponent);
    fixture.detectChanges();

    const buttons = fixture.nativeElement.querySelectorAll('button.grid-cell');
    expect(buttons[0].disabled).toBe(true);
    expect(buttons[0].getAttribute('aria-disabled')).toBe('true');
    expect(buttons[1].disabled).toBe(false);
  });

  it('should call playMove when an empty cell is clicked', () => {
    const fixture = TestBed.createComponent(GameBoardComponent);
    fixture.detectChanges();

    const buttons = fixture.nativeElement.querySelectorAll('button.grid-cell');
    buttons[4].click();

    expect(mockFacade.playMove).toHaveBeenCalledWith(4);
  });

  it('should apply winning-cell class to winning cells', () => {
    mockFacade.winningCells.set([0, 1, 2]);
    const fixture = TestBed.createComponent(GameBoardComponent);
    fixture.detectChanges();

    const buttons = fixture.nativeElement.querySelectorAll('button.grid-cell');
    expect(buttons[0].classList.contains('winning-cell')).toBe(true);
    expect(buttons[1].classList.contains('winning-cell')).toBe(true);
    expect(buttons[2].classList.contains('winning-cell')).toBe(true);
    expect(buttons[3].classList.contains('winning-cell')).toBe(false);
  });

  it('should disable all cells with native [disabled] when isGameOver is true', () => {
    mockFacade.isGameOver.set(true);
    const fixture = TestBed.createComponent(GameBoardComponent);
    fixture.detectChanges();

    const buttons: HTMLButtonElement[] = Array.from(fixture.nativeElement.querySelectorAll('button.grid-cell'));
    expect(buttons.length).toBe(9);
    expect(buttons.every(b => b.disabled)).toBe(true);
  });

  it('should disable all cells with native [disabled] when loading is true', () => {
    mockFacade.loading.set(true);
    const fixture = TestBed.createComponent(GameBoardComponent);
    fixture.detectChanges();

    const buttons: HTMLButtonElement[] = Array.from(fixture.nativeElement.querySelectorAll('button.grid-cell'));
    expect(buttons.length).toBe(9);
    expect(buttons.every(b => b.disabled)).toBe(true);
  });

  it('should render cells with role="gridcell" and type="button"', () => {
    const fixture = TestBed.createComponent(GameBoardComponent);
    fixture.detectChanges();

    const buttons: HTMLButtonElement[] = Array.from(fixture.nativeElement.querySelectorAll('button.grid-cell'));
    buttons.forEach((button) => {
      expect(button.getAttribute('role')).toBe('gridcell');
      expect(button.type).toBe('button');
    });
  });
});
