import { Injectable } from '@angular/core';
import { Logger } from '@opentelemetry/api-logs';
import { getLogger, SeverityNumber } from '../telemetry';

@Injectable({ providedIn: 'root' })
export class LoggerService {
  private readonly otelLogger: Logger = getLogger('sky-route.web');

  info(message: string, attributes?: Record<string, unknown>): void {
    this.emit(SeverityNumber.INFO, 'INFO', message, attributes);
    console.log(message, attributes ?? '');
  }

  warn(message: string, attributes?: Record<string, unknown>): void {
    this.emit(SeverityNumber.WARN, 'WARN', message, attributes);
    console.warn(message, attributes ?? '');
  }

  error(message: string, attributes?: Record<string, unknown>): void {
    this.emit(SeverityNumber.ERROR, 'ERROR', message, attributes);
    console.error(message, attributes ?? '');
  }

  private emit(
    severityNumber: SeverityNumber,
    severityText: string,
    message: string,
    attributes?: Record<string, unknown>,
  ): void {
    this.otelLogger.emit({
      severityNumber,
      severityText,
      body: message,
      attributes: attributes as Record<string, string | number | boolean> | undefined,
    });
  }
}
