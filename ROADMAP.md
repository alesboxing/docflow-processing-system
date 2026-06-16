# Roadmap — DocFlow Processing System

## MVP scope

- Domain model for document lifecycle.
- Upload document use case.
- File validation policy.
- Local file storage abstraction.
- EF Core persistence.
- PostgreSQL support.
- Fake document processor.
- Processing workflow.
- Failure handling.
- Retry workflow.
- Cancel workflow.
- History tracking.
- Download endpoint.
- JWT authentication.
- Operator/Admin roles.
- FluentValidation.
- Global exception handling.
- Docker Compose.
- Health checks.
- Structured logging.
- API integration tests.

## Next improvements

### Background processing

- Add Hangfire or Quartz.
- Replace manual processing endpoint with real background job execution.
- Keep `IBackgroundJobClient` as abstraction.

### Outbox pattern

- Add Outbox table.
- Save document and outbox message in one transaction.
- Dispatch jobs from outbox table.

### Real document processor

- Add real `.txt` extraction.
- Add basic `.docx` extraction.
- Add basic `.pdf` metadata extraction.
- Avoid paid OCR APIs in the next step.

### File security

- Magic number validation.
- Content sniffing.
- Antivirus scanning.
- Upload quarantine.
- File encryption at rest.

### Real identity

- Real users table.
- Password hashing.
- Refresh tokens.
- Token revocation.
- Document ownership.

### Observability

- OpenTelemetry.
- Distributed tracing.
- Metrics.
- Dashboards.
- Alerting.

## Not planned for current portfolio version

- Microservices.
- Kafka.
- RabbitMQ.
- Event sourcing.
- Full CQRS split.
- Complex BPM engine.
- Paid OCR APIs.
- Frontend-heavy UI.
