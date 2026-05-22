import { logs, SeverityNumber, Logger } from '@opentelemetry/api-logs';
import {
  LoggerProvider,
  BatchLogRecordProcessor,
} from '@opentelemetry/sdk-logs';
import { OTLPLogExporter } from '@opentelemetry/exporter-logs-otlp-http';
import { resourceFromAttributes } from '@opentelemetry/resources';
import { ATTR_SERVICE_NAME } from '@opentelemetry/semantic-conventions';
import { environment } from './environments/environment';

let initialized = false;

export function initTelemetry(): void {
  if (initialized) {
    return;
  }
  initialized = true;

  const exporter = new OTLPLogExporter({
    url: `${environment.otlpEndpoint}/v1/logs`,
  });

  const loggerProvider = new LoggerProvider({
    resource: resourceFromAttributes({
      [ATTR_SERVICE_NAME]: environment.serviceName,
    }),
    processors: [new BatchLogRecordProcessor(exporter)],
  });

  logs.setGlobalLoggerProvider(loggerProvider);
}

export function getLogger(name = 'sky-route'): Logger {
  return logs.getLogger(name);
}

export { SeverityNumber };
