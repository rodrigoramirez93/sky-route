# AGENTS.md — SkyRoute Travel Platform

This file is the authoritative working brief for AI coding agents (Copilot,
Cursor, Claude, …) contributing to this repository. Read this **before**
making changes.

> Status: ✅ Functional. Backend (3-layer .NET) and frontend (two independent
> Angular feature modules) implement the full Flight Search & Booking flow.
> See `README.md` for run instructions and architecture summary.

---

## 1. What this project is

A Flight Search & Booking module for **SkyRoute**, a flight aggregator. The
domain is sourced from `docs/requirements.txt`.

- Two airlines, mocked: **GlobalAir** (base × 1.15) and **BudgetWings**
  (max(base × 0.90, $29.99)).
- Search → sortable results (client-side) → booking flow with route-aware
  document field (Passport for international, National ID for domestic).
- Backend exposes the search/book API; observability via Aspire Dashboard.

---

## 2. Architecture (non-negotiable)

### Backend — strict 3 layers

```
SkyRoute.Api  →  SkyRoute.BusinessLogic  →  SkyRoute.DataAccessLayer
```

- `Api` references `BusinessLogic` everywhere and `DataAccessLayer` **only**
  in its composition root (`Composition/SkyRouteServiceCollectionExtensions.cs`).
- `BusinessLogic` is pure C# — no ASP.NET, no EF, no IO directly. Talks to
  external systems exclusively through interfaces.
- `DataAccessLayer` implements those interfaces (airport catalog, in-memory
  booking store, mocked provider clients). It depends on `BusinessLogic` for
  contracts/DTOs.

### Strategy pattern — the operator-onboarding seam

`IFlightProviderStrategy` (in `BusinessLogic/Providers`) is the contract every
airline implements. `FlightSearchService` injects
`IEnumerable<IFlightProviderStrategy>` and aggregates results in parallel.
**Each operator owns its own pricing rule inside its strategy** — no shared
base class.

**Adding a new airline is additive only:**

1. New raw client (interface + mock) under `DataAccessLayer/Providers/<Name>`.
2. New `<Name>Provider : IFlightProviderStrategy` under
   `BusinessLogic/Providers/<Name>`.
3. Two `services.AddSingleton<...>` lines in `AddSkyRoute()`.

Do not modify any existing strategy when onboarding a new one.

### Frontend — two self-contained, exportable feature modules

```
src/app/
  shared/                                  ← DTOs + DI tokens both features share
  features/
    search/   index.ts (PUBLIC API only)
    book/     index.ts (PUBLIC API only)
```

Rules:

- Each feature module is **independently consumable by a different host
  application**. The only allowed import path for a host is the feature's
  `index.ts` barrel.
- **Features must not import from each other.** They only know about
  `../../shared`. Cross-feature interaction is host-mediated via injection
  tokens (`FLIGHT_SELECTION_HANDLER`, `BOOKING_CONFIRMED_HANDLER`) and the
  book feature's public `BookingService.setSelectedOffer`.
- Each feature exposes `provideXFeature(config)` returning
  `EnvironmentProviders` for the host to plug into `ApplicationConfig.providers`.
- Each feature exposes a `xFeatureRoutes: Routes` array for the host to lazy-load.

If you need to add cross-feature state, add it to `shared/` (a DTO or token).
**Do not** add an import from `features/book` into `features/search` or
vice versa.

#### Component file layout (mandatory)

Every Angular component lives in **its own folder** containing exactly four
co-located files:

```
<feature>/components/<component-name>/
  <component-name>.component.ts
  <component-name>.component.html
  <component-name>.component.css
  <component-name>.component.spec.ts
```

- **No inline `styles: [...]`** in `@Component` metadata — always reference an
  external stylesheet via `styleUrls: ['./<component-name>.component.css']`
  (or `styleUrl` for the root `App`).
- **No inline `template`** — always use `templateUrl`.
- The root `App` (`src/app/app.{ts,html,css,spec.ts}`) is the only allowed
  exception to the folder-per-component rule and already satisfies the
  external-template/styles rule.
- Apply these conventions to every new component and when touching existing
  ones.

