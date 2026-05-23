import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { tap } from 'rxjs';
import { HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { TelemetryService } from './telemetry.service';
import { MetricsService } from './metrics.service';

const SESSION_HEADER = 'X-Session-Id';
const CORRELATION_HEADER = 'X-Correlation-Id';

function newCorrelationId(): string {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
    return crypto.randomUUID();
  }
  return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

/**
 * Attaches per-tab session id and a fresh correlation id to every outgoing
 * HTTP request. The backend echoes the correlation id back via the same
 * response header and stamps it on its server-side spans/logs, giving us a
 * single key that joins browser + API for one user action.
 *
 * Also records the duration / outcome of every HTTP call into the
 * `web.api.duration` and `web.api.errors` OpenTelemetry instruments. The
 * route label uses the URL path with numeric/UUID segments masked, keeping
 * cardinality bounded.
 */
export const correlationInterceptor: HttpInterceptorFn = (req, next) => {
  const telemetry = inject(TelemetryService);
  const metrics = inject(MetricsService);
  const headers: Record<string, string> = {
    [SESSION_HEADER]: telemetry.sessionId,
  };
  if (!req.headers.has(CORRELATION_HEADER)) {
    headers[CORRELATION_HEADER] = newCorrelationId();
  }
  const cloned = req.clone({ setHeaders: headers });
  const startedAt = performance.now();
  const route = logicalRoute(req.url);

  return next(cloned).pipe(
    tap({
      next: (event) => {
        if (event instanceof HttpResponse) {
          metrics.recordApiCall(req.method, route, event.status, performance.now() - startedAt);
        }
      },
      error: (err) => {
        const status = err instanceof HttpErrorResponse ? err.status : 0;
        metrics.recordApiCall(req.method, route, status, performance.now() - startedAt);
      },
    }),
  );
};

function logicalRoute(url: string): string {
  try {
    const path = new URL(url, 'http://placeholder').pathname;
    return path
      .split('/')
      .map((segment) => {
        if (!segment) return segment;
        if (/^\d+$/.test(segment)) return ':id';
        if (/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(segment)) {
          return ':uuid';
        }
        return segment;
      })
      .join('/');
  } catch {
    return url;
  }
}
