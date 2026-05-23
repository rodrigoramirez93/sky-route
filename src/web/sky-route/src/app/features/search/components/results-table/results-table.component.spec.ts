import { TestBed } from '@angular/core/testing';
import { ResultsTableComponent } from './results-table.component';

describe('ResultsTableComponent', () => {
  it('creates and formats duration as hours/minutes', async () => {
    await TestBed.configureTestingModule({ imports: [ResultsTableComponent] }).compileComponents();
    const fixture = TestBed.createComponent(ResultsTableComponent);
    fixture.componentRef.setInput('offers', []);
    fixture.componentRef.setInput('sortKey', 'price');
    fixture.detectChanges();
    expect(fixture.componentInstance.formatDuration(125)).toBe('2h 05m');
  });
});
