import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

const NATIONAL_ID_PATTERN = /^\d{5,12}$/;

export function nationalIdValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '').toString().trim();
    if (!value) {
      return { required: true };
    }
    return NATIONAL_ID_PATTERN.test(value) ? null : { nationalId: true };
  };
}
