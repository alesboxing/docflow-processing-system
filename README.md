# DocFlow Processing System

.NET 9 Clean Architecture backend for document workflow processing.

This is a portfolio backend project, not a simple CRUD API. The core question is:

> What is happening with this document now, why is it in this status, and what should be done next?

## Current CI status

```text
Build succeeded
Total tests: 13
Passed: 13
0 warnings
0 errors
```

## Implemented features

- JWT authentication with demo `Operator` and `Admin` users.
- Protected document endpoints.
- Document upload through `multipart/form-data`.
- File validation with unsupported extension rejection.
- Local file storage abstraction.
- Download of the original uploaded file.
- Document lifecycle modeling.
- Processing success flow.
- Processing failure capture.
- Retry failed document.
- Cancel uploaded document.
- Document history endpoint.
- EF Core/PostgreSQL infrastructure.
- Docker Compose setup.
- GitHub Actions CI.

## Document lifecycle

```text
Uploaded -> Queued -> Processing -> Processed
Uploaded -> Queued -> Processing -> Failed
Failed -> Queued
Uploaded -> Cancelled
Queued -> Cancelled
Failed -> Cancelled
```

## CI-proven scenarios

- health endpoint returns OK;
- login succeeds with valid credentials;
- login fails with invalid credentials;
- protected document endpoint rejects anonymous request;
- `/api/auth/me` works with JWT;
- valid `.txt` upload returns `201 Created`;
- unsupported extension returns `400 BadRequest` with `File.UnsupportedExtension`;
- uploaded document can be fetched by id;
- uploaded file can be downloaded with original content;
- document can be processed successfully;
- history returns processing transitions;
- processor failure marks document as `Failed`;
- retry moves failed document back to `Queued`;
- cancel moves uploaded document to `Cancelled`.

## Main endpoints

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

## Demo users

| User | Password | Role |
|---|---|---|
| `operator` | `Operator123!` | `Operator` |
| `admin` | `Admin123!` | `Admin` |

## Run locally

```bash
docker compose up --build
```

or:

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/DocFlow.Api
```

## Portfolio value

The project demonstrates Clean Architecture, DDD-lite aggregate modeling, protected state transitions, file upload/download, failure handling, retry logic, history tracking, JWT-protected API endpoints, persistence and integration testing.

## Intentional limitations

- no real OCR/PDF parser;
- no real background queue;
- no production identity provider;
- no S3/MinIO/Azure Blob storage;
- no full observability stack;
- no frontend.
