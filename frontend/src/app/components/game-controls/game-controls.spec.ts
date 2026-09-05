import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { GameControlsComponent } from './game-controls';
import { GameFacade } from '../../services/game.facade';

describe('GameControlsComponent', () => {
  let mockFacade: any;

  beforeEach(() => {
    mockFacade = {
      canUndo: signal<boolean>(false),
      loading: signal<boolean>(false),
      isInitialized: signal<boolean>(true),
      mode: signal<string>('TwoPlayer'),
      undo: vi.fn(),
      resetGame: vi.fn(),
      newGame: vi.fn()
    };

    TestBed.configureTestingModule({
      imports: [GameControlsComponent],
      providers: [{ provide: GameFacade, useValue: mockFacade }]
    });
  });

  it('should disable Undo when canUndo is false', () => {
    const fixture = TestBed.createComponent(GameControlsComponent);
    fixture.detectChanges();

    const undoBtn = fixture.nativeElement.querySelector('#btn-undo') as HTMLButtonElement;
    expect(undoBtn.disabled).toBe(true);
  });

  it('should enable Undo when canUndo is true and call undo() on click', () => {
    mockFacade.canUndo.set(true);
    const fixture = TestBed.createComponent(GameControlsComponent);
    fixture.detectChanges();

    const undoBtn = fixture.nativeElement.querySelector('#btn-undo') as HTMLButtonElement;
    expect(undoBtn.disabled).toBe(false);

    undoBtn.click();
    expect(mockFacade.undo).toHaveBeenCalled();
  });

  it('should call resetGame() when Reset Board is clicked', () => {
    const fixture = TestBed.createComponent(GameControlsComponent);
    fixture.detectChanges();

    const resetBtn = fixture.nativeElement.querySelector('#btn-reset-game') as HTMLButtonElement;
    resetBtn.click();

    expect(mockFacade.resetGame).toHaveBeenCalled();
  });

  it('should call newGame() with active mode when New Game is clicked', () => {
    mockFacade.mode.set('Computer');
    const fixture = TestBed.createComponent(GameControlsComponent);
    fixture.detectChanges();

    const newGameBtn = fixture.nativeElement.querySelector('#btn-new-game') as HTMLButtonElement;
    newGameBtn.click();

    expect(mockFacade.newGame).toHaveBeenCalledWith('Computer');
  });
});
