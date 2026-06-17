# DocFlow Processing System

.NET 9 Clean Architecture backend for document upload, processing, status tracking, retry, history, JWT authentication, PostgreSQL persistence, Docker Compose and integration tests.

The project is built as a portfolio backend system, not as a simple CRUD API.

## Main idea

The central question of the system is:

> What is happening with this document now, why is it in this status, and what should be done next?

DocFlow models a document as a small workflow with explicit lifecycle transitions, protected mutation points, failure handling and audit-style history.

## Current status

Implemented and covered by CI:

- document upload through `multipart/form-data`;
- file validation;
- local file storage abstraction;
- PostgreSQL persistence through EF Core;
- document status lifecycle;
- processing success flow;
- processing failure flow;
- retry workflow;
- document history endpoint;
- document download endpoint;
- JWT authentication;
- role-based protected document endpoints;
- Docker Compose setup;
- GitHub Actions CI;
- domain and API integration tests.

Current CI result:

```text
Total tests: 10
Passed: 10
Build succeeded
0 warnings
0 errors
```

## Tech stack

- .NET 9
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- Docker Compose
- xUnit
- FluentAssertions
- WebApplicationFactory
- JWT Bearer authentication
- GitHub Actions

## Architecture

```text
src/
  DocFlow.Domain
  DocFlow.Application
  DocFlow.Infrastructure
  DocFlow.Api

tests/
  DocFlow.Domain.Tests
  DocFlow.Api.Tests
```

Dependency direction:

```text
DocFlow.Api -> DocFlow.Application -> DocFlow.Domain
DocFlow.Api -> DocFlow.Infrastructure -> DocFlow.Application / DocFlow.Domain
DocFlow.Infrastructure -> DocFlow.Application / DocFlow.Domain
DocFlow.Domain -> no project dependency
```

The domain layer does not know about EF Core, HTTP, controllers, DTOs, file system storage or authentication.

## Document lifecycle

Successful workflow:

```text
Uploaded -> Queued -> Processing -> Processed
```

Failure workflow:

```text
Queued -> Processing -> Failed
```

Retry workflow:

```text
Failed -> Queued -> Processing -> Processed
```

Cancel workflow:

```text
Uploaded -> Cancelled
Queued -> Cancelled
Failed -> Cancelled
```

## Implemented scenarios

### 1. Upload document

A user uploads a text, PDF or DOCX-like document through the API. The system validates the file, stores it through the storage abstraction, calculates metadata and creates a `Document` aggregate.

Initial status:

```text
Uploaded
```

### 2. Process document successfully

A document can be processed explicitly through the API.

The system moves the document through the processing lifecycle:

```text
Uploaded -> Queued -> Processing -> Processed
```

Processing stores extracted metadata such as title, text preview, page count and processing timestamp.

### 3. Capture processing failure

If the document processor throws an exception, the application service does not allow the exception to escape as an untracked workflow state.

The document is marked as:

```text
Failed
```

The failure reason is persisted and the failure transition is added to document history.

### 4. Retry failed document

A failed document can be retried through the API.

The retry operation:

- increments retry count;
- clears the failure reason;
- moves the document back to `Queued`;
- records the retry reason in document history.

### 5. Inspect document history

The API exposes document history so the user can understand why the document is in the current status.

Example transitions:

```text
Uploaded
Queued
Processing
Processed
Failed
Queued
```

## API endpoints

### Authentication

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/auth/login` | Login and receive JWT token |
| `GET` | `/api/auth/me` | Return current authenticated user |

### Documents

All document endpoints require a valid JWT token with `Operator` or `Admin` role.

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/documents` | Upload document |
| `GET` | `/api/documents` | Get paged document list |
| `GET` | `/api/documents/{documentId}` | Get document by id |
| `POST` | `/api/documents/{documentId}/process` | Process document |
| `POST` | `/api/documents/{documentId}/retry` | Retry failed document |
| `POST` | `/api/documents/{documentId}/cancel` | Cancel document |
| `GET` | `/api/documents/{documentId}/history` | Get document status history |
| `GET` | `/api/documents/{documentId}/download` | Download stored file |

## Demo users

The project uses demo authentication for portfolio and local testing.

| User | Password | Role |
|---|---|---|
| `operator` | `Operator123!` | `Operator` |
| `admin` | `Admin123!` | `Admin` |

Example login request:

```http
POST /api/auth/login
Content-Type: application/json

{
  "userName": "operator",
  "password": "Operator123!"
}
```

## Running locally

### Option 1: Docker Compose

```bash
docker compose up --build
```

Expected services:

- API: `http://localhost:5000`
- PostgreSQL: `localhost:5432`

### Option 2: .NET CLI

```bash
dotnet restore
dotnet build
dotnet test
```

To run the API directly, configure the `DefaultConnection` connection string for PostgreSQL and start the API project:

```bash
dotnet run --project src/DocFlow.Api
```

## Testing

The project contains domain tests and API integration tests.

Current tested scenarios:

- document aggregate creation;
- invalid document processing transition;
- successful processing transition;
- health endpoint;
- login success;
- login failure;
- protected document endpoint without token;
- `/api/auth/me` with token;
- document upload;
- get uploaded document by id;
- process document and mark it as processed;
- get document history after processing;
- mark document as failed when processor throws;
- retry failed document and move it back to queued.

Run tests:

```bash
dotnet test
```

CI runs on push and pull request to `main` and `develop`.

## Portfolio positioning

DocFlow Processing System demonstrates:

- Clean Architecture boundaries;
- DDD-lite aggregate modeling;
- explicit state transitions;
- protected mutation points;
- stored state vs derived workflow data;
- error handling without leaking infrastructure details into controllers;
- file upload and storage abstraction;
- EF Core persistence;
- JWT authentication and role-based authorization;
- integration testing through WebApplicationFactory;
- Dockerized local infrastructure;
- GitHub Actions CI.

## Current limitations

This is a portfolio demo, not a production document platform.

Known intentional limitations:

- no real OCR or PDF parser;
- no background queue such as RabbitMQ or Kafka;
- no distributed processing;
- no external identity provider;
- no object storage such as S3 or MinIO;
- no full-text search;
- no production-grade observability stack.

These limitations are intentional to keep the project focused on backend architecture, document workflow modeling, persistence, API contracts and integration tests.