---

## 3. Code style

- **C# `.editorconfig` is immutable.** File-scoped namespaces, `_camelCase`
  private fields, `var` when type is apparent, CRLF, 4-space indent in `.cs`,
  2-space in props/json/yml.
- `Directory.Build.props` enables `Nullable`, `ImplicitUsings`,
  `TreatWarningsAsErrors`, `LangVersion=latest` for all production projects.
  Test projects opt out of `TreatWarningsAsErrors`.
- Frontend: Angular 21 idioms — standalone components, `inject()`, signals,
  `input()`/`output()`, OnPush change detection. Prettier (`.prettierrc`) is
  the formatter.

---

## 4. Repository layout

```
sky-route/
├── AGENTS.md, CLAUDE.md, README.md, LICENSE
├── docker-compose.yml                  ← aspire-dashboard + api + web
├── docs/requirements.txt               ← source of truth for the challenge
├── scripts/                            ← convenience scripts (dev/test-all)
├── src/
│   ├── api/SkyRoute/                   ← .NET solution
│   │   ├── .editorconfig               ← DO NOT TOUCH
│   │   ├── Directory.Packages.props    (central package versions)
│   │   ├── Directory.Build.props       (nullable, warnings-as-errors, …)
│   │   ├── src/
│   │   │   ├── SkyRoute.Api/           (Controllers, Contracts, Composition)
│   │   │   ├── SkyRoute.BusinessLogic/ (Domain, Providers, Search, Booking, Documents)
│   │   │   └── SkyRoute.DataAccessLayer/ (Airports, Booking, Providers/<Name>)
│   │   └── test/                       (xUnit + Moq, one project per layer)
│   └── web/sky-route/                  ← Angular 21 app
│       ├── src/
│       │   ├── app/shared/             (contracts shared between features)
│       │   ├── app/features/search/    (self-contained module + index.ts)
│       │   ├── app/features/book/      (self-contained module + index.ts)
│       │   ├── app/app.{ts,html,css,routes.ts,config.ts}
│       │   ├── environments/           (apiUrl, otlpEndpoint, serviceName)
│       │   ├── main.ts                 (bootstraps + initTelemetry)
│       │   └── telemetry.ts            (OTLP/HTTP logs → aspire-dashboard:18890)
│       └── package.json
└── test/
    └── black-box-e2e-selenium-project.txt   (placeholder for future E2E)
```

---

## 5. How to run / build / test

### Backend

```powershell
cd src/api/SkyRoute
dotnet restore
dotnet build
dotnet test                              # 39 tests, all green
dotnet run --project src/SkyRoute.Api    # http://localhost:8080
```

### Frontend

```powershell
cd src/web/sky-route
npm install
npm start            # ng serve → http://localhost:4200
npm run build        # production bundle into dist/
npm test             # Vitest in run mode, 20 tests
```

### Full stack via Docker

```powershell
docker compose up --build
# Web: 4200 · API: 8080 · Dashboard: 18888
```

---

## 6. Working guidelines for agents

- **Do not modify `src/api/SkyRoute/.editorconfig`.** Off-limits per the brief.
- **Do not introduce cross-feature imports** between `features/search` and
  `features/book`. Use `shared/` or host-provided tokens.
- **Do not modify existing provider strategies** when onboarding a new
  airline — extend, don't edit.
- Keep money in `decimal`; round only via `PriceCalculator.Round2` (away
  from zero) — matches the GlobalAir and BudgetWings tests.
- Surface domestic/international in `FlightOffer.IsInternational` (derived
  in the strategies from airport `Country`); the booking screen reuses it to
  pick the document validator.
- Add unit tests for any new strategy (pricing math + edge cases like the
  $29.99 floor) and any new validator.
- Don't strip the OpenTelemetry / Serilog wiring. Add spans/logs around new
  search or booking operations using `ActivitySource("SkyRoute.BusinessLogic")`.

## 7. Out of scope (unless explicitly asked)

- Real DB, auth, payments, real airline APIs, cloud deploy, i18n.
- Anything skipped due to time stays in README "Known limitations".
