import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { BookingService, Booking } from './booking.service';
import { LoggerService } from './logger.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  protected readonly title = signal('Sky Route');
  private bookingService = inject(BookingService);
  private logger = inject(LoggerService);

  bookings = signal<Booking[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit(): void {
    this.logger.info('Sky Route Angular app initialized', { component: 'App' });

    this.bookingService.getBookings().subscribe({
      next: (data) => {
        this.bookings.set(data);
        this.loading.set(false);
        this.logger.info('Bookings loaded', { count: data.length });
      },
      error: (err) => {
        this.error.set('Failed to load bookings: ' + err.message);
        this.loading.set(false);
        this.logger.error('Failed to load bookings', { message: err.message });
      }
    });
  }
}
