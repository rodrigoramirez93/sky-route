import { ChangeDetectionStrategy, Component, OnInit, inject, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Airport, CABIN_CLASSES, CabinClass, FlightSearchCriteria } from '../../../shared';
import { FlightSearchService } from '../services/flight-search.service';

@Component({
  selector: 'sr-search-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <form [formGroup]="form" (ngSubmit)="submit()" class="sr-search-form">
      <label>
        <span class="sr-label">✈ Origin</span>
        <select formControlName="originCode">
          <option value="" disabled>Select…</option>
          @for (a of airports(); track a.code) {
            <option [value]="a.code">{{ a.code }} — {{ a.city }} ({{ a.country }})</option>
          }
        </select>
      </label>

      <label>
        <span class="sr-label">🎯 Destination</span>
        <select formControlName="destinationCode">
          <option value="" disabled>Select…</option>
          @for (a of airports(); track a.code) {
            <option [value]="a.code">{{ a.code }} — {{ a.city }} ({{ a.country }})</option>
          }
        </select>
      </label>

      <label>
        <span class="sr-label">Departure</span>
        <input type="date" formControlName="departureDate" [min]="today" />
      </label>

      <label>
        <span class="sr-label">Passengers</span>
        <input type="number" min="1" max="9" formControlName="passengers" />
      </label>

      <label>
        <span class="sr-label">Cabin</span>
        <select formControlName="cabin">
          @for (c of cabins; track c) {
            <option [value]="c">{{ c }}</option>
          }
        </select>
      </label>

      <div class="sr-submit-cell">
        <button type="submit" class="sr-btn-primary" [disabled]="form.invalid || sameRoute()">
          Search flights
        </button>
      </div>

      @if (sameRoute()) {
        <p class="sr-form-hint">Origin and destination must differ.</p>
      }
    </form>
  `,
  styles: [
    `
      .sr-search-form {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(170px, 1fr));
        gap: 0.85rem;
        align-items: end;
      }
      label { display: flex; flex-direction: column; gap: 0.3rem; }
      .sr-label { font-size: 0.8rem; font-weight: 600; color: var(--sr-muted); letter-spacing: 0.01em; }
      .sr-submit-cell { display: flex; align-items: end; }
      .sr-submit-cell button { width: 100%; }
      .sr-form-hint { color: var(--sr-danger); grid-column: 1 / -1; margin: 0; font-size: 0.85rem; }
    `,
  ],
})
export class SearchFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(FlightSearchService);

  readonly cabins = CABIN_CLASSES;
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
