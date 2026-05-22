import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Airport,
  FlightOffer,
  FlightSearchCriteria,
  CABIN_CLASS_VALUE,
  SEARCH_API_BASE_URL,
} from '../../../shared';

@Injectable()
export class FlightSearchService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(SEARCH_API_BASE_URL);

  getAirports(): Observable<Airport[]> {
    return this.http.get<Airport[]>(`${this.baseUrl}/api/airports`);
  }

  search(criteria: FlightSearchCriteria): Observable<FlightOffer[]> {
    const cabinValue =
      typeof criteria.cabin === 'number' ? criteria.cabin : CABIN_CLASS_VALUE[criteria.cabin];

    return this.http.post<FlightOffer[]>(`${this.baseUrl}/api/flights/search`, {
      originCode: criteria.originCode,
      destinationCode: criteria.destinationCode,
      departureDate: criteria.departureDate,
      passengers: criteria.passengers,
      cabin: cabinValue,
    });
  }
}
