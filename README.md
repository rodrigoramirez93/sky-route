# SkyRoute

Flight Search & Booking module for the SkyRoute travel aggregator.
Senior Full-Stack Developer Challenge.

- **Backend:** ASP.NET Core (.NET 10), 3-layer architecture (Presentation /
  Business / Data Access), Strategy pattern for flight operators.
- **Frontend:** Angular 21 standalone, two **independent, exportable feature
  modules** (`search` and `book`) wired by a thin host shell.
- **Observability:** OpenTelemetry + Serilog → Aspire Dashboard.

---

## Quick start

### Option A — Docker (everything together)

```powershell
docker compose up --build
# Web:        http://localhost:4200
# API:        http://localhost:8080
# Dashboard:  http://localhost:18888
```

### Option B — Run locally

Backend:

```powershell
cd src/api/SkyRoute
dotnet restore
dotnet build
dotnet test                              # runs all xUnit projects
dotnet run --project src/SkyRoute.Api    # http://localhost:8080
```

Frontend:

```powershell
cd src/web/sky-route
npm install
npm start            # ng serve → http://localhost:4200
npm run build        # production build
npm test             # Vitest unit tests (run mode)
```

> The frontend talks to the backend at `http://localhost:8080` by default
> (see `src/environments/environment.development.ts`).

---

## API surface

| Method | Path                  | Body / Result                                       |
| ------ | --------------------- | --------------------------------------------------- |
| GET    | `/api/airports`       | `AirportDto[]` — hardcoded catalog                  |
| POST   | `/api/flights/search` | `FlightSearchRequestDto` → `FlightOfferDto[]`       |
| POST   | `/api/bookings`       | `BookingRequestDto` → `BookingConfirmationDto`      |

Each `FlightOfferDto` carries both `pricePerPassenger` and `totalPrice` so the
UI never recomputes pricing, plus an `isInternational` flag derived from the
airport countries (used to switch the document-type field in the booking
screen).

OpenAPI is exposed in Development at `/openapi/v1.json` (Aspire-style minimal
OpenAPI; ready to plug into Swagger UI if desired).

---

## Architecture

### Backend (3 layers)

```
SkyRoute.Api (Presentation)
  ├── Controllers (AirportsController, FlightsController, BookingsController)
  ├── Contracts   (DTOs + explicit mappings)
  └── Composition (AddSkyRoute() registers everything)
        │
        ▼
SkyRoute.BusinessLogic
  ├── Domain      (Airport, FlightSearchCriteria, FlightOffer, BookingRequest, …)
  ├── Providers   (IFlightProviderStrategy + GlobalAirProvider + BudgetWingsProvider)
  ├── Search      (FlightSearchService — fans out across strategies in parallel)
  ├── Booking     (BookingService, BookingReferenceGenerator)
  └── Documents   (NationalIdValidator, PassportValidator, DocumentValidatorFactory)
        │
        ▼
SkyRoute.DataAccessLayer
  ├── Airports    (AirportRepository — hardcoded catalog of 7 airports / 4 countries)
  ├── Booking     (InMemoryBookingRepository)
  └── Providers   (MockGlobalAirClient, MockBudgetWingsClient — deterministic-ish feeds)
```

**Dependency direction is strict:** `Api → BusinessLogic → DataAccessLayer`.
The Api only references DAL in its composition root for DI registrations.

### Strategy pattern — onboarding new operators

`IFlightProviderStrategy` is the seam:

```csharp
public interface IFlightProviderStrategy
{
    string ProviderKey { get; }
    Task<IReadOnlyList<FlightOffer>> SearchAsync(
        FlightSearchCriteria criteria, CancellationToken ct);
}
```

`FlightSearchService` injects `IEnumerable<IFlightProviderStrategy>` and fans
out in parallel. Provider-specific pricing rules live inside each strategy
(no shared base class — providers are truly independent).

To add a new airline:

1. Add a raw client interface + mock/real implementation in `DataAccessLayer/Providers/<Name>`.
2. Add a `class <Name>Provider : IFlightProviderStrategy` in
   `BusinessLogic/Providers/<Name>` that applies the airline's pricing rule.
3. Register both in `SkyRouteServiceCollectionExtensions.AddSkyRoute()`:

   ```csharp
   services.AddSingleton<I<Name>Client, Mock<Name>Client>();
   services.AddSingleton<IFlightProviderStrategy, <Name>Provider>();
   ```

No existing class is touched. Open/closed.

### Frontend — two independent, exportable feature modules

