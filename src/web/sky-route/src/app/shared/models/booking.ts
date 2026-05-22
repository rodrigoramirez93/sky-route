import { CabinClass } from './cabin-class';

export interface PassengerDetails {
  fullName: string;
  email: string;
  documentNumber: string;
}

export interface BookingRequest {
  providerKey: string;
  flightNumber: string;
  originCode: string;
  destinationCode: string;
  departureUtc: string;
  arrivalUtc: string;
  cabin: CabinClass | number;
  passengers: number;
  pricePerPassenger: number;
  currency: string;
  passengerDetails: PassengerDetails[];
}

export interface BookingConfirmation {
  reference: string;
  totalPrice: number;
  currency: string;
  createdAtUtc: string;
}
