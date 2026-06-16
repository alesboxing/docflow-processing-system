# DocFlow Processing System

.NET Clean Architecture backend for document upload, processing, retry, history, JWT auth, PostgreSQL, Docker and integration tests.

## Project status

This repository has been created for the DocFlow Processing System portfolio project.

Current repository state:

- documentation scaffold is published;
- architecture plan is published;
- source code should be pushed from the local project folder or added in the next implementation commits.

## Main idea

The central question of the system is:

> What is happening with this document now, why is it in this status, and what should be done next?

DocFlow is designed as a backend workflow system, not as a simple CRUD API.

## Planned features

- document upload through multipart/form-data;
- file validation;
- local file storage abstraction;
- document status lifecycle;
- processing failure handling;
- retry workflow;
- document history;
- download endpoint;
- JWT authentication;
- role-based protected endpoints;
- PostgreSQL persistence;
- Docker Compose setup;
- API integration tests.

## Target architecture

```text
DocFlow.Domain
DocFlow.Application
DocFlow.Infrastructure
DocFlow.Api
```

Dependency direction:

```text
Api -> Application -> Domain
Api -> Infrastructure -> Application/Domain
Infrastructure -> Application/Domain
Domain -> no project dependency
```

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

## Docker target

The intended local demo setup:

```bash
docker compose up --build
```

Expected services:

- API;
- PostgreSQL.

## Portfolio positioning

DocFlow Processing System is intended to demonstrate:

- Clean Architecture;
- DDD-lite aggregate modeling;
- lifecycle/state transition protection;
- file upload and storage abstraction;
- failure handling and retry rules;
- authentication and protected API endpoints;
- integration testing with WebApplicationFactory;
- Dockerized local infrastructure.

## Repository note

This repository was initialized from GitHub mobile. The next required step is to push or generate the actual source code structure under `src/` and `tests/`.
