# Architecture — DocFlow Processing System

## Goal

DocFlow Processing System is designed as a backend workflow system for document processing.

The central question is:

> What is happening with this document now, why is it in this status, and what should be done next?

## Layers

The solution is planned around Clean Architecture:

```text
Domain
Application
Infrastructure
API
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

## Domain layer

Responsibilities:

- document aggregate;
- document status lifecycle;
- allowed and forbidden transitions;
- retry rules;
- cancel rules;
- processing result rules;
- history creation;
- domain errors;
- Result pattern.

Domain must not know about HTTP, EF Core, PostgreSQL, file system, JWT, Swagger or Docker.

## Document aggregate

`Document` is the aggregate root.

It owns:

- document metadata;
- current status;
- retry count;
- failure reason;
- extracted data;
- processing history.

State should be changed only through methods:

```text
MarkQueued
StartProcessing
MarkProcessed
MarkFailed
Retry
Cancel
```

## Application layer

Responsibilities:

- upload workflow;
- processing workflow;
- retry workflow;
- cancel workflow;
- history query;
- download use case;
- pagination;
- application DTOs;
- abstractions for infrastructure.

Application abstractions:

```text
IDocumentRepository
IFileStorage
IBackgroundJobClient
IDocumentProcessor
IChecksumService
IDateTimeProvider
IUnitOfWork
```

## Infrastructure layer

Responsibilities:

- EF Core DbContext;
- PostgreSQL configuration;
- repositories;
- local file storage;
- checksum calculation;
- fake document processor;
- date/time provider;
- unit of work.

## API layer

Responsibilities:

- HTTP endpoints;
- request binding;
- FluentValidation;
- authentication;
- authorization;
- error mapping;
- Swagger;
- health checks;
- middleware.

## Design decisions

- No real OCR in the first version.
- No microservices.
- No Kafka/RabbitMQ.
- No Outbox in MVP, but it is documented as future technical debt.
- Background processing is abstracted behind `IBackgroundJobClient`.
