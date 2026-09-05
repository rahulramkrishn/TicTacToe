import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { describe, it, expect, beforeEach } from 'vitest';
import { MoveHistoryComponent } from './move-history';
import { GameFacade } from '../../services/game.facade';
import { MoveDto } from '../../models/game.models';

describe('MoveHistoryComponent', () => {
  let mockFacade: any;

  beforeEach(() => {
    mockFacade = {
      moveHistory: signal<MoveDto[]>([])
    };

    TestBed.configureTestingModule({
      imports: [MoveHistoryComponent],
      providers: [{ provide: GameFacade, useValue: mockFacade }]
    });
  });

  it('should render empty history state when moveHistory is empty', () => {
    const fixture = TestBed.createComponent(MoveHistoryComponent);
    fixture.detectChanges();

    const empty = fixture.nativeElement.querySelector('.empty-history');
    expect(empty).not.toBeNull();
    expect(empty.textContent).toContain('No moves played yet');
  });

  it('should render list of moves with derived 1-based row and column numbers', () => {
    mockFacade.moveHistory.set([
      { moveNumber: 1, player: 'X', cellIndex: 0 },
      { moveNumber: 2, player: 'O', cellIndex: 4 },
      { moveNumber: 3, player: 'X', cellIndex: 8 }
    ]);

    const fixture = TestBed.createComponent(MoveHistoryComponent);
    fixture.detectChanges();

    const items = fixture.nativeElement.querySelectorAll('.move-item');
    expect(items.length).toBe(3);

    // Cell 0 -> Row 1, Col 1
    expect(items[0].textContent).toContain('#1');
    expect(items[0].textContent).toContain('Cell 0');
    expect(items[0].textContent).toContain('Row 1, Col 1');

    // Cell 4 -> Row 2, Col 2
    expect(items[1].textContent).toContain('#2');
    expect(items[1].textContent).toContain('Cell 4');
    expect(items[1].textContent).toContain('Row 2, Col 2');

    // Cell 8 -> Row 3, Col 3
    expect(items[2].textContent).toContain('#3');
    expect(items[2].textContent).toContain('Cell 8');
    expect(items[2].textContent).toContain('Row 3, Col 3');
  });
});
