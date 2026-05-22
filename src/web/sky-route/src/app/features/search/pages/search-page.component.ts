import { ChangeDetectionStrategy, Component, Inject, Optional, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FlightOffer, FLIGHT_SELECTION_HANDLER, FlightSelectionHandler } from '../../../shared';
import { SearchFormComponent } from '../components/search-form.component';
import { ResultsTableComponent } from '../components/results-table.component';
import { SearchStore } from '../state/search.store';

@Component({
  selector: 'sr-search-page',
  standalone: true,
  imports: [CommonModule, SearchFormComponent, ResultsTableComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [SearchStore],
  template: `
    <section class="sr-search-page">
      <h2>Find a flight</h2>
      <sr-search-form (submitted)="store.search($event)" />

      @if (store.loading()) {
        <p class="sr-status">Searching flights…</p>
      } @else if (store.error(); as err) {
        <p class="sr-status sr-error">{{ err }}</p>
      } @else if (store.isEmpty()) {
        <p class="sr-status">No flights match your search. Try a different route or date.</p>
      } @else if (store.searched()) {
        <sr-results-table
          [offers]="store.offers()"
          [sortKey]="store.sort()"
          (sortChange)="store.setSort($event)"
          (select)="onSelect($event)"
        />
      }
    </section>
  `,
  styles: [
    `
      .sr-search-page { display: flex; flex-direction: column; gap: 1rem; }
      .sr-status { padding: 0.75rem; background: #f6f6f6; border-radius: 4px; }
      .sr-error { background: #fde2e2; color: #b00020; }
    `,
  ],
})
export class SearchPageComponent {
  protected readonly store = inject(SearchStore);

  constructor(
    @Optional() @Inject(FLIGHT_SELECTION_HANDLER) private readonly handler: FlightSelectionHandler | null,
  ) {}

  onSelect(offer: FlightOffer): void {
    this.handler?.(offer);
  }
}
