import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

const PASSPORT_PATTERN = /^[A-Z0-9]{6,9}$/;

export function passportValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '').toString().trim().toUpperCase();
    if (!value) {
      return { required: true };
    }
    return PASSPORT_PATTERN.test(value) ? null : { passport: true };
  };
}
