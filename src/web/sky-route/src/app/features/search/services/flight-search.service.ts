import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, defer, from } from 'rxjs';
import {
  Airport,
  FlightOffer,
  FlightSearchCriteria,
  CABIN_CLASS_VALUE,
  SEARCH_API_BASE_URL,
} from '../../../shared';
import { TelemetryService } from '../../../telemetry.service';
import { LoggerService } from '../../../logger.service';

const SCOPE = 'features.search.FlightSearchService';

@Injectable()
export class FlightSearchService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(SEARCH_API_BASE_URL);
  private readonly telemetry = inject(TelemetryService);
  private readonly logger = inject(LoggerService);

  getAirports(): Observable<Airport[]> {
    return defer(() =>
      from(
        this.telemetry.withSpan('Catalog.GetAirports', {}, () =>
          this.http.get<Airport[]>(`${this.baseUrl}/api/airports`).toPromise() as Promise<Airport[]>,
        ),
      ),
    );
  }

  search(criteria: FlightSearchCriteria): Observable<FlightOffer[]> {
    const cabinValue =
      typeof criteria.cabin === 'number' ? criteria.cabin : CABIN_CLASS_VALUE[criteria.cabin];

    const attributes = {
      'flight.origin': criteria.originCode,
      'flight.destination': criteria.destinationCode,
      'flight.departure_date': criteria.departureDate,
      'flight.passenger_count': criteria.passengers,
      'flight.cabin': String(criteria.cabin),
    };

    this.logger.info('Flight search submitted', SCOPE, attributes);

    return defer(() =>
      from(
        this.telemetry.withSpan('Flights.Search', attributes, async () => {
          const offers = (await this.http
            .post<FlightOffer[]>(`${this.baseUrl}/api/flights/search`, {
              originCode: criteria.originCode,
              destinationCode: criteria.destinationCode,
              departureDate: criteria.departureDate,
              passengers: criteria.passengers,
              cabin: cabinValue,
            })
            .toPromise()) as FlightOffer[];

          this.logger.info('Flight search returned results', SCOPE, {
            ...attributes,
            'flight.results_count': offers?.length ?? 0,
          });
          return offers;
        }),
      ),
    );
  }
}
