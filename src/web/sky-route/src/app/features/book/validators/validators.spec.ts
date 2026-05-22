import { describe, expect, it } from 'vitest';
import { FormControl } from '@angular/forms';
import { passportValidator } from './passport.validator';
import { nationalIdValidator } from './national-id.validator';

describe('passportValidator', () => {
  const validate = passportValidator();

  it.each(['AB12345', '123456', 'ABCDE6789'])('accepts %s', (value) => {
    expect(validate(new FormControl(value))).toBeNull();
  });

  it.each(['AB', 'AB1234567890', 'AB-1234', ''])('rejects %s', (value) => {
    expect(validate(new FormControl(value))).not.toBeNull();
  });
});

describe('nationalIdValidator', () => {
  const validate = nationalIdValidator();

  it.each(['12345', '123456789012'])('accepts %s', (value) => {
    expect(validate(new FormControl(value))).toBeNull();
  });

  it.each(['1234', '1234567890123', '12A45', ''])('rejects %s', (value) => {
    expect(validate(new FormControl(value))).not.toBeNull();
  });
});
