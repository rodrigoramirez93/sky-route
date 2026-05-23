import { Injectable } from '@angular/core';
import { Logger } from '@opentelemetry/api-logs';
import { environment } from '../environments/environment';
import {
  getActiveTraceContext,
  getLogger,
  getSessionId,
  SeverityNumber,
} from '../telemetry';

export type LogAttributes = Record<string, string | number | boolean | undefined>;

/**
 * Structured logger that emits to the OTel logs pipeline. Every record is
 * automatically enriched with the current trace/span ids and the per-tab
 * session id so the Aspire dashboard can group an entire customer journey.
 * Mirroring to the browser console is gated on environment.mirrorLogsToConsole.
 */
@Injectable({ providedIn: 'root' })
export class LoggerService {
  private readonly otelLogger: Logger = getLogger('sky-route.web');
  private readonly sessionId = getSessionId();

  info(message: string, scope: string, attributes?: LogAttributes): void {
    this.emit(SeverityNumber.INFO, 'INFO', message, scope, attributes);
    if (environment.mirrorLogsToConsole) console.log(message, attributes ?? '');
  }

  warn(message: string, scope: string, attributes?: LogAttributes): void {
    this.emit(SeverityNumber.WARN, 'WARN', message, scope, attributes);
    if (environment.mirrorLogsToConsole) console.warn(message, attributes ?? '');
  }

  error(message: string, scope: string, attributes?: LogAttributes): void {
    this.emit(SeverityNumber.ERROR, 'ERROR', message, scope, attributes);
    if (environment.mirrorLogsToConsole) console.error(message, attributes ?? '');
  }

  private emit(
    severityNumber: SeverityNumber,
    severityText: string,
    message: string,
    scope: string,
    attributes?: LogAttributes,
  ): void {
    const trace = getActiveTraceContext();
    const enriched: Record<string, string | number | boolean> = {
      'session.id': this.sessionId,
      'code.namespace': scope,
    };
    if (trace.traceId) enriched['trace_id'] = trace.traceId;
    if (trace.spanId) enriched['span_id'] = trace.spanId;
    if (attributes) {
      for (const [key, value] of Object.entries(attributes)) {
        if (value !== undefined && value !== null) {
          enriched[key] = value as string | number | boolean;
        }
      }
    }

    this.otelLogger.emit({
      severityNumber,
      severityText,
      body: message,
      attributes: enriched,
    });
  }
}
