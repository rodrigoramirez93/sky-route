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
      <h3>{{ offer().origin.code }} → {{ offer().destination.code }}</h3>
      <p>
        <strong>{{ offer().providerKey }}</strong> · Flight {{ offer().flightNumber }} ·
        {{ offer().cabin }}
      </p>
      <p>
        Departs {{ offer().departureUtc | date: 'medium' : 'UTC' }} <br />
        Arrives {{ offer().arrivalUtc | date: 'medium' : 'UTC' }}
      </p>
      <p>
        Route is
        <strong>{{ offer().isInternational ? 'international' : 'domestic' }}</strong>.
      </p>
    </div>
  `,
  styles: [`.sr-summary { padding: 0.75rem; background: #f6f6f6; border-radius: 4px; }`],
})
export class FlightSummaryComponent {
  readonly offer = input.required<FlightOffer>();
}
