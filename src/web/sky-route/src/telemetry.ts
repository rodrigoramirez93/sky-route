import { context, trace, propagation, metrics, Tracer, Meter, Span, SpanStatusCode } from '@opentelemetry/api';
import { logs, SeverityNumber, Logger } from '@opentelemetry/api-logs';
import { LoggerProvider, BatchLogRecordProcessor } from '@opentelemetry/sdk-logs';
import { OTLPLogExporter } from '@opentelemetry/exporter-logs-otlp-http';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import {
  OTLPMetricExporter,
  AggregationTemporalityPreference,
} from '@opentelemetry/exporter-metrics-otlp-http';
import {
  MeterProvider,
  PeriodicExportingMetricReader,
} from '@opentelemetry/sdk-metrics';
import {
  WebTracerProvider,
  BatchSpanProcessor,
} from '@opentelemetry/sdk-trace-web';
import { ZoneContextManager } from '@opentelemetry/context-zone';
import { W3CTraceContextPropagator } from '@opentelemetry/core';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch';
import { XMLHttpRequestInstrumentation } from '@opentelemetry/instrumentation-xml-http-request';
import { DocumentLoadInstrumentation } from '@opentelemetry/instrumentation-document-load';
import { UserInteractionInstrumentation } from '@opentelemetry/instrumentation-user-interaction';
import { resourceFromAttributes } from '@opentelemetry/resources';
import {
  ATTR_SERVICE_NAME,
  ATTR_SERVICE_VERSION,
} from '@opentelemetry/semantic-conventions';
import { environment } from './environments/environment';

const TRACER_NAME = 'sky-route.web';

let initialized = false;
let sessionId = '';

function generateId(): string {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
    return crypto.randomUUID().replace(/-/g, '');
  }
  return Math.random().toString(16).slice(2) + Math.random().toString(16).slice(2);
}

/**
 * Returns the per-tab session id used to correlate every browser-emitted
 * span/log/HTTP request for a single user session.
 */
export function getSessionId(): string {
  if (!sessionId) {
    sessionId = generateId();
  }
  return sessionId;
}

export function initTelemetry(): void {
  if (initialized) {
    return;
  }
  initialized = true;

  const resource = resourceFromAttributes({
    [ATTR_SERVICE_NAME]: environment.serviceName,
    [ATTR_SERVICE_VERSION]: environment.serviceVersion,
    'deployment.environment': environment.deploymentEnvironment,
    'session.id': getSessionId(),
  });

  // ----- Logs -----
  const loggerProvider = new LoggerProvider({
    resource,
    processors: [
      new BatchLogRecordProcessor(
        new OTLPLogExporter({ url: `${environment.otlpEndpoint}/v1/logs` }),
      ),
    ],
  });
  logs.setGlobalLoggerProvider(loggerProvider);

  // ----- Metrics -----
  // Browser-friendly 60s export interval; DELTA temporality lines up with how
  // the Aspire Dashboard ingests OTLP metrics over HTTP.
  const meterProvider = new MeterProvider({
    resource,
    readers: [
      new PeriodicExportingMetricReader({
        exporter: new OTLPMetricExporter({
          url: `${environment.otlpEndpoint}/v1/metrics`,
          temporalityPreference: AggregationTemporalityPreference.DELTA,
        }),
        exportIntervalMillis: 60_000,
      }),
    ],
  });
  metrics.setGlobalMeterProvider(meterProvider);

  // ----- Traces -----
  const tracerProvider = new WebTracerProvider({
    resource,
    spanProcessors: [
      new BatchSpanProcessor(
        new OTLPTraceExporter({ url: `${environment.otlpEndpoint}/v1/traces` }),
      ),
    ],
  });
  tracerProvider.register({
    contextManager: new ZoneContextManager(),
    propagator: new W3CTraceContextPropagator(),
  });

  // The propagateTraceHeaderCorsUrls regex is critical: without it the
  // fetch/xhr instrumentations will NOT inject `traceparent` on cross-origin
  // calls, breaking continuity with the .NET API.
  const apiUrlPattern = new RegExp(escapeRegex(environment.apiUrl));
  registerInstrumentations({
    instrumentations: [
      new FetchInstrumentation({
        propagateTraceHeaderCorsUrls: [apiUrlPattern],
        clearTimingResources: true,
      }),
      new XMLHttpRequestInstrumentation({
        propagateTraceHeaderCorsUrls: [apiUrlPattern],
      }),
      new DocumentLoadInstrumentation(),
      new UserInteractionInstrumentation({
        eventNames: ['click', 'submit'],
      }),
    ],
  });
}

function escapeRegex(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

export function getTracer(): Tracer {
  return trace.getTracer(TRACER_NAME, environment.serviceVersion);
}

export function getLogger(name = TRACER_NAME): Logger {
  return logs.getLogger(name, environment.serviceVersion);
}

export function getMeter(name = TRACER_NAME): Meter {
  return metrics.getMeter(name, environment.serviceVersion);
}

export function getActiveTraceContext(): { traceId?: string; spanId?: string } {
  const span = trace.getActiveSpan();
  if (!span) return {};
  const ctx = span.spanContext();
  return { traceId: ctx.traceId, spanId: ctx.spanId };
}

export function getPropagationHeaders(): Record<string, string> {
  const carrier: Record<string, string> = {};
  propagation.inject(context.active(), carrier);
  return carrier;
}

export { SeverityNumber, SpanStatusCode, trace, context };
export type { Span };
