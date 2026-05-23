import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PassengerFormComponent } from './passenger-form.component';
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
  passengers: 2,
  pricePerPassenger: 250,
  totalPrice: 500,
  currency: 'USD',
  isInternational: false,
};

describe('PassengerFormComponent', () => {
  it('creates one form group per passenger', async () => {
    await TestBed.configureTestingModule({
      imports: [PassengerFormComponent],
      providers: [provideRouter([])],
    }).compileComponents();

    const fixture = TestBed.createComponent(PassengerFormComponent);
    fixture.componentRef.setInput('offer', offer);
    fixture.detectChanges();
    const passengers = fixture.componentInstance['passengers'];
    expect(passengers.length).toBe(2);
  });
});
