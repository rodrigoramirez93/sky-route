import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, defer, from } from 'rxjs';
import {
  BOOK_API_BASE_URL,
  BookingConfirmation,
  BookingRequest,
  CABIN_CLASS_VALUE,
  FlightOffer,
} from '../../../shared';
import { TelemetryService } from '../../../telemetry.service';
import { LoggerService } from '../../../logger.service';

const SCOPE = 'features.book.BookingService';

/**
 * Public service of the book feature. Hosts may hand off a selected flight
 * via {@link setSelectedOffer} before navigating to the booking page.
 */
@Injectable()
export class BookingService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(BOOK_API_BASE_URL);
  private readonly telemetry = inject(TelemetryService);
  private readonly logger = inject(LoggerService);

  private readonly _selectedOffer = signal<FlightOffer | null>(null);
  readonly selectedOffer = this._selectedOffer.asReadonly();

  setSelectedOffer(offer: FlightOffer | null): void {
    this._selectedOffer.set(offer);
    if (offer) {
      this.logger.info('Booking offer selected', SCOPE, {
        'booking.provider': offer.providerKey,
        'booking.origin': offer.origin.code,
        'booking.destination': offer.destination.code,
        'booking.cabin': String(offer.cabin),
        'booking.passenger_count': offer.passengers,
        'booking.is_international': offer.isInternational,
        'booking.currency': offer.currency,
      });
    }
  }

  confirm(request: BookingRequest): Observable<BookingConfirmation> {
    const cabin =
      typeof request.cabin === 'number' ? request.cabin : CABIN_CLASS_VALUE[request.cabin];

    // Never log PII (names, emails, document numbers) — only counts & route.
    const attributes = {
      'booking.provider': request.providerKey,
      'booking.origin': request.originCode,
      'booking.destination': request.destinationCode,
      'booking.cabin': String(request.cabin),
      'booking.passenger_count': request.passengers,
      'booking.currency': request.currency,
      'booking.total_price': request.pricePerPassenger * request.passengers,
    };

    this.logger.info('Booking submitted', SCOPE, attributes);

    return defer(() =>
      from(
        this.telemetry.withSpan('Booking.Confirm', attributes, async () => {
          try {
            const confirmation = (await this.http
              .post<BookingConfirmation>(`${this.baseUrl}/api/bookings`, {
                ...request,
                cabin,
              })
              .toPromise()) as BookingConfirmation;
            this.logger.info('Booking confirmed', SCOPE, {
              ...attributes,
              'booking.reference': confirmation.reference,
            });
            return confirmation;
          } catch (err) {
            const reason = err instanceof Error ? err.message : String(err);
            this.logger.warn('Booking failed', SCOPE, { ...attributes, reason });
            throw err;
          }
        }),
      ),
    );
  }
}
