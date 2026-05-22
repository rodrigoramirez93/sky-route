import { InjectionToken } from '@angular/core';
import { FlightOffer } from '../models/flight';

/** Base URL for the SkyRoute backend used by the search feature. */
export const SEARCH_API_BASE_URL = new InjectionToken<string>('SEARCH_API_BASE_URL');

/** Base URL for the SkyRoute backend used by the book feature. */
export const BOOK_API_BASE_URL = new InjectionToken<string>('BOOK_API_BASE_URL');

/**
 * Host-provided handler invoked when a user selects a flight in the search
 * feature. The host decides how to route to the book feature (e.g. router
 * navigation). Keeps search and book fully decoupled.
 */
export type FlightSelectionHandler = (offer: FlightOffer) => void;

export const FLIGHT_SELECTION_HANDLER = new InjectionToken<FlightSelectionHandler>('FLIGHT_SELECTION_HANDLER');

/**
 * Host-provided handler invoked when a booking is successfully confirmed.
 */
export type BookingConfirmedHandler = (reference: string) => void;

export const BOOKING_CONFIRMED_HANDLER = new InjectionToken<BookingConfirmedHandler>('BOOKING_CONFIRMED_HANDLER');
