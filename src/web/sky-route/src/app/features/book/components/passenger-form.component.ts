import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FlightOffer, PassengerDetails } from '../../../shared';
import { DocumentFieldComponent } from './document-field.component';

@Component({
  selector: 'sr-passenger-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DocumentFieldComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <form [formGroup]="form" (ngSubmit)="submit()" class="sr-passenger-form">
      <ng-container formArrayName="passengers">
        @for (group of passengers.controls; track $index) {
          <fieldset [formGroupName]="$index">
            <legend>Passenger {{ $index + 1 }}</legend>

            <label>
              Full name
              <input type="text" formControlName="fullName" />
            </label>

            <label>
              Email
              <input type="email" formControlName="email" />
            </label>

            <sr-document-field
              [isInternational]="offer().isInternational"
              formControlName="documentNumber"
            />
          </fieldset>
        }
      </ng-container>

      <button type="submit" [disabled]="form.invalid || submitting()">
        {{ submitting() ? 'Booking…' : 'Confirm booking' }}
      </button>
    </form>
  `,
  styles: [
    `
      .sr-passenger-form { display: flex; flex-direction: column; gap: 0.75rem; }
      fieldset { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 0.75rem; padding: 0.75rem; }
      legend { font-weight: 600; }
      label { display: flex; flex-direction: column; gap: 0.25rem; }
      button { padding: 0.6rem 1rem; align-self: flex-start; font-weight: 600; }
    `,
  ],
})
export class PassengerFormComponent {
  private readonly fb = inject(FormBuilder);

  readonly offer = input.required<FlightOffer>();
  readonly submitting = input<boolean>(false);
  readonly confirmed = output<PassengerDetails[]>();

  protected readonly form: FormGroup = this.fb.group({
    passengers: this.fb.array<FormGroup>([]),
  });

  protected get passengers(): FormArray<FormGroup> {
    return this.form.get('passengers') as FormArray<FormGroup>;
  }

  private readonly passengersCount = computed(() => this.offer().passengers);

  constructor() {
    // React to passenger count changes by rebuilding the FormArray.
    let lastCount = -1;
    effect(() => {
      const count = this.passengersCount();
      if (count === lastCount) {
        return;
      }
      lastCount = count;
      this.passengers.clear();
      for (let i = 0; i < count; i++) {
        this.passengers.push(
          this.fb.group({
            fullName: ['', [Validators.required, Validators.minLength(2)]],
            email: ['', [Validators.required, Validators.email]],
            documentNumber: [''],
          }),
        );
      }
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.confirmed.emit(this.passengers.controls.map((g) => g.value as PassengerDetails));
  }
}
