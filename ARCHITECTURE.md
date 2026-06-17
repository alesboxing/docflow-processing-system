# Architecture — DocFlow Processing System

## Goal

DocFlow is a backend workflow system for document processing.

It answers:

> What is happening with this document now, why is it in this status, and what should be done next?

## Architecture style

```text
DocFlow.Api -> DocFlow.Application -> DocFlow.Domain
DocFlow.Api -> DocFlow.Infrastructure
DocFlow.Infrastructure -> DocFlow.Application / DocFlow.Domain
DocFlow.Domain -> no project dependency
```

The domain layer does not depend on HTTP, EF Core, PostgreSQL, file system, JWT, Swagger or Docker.

## Layers

### Domain

Owns business rules:

- `Document` aggregate root;
- document lifecycle;
- status transition rules;
- failure and retry rules;
- cancel rules;
- history child records;
- domain errors and result pattern.

### Application

Coordinates use cases:

- upload;
- upload validation;
- download;
- processing;
- failure capture;
- retry;
- cancel;
- history query;
- pagination;
- DTO mapping.

### Infrastructure

Implements technical details:

- EF Core `AppDbContext`;
- PostgreSQL configuration;
- document repository;
- unit of work;
- local file storage;
- checksum service;
- fake document processor;
- date/time provider.

### API

Owns HTTP concerns:

- controllers;
- request binding;
- response mapping;
- JWT authentication;
- role authorization;
- Swagger;
- health checks.

## Document lifecycle

```text
Uploaded -> Queued -> Processing -> Processed
Uploaded -> Queued -> Processing -> Failed
Failed -> Queued
Uploaded -> Cancelled
Queued -> Cancelled
Failed -> Cancelled
```

Invalid transitions are rejected by domain methods.

Examples:

```text
Uploaded -> Processing      forbidden
Processed -> Cancelled      forbidden
Failed -> Processed         forbidden
```

## File validation flow

```text
POST /api/documents
-> validate file presence/name/type/size/extension
-> reject unsupported extension before persistence
-> return 400 BadRequest + File.UnsupportedExtension
```

CI-proven example:

```text
virus.exe -> 400 BadRequest -> File.UnsupportedExtension
```

## File storage and download flow

```text
POST /api/documents
-> IFileStorage.SaveAsync
-> persist document metadata
-> GET /api/documents/{id}/download
-> IFileStorage.OpenReadAsync
-> return original file content
```

## Processing flow

```text
POST /api/documents/{id}/process
-> load Document
-> MarkQueued / StartProcessing
-> open stored file
-> run IDocumentProcessor
-> MarkProcessed or MarkFailed
-> save changes
```

A processor exception becomes workflow state `Failed`. It is not treated as an untracked crash.

## History model

`DocumentProcessingHistory` is a child record owned by the `Document` aggregate.

History stores:

- previous status;
- target status;
- action;
- optional reason;
- UTC timestamp.

## API endpoints

```text
POST   /api/auth/login
GET    /api/auth/me
POST   /api/documents
GET    /api/documents
GET    /api/documents/{documentId}
GET    /api/documents/{documentId}/download
POST   /api/documents/{documentId}/process
POST   /api/documents/{documentId}/retry
POST   /api/documents/{documentId}/cancel
GET    /api/documents/{documentId}/history
GET    /health
```

## Testing architecture

Current CI-proven test state:

```text
Total tests: 13
Passed: 13
```

Covered areas:

- domain aggregate behavior;
- JWT authentication;
- protected endpoint authorization;
- valid upload;
- unsupported extension rejection;
- get by id;
- download original file;
- processing success;
- history;
- processing failure;
- retry;
- cancel.

## Design decisions

- DDD-lite, not full enterprise DDD.
- Manual processing endpoint instead of background queue.
- Fake processor for portfolio scope.
- Local file storage behind abstraction.
- Demo JWT authentication.

## Non-goals

Current version intentionally does not include microservices, Kafka, RabbitMQ, Outbox, event sourcing, real OCR, production identity, antivirus scanning, object storage or frontend.

## Summary

DocFlow demonstrates Clean Architecture, DDD-lite aggregate modeling, explicit document lifecycle, upload validation, file download, failure handling, retry, cancel, history, JWT-protected API, EF Core/PostgreSQL and CI-tested integration tests.
