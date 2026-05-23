import { Injectable } from '@angular/core';
import { SpanStatusCode, context, trace } from '@opentelemetry/api';
import { getTracer, getSessionId } from '../telemetry';

export type SpanAttributes = Record<string, string | number | boolean | undefined>;

/**
 * Thin convenience wrapper around the OpenTelemetry Web tracer.
 * Use {@link withSpan} for async business operations so the span is closed
 * even on errors and trace-context flows through nested HTTP calls.
 */
@Injectable({ providedIn: 'root' })
export class TelemetryService {
  readonly sessionId = getSessionId();

  startSpan(name: string, attributes?: SpanAttributes) {
    const span = getTracer().startSpan(name, {
      attributes: { 'session.id': this.sessionId, ...sanitize(attributes) },
    });
    return span;
  }

  async withSpan<T>(
    name: string,
    attributes: SpanAttributes,
    fn: () => Promise<T>,
  ): Promise<T> {
    const span = this.startSpan(name, attributes);
    return await context.with(trace.setSpan(context.active(), span), async () => {
      try {
        const result = await fn();
        span.setStatus({ code: SpanStatusCode.OK });
        return result;
      } catch (error) {
        span.setStatus({
          code: SpanStatusCode.ERROR,
          message: error instanceof Error ? error.message : String(error),
        });
        if (error instanceof Error) {
          span.recordException(error);
        }
        throw error;
      } finally {
        span.end();
      }
    });
  }
}

function sanitize(attributes?: SpanAttributes): Record<string, string | number | boolean> {
  if (!attributes) return {};
  const result: Record<string, string | number | boolean> = {};
  for (const [key, value] of Object.entries(attributes)) {
    if (value !== undefined && value !== null) {
      result[key] = value;
    }
  }
  return result;
}
