import { Injectable, computed, inject, signal } from '@angular/core';
import { FlightOffer, FlightSearchCriteria } from '../../../shared';
import { FlightSearchService } from '../services/flight-search.service';
import { SortKey, sortOffers } from './sort-offers';

@Injectable()
export class SearchStore {
  private readonly service = inject(FlightSearchService);

  private readonly _offers = signal<FlightOffer[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _sort = signal<SortKey>('price-asc');
  private readonly _searched = signal(false);

  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly sort = this._sort.asReadonly();
  readonly searched = this._searched.asReadonly();

  readonly offers = computed(() => sortOffers(this._offers(), this._sort()));
  readonly isEmpty = computed(() => this._searched() && !this._loading() && this._offers().length === 0);

  setSort(key: SortKey): void {
    this._sort.set(key);
  }

  search(criteria: FlightSearchCriteria): void {
    this._loading.set(true);
    this._error.set(null);
    this._searched.set(true);

    this.service.search(criteria).subscribe({
      next: (offers) => {
        this._offers.set(offers);
        this._loading.set(false);
      },
      error: (err) => {
        this._error.set(err?.message ?? 'Search failed');
        this._offers.set([]);
        this._loading.set(false);
      },
    });
  }
}
