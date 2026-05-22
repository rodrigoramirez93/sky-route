import { ChangeDetectionStrategy, Component, Inject, Optional, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
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
  imports: [CommonModule, RouterLink, FlightSummaryComponent, PriceBreakdownComponent, PassengerFormComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (offer(); as o) {
      <section class="sr-booking-page">
        <header class="sr-page-header">
          <h2>Confirm your booking</h2>
          <p class="sr-page-subtitle">Review your selection and add passenger details.</p>
        </header>

        <div class="sr-booking-grid">
          <div class="sr-booking-form">
            <div class="sr-card">
              <sr-passenger-form
                [offer]="o"
                [submitting]="submitting()"
                (confirmed)="onConfirm(o, $event)"
              />
            </div>

            <div class="sr-status-region" aria-live="polite">
              @if (error(); as err) {
                <div class="sr-card sr-error" role="alert">
                  <span aria-hidden="true">⚠</span> {{ err }}
                </div>
              }
              @if (confirmation(); as conf) {
                <div class="sr-card sr-success" role="status">
                  <div class="sr-success-icon" aria-hidden="true">✓</div>
                  <div>
                    <strong>Booking confirmed.</strong>
                    <p>Reference: <code>{{ conf }}</code></p>
                    <a class="sr-link" routerLink="/search">Book another flight →</a>
                  </div>
                </div>
              }
            </div>
          </div>

          <aside class="sr-booking-aside">
            <div class="sr-card">
              <sr-flight-summary [offer]="o" />
            </div>
            <div class="sr-card">
              <sr-price-breakdown [offer]="o" />
            </div>
          </aside>
        </div>
      </section>
    } @else {
      <section class="sr-booking-page">
        <div class="sr-card sr-empty">
          <span class="sr-empty-icon" aria-hidden="true">🧳</span>
          <div>
            <strong>No flight selected.</strong>
            <p>Pick a flight from the search results to start booking.</p>
            <a class="sr-btn-primary sr-btn-link" routerLink="/search">Go to search</a>
          </div>
        </div>
      </section>
    }
  `,
  styles: [
    `
      .sr-booking-page { display: flex; flex-direction: column; gap: 1.25rem; }
      .sr-page-header h2 { font-size: 1.5rem; }
      .sr-page-subtitle { color: var(--sr-muted); margin: 0.25rem 0 0; }

      .sr-booking-grid {
        display: grid;
        grid-template-columns: minmax(0, 1fr) 320px;
        gap: 1.25rem;
        align-items: start;
      }
      .sr-booking-form { display: flex; flex-direction: column; gap: 1rem; min-width: 0; }
      .sr-booking-aside { display: flex; flex-direction: column; gap: 1rem; position: sticky; top: 80px; }

      @media (max-width: 900px) {
        .sr-booking-grid { grid-template-columns: 1fr; }
        .sr-booking-aside { position: static; order: -1; }
      }

      .sr-error { display: flex; gap: 0.6rem; background: var(--sr-danger-soft); color: var(--sr-danger); border-color: #f5c2c7; }
      .sr-success { display: flex; gap: 0.85rem; align-items: flex-start; background: var(--sr-success-soft); color: var(--sr-success); border-color: #b9e2c2; }
      .sr-success-icon {
        width: 32px; height: 32px; border-radius: 50%;
        background: var(--sr-success); color: #fff;
        display: inline-flex; align-items: center; justify-content: center;
        font-weight: 700;
      }
      .sr-success p { margin: 0.25rem 0; }
      .sr-success code { background: rgba(0,0,0,0.06); padding: 0.1rem 0.4rem; border-radius: 4px; font-size: 0.95rem; }
      .sr-link { color: var(--sr-primary); font-weight: 600; text-decoration: none; }
      .sr-link:hover { text-decoration: underline; }

      .sr-empty { display: flex; gap: 1rem; align-items: center; }
      .sr-empty-icon { font-size: 2rem; }
      .sr-empty strong { color: var(--sr-text); }
      .sr-empty p { margin: 0.25rem 0 0.75rem; color: var(--sr-muted); }
      .sr-btn-link { display: inline-block; text-decoration: none; }
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
