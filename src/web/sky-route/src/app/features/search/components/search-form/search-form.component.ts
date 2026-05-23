import { ChangeDetectionStrategy, Component, OnInit, inject, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Airport, CABIN_CLASSES, CABIN_CLASS_LABEL, CabinClass, FlightSearchCriteria } from '../../../../shared';
import { FlightSearchService } from '../../services/flight-search.service';

@Component({
  selector: 'sr-search-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './search-form.component.html',
  styleUrls: ['./search-form.component.css'],
})
export class SearchFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(FlightSearchService);

  readonly cabins = CABIN_CLASSES;
  readonly cabinLabel = CABIN_CLASS_LABEL;
  readonly today = new Date().toISOString().slice(0, 10);

  readonly airports = signal<Airport[]>([]);
  readonly submitted = output<FlightSearchCriteria>();

  readonly form = this.fb.nonNullable.group({
    originCode: ['', Validators.required],
    destinationCode: ['', Validators.required],
    departureDate: [this.today, Validators.required],
    passengers: [1, [Validators.required, Validators.min(1), Validators.max(9)]],
    cabin: ['Economy' as CabinClass, Validators.required],
  });

  ngOnInit(): void {
    this.service.getAirports().subscribe({
      next: (airports) => this.airports.set(airports),
    });
  }

  selectedAirportLabel(code: string | null | undefined): string {
    if (!code) {
      return '';
    }
    const airport = this.airports().find((a) => a.code === code);
    return airport ? `${airport.code} — ${airport.city} (${airport.country})` : '';
  }

  sameRoute(): boolean {
    const { originCode, destinationCode } = this.form.getRawValue();
    return !!originCode && originCode === destinationCode;
  }

  submit(): void {
    if (this.form.invalid || this.sameRoute()) {
      return;
    }
    this.submitted.emit(this.form.getRawValue() as FlightSearchCriteria);
  }
}
