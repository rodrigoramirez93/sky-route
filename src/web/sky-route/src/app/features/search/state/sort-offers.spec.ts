import { describe, expect, it } from 'vitest';
import { sortOffers } from './sort-offers';
import { FlightOffer } from '../../../shared';

function offer(partial: Partial<FlightOffer>): FlightOffer {
  return {
    providerKey: 'GlobalAir',
    flightNumber: 'GA1',
    origin: { code: 'JFK', name: 'JFK', city: 'NY', country: 'USA' },
    destination: { code: 'LHR', name: 'LHR', city: 'London', country: 'UK' },
    departureUtc: '2026-01-01T10:00:00Z',
    arrivalUtc: '2026-01-01T18:00:00Z',
    durationMinutes: 480,
    cabin: 'Economy',
    pricePerPassenger: 100,
    totalPrice: 100,
    passengers: 1,
    currency: 'USD',
    isInternational: true,
    ...partial,
  };
}

describe('sortOffers', () => {
  const a = offer({ flightNumber: 'A', totalPrice: 300, durationMinutes: 120, departureUtc: '2026-01-01T08:00:00Z' });
  const b = offer({ flightNumber: 'B', totalPrice: 100, durationMinutes: 400, departureUtc: '2026-01-01T14:00:00Z' });
  const c = offer({ flightNumber: 'C', totalPrice: 200, durationMinutes: 220, departureUtc: '2026-01-01T06:00:00Z' });

  it('sorts by price ascending', () => {
    expect(sortOffers([a, b, c], 'price-asc').map((o) => o.flightNumber)).toEqual(['B', 'C', 'A']);
  });

  it('sorts by price descending', () => {
    expect(sortOffers([a, b, c], 'price-desc').map((o) => o.flightNumber)).toEqual(['A', 'C', 'B']);
  });

  it('sorts by duration (shortest first)', () => {
    expect(sortOffers([a, b, c], 'duration').map((o) => o.flightNumber)).toEqual(['A', 'C', 'B']);
  });

  it('sorts by departure time', () => {
    expect(sortOffers([a, b, c], 'departure').map((o) => o.flightNumber)).toEqual(['C', 'A', 'B']);
  });

  it('does not mutate input', () => {
    const input = [a, b, c];
    sortOffers(input, 'price-asc');
    expect(input.map((o) => o.flightNumber)).toEqual(['A', 'B', 'C']);
  });
});
