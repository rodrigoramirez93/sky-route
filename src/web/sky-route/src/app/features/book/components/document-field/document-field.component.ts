import { ChangeDetectionStrategy, Component, forwardRef, input, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ControlValueAccessor,
  FormControl,
  NG_VALIDATORS,
  NG_VALUE_ACCESSOR,
  ReactiveFormsModule,
  ValidationErrors,
  Validator,
} from '@angular/forms';
import { nationalIdValidator } from '../../validators/national-id.validator';
import { passportValidator } from '../../validators/passport.validator';

/**
 * Self-contained form control that swaps both its label and validator based on
 * whether the booked route is international (passport) or domestic (national ID).
 */
@Component({
  selector: 'sr-document-field',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => DocumentFieldComponent), multi: true },
    { provide: NG_VALIDATORS, useExisting: forwardRef(() => DocumentFieldComponent), multi: true },
  ],
  templateUrl: './document-field.component.html',
  styleUrls: ['./document-field.component.css'],
})
export class DocumentFieldComponent implements ControlValueAccessor, Validator {
  readonly isInternational = input.required<boolean>();

  protected readonly control = new FormControl<string>('', { nonNullable: true });
  protected onTouched: () => void = () => {};

  private onChange: (value: string) => void = () => {};

  constructor() {
    this.control.valueChanges.subscribe((value) => this.onChange(value));

    effect(() => {
      // Swap validator whenever the route type flips. This is the core of the
      // domestic/international document-type requirement.
      const validator = this.isInternational() ? passportValidator() : nationalIdValidator();
      this.control.setValidators(validator);
      this.control.updateValueAndValidity({ emitEvent: false });
    });
  }

  label(): string {
    return this.isInternational() ? 'Passport Number' : 'National ID';
  }

  helperText(): string {
    return this.isInternational()
      ? 'Used for international travel (6–9 alphanumeric characters).'
      : 'Used for domestic flights (5–12 digits).';
  }

  protected readonly hintId = `sr-doc-hint-${Math.random().toString(36).slice(2, 9)}`;

  errorMessage(): string {
    if (this.control.hasError('required')) {
      return `${this.label()} is required.`;
    }
    if (this.isInternational()) {
      return 'Passport must be 6–9 alphanumeric characters.';
    }
    return 'National ID must be 5–12 digits.';
  }

  writeValue(value: string | null): void {
    this.control.setValue(value ?? '', { emitEvent: false });
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(disabled: boolean): void {
    disabled ? this.control.disable({ emitEvent: false }) : this.control.enable({ emitEvent: false });
  }

  validate(): ValidationErrors | null {
    return this.control.valid ? null : this.control.errors;
  }
}
