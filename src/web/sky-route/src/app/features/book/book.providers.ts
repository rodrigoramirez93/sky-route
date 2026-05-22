import { EnvironmentProviders, makeEnvironmentProviders } from '@angular/core';
import {
  BOOK_API_BASE_URL,
  BOOKING_CONFIRMED_HANDLER,
  BookingConfirmedHandler,
} from '../../shared';
import { BookingService } from './services/booking.service';

export interface BookFeatureConfig {
  apiBaseUrl: string;
  onBookingConfirmed?: BookingConfirmedHandler;
}

/**
 * Registers providers required by the book feature. The {@link BookingService}
 * is provided in root so the host can call setSelectedOffer before navigating
 * into the feature's routes.
 */
export function provideBookFeature(config: BookFeatureConfig): EnvironmentProviders {
  const providers: unknown[] = [
    { provide: BOOK_API_BASE_URL, useValue: config.apiBaseUrl },
    BookingService,
  ];

  if (config.onBookingConfirmed) {
    providers.push({ provide: BOOKING_CONFIRMED_HANDLER, useValue: config.onBookingConfirmed });
  }

  return makeEnvironmentProviders(providers as never[]);
}
