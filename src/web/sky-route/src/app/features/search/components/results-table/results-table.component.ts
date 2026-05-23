import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FlightOffer } from '../../../../shared';
import { SortKey } from '../../state/sort-offers';

@Component({
  selector: 'sr-results-table',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './results-table.component.html',
  styleUrls: ['./results-table.component.css'],
})
export class ResultsTableComponent {
  readonly offers = input.required<ReadonlyArray<FlightOffer>>();
  readonly sortKey = input.required<SortKey>();

  readonly sortChange = output<SortKey>();
  readonly select = output<FlightOffer>();

  formatDuration(minutes: number): string {
    const h = Math.floor(minutes / 60);
    const m = minutes % 60;
    return `${h}h ${m.toString().padStart(2, '0')}m`;
  }
}
