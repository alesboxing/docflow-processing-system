# Technical Debt — DocFlow Processing System

## Background processing

Current planned MVP processing can be manually triggered.

Future improvement:

- add Hangfire or Quartz;
- process queued documents automatically;
- add retry policies for background jobs.

## Outbox

Current simple flow:

```text
Save Document
-> Enqueue processing job
```

This is not fully reliable if enqueue fails after database save.

Future improvement:

- implement Outbox pattern;
- save outbox message in the same transaction;
- dispatch jobs from outbox table.

## Authentication

Current planned authentication is demo JWT auth.

Limitations:

- users can be configured in appsettings;
- no real user persistence initially;
- no refresh tokens initially;
- no token revocation initially;
- no fine-grained permissions initially.

Future improvement:

- real user table;
- password hashing;
- refresh tokens;
- permission model;
- document ownership.

## File security

Initial validation checks:

- extension;
- content type;
- file size.

Future improvement:

- content sniffing;
- magic number validation;
- antivirus integration;
- upload quarantine;
- secure download policy.

## Storage

Initial storage target is local disk.

Limitations:

- not suitable for distributed deployment;
- no cloud storage;
- no lifecycle policy.

Future improvement:

- S3 adapter;
- Azure Blob adapter;
- signed download URLs.

## OCR / document processing

Initial processor can be fake to keep the workflow testable.

Future improvement:

- real `.txt` extraction;
- `.docx` parser;
- `.pdf` metadata extraction;
- optional OCR adapter later.

## Observability

Initial observability target:

- Serilog console logs;
- correlation id;
- health checks.

Future improvement:

- OpenTelemetry;
- centralized logs;
- metrics;
- dashboards.

## Deployment

Initial Docker Compose setup is for local demo only.

Limitations:

- no reverse proxy;
- no HTTPS production setup;
- no production secrets;
- no backup strategy;
- no CI/CD deployment pipeline.
