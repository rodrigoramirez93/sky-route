import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { FlightOffer, PassengerDetails } from '../../../shared';
import { DocumentFieldComponent } from './document-field.component';

@Component({
  selector: 'sr-passenger-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, DocumentFieldComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <form [formGroup]="form" (ngSubmit)="submit()" class="sr-passenger-form">
      <ng-container formArrayName="passengers">
        @for (group of passengers.controls; track $index) {
          <fieldset [formGroupName]="$index" class="sr-passenger">
            <legend>Passenger {{ $index + 1 }}</legend>

            <label class="sr-field">
              <span class="sr-label">Full name</span>
              <input type="text" formControlName="fullName" autocomplete="name" />
              @if (showError($index, 'fullName')) {
                <small class="sr-error-text">Please enter the passenger's full name.</small>
              }
            </label>

            <label class="sr-field">
              <span class="sr-label">Email</span>
              <input type="email" formControlName="email" autocomplete="email" />
              @if (showError($index, 'email')) {
                <small class="sr-error-text">Enter a valid email address.</small>
              }
            </label>

            <div class="sr-field">
              <sr-document-field
                [isInternational]="offer().isInternational"
                formControlName="documentNumber"
              />
            </div>
          </fieldset>
        }
      </ng-container>

      <div class="sr-actions">
        <a class="sr-btn-ghost sr-back-link" routerLink="/search">← Back to search</a>
        <button type="submit" class="sr-btn-primary" [disabled]="form.invalid || submitting()">
          @if (submitting()) {
            <span class="sr-spinner" aria-hidden="true"></span>
            <span>Booking…</span>
          } @else {
            <span>Confirm booking</span>
          }
        </button>
      </div>
    </form>
  `,
  styles: [
    `
      .sr-passenger-form { display: flex; flex-direction: column; gap: 1rem; }
      .sr-passenger {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 0.85rem;
        padding: 1rem 1.1rem;
        border: 1px solid var(--sr-border);
        border-radius: var(--sr-radius);
        background: var(--sr-surface);
      }
      legend {
        font-weight: 600;
        color: var(--sr-primary);
        padding: 0 0.4rem;
        font-size: 0.9rem;
      }
      .sr-field { display: flex; flex-direction: column; gap: 0.3rem; min-width: 0; }
      .sr-label { font-size: 0.8rem; font-weight: 600; color: var(--sr-muted); }
      .sr-error-text { color: var(--sr-danger); font-size: 0.78rem; }

      .sr-actions {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.75rem;
        flex-wrap: wrap;
      }
      .sr-actions button { display: inline-flex; align-items: center; gap: 0.5rem; }
      .sr-back-link { text-decoration: none; display: inline-block; }

      @media (max-width: 640px) {
        .sr-passenger { grid-template-columns: 1fr; }
      }
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

  protected showError(index: number, controlName: 'fullName' | 'email'): boolean {
    const ctrl = this.passengers.at(index).get(controlName);
    return !!ctrl && ctrl.invalid && (ctrl.touched || ctrl.dirty);
  }
}
