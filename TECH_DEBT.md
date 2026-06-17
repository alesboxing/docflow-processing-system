# Technical Debt — DocFlow Processing System

## Purpose

This file documents known trade-offs and intentional simplifications in the current portfolio version of DocFlow Processing System.

The project is currently strong enough for a backend portfolio demo:

```text
Build succeeded
Total tests: 10
Passed: 10
```

The items below are not hidden defects. They are explicit boundaries of the current version.

## 1. Background processing

### Current state

Processing is currently triggered manually through:

```http
POST /api/documents/{documentId}/process
```

The application has an `IBackgroundJobClient` abstraction, but the current implementation is a no-op/demo implementation.

### Why this is acceptable now

The project goal is to demonstrate document lifecycle modeling, state transitions, failure handling and retry logic.

A real queue would add operational complexity without improving the core portfolio story at this stage.

### Future improvement

Possible options:

- add `BackgroundService` for a simple internal worker;
- add Hangfire for persistent jobs;
- add Quartz for scheduled polling;
- add RabbitMQ only if the system becomes distributed;
- add retry policies for failed jobs;
- expose job status separately from document status.

## 2. Outbox pattern

### Current state

Current simplified flow:

```text
Save document
-> enqueue processing request through abstraction
```

This is acceptable for the demo, but it is not fully reliable in production if enqueueing fails after database save.

### Risk

A document can be saved as queued while the job dispatch step fails.

### Future improvement

Implement an Outbox pattern:

```text
Save Document + OutboxMessage in one transaction
-> background dispatcher reads outbox table
-> dispatcher publishes/processes job
-> message marked as dispatched
```

This should be implemented only when real background execution is added.

## 3. Authentication and user management

### Current state

The project uses demo JWT authentication.

Demo users:

| User | Password | Role |
|---|---|---|
| `operator` | `Operator123!` | `Operator` |
| `admin` | `Admin123!` | `Admin` |

### Current limitations

- no user table;
- no password hashing;
- no refresh tokens;
- no token revocation;
- no document ownership;
- no fine-grained permissions;
- demo credentials are fixed in code.

### Why this is acceptable now

The current goal is to demonstrate protected API workflows, not production identity management.

### Future improvement

- add persisted users;
- hash passwords;
- add refresh tokens;
- add token revocation;
- add document ownership;
- add role/permission matrix;
- move secrets and signing keys to configuration or secret storage.

## 4. JWT signing key

### Current state

The JWT signing key is simplified for local demo usage.

### Risk

Hardcoded/demo signing configuration is not production-ready.

### Future improvement

- read signing key from environment variables;
- validate key length at startup;
- use secret manager in deployed environments;
- rotate signing keys;
- support separate issuer/audience per environment.

## 5. File validation and file security

### Current state

The current validation checks:

- file presence;
- original file name;
- content type;
- file size;
- allowed extension;
- max size of 10 MB.

Allowed extensions:

```text
.pdf
.docx
.txt
```

Allowed content types:

```text
application/pdf
text/plain
```

`.docx` is accepted by extension in the current validation policy.

### Current limitations

- no magic number validation;
- no file signature detection;
- no antivirus scan;
- no quarantine folder;
- no content disarm and reconstruction;
- no rate limiting;
- no per-user storage quota.

### Future improvement

- validate file signatures;
- add MIME sniffing;
- add antivirus integration;
- add upload quarantine;
- reject suspicious file names;
- add per-user upload limits;
- add secure download authorization policy.

## 6. Storage

### Current state

Files are stored locally behind `IFileStorage`.

This keeps infrastructure simple and replaceable.

### Current limitations

- not suitable for multiple API instances;
- no object storage;
- no retention policy;
- no lifecycle policy;
- no signed URLs;
- no storage-level encryption configuration.

### Future improvement

- add S3 adapter;
- add MinIO adapter for local object storage;
- add Azure Blob adapter;
- add signed download URLs;
- add retention policy;
- add cleanup job for orphaned files.

## 7. Document processor

### Current state

The processor is fake/demo-oriented.

It exists to prove workflow orchestration:

```text
Queued -> Processing -> Processed
Queued -> Processing -> Failed
```

### Current limitations

- no real OCR;
- no real PDF parsing;
- no real DOCX parsing;
- no text extraction quality checks;
- no processing timeout policy;
- no processor-specific error taxonomy.

### Future improvement

Suggested order:

1. Implement real `.txt` extraction first.
2. Add small PDF metadata extraction.
3. Add DOCX parser.
4. Add OCR adapter only after the workflow is stable.
5. Add processor-specific error codes.
6. Add timeout and cancellation handling tests.

## 8. Error response contract

### Current state

Current error response shape:

```json
{
  "code": "Document.NotFound",
  "message": "Document was not found.",
  "type": "NotFound"
}
```

### Current limitations

