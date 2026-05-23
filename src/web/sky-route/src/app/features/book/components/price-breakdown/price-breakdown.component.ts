import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FlightOffer } from '../../../../shared';

@Component({
  selector: 'sr-price-breakdown',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './price-breakdown.component.html',
  styleUrls: ['./price-breakdown.component.css'],
})
export class PriceBreakdownComponent {
  readonly offer = input.required<FlightOffer>();
}
