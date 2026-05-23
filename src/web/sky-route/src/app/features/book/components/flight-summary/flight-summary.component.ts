import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FlightOffer } from '../../../../shared';

@Component({
  selector: 'sr-flight-summary',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './flight-summary.component.html',
  styleUrls: ['./flight-summary.component.css'],
})
export class FlightSummaryComponent {
  readonly offer = input.required<FlightOffer>();
}
