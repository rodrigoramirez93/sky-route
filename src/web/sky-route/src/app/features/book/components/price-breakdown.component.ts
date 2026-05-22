import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FlightOffer } from '../../../shared';

@Component({
  selector: 'sr-price-breakdown',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <table class="sr-breakdown">
      <tbody>
        <tr>
          <td>Per passenger</td>
          <td>{{ offer().currency }} {{ offer().pricePerPassenger | number: '1.2-2' }}</td>
        </tr>
        <tr>
          <td>Passengers</td>
          <td>× {{ offer().passengers }}</td>
        </tr>
        <tr class="sr-total">
          <td>Total</td>
          <td>{{ offer().currency }} {{ offer().totalPrice | number: '1.2-2' }}</td>
        </tr>
      </tbody>
    </table>
  `,
  styles: [
    `
      .sr-breakdown { width: 100%; border-collapse: collapse; }
      td { padding: 0.5rem 0.2rem; border-bottom: 1px solid var(--sr-border); color: var(--sr-muted); }
      td:last-child { text-align: right; color: var(--sr-text); font-variant-numeric: tabular-nums; }
      .sr-total td { font-weight: 700; border-bottom: none; padding-top: 0.85rem; font-size: 1.1rem; }
      .sr-total td:last-child { color: var(--sr-primary); }
    `,
  ],
})
export class PriceBreakdownComponent {
  readonly offer = input.required<FlightOffer>();
}
