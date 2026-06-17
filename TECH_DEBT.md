# Technical Debt — DocFlow Processing System

## Purpose

This file documents known trade-offs in the current portfolio version.

Current verified state:

```text
Build succeeded
Total tests: 13
Passed: 13
```

The items below are explicit limitations, not hidden defects.

## 1. Background processing

Processing is triggered manually:

```http
POST /api/documents/{documentId}/process
```

A real background queue is not implemented. This is acceptable because the demo focuses on lifecycle modeling, failure handling and retry.

Future options: `BackgroundService`, Hangfire, Quartz or RabbitMQ when distributed processing is actually needed.

## 2. Outbox pattern

There is no Outbox pattern yet.

This is acceptable while processing is manually triggered. If real async dispatch is added, use:

```text
Save Document + OutboxMessage
-> dispatcher reads outbox
-> dispatcher starts processing
-> message marked as dispatched
```

## 3. Authentication

The project uses demo JWT users:

| User | Password | Role |
|---|---|---|
| `operator` | `Operator123!` | `Operator` |
| `admin` | `Admin123!` | `Admin` |

Limitations:

- no user table;
- no password hashing;
- no refresh tokens;
- no token revocation;
- no external identity provider.

## 4. File validation and security

Current validation checks file presence, file name, content type, size and extension.

CI now proves unsupported extension rejection:

```text
virus.exe -> 400 BadRequest -> File.UnsupportedExtension
```

Remaining limitations:

- no magic number validation;
- no antivirus scan;
- no quarantine;
- no rate limiting;
- no per-user quota.

## 5. Storage

Files are stored locally behind `IFileStorage`.

CI proves:

```text
upload -> store -> download
```

Limitations:

- not suitable for multiple API instances;
- no object storage;
- no signed URLs;
- no retention policy;
- no cleanup job for orphaned files.

Future improvement: add S3, MinIO or Azure Blob adapter.

## 6. Document processor

The processor is fake/demo-oriented.

It proves workflow orchestration:

```text
Queued -> Processing -> Processed
Queued -> Processing -> Failed
```

Limitations:

- no real OCR;
- no real PDF parsing;
- no real DOCX parsing;
- no processing timeout policy.

Best next improvement: implement a small real `.txt` extractor before adding PDF/DOCX/OCR complexity.

## 7. Error response contract

Current shape:

```json
{
  "code": "Document.NotFound",
  "message": "Document was not found.",
  "type": "NotFound"
}
```

Limitations:

- no `traceId`;
- no `correlationId`;
- no validation dictionary;
- no `ProblemDetails` standardization.

## 8. EF Core InMemory tests

API tests use EF Core InMemory for speed and CI stability.

This is acceptable for HTTP pipeline and workflow coverage, but it is not equivalent to PostgreSQL.

Future improvement: add a few Testcontainers-based PostgreSQL tests for persistence-critical behavior.

## 9. EF tracking workaround

`EfUnitOfWork` contains history tracking normalization.

This is acceptable for the demo, but long-term it should be replaced by cleaner aggregate graph persistence behavior or relational integration tests.

## 10. Observability

Current observability is basic ASP.NET Core logging and `/health`.

Missing:

- structured logging policy;
- correlation id;
- tracing;
- metrics;
- dashboard.

## 11. Swagger

Swagger is enabled in Development, but JWT authorization UI is not fully configured.

Future improvement: add Bearer auth button and response examples.

## Remaining useful tests

- paged list test;
- retry conflict test;
- process not found test;
- download not found test;
- missing file upload test;
- file too large test;
- PostgreSQL Testcontainers test.

## Priority roadmap

```text
P1: Add paged list test.
P1: Add retry conflict test.
P1: Add not-found tests.
P2: Add Swagger JWT config.
P2: Add PostgreSQL Testcontainers test.
P3: Add real .txt extraction.
P3: Add background worker simulation.
```

## Final note

The technical debt is acceptable for a portfolio backend project.

The project should be presented as a focused workflow backend demo, not as a production document management platform.
