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
      <div class="sr-results-header">
        <span class="sr-results-count">{{ offers().length }} flight{{ offers().length === 1 ? '' : 's' }} found</span>
        <label class="sr-sort">
          <span class="sr-sort-label">Sort by</span>
          <select [value]="sortKey()" (change)="sortChange.emit($any($event.target).value)">
            <option value="price-asc">Price (low → high)</option>
            <option value="price-desc">Price (high → low)</option>
            <option value="duration">Duration (shortest)</option>
            <option value="departure">Departure time</option>
          </select>
        </label>
      </div>

      <ul class="sr-flight-list">
        @for (o of offers(); track o.providerKey + o.flightNumber + o.departureUtc) {
          <li class="sr-flight-card">
            <div class="sr-flight-meta">
              <span class="sr-provider" [attr.data-provider]="o.providerKey">{{ o.providerKey }}</span>
              <span class="sr-flight-number">Flight {{ o.flightNumber }}</span>
            </div>

            <div class="sr-flight-route">
              <div class="sr-endpoint">
                <span class="sr-time">{{ o.departureUtc | date: 'shortTime' : 'UTC' }}</span>
                <span class="sr-code">{{ o.origin.code }}</span>
                <span class="sr-date">{{ o.departureUtc | date: 'mediumDate' : 'UTC' }}</span>
              </div>
              <div class="sr-route-line" aria-hidden="true">
                <span class="sr-duration">{{ formatDuration(o.durationMinutes) }}</span>
                <span class="sr-line"></span>
                <span class="sr-plane">✈</span>
              </div>
              <div class="sr-endpoint sr-endpoint--end">
                <span class="sr-time">{{ o.arrivalUtc | date: 'shortTime' : 'UTC' }}</span>
                <span class="sr-code">{{ o.destination.code }}</span>
                <span class="sr-date">{{ o.arrivalUtc | date: 'mediumDate' : 'UTC' }}</span>
              </div>
            </div>

            <div class="sr-flight-tags">
              <span class="sr-badge">{{ o.cabin }}</span>
              <span class="sr-badge" [class.sr-badge--intl]="o.isInternational" [class.sr-badge--dom]="!o.isInternational">
                {{ o.isInternational ? 'International' : 'Domestic' }}
              </span>
            </div>

            <div class="sr-flight-price">
              <span class="sr-total"><strong>{{ o.currency }} {{ o.totalPrice | number: '1.2-2' }}</strong></span>
              <span class="sr-per-pax">{{ o.currency }} {{ o.pricePerPassenger | number: '1.2-2' }} / passenger</span>
              <button type="button" class="sr-btn-primary" (click)="select.emit(o)">Select</button>
            </div>
          </li>
        }
      </ul>
    </div>
  `,
  styles: [
    `
      .sr-results { display: flex; flex-direction: column; gap: 0.85rem; }
      .sr-results-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
        flex-wrap: wrap;
      }
      .sr-results-count { color: var(--sr-muted); font-size: 0.9rem; }
      .sr-sort { display: inline-flex; align-items: center; gap: 0.5rem; }
      .sr-sort-label { font-size: 0.85rem; color: var(--sr-muted); }
      .sr-sort select { width: auto; }

      .sr-flight-list { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 0.75rem; }
      .sr-flight-card {
        background: var(--sr-surface);
        border: 1px solid var(--sr-border);
        border-radius: var(--sr-radius);
        box-shadow: var(--sr-shadow);
        padding: 1rem 1.25rem;
        display: grid;
        grid-template-columns: 180px 1fr auto;
        grid-template-areas:
          'meta route price'
          'tags route price';
        gap: 0.75rem 1.25rem;
        align-items: center;
        transition: transform 120ms ease, box-shadow 120ms ease;
      }
      .sr-flight-card:hover { transform: translateY(-1px); box-shadow: var(--sr-shadow-lg); }

      .sr-flight-meta { grid-area: meta; display: flex; flex-direction: column; gap: 0.3rem; }
      .sr-provider {
        display: inline-block;
        font-weight: 700;
        font-size: 0.78rem;
        letter-spacing: 0.04em;
        text-transform: uppercase;
        padding: 0.2rem 0.55rem;
        border-radius: 4px;
        background: var(--sr-primary-soft);
        color: var(--sr-primary);
        align-self: flex-start;
      }
      .sr-provider[data-provider='BudgetWings'] { background: #fff4e0; color: #a25a00; }
      .sr-flight-number { color: var(--sr-muted); font-size: 0.85rem; }

      .sr-flight-route {
        grid-area: route;
        display: grid;
        grid-template-columns: 1fr auto 1fr;
        align-items: center;
        gap: 0.75rem;
      }
      .sr-endpoint { display: flex; flex-direction: column; gap: 0.1rem; min-width: 0; }
      .sr-endpoint--end { text-align: right; }
      .sr-time { font-size: 1.25rem; font-weight: 700; color: var(--sr-text); }
      .sr-code { font-weight: 600; color: var(--sr-primary); letter-spacing: 0.04em; }
      .sr-date { font-size: 0.78rem; color: var(--sr-muted); }

      .sr-route-line { display: flex; align-items: center; flex-direction: column; gap: 0.25rem; min-width: 120px; color: var(--sr-muted); font-size: 0.8rem; }
      .sr-route-line .sr-line {
        width: 100%;
        height: 2px;
        background: linear-gradient(to right, var(--sr-border), var(--sr-primary), var(--sr-border));
        border-radius: 2px;
      }
      .sr-plane { color: var(--sr-primary); }

      .sr-flight-tags { grid-area: tags; display: flex; gap: 0.4rem; flex-wrap: wrap; }

      .sr-flight-price {
        grid-area: price;
        display: flex;
        flex-direction: column;
        align-items: flex-end;
        gap: 0.35rem;
        min-width: 160px;
      }
      .sr-total { font-size: 1.25rem; color: var(--sr-primary); }
      .sr-per-pax { color: var(--sr-muted); font-size: 0.8rem; }
      .sr-flight-price button { margin-top: 0.25rem; }

      @media (max-width: 780px) {
        .sr-flight-card {
          grid-template-columns: 1fr;
          grid-template-areas: 'meta' 'route' 'tags' 'price';
        }
        .sr-flight-price { align-items: stretch; }
        .sr-flight-price button { width: 100%; }
        .sr-endpoint--end { text-align: left; }
      }
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
