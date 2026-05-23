import { TestBed } from '@angular/core/testing';
import { DocumentFieldComponent } from './document-field.component';

describe('DocumentFieldComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DocumentFieldComponent],
    }).compileComponents();
  });

  it('creates and renders the international (passport) label', () => {
    const fixture = TestBed.createComponent(DocumentFieldComponent);
    fixture.componentRef.setInput('isInternational', true);
    fixture.detectChanges();
    expect(fixture.componentInstance.label()).toBe('Passport Number');
  });

  it('renders the national-id label when route is domestic', () => {
    const fixture = TestBed.createComponent(DocumentFieldComponent);
    fixture.componentRef.setInput('isInternational', false);
    fixture.detectChanges();
    expect(fixture.componentInstance.label()).toBe('National ID');
  });
});
