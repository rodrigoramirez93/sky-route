import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FlightOffer } from '../../../shared';

@Component({
  selector: 'sr-flight-summary',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="sr-summary">
      <div class="sr-summary-head">
        <h3>
          <span class="sr-code">{{ offer().origin.code }}</span>
          <span class="sr-arrow" aria-hidden="true">→</span>
          <span class="sr-code">{{ offer().destination.code }}</span>
        </h3>
        <span class="sr-badge" [class.sr-badge--intl]="offer().isInternational" [class.sr-badge--dom]="!offer().isInternational">
          {{ offer().isInternational ? 'International' : 'Domestic' }}
        </span>
      </div>
      <p class="sr-summary-meta">
        <strong>{{ offer().providerKey }}</strong> · Flight {{ offer().flightNumber }} · {{ offer().cabin }}
      </p>
      <dl class="sr-summary-times">
        <div>
          <dt>Departs</dt>
          <dd>{{ offer().departureUtc | date: 'medium' : 'UTC' }}</dd>
        </div>
        <div>
          <dt>Arrives</dt>
          <dd>{{ offer().arrivalUtc | date: 'medium' : 'UTC' }}</dd>
        </div>
      </dl>
    </div>
  `,
  styles: [
    `
      .sr-summary { display: flex; flex-direction: column; gap: 0.65rem; }
      .sr-summary-head { display: flex; align-items: center; justify-content: space-between; gap: 0.75rem; flex-wrap: wrap; }
      h3 { display: inline-flex; align-items: center; gap: 0.5rem; font-size: 1.15rem; }
      .sr-code { color: var(--sr-primary); letter-spacing: 0.04em; }
      .sr-arrow { color: var(--sr-muted); }
      .sr-summary-meta { margin: 0; color: var(--sr-muted); font-size: 0.9rem; }
      .sr-summary-times { display: grid; grid-template-columns: 1fr; gap: 0.5rem; margin: 0; }
      .sr-summary-times dt { font-size: 0.75rem; color: var(--sr-muted); text-transform: uppercase; letter-spacing: 0.04em; }
      .sr-summary-times dd { margin: 0.1rem 0 0; font-weight: 500; }
    `,
  ],
})
export class FlightSummaryComponent {
  readonly offer = input.required<FlightOffer>();
}
