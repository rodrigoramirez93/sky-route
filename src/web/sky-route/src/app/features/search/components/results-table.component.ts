import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FlightOffer } from '../../../shared';
import { SortKey } from '../state/sort-offers';

@Component({
  selector: 'sr-results-table',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="sr-results">
      <div class="sr-sort">
        <label>
          Sort by
          <select [value]="sortKey()" (change)="sortChange.emit($any($event.target).value)">
            <option value="price-asc">Price (low → high)</option>
            <option value="price-desc">Price (high → low)</option>
            <option value="duration">Duration (shortest)</option>
            <option value="departure">Departure time</option>
          </select>
        </label>
      </div>

      <table>
        <thead>
          <tr>
            <th>Provider</th>
            <th>Flight</th>
            <th>Departure</th>
            <th>Arrival</th>
            <th>Duration</th>
            <th>Cabin</th>
            <th>Price</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          @for (o of offers(); track o.providerKey + o.flightNumber + o.departureUtc) {
            <tr>
              <td>{{ o.providerKey }}</td>
              <td>{{ o.flightNumber }}</td>
              <td>{{ o.departureUtc | date: 'short' : 'UTC' }}</td>
              <td>{{ o.arrivalUtc | date: 'short' : 'UTC' }}</td>
              <td>{{ formatDuration(o.durationMinutes) }}</td>
              <td>{{ o.cabin }}</td>
              <td>
                <strong>{{ o.currency }} {{ o.totalPrice | number: '1.2-2' }} total</strong>
                <br />
                <small>{{ o.currency }} {{ o.pricePerPassenger | number: '1.2-2' }} per person</small>
              </td>
              <td><button type="button" (click)="select.emit(o)">Select</button></td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
  styles: [
    `
      table { width: 100%; border-collapse: collapse; }
      th, td { text-align: left; padding: 0.5rem 0.75rem; border-bottom: 1px solid #eee; vertical-align: top; }
      .sr-sort { display: flex; justify-content: flex-end; margin-bottom: 0.5rem; }
      button { padding: 0.4rem 0.8rem; }
    `,
  ],
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
