# AGENTS.md — SkyRoute Travel Platform

This file is the working brief for AI coding agents (Copilot, Cursor, Claude, etc.)
contributing to this repository. It captures **what the project must become**
(per the hiring challenge) and **what currently exists** (work-in-progress
scaffolding). Keep it up to date as the implementation evolves.

> Status: 🚧 Scaffolding stage. Domain code for flight search and booking is
> **not yet implemented** — only Angular + ASP.NET Core project skeletons,
> OpenTelemetry wiring, and an Aspire Dashboard via docker-compose.

---

## 1. Goal — What we're building

A **Flight Search & Booking module** for SkyRoute, a flight aggregator. The
scope is taken from `docs/Requirements.pdf` (Senior Full-Stack Challenge,
3–4 hours, Angular + .NET).

### 1.1 Business rules

SkyRoute aggregates flights from multiple **providers**. For this challenge,
two providers must be **mocked in the backend** (no real external APIs):

| Provider     | Pricing rule                                                                                                            |
| ------------ | ----------------------------------------------------------------------------------------------------------------------- |
| GlobalAir    | `final = baseFare × 1.15` (15% fuel surcharge). Round to 2 decimals.                                                    |
| BudgetWings  | `final = max(baseFare × 0.90, 29.99)` (10% promo discount on base fare only; $29.99 floor).                              |

⚠️ The architecture **must make it easy to add new providers later**
(open/closed — new provider = new class/strategy, no edits to existing ones).

### 1.2 Functional requirements

**Flight Search form** (frontend → backend):
- Origin & destination airports — dropdown, hardcoded ≥ 6 airports across ≥ 2 countries.
- Departure date.
- Number of passengers (1–9).
- Cabin class: Economy / Business / First.

**Results list/table** shows: provider, flight number, departure time,
arrival time, duration, cabin class, price.

- **Price displayed must be the TOTAL for all passengers**, with the
  per-passenger price shown as secondary info (e.g. `USD 320.00 total / USD 160.00 per person`).
- Sorting on the **frontend only** (no extra API call) by: price asc, price desc,
  duration (shortest first), departure time.
- Loading indicator while searching; clear empty state when no results.

**Booking flow**:
- Selecting a flight opens a booking screen with:
  - Flight summary (route, provider, times, cabin class).
  - Price breakdown (per-passenger × passengers = total).
  - Passenger form: full name, email, document number.
  - Confirm action → backend → returns a booking reference code.
- ⚠️ **Document field is route-dependent**:
  - Origin & destination in **different countries** → label `Passport Number`, validate as passport.
  - Same country (domestic) → label `National ID`, validate accordingly.
  - Both label and validation must switch dynamically.

**Backend API**: design endpoints & contracts for the flows above.

### 1.3 Deliverables (per the PDF)

1. Working app (frontend + backend) runnable locally.
2. Source in a Git repo (this one).
3. README with: setup/run instructions, architecture decisions, trade-offs / known limitations.
4. No cloud deployment required. Document anything skipped due to time.

---

## 2. Tech stack (mandated)

- **Frontend:** Angular 21 (standalone components, signals, Vitest for unit tests).
- **Backend:** ASP.NET Core (.NET 10 preview, `Microsoft.AspNetCore.OpenApi` 10.0.8),
  layered as `Api` → `BusinessLogic` → `DataAccessLayer`.
- **Observability:** OpenTelemetry (traces, metrics, logs) + Serilog on the API;
  OTLP/HTTP logs exporter in the Angular app. Aspire Dashboard collects both.
- **Local infra:** `docker-compose.yml` runs `aspire-dashboard`, `api`, `web`.
- **Tests:** xUnit + Moq (.NET), Vitest (Angular). E2E placeholder: `test/black-box-e2e-selenium-project.txt`.

---

## 3. Repository layout

```
sky-route/
├── AGENTS.md                       ← this file
├── README.md                       ← currently just a title; needs setup + architecture notes
├── LICENSE
├── docker-compose.yml              ← aspire-dashboard + api + web
├── docs/
│   └── Requirements.pdf            ← source of truth for the challenge
├── src/
│   ├── api/SkyRoute/               ← .NET solution (SkyRoute.slnx)
│   │   ├── Directory.Packages.props  (central package versions)
│   │   ├── Dockerfile
│   │   ├── src/
│   │   │   ├── SkyRoute.Api/        (Controllers/, Program.cs — OTel + Serilog + CORS)
│   │   │   ├── SkyRoute.BusinessLogic/   (empty Class1.cs — to be filled)
│   │   │   └── SkyRoute.DataAccessLayer/ (empty Class1.cs — to be filled)
│   │   └── test/
│   │       ├── SkyRoute.Api.UnitTest/
│   │       ├── SkyRoute.BusinessLogic.UnitTest/
│   │       └── SkyRoute.DataAccessLayer.UnitTest/
│   └── web/sky-route/              ← Angular 21 app
│       ├── package.json
│       ├── nginx.conf              (serves the built dist behind nginx in Docker)
│       ├── Dockerfile
│       └── src/
│           ├── main.ts             (bootstraps + initTelemetry)
│           ├── telemetry.ts        (OTLP/HTTP logs → aspire-dashboard:18890)
│           └── app/                (app.ts, booking.service.ts, logger.service.ts, …)
└── test/
    └── black-box-e2e-selenium-project.txt   (placeholder for future Selenium E2E)
```

