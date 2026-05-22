import { Airport } from './airport';
import { CabinClass } from './cabin-class';

export interface FlightSearchCriteria {
  originCode: string;
  destinationCode: string;
  departureDate: string; // ISO yyyy-MM-dd
  passengers: number;
  cabin: CabinClass;
}

export interface FlightOffer {
  providerKey: string;
  flightNumber: string;
  origin: Airport;
  destination: Airport;
  departureUtc: string;
  arrivalUtc: string;
  durationMinutes: number;
  cabin: CabinClass | number;
  pricePerPassenger: number;
  totalPrice: number;
  passengers: number;
  currency: string;
  isInternational: boolean;
}
