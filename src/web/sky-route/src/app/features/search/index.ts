/**
 * Public API for the SkyRoute search feature module.
 * The host application should only import from this barrel.
 */
export { provideSearchFeature, type SearchFeatureConfig } from './search.providers';
export { searchFeatureRoutes } from './search.routes';
export { SearchPageComponent } from './pages/search-page.component';
export { FlightSearchService } from './services/flight-search.service';
export type { SortKey } from './state/sort-offers';
export { sortOffers } from './state/sort-offers';
