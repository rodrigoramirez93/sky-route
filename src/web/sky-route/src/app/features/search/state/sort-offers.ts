import { FlightOffer } from '../../../shared';

export type SortKey = 'price-asc' | 'price-desc' | 'duration' | 'departure';

export function sortOffers(offers: ReadonlyArray<FlightOffer>, key: SortKey): FlightOffer[] {
  const copy = [...offers];
  switch (key) {
    case 'price-asc':
      return copy.sort((a, b) => a.totalPrice - b.totalPrice);
    case 'price-desc':
      return copy.sort((a, b) => b.totalPrice - a.totalPrice);
    case 'duration':
      return copy.sort((a, b) => a.durationMinutes - b.durationMinutes);
    case 'departure':
      return copy.sort(
        (a, b) => new Date(a.departureUtc).getTime() - new Date(b.departureUtc).getTime(),
      );
  }
}
