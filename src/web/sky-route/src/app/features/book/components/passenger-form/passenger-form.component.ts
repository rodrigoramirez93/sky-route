import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { FlightOffer, PassengerDetails } from '../../../../shared';
import { DocumentFieldComponent } from '../document-field/document-field.component';

@Component({
  selector: 'sr-passenger-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, DocumentFieldComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './passenger-form.component.html',
  styleUrls: ['./passenger-form.component.css'],
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