- no `traceId` in error response;
- no `correlationId` in error response;
- no validation error dictionary;
- no global exception handler response model;
- no RFC 7807 `ProblemDetails` standardization.

### Future improvement

- add global exception handling middleware;
- add `traceId`;
- add `correlationId`;
- standardize validation responses;
- consider `ProblemDetails`;
- add tests for error responses.

## 9. EF Core InMemory in API tests

### Current state

API integration tests use EF Core InMemory for speed and simplicity.

### Why this is acceptable now

The tests validate the HTTP pipeline, authentication, controllers, application services, domain transitions and persistence calls quickly in CI.

### Current limitation

EF Core InMemory is not a relational database. It does not fully behave like PostgreSQL.

### Future improvement

Add a small number of PostgreSQL-backed integration tests using Testcontainers.

Recommended scope:

- repository save/load with history;
- processing transition persistence;
- retry transition persistence;
- migration startup check.

Do not replace all fast API tests with Testcontainers. Use both.

## 10. EF tracking workaround in Unit of Work

### Current state

`EfUnitOfWork` contains a normalization step for `DocumentProcessingHistory` tracking before saving changes.

This was added because EF InMemory could treat newly created child history records as `Modified` instead of `Added` in the test workflow.

### Risk

The Unit of Work now knows about a domain child entity type.

That is acceptable for the demo because it keeps CI stable, but it is not the cleanest long-term design.

### Future improvement

Preferred cleanup options:

1. Move history tracking configuration fully into EF mapping.
2. Replace EF InMemory with SQLite/Testcontainers for relevant persistence tests.
3. Add repository-level attach/update behavior for aggregate graphs.
4. Remove domain-specific tracking normalization from `EfUnitOfWork` after persistence tests prove the replacement.

## 11. Observability

### Current state

The project relies on default ASP.NET Core logging and health checks.

### Current limitations

- no structured logging policy;
- no correlation id middleware;
- no tracing;
- no metrics;
- no dashboards;
- no processing failure analytics.

### Future improvement

- add Serilog or structured Microsoft logging configuration;
- add correlation id middleware;
- add OpenTelemetry tracing;
- expose metrics;
- add failure counters by content type/status;
- add dashboard in later production-like version.

## 12. Health checks

### Current state

The API exposes:

```http
GET /health
```

### Current limitations

- no separate liveness endpoint;
- no separate readiness endpoint;
- no detailed database readiness response;
- no storage health check.

### Future improvement

- add `/health/live`;
- add `/health/ready`;
- include database readiness;
- include storage path readiness;
- keep public health output minimal.

## 13. API documentation / Swagger

### Current state

Swagger is enabled in Development.

### Current limitations

- Swagger JWT authorization is not fully configured;
- no example request/response annotations;
- no OpenAPI descriptions for lifecycle behavior.

### Future improvement

- add Swagger Bearer authentication button;
- add XML comments or endpoint descriptions;
- document response codes;
- add examples for upload, process, retry and history.

## 14. Missing useful tests

The current 10 tests are enough for the portfolio version, but these tests would improve confidence.

Recommended next tests:

1. `Cancel_ShouldReturn200_WhenDocumentUploaded`
2. `Download_ShouldReturnOriginalFile_WhenDocumentExists`
3. `Upload_ShouldReturn400_WhenExtensionUnsupported`
4. `GetDocuments_ShouldReturnPagedList`
5. `Retry_ShouldReturn409_WhenDocumentIsNotFailed`
6. `Process_ShouldReturn404_WhenDocumentNotFound`
7. `Upload_ShouldReturn400_WhenFileMissing`
8. `Upload_ShouldReturn400_WhenFileTooLarge`

## 15. Deployment

### Current state

Docker Compose is available for local demo usage.

### Current limitations

- no production Docker hardening;
- no HTTPS reverse proxy;
- no production secrets;
- no backup strategy;
- no deployment pipeline;
- no infrastructure-as-code.

### Future improvement

- add production Dockerfile hardening;
- add reverse proxy example;
- move secrets to environment/secret manager;
- add backup/restore notes;
- add CI/CD deployment only after the portfolio version is stable.

## Priority roadmap

Best next steps if the project continues:

```text
P1: Add cancel endpoint test.
P1: Add download endpoint test.
P1: Add unsupported extension test.
P2: Add Swagger JWT config.
P2: Add PostgreSQL Testcontainers persistence test.
P2: Add structured processing failure logs.
P3: Replace fake .txt processor with simple real .txt extraction.
P3: Add background worker simulation.
```

## Final note

The current technical debt is acceptable for a portfolio backend project.

The project should be presented as:

```text
A focused workflow backend demo with Clean Architecture, DDD-lite aggregate modeling,
JWT-protected API, EF Core persistence, failure handling, retry, history and CI-tested integration tests.
```

It should not be presented as a production document management platform.
