import { DestroyRef, Injectable, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, NavigationStart, Router } from '@angular/router';
import { Counter, Histogram } from '@opentelemetry/api';
import { onCLS, onFCP, onINP, onLCP, onTTFB, type Metric } from 'web-vitals';
import { getMeter } from '../telemetry';

type Attrs = Record<string, string | number | boolean>;

/**
 * Owns every browser-side OpenTelemetry metric instrument and the wiring
 * (Router events, PerformanceNavigationTiming, web-vitals) that feeds them.
 * Tag cardinality is intentionally tight: only route templates, HTTP method,
 * status class, and web-vitals "rating" reach the exporter — never raw URLs,
 * session ids, or user identifiers.
 */
@Injectable({ providedIn: 'root' })
export class MetricsService {
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly meter = getMeter('sky-route.web');

  private readonly pageViews: Counter = this.meter.createCounter('web.page_views', {
    description: 'SPA page views by route template.',
    unit: '{view}',
  });
  private readonly navigationDuration: Histogram = this.meter.createHistogram(
    'web.route_navigation.duration',
    { description: 'Time between NavigationStart and NavigationEnd.', unit: 'ms' },
  );
  private readonly documentLoad: Histogram = this.meter.createHistogram('web.document.load', {
    description: 'Initial HTML document load duration (PerformanceNavigationTiming).',
    unit: 'ms',
  });
  private readonly apiDuration: Histogram = this.meter.createHistogram('web.api.duration', {
    description: 'Browser-observed duration of API calls (includes network).',
    unit: 'ms',
  });
  private readonly apiErrors: Counter = this.meter.createCounter('web.api.errors', {
    description: 'API responses with status >= 400 or transport failures.',
    unit: '{error}',
  });

  private readonly vitals: Record<string, Histogram> = {
    LCP: this.meter.createHistogram('web.vitals.lcp', { unit: 'ms', description: 'Largest Contentful Paint.' }),
    CLS: this.meter.createHistogram('web.vitals.cls', { unit: '1', description: 'Cumulative Layout Shift.' }),
    INP: this.meter.createHistogram('web.vitals.inp', { unit: 'ms', description: 'Interaction to Next Paint.' }),
    FCP: this.meter.createHistogram('web.vitals.fcp', { unit: 'ms', description: 'First Contentful Paint.' }),
    TTFB: this.meter.createHistogram('web.vitals.ttfb', { unit: 'ms', description: 'Time to First Byte.' }),
  };

  private navigationStartedAt: number | null = null;

  start(): void {
    this.observeNavigation();
    this.observeDocumentLoad();
    this.observeWebVitals();
  }

  recordApiCall(method: string, route: string, statusCode: number, durationMs: number): void {
    const attrs: Attrs = {
      'http.method': method.toUpperCase(),
      'http.route': route,
      'http.status_class': statusClass(statusCode),
    };
    this.apiDuration.record(durationMs, attrs);
    if (statusCode === 0 || statusCode >= 400) {
      this.apiErrors.add(1, attrs);
    }
  }

  private observeNavigation(): void {
    this.router.events.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((event) => {
      if (event instanceof NavigationStart) {
        this.navigationStartedAt = performance.now();
      } else if (event instanceof NavigationEnd) {
        const route = routeTemplateFor(this.router, event.urlAfterRedirects);
        this.pageViews.add(1, { 'http.route': route });
        if (this.navigationStartedAt !== null) {
          this.navigationDuration.record(performance.now() - this.navigationStartedAt, {
            'http.route': route,
          });
          this.navigationStartedAt = null;
        }
      }
    });
  }

  private observeDocumentLoad(): void {
    if (typeof performance === 'undefined' || !('getEntriesByType' in performance)) {
      return;
    }
    const record = () => {
      const [nav] = performance.getEntriesByType('navigation') as PerformanceNavigationTiming[];
      if (nav && nav.loadEventEnd > 0) {
        this.documentLoad.record(nav.loadEventEnd - nav.startTime);
      }
    };
    if (document.readyState === 'complete') {
      record();
    } else {
      window.addEventListener('load', () => setTimeout(record, 0), { once: true });
    }
  }

  private observeWebVitals(): void {
    const report = (metric: Metric) => {
      const histogram = this.vitals[metric.name];
      if (!histogram) return;
      histogram.record(metric.value, { 'metric.rating': metric.rating });
    };
    onLCP(report);
    onCLS(report);
    onINP(report);
    onFCP(report);
    onTTFB(report);
  }
}

function statusClass(status: number): string {
  if (status === 0) return 'transport_error';
  if (status >= 500) return '5xx';
  if (status >= 400) return '4xx';
  if (status >= 300) return '3xx';
  if (status >= 200) return '2xx';
  return 'unknown';
}

function routeTemplateFor(router: Router, url: string): string {
  // Walk the activated router tree and concatenate configured path segments
  // (which are templates like ":offerId") so cardinality stays bounded.
  const segments: string[] = [];
  let route = router.routerState.snapshot.root;
  while (route.firstChild) {
    route = route.firstChild;
    const path = route.routeConfig?.path;
    if (path) segments.push(path);
  }
  const template = '/' + segments.filter(Boolean).join('/');
  return template === '/' ? url.split('?')[0] || '/' : template;
}
