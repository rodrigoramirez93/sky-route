import { EnvironmentProviders, makeEnvironmentProviders } from '@angular/core';
import { FlightSelectionHandler, FLIGHT_SELECTION_HANDLER, SEARCH_API_BASE_URL } from '../../shared';
import { FlightSearchService } from './services/flight-search.service';

export interface SearchFeatureConfig {
  apiBaseUrl: string;
  /**
   * Optional handler invoked when a user selects a flight from the results.
   * Hosts typically use this to navigate to the book feature.
   */
  onFlightSelected?: FlightSelectionHandler;
}

/**
 * Registers the providers needed by the search feature. Call this once from
 * the host's ApplicationConfig.providers.
 */
export function provideSearchFeature(config: SearchFeatureConfig): EnvironmentProviders {
  const providers: unknown[] = [
    { provide: SEARCH_API_BASE_URL, useValue: config.apiBaseUrl },
    FlightSearchService,
  ];

  if (config.onFlightSelected) {
    providers.push({ provide: FLIGHT_SELECTION_HANDLER, useValue: config.onFlightSelected });
  }

  return makeEnvironmentProviders(providers as never[]);
}
