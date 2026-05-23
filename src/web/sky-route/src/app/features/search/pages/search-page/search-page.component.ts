import { ChangeDetectionStrategy, Component, Inject, Optional, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FlightOffer, FLIGHT_SELECTION_HANDLER, FlightSelectionHandler } from '../../../../shared';
import { SearchFormComponent } from '../../components/search-form/search-form.component';
import { ResultsTableComponent } from '../../components/results-table/results-table.component';
import { SearchStore } from '../../state/search.store';

@Component({
  selector: 'sr-search-page',
  standalone: true,
  imports: [CommonModule, SearchFormComponent, ResultsTableComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [SearchStore],
  templateUrl: './search-page.component.html',
  styleUrls: ['./search-page.component.css'],
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
