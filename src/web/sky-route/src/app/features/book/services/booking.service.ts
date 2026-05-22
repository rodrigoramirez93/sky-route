import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  BOOK_API_BASE_URL,
  BookingConfirmation,
  BookingRequest,
  CABIN_CLASS_VALUE,
  FlightOffer,
} from '../../../shared';

/**
 * Public service of the book feature. Hosts may hand off a selected flight
 * via {@link setSelectedOffer} before navigating to the booking page.
 */
@Injectable()
export class BookingService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(BOOK_API_BASE_URL);

  private readonly _selectedOffer = signal<FlightOffer | null>(null);
  readonly selectedOffer = this._selectedOffer.asReadonly();

  setSelectedOffer(offer: FlightOffer | null): void {
    this._selectedOffer.set(offer);
  }

  confirm(request: BookingRequest): Observable<BookingConfirmation> {
    const cabin =
      typeof request.cabin === 'number' ? request.cabin : CABIN_CLASS_VALUE[request.cabin];

    return this.http.post<BookingConfirmation>(`${this.baseUrl}/api/bookings`, {
      ...request,
      cabin,
    });
  }
}
