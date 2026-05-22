import { ChangeDetectionStrategy, Component, Inject, Optional, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  BOOKING_CONFIRMED_HANDLER,
  BookingConfirmedHandler,
  BookingRequest,
  FlightOffer,
  PassengerDetails,
} from '../../../shared';
import { BookingService } from '../services/booking.service';
import { FlightSummaryComponent } from '../components/flight-summary.component';
import { PriceBreakdownComponent } from '../components/price-breakdown.component';
import { PassengerFormComponent } from '../components/passenger-form.component';

@Component({
  selector: 'sr-booking-page',
  standalone: true,
  imports: [CommonModule, FlightSummaryComponent, PriceBreakdownComponent, PassengerFormComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (offer(); as o) {
      <section class="sr-booking-page">
        <h2>Confirm your booking</h2>
        <sr-flight-summary [offer]="o" />
        <sr-price-breakdown [offer]="o" />
        <sr-passenger-form [offer]="o" [submitting]="submitting()" (confirmed)="onConfirm(o, $event)" />

        @if (error(); as err) {
          <p class="sr-error">{{ err }}</p>
        }
        @if (confirmation(); as conf) {
          <p class="sr-success">
            Booking confirmed. Reference: <strong>{{ conf }}</strong>
          </p>
        }
      </section>
    } @else {
      <p class="sr-status">No flight selected. Please return to search.</p>
    }
  `,
  styles: [
    `
      .sr-booking-page { display: flex; flex-direction: column; gap: 1rem; }
      .sr-error { color: #b00020; }
      .sr-success { color: #1b6e2e; background: #e6f7e9; padding: 0.75rem; border-radius: 4px; }
      .sr-status { padding: 0.75rem; background: #f6f6f6; border-radius: 4px; }
    `,
  ],
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
