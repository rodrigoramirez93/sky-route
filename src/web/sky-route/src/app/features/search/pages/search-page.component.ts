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
      <header class="sr-page-header">
        <h2>Find a flight</h2>
        <p class="sr-page-subtitle">Compare GlobalAir and BudgetWings in one search.</p>
      </header>

      <div class="sr-card sr-search-card">
        <sr-search-form (submitted)="store.search($event)" />
      </div>

      <div class="sr-status-region" aria-live="polite">
        @if (store.loading()) {
          <div class="sr-card sr-status">
            <span class="sr-spinner" aria-hidden="true"></span>
            <span>Searching flights…</span>
          </div>
        } @else if (store.error(); as err) {
          <div class="sr-card sr-status sr-error" role="alert">
            <span aria-hidden="true">⚠</span>
            <span>{{ err }}</span>
          </div>
        } @else if (store.isEmpty()) {
          <div class="sr-card sr-status sr-empty">
            <span class="sr-empty-icon" aria-hidden="true">🔍</span>
            <div>
              <strong>No flights match your search.</strong>
              <p>Try a different route or date.</p>
            </div>
          </div>
        } @else if (store.searched()) {
          <sr-results-table
            [offers]="store.offers()"
            [sortKey]="store.sort()"
            (sortChange)="store.setSort($event)"
            (select)="onSelect($event)"
          />
        }
      </div>
    </section>
  `,
  styles: [
    `
      .sr-search-page { display: flex; flex-direction: column; gap: 1.25rem; }
      .sr-page-header h2 { font-size: 1.5rem; }
      .sr-page-subtitle { color: var(--sr-muted); margin: 0.25rem 0 0; }
      .sr-search-card { padding: 1.25rem 1.25rem 1rem; }
      .sr-status { display: flex; align-items: center; gap: 0.75rem; color: var(--sr-muted); }
      .sr-status.sr-error { background: var(--sr-danger-soft); color: var(--sr-danger); border-color: #f5c2c7; }
      .sr-empty { gap: 1rem; }
      .sr-empty-icon { font-size: 1.5rem; }
      .sr-empty strong { color: var(--sr-text); }
      .sr-empty p { margin: 0.25rem 0 0; color: var(--sr-muted); }
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