---

## 4. Current state (what exists vs. what's missing)

### Backend (`src/api/SkyRoute`)
- ✅ Solution with 3 projects + matching unit-test projects.
- ✅ `Program.cs` configures Serilog, OpenTelemetry (traces/metrics/logs → OTLP),
  CORS for `http://localhost:4200`, controllers, OpenAPI.
- ⚠️ `FlightSearchController` and `BookingController` are **stubs returning `WeatherForecast`**
  — to be rewritten for the real domain.
- ⚠️ `BusinessLogic` and `DataAccessLayer` projects contain only an empty `Class1.cs`.
- ❌ No provider abstraction, no mocks for GlobalAir / BudgetWings, no pricing strategies, no booking persistence.

### Frontend (`src/web/sky-route`)
- ✅ Angular 21 standalone app, OTel logs initialized in `main.ts`.
- ✅ `BookingService` + `LoggerService` exist but `Booking` interface still mirrors the
  weather-forecast stub.
- ⚠️ `app.routes.ts` is empty — no routes wired yet.
- ❌ No search form, results list, sort controls, booking screen, route-dependent
  document validation, or airport catalog.

### Infra / tooling
- ✅ `docker-compose.yml` brings up dashboard + api + web.
- ✅ Aspire Dashboard at `http://localhost:18888` (OTLP gRPC `18889`, OTLP HTTP `18890`).
- ❌ README still skeletal — needs setup instructions and architecture notes (a deliverable).

---

## 5. How to run / build / test

### Backend (.NET)
```powershell
cd src/api/SkyRoute
dotnet restore
dotnet build
dotnet test                              # runs all xUnit projects
dotnet run --project src/SkyRoute.Api    # http://localhost:8080 (or launchSettings)
```

### Frontend (Angular)
```powershell
cd src/web/sky-route
npm install
npm start            # ng serve → http://localhost:4200
npm run build        # production build into dist/
npm test             # ng test (Vitest)
```

### Full stack via Docker
```powershell
docker compose up --build
# API:        http://localhost:8080
# Web:        http://localhost:4200
# Dashboard:  http://localhost:18888
```

---

## 6. Conventions & guidelines for agents

- **Language/style**
  - .NET: nullable on, file-scoped namespaces, primary constructors where natural,
    follow the repo `.editorconfig` (in `src/api/SkyRoute/.editorconfig`).
  - Angular: standalone components, `inject()` over constructor DI, signals for
    component state, Prettier (`.prettierrc`) for formatting.
- **Architecture**
  - Keep the API → BusinessLogic → DataAccessLayer separation. Controllers stay thin.
  - Model providers behind an interface (e.g. `IFlightProvider`) plus a pricing
    strategy per provider so adding a new airline is a new class, not edits to existing ones.
  - Money: use `decimal` end-to-end; round only at the boundary required by the provider rule.
- **Pricing**
  - Compute per-passenger price using provider rules, then total = per-passenger × passengers.
  - Always return both numbers in the API response so the UI doesn't recompute.
- **Domestic vs. international**
  - Determined by comparing the `country` of origin & destination airports.
  - Surface this flag in the search-result item so the booking screen can pick the right
    document label/validator without re-querying.
- **Testing**
  - Unit-test each provider's pricing rule (including BudgetWings `$29.99` floor).
  - Unit-test domestic/international detection and the document-validation switch.
- **Observability**
  - Don't strip the existing OTel/Serilog wiring; add spans/logs around new
    search and booking operations.
- **Out of scope (unless time permits)**
  - Real DB (in-memory store is fine), auth, payments, real airline APIs, cloud deploy.
  - Anything skipped must be called out in `README.md` under "Known limitations".

---

## 7. Suggested next steps (rough order)

1. Define domain types in `SkyRoute.BusinessLogic`: `Airport`, `FlightSearchRequest`,
   `FlightOffer`, `PassengerCount`, `CabinClass`, `Money`, `BookingRequest`, `BookingConfirmation`.
2. Introduce `IFlightProvider` + `GlobalAirProvider` / `BudgetWingsProvider` mocks
   (deterministic-ish fake data) and a `FlightSearchService` that fans out and aggregates.
3. Replace `FlightSearchController` and `BookingController` stubs with the real endpoints
   (`POST /api/flights/search`, `POST /api/bookings`) and DTOs.
4. Add an in-memory booking store in `SkyRoute.DataAccessLayer` returning a reference code.
5. Frontend: airport catalog, search form, results table with client-side sort, booking
   screen with route-aware document field, wire `BookingService` to the real API.
6. Tests for pricing rules, sorting helpers, document validators.
7. Flesh out the top-level `README.md` with run instructions, architecture decisions,
   and trade-offs (this is an explicit deliverable).
