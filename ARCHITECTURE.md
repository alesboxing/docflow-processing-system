# Architecture — DocFlow Processing System

## Goal

DocFlow Processing System is a backend workflow system for document processing.

The central architectural question is:

> What is happening with this document now, why is it in this status, and what should be done next?

The project is intentionally built around document lifecycle rules rather than simple CRUD.

## Architecture style

The solution follows a Clean Architecture / DDD-lite structure:

```text
DocFlow.Domain
DocFlow.Application
DocFlow.Infrastructure
DocFlow.Api
```

The core idea is simple:

```text
Business rules live in Domain.
Use cases live in Application.
Technical details live in Infrastructure.
HTTP concerns live in API.
```

## Dependency rule

Allowed dependencies:

```text
DocFlow.Api -> DocFlow.Application
DocFlow.Api -> DocFlow.Infrastructure
DocFlow.Api -> DocFlow.Domain
DocFlow.Infrastructure -> DocFlow.Application
DocFlow.Infrastructure -> DocFlow.Domain
DocFlow.Application -> DocFlow.Domain
DocFlow.Domain -> no project dependency
```

Forbidden dependencies:

```text
Domain -> Application / Infrastructure / API
Application -> Infrastructure / API
Infrastructure -> API
```

This keeps the domain model independent from HTTP, EF Core, PostgreSQL, storage, JWT and Swagger.

## Runtime flow

Typical successful processing flow:

```text
Client
  -> POST /api/auth/login
  -> receives JWT token
  -> POST /api/documents
  -> document is uploaded and stored
  -> Document aggregate is created with Uploaded status
  -> POST /api/documents/{id}/process
  -> Document moves Queued -> Processing -> Processed
  -> GET /api/documents/{id}/history
  -> client sees all transitions
```

Failure and retry flow:

```text
Uploaded -> Queued -> Processing -> Failed -> Queued
```

If processing throws an exception, the application service catches it, calls the domain method `MarkFailed`, persists the failure reason and allows retry through the retry endpoint.

## Domain layer

Project:

```text
src/DocFlow.Domain
```

Responsibilities:

- document aggregate;
- document status lifecycle;
- allowed and forbidden state transitions;
- retry rules;
- cancel rules;
- processing result state;
- processing history creation;
- domain errors;
- Result pattern.

Domain must not know about:

- HTTP;
- controllers;
- EF Core;
- PostgreSQL;
- file system;
- JWT;
- Swagger;
- Docker;
- GitHub Actions.

## Document aggregate

`Document` is the aggregate root.

It owns:

- `Id`;
- original file name;
- stored file name;
- content type;
- file size;
- checksum;
- current status;
- upload timestamp;
- processed timestamp;
- failed timestamp;
- failure reason;
- retry count;
- max retry count;
- extracted title;
- extracted text preview;
- page count;
- metadata JSON;
- processing history.

The aggregate exposes state through read-only properties and protects mutation through methods.

Allowed mutation methods:

```text
Create
MarkQueued
StartProcessing
MarkProcessed
MarkFailed
Retry
Cancel
```

The history collection is internally owned by the aggregate and exposed as `IReadOnlyCollection<DocumentProcessingHistory>`.

## Document lifecycle

Current lifecycle:

```text
Uploaded -> Queued -> Processing -> Processed
Uploaded -> Queued -> Processing -> Failed
Failed -> Queued
Uploaded -> Cancelled
Queued -> Cancelled
Failed -> Cancelled
```

Invalid transitions are rejected by the domain layer.

Examples:

```text
Uploaded -> Processing      forbidden
Processed -> Failed         forbidden
Processed -> Cancelled      forbidden
Queued -> Processed         forbidden
Failed -> Processed         forbidden
```

## History model

Every important state transition creates a `DocumentProcessingHistory` record.

History records store:

- document id;
- previous status;
- target status;
- action;
- optional reason;
- UTC timestamp.

History is not a separate aggregate. It is a child record owned by the `Document` aggregate.

This is why the history collection is mapped through a backing field in EF Core.

## Application layer

Project:

```text
src/DocFlow.Application
```

Responsibilities:

- upload use case;
- processing use case;
- failure capture;
- retry use case;
- cancel use case;
- history query;
- download use case;
- pagination;
- DTO mapping;
- application errors;
- infrastructure abstractions.

Application services coordinate work but do not own domain rules.

Correct pattern:

```text
Application service loads aggregate.
Application service calls aggregate method.
Aggregate validates transition.
Application service persists through Unit of Work.
```

Wrong pattern:

```text
Controller changes status directly.
Application service assigns Status directly.
Repository decides business transitions.
```

## Application abstractions

The application layer defines contracts that infrastructure implements:

```text
IDocumentRepository
IFileStorage
IBackgroundJobClient
IDocumentProcessor
IChecksumService
IDateTimeProvider
IUnitOfWork
```

This allows API tests to replace infrastructure services when needed.

## Processing use case

`DocumentProcessingService` coordinates processing:

```text
1. Load document by id.
2. Start processing through the aggregate.
3. Open stored file through IFileStorage.
4. Run IDocumentProcessor.
5. On success, call MarkProcessed.
6. On exception, call MarkFailed.
7. Save changes through IUnitOfWork.
8. Return DocumentResponse.
```

The processor can fail without losing the workflow state. The document becomes `Failed`, and the failure reason is stored.

## Infrastructure layer

Project:

```text
src/DocFlow.Infrastructure
```

Responsibilities:

- EF Core `AppDbContext`;
- PostgreSQL configuration;
- database migrations;
- document repository;
- EF unit of work;
- local file storage;
- checksum calculation;
- fake document processor;
- date/time provider;
- no-op background job client.

## EF Core mapping

Persistence maps:

```text
documents
document_processing_history
```

Important mapping details:

- `DocumentStatus` is stored as string;
- history has a foreign key to document id;
- history is cascade-deleted with document;
- history uses the `_history` backing field;
- property access mode is configured as field access for history.

The backing-field mapping is important because the domain exposes history as read-only but EF Core still needs to persist child records.

## API layer

Project:

```text
src/DocFlow.Api
```

Responsibilities:

- HTTP endpoints;
- request binding;
- authentication;
- authorization;
- error mapping;
- Swagger;
- health checks;
- middleware pipeline;
- request size limit.

The API uses JWT Bearer authentication and a document user policy.

Allowed roles:

```text
Operator
Admin
```

Protected document endpoints require:

```text
RequireAuthenticatedUser
RequireRole(Operator, Admin)
```

## API endpoints

Current document endpoints:

```text
POST   /api/documents
GET    /api/documents
GET    /api/documents/{documentId}
POST   /api/documents/{documentId}/process
POST   /api/documents/{documentId}/retry
POST   /api/documents/{documentId}/cancel
GET    /api/documents/{documentId}/history
GET    /api/documents/{documentId}/download
```

Current auth endpoints:

```text
POST   /api/auth/login
GET    /api/auth/me
```

Health endpoint:

```text
GET    /health
```

## Testing architecture

Test projects:

```text
tests/DocFlow.Domain.Tests
tests/DocFlow.Api.Tests
```

Domain tests verify aggregate behavior directly.

API integration tests use `WebApplicationFactory<Program>` and override infrastructure services for test isolation.

Current CI-proven test coverage:

```text
Total tests: 10
Passed: 10
```

Covered scenarios:

- domain creation;
- invalid domain transition;
- successful processing transition;
- health endpoint;
- login success;
- login failure;
- protected endpoint without token;
- authenticated `/api/auth/me`;
- upload valid `.txt` document;
- get document by id after upload;
- process uploaded document;
- history after processing;
- failed processing state;
- retry after failure.

## CI architecture

GitHub Actions runs:

```text
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
```

The current main branch has a passing CI state.

## Docker architecture

Docker Compose is used for local infrastructure.

The project includes:

- API service;
- PostgreSQL service;
- connection string wiring through configuration/environment.

The current Docker setup is for local demo usage, not production deployment.

## Key design decisions

### 1. DDD-lite, not full enterprise DDD

The project has aggregate modeling and state protection, but does not introduce unnecessary complexity such as event sourcing, sagas or distributed transactions.

### 2. Fake processor in MVP

The processor is fake by design.

The goal is to prove workflow orchestration, not OCR/PDF parsing.

### 3. Manual processing trigger

Processing is triggered through an API endpoint.

Background processing is abstracted through `IBackgroundJobClient`, but no real queue is used in the first version.

### 4. Local file storage

Files are stored locally behind `IFileStorage`.

This keeps the project simple while preserving a clean replacement point for S3, MinIO or Azure Blob Storage later.

### 5. Demo authentication

JWT auth is implemented, but user management is intentionally simplified.

The project should not be presented as production authentication.

## Explicit non-goals

The current version intentionally does not include:

- microservices;
- Kafka;
- RabbitMQ;
- Outbox;
- distributed transactions;
- event sourcing;
- real OCR;
- production identity server;
- antivirus scanning;
- cloud object storage;
- frontend.

## Final architecture summary

DocFlow Processing System demonstrates:

```text
Clean Architecture + DDD-lite aggregate + document lifecycle + failure handling + retry + history + JWT-protected API + EF Core/PostgreSQL + Docker + CI-tested integration tests.
```

The strongest architectural point is that document state is not just stored. It is controlled by explicit domain transitions and explained through history.
