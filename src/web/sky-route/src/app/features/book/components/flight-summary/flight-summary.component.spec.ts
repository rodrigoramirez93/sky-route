import { TestBed } from '@angular/core/testing';
import { FlightSummaryComponent } from './flight-summary.component';
import { FlightOffer } from '../../../../shared';

const offer: FlightOffer = {
  providerKey: 'GlobalAir',
  flightNumber: 'GA100',
  origin: { code: 'JFK', name: 'JFK', city: 'New York', country: 'US' },
  destination: { code: 'LAX', name: 'LAX', city: 'Los Angeles', country: 'US' },
  departureUtc: '2025-01-01T10:00:00Z',
  arrivalUtc: '2025-01-01T13:00:00Z',
  durationMinutes: 180,
  cabin: 'Economy',
  passengers: 1,
  pricePerPassenger: 250,
  totalPrice: 250,
  currency: 'USD',
  isInternational: false,
};

describe('FlightSummaryComponent', () => {
  it('creates with a required offer input', async () => {
    await TestBed.configureTestingModule({ imports: [FlightSummaryComponent] }).compileComponents();
    const fixture = TestBed.createComponent(FlightSummaryComponent);
    fixture.componentRef.setInput('offer', offer);
    fixture.detectChanges();
    expect(fixture.componentInstance.offer()).toEqual(offer);
  });
});
