import { ApplicationConfig, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { Router, provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { environment } from '../environments/environment';
import { provideSearchFeature } from './features/search';
import { provideBookFeature, BookingService } from './features/book';
import { FLIGHT_SELECTION_HANDLER, FlightSelectionHandler } from './shared';
import { inject } from '@angular/core';
import { correlationInterceptor } from './correlation.interceptor';
import { MetricsService } from './metrics.service';

/**
 * Bridges the search feature's selection event to the book feature.
 * Lives in the host because neither feature is allowed to know about the other.
 */
function flightSelectionBridge(): FlightSelectionHandler {
  const router = inject(Router);
  const booking = inject(BookingService);
  return (offer) => {
    booking.setSelectedOffer(offer);
    void router.navigateByUrl('/book');
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([correlationInterceptor])),
    provideBookFeature({ apiBaseUrl: environment.apiUrl }),
    provideSearchFeature({ apiBaseUrl: environment.apiUrl }),
    { provide: FLIGHT_SELECTION_HANDLER, useFactory: flightSelectionBridge },
    provideAppInitializer(() => inject(MetricsService).start()),
  ],
};
