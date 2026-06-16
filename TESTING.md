# Testing — DocFlow Processing System

## Test strategy

The test suite should be split by architecture layer:

```text
Domain.Tests
Application.Tests
Infrastructure.Tests
Api.Tests
```

Each layer tests its own responsibility.

## Domain tests

Domain tests verify:

- document creation;
- required metadata;
- UTC date validation;
- allowed status transitions;
- forbidden status transitions;
- retry rules;
- cancel rules;
- failure rules;
- processing result validation;
- history creation.

Domain tests should not use EF Core, database, file system or API.

## Application tests

Application tests verify use cases:

- upload document;
- file validation policy;
- checksum orchestration;
- storage abstraction usage;
- processing workflow;
- failure handling;
- retry workflow;
- cancel workflow;
- history query;
- download use case.

Application tests should use fake implementations of:

```text
IDocumentRepository
IFileStorage
IDocumentProcessor
IBackgroundJobClient
IDateTimeProvider
IChecksumService
IUnitOfWork
```

## Infrastructure tests

Infrastructure tests verify:

- EF Core repository behavior;
- local file storage;
- checksum service;
- fake document processor;
- storage path safety.

Repository tests should not retest Domain transitions.

## API integration tests

API tests should use `WebApplicationFactory<Program>`.

They verify the full HTTP pipeline:

```text
HTTP request
-> routing
-> model binding
-> validation
-> authorization
-> controller
-> application
-> infrastructure
-> database
-> HTTP response
```

API tests should use:

- SQLite in-memory database;
- temporary file storage;
- test JWT users;
- fake document processor;
- throwing document processor for failure cases.

## Running tests

```bash
dotnet test
```

Run only API tests:

```bash
dotnet test tests/DocFlow.Api.Tests
```