```
src/app/
  app.config.ts, app.routes.ts, app.ts   ← host shell
  shared/                                ← contracts both features agree on
    models/    (Airport, FlightOffer, BookingRequest, CabinClass)
    tokens/    (SEARCH_API_BASE_URL, BOOK_API_BASE_URL, FLIGHT_SELECTION_HANDLER,
                BOOKING_CONFIRMED_HANDLER)
  features/
    search/                              ← self-contained module
      index.ts                           ← PUBLIC API (the only host import)
      search.routes.ts                   ← searchFeatureRoutes
      search.providers.ts                ← provideSearchFeature(config)
      pages/   components/   services/   state/
    book/                                ← self-contained module
      index.ts                           ← PUBLIC API
      book.routes.ts                     ← bookFeatureRoutes
      book.providers.ts                  ← provideBookFeature(config)
      pages/   components/   services/   validators/
```

Properties:

- Each feature exposes a single barrel (`index.ts`). The host (or any external
  application) only imports from `./features/search` and `./features/book`.
- Each feature exposes a `withXFeature({...})`-style provider factory returning
  `EnvironmentProviders`. Config is explicit: `apiBaseUrl`, optional handlers.
- **No cross-module imports.** Features communicate only through the `shared/`
  contracts (DTOs + injection tokens). Cross-feature navigation is host-owned:
  the host registers a `FLIGHT_SELECTION_HANDLER` that captures the selected
  offer (via `BookingService.setSelectedOffer`) and routes to `/book`.
- The book feature's `document-field` component swaps both **label and
  validator** at runtime based on `offer.isInternational`:
  - International → `Passport Number` + 6–9 alphanumeric.
  - Domestic → `National ID` + 5–12 digits.

To consume the features from a different host application:

```ts
import { provideRouter } from '@angular/router';
import { provideSearchFeature, searchFeatureRoutes } from 'sky-route/features/search';
import { provideBookFeature, bookFeatureRoutes } from 'sky-route/features/book';

bootstrapApplication(MyHost, {
  providers: [
    provideSearchFeature({ apiBaseUrl: 'https://api.example.com' }),
    provideBookFeature({ apiBaseUrl: 'https://api.example.com' }),
    provideRouter([
      { path: 'search', loadChildren: () => searchFeatureRoutes },
      { path: 'book',   loadChildren: () => bookFeatureRoutes },
    ]),
  ],
});
```

---

## Pricing rules

| Provider     | Per-passenger price                                                    |
| ------------ | ---------------------------------------------------------------------- |
| GlobalAir    | `round(baseFare × 1.15, 2)` — 15% fuel surcharge                       |
| BudgetWings  | `max(round(baseFare × 0.90, 2), 29.99)` — 10% promo, $29.99 floor      |

Total price = per-passenger price × number of passengers, also rounded to 2
decimals. The API always returns both numbers.

---

## Testing

Backend (39 tests):

```powershell
cd src/api/SkyRoute
dotnet test
```

Frontend (20 tests, Vitest run mode):

```powershell
cd src/web/sky-route
npm test
```

Both suites cover the items called out in the brief: pricing rules (including
the BudgetWings floor), domestic vs international detection, document
validation switching, search aggregation, client-side sorting.

---

## Convenience scripts

From the repo root:

```powershell
./scripts/dev.ps1        # docker compose up --build
./scripts/test-all.ps1   # dotnet test + npm test
```

---

## Decisions and trade-offs

- **Hardcoded airport catalog.** Six airports across four countries (USA, UK,
  Argentina, Spain). Internal flights for USA and Argentina exercise the
  domestic-document path.
- **Mocked providers, deterministic-ish feeds.** Each mock client seeds its
  own RNG from `(provider, route, date, cabin)` so the same search produces the
  same flights — easier to demo. Cabin classes apply a multiplier on the base
  fare before each provider's own rule.
- **In-memory booking store.** A `ConcurrentDictionary<string, BookingRecord>`
  is sufficient for the brief. Swap with EF Core + a real DB by replacing the
  one DI registration of `IBookingRepository`.
- **No shared base class for providers.** Each strategy owns its own pricing.
  This keeps the open/closed promise clean — a new operator never touches an
  existing file.
- **`TreatWarningsAsErrors`** on production code; relaxed in test projects.
- **`.editorconfig` for C#** is treated as immutable per the task brief.

## Known limitations (would do next)

- No persistence beyond process lifetime; no migrations.
- No authentication/authorization, no payments.
- No real airline APIs (mocked only).
- No cloud deployment.
- No E2E suite — the placeholder file `test/black-box-e2e-selenium-project.txt`
  stays. Would add Playwright/Selenium covering: airport dropdown loads → search
  returns ≥1 result → sort works → select → booking confirms with reference.
- No i18n; basic accessibility (labels + semantic HTML) but no full audit.
- Single Aspire Dashboard for telemetry; not configured for shipping to a real
  collector.
