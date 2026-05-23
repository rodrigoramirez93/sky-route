import { ChangeDetectionStrategy, Component, Inject, Optional, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import {
  BOOKING_CONFIRMED_HANDLER,
  BookingConfirmedHandler,
  BookingRequest,
  FlightOffer,
  PassengerDetails,
} from '../../../../shared';
import { BookingService } from '../../services/booking.service';
import { FlightSummaryComponent } from '../../components/flight-summary/flight-summary.component';
import { PriceBreakdownComponent } from '../../components/price-breakdown/price-breakdown.component';
import { PassengerFormComponent } from '../../components/passenger-form/passenger-form.component';

@Component({
  selector: 'sr-booking-page',
  standalone: true,
  imports: [CommonModule, RouterLink, FlightSummaryComponent, PriceBreakdownComponent, PassengerFormComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './booking-page.component.html',
  styleUrls: ['./booking-page.component.css'],
})
export class BookingPageComponent {
  private readonly bookingService = inject(BookingService);

  protected readonly offer = this.bookingService.selectedOffer;
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly confirmation = signal<string | null>(null);

  constructor(
    @Optional() @Inject(BOOKING_CONFIRMED_HANDLER) private readonly onConfirmed: BookingConfirmedHandler | null,
  ) {}

  onConfirm(offer: FlightOffer, passengers: PassengerDetails[]): void {
    this.submitting.set(true);
    this.error.set(null);
    this.confirmation.set(null);

    const request: BookingRequest = {
      providerKey: offer.providerKey,
      flightNumber: offer.flightNumber,
      originCode: offer.origin.code,
      destinationCode: offer.destination.code,
      departureUtc: offer.departureUtc,
      arrivalUtc: offer.arrivalUtc,
      cabin: offer.cabin,
      passengers: offer.passengers,
      pricePerPassenger: offer.pricePerPassenger,
      currency: offer.currency,
      passengerDetails: passengers,
    };

    this.bookingService.confirm(request).subscribe({
      next: (conf) => {
        this.confirmation.set(conf.reference);
        this.submitting.set(false);
        this.onConfirmed?.(conf.reference);
      },
      error: (err) => {
        this.error.set(err?.error?.detail ?? err?.message ?? 'Booking failed');
        this.submitting.set(false);
      },
    });
  }
}
