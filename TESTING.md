# Testing — DocFlow Processing System

## Purpose

The test suite proves the main project question:

> What is happening with this document now, why is it in this status, and what should be done next?

The tests are focused on document workflow behavior, not only on controller status codes.

## Current CI status

Current GitHub Actions result:

```text
Total tests: 10
Passed: 10
Build succeeded
0 warnings
0 errors
```

## Test projects

```text
tests/
  DocFlow.Domain.Tests
  DocFlow.Api.Tests
```

Current implemented test layers:

| Project | Purpose |
|---|---|
| `DocFlow.Domain.Tests` | Tests aggregate behavior without EF Core, HTTP or file system |
| `DocFlow.Api.Tests` | Tests real HTTP pipeline through `WebApplicationFactory<Program>` |

Planned but not required for the current demo:

```text
DocFlow.Application.Tests
DocFlow.Infrastructure.Tests
```

The current demo intentionally keeps the test suite compact and CI-fast.

## Domain tests

Domain tests verify the `Document` aggregate directly.

Current domain test coverage:

| Test | What it proves |
|---|---|
| `Create_ShouldCreateUploadedDocument_WhenValid` | A valid document can be created as an aggregate |
| `StartProcessing_ShouldFail_WhenDocumentIsUploaded` | Invalid status transition is rejected by Domain |
| `MarkProcessed_ShouldMoveProcessingToProcessed` | Valid processing transition is handled by Domain |

Domain test rule:

```text
No EF Core.
No database.
No controllers.
No HTTP.
No file system.
```

The goal is to prove business rules at the aggregate boundary.

## API integration tests

API tests use:

```text
WebApplicationFactory<Program>
ASP.NET Core TestServer
EF Core InMemory provider
Real application services
Real controllers
Real authorization pipeline
Fake or overridden document processor when needed
```

The custom API test factory replaces the production EF Core PostgreSQL configuration with an isolated EF Core InMemory database per factory instance.

Why this matters:

- tests do not require PostgreSQL in CI;
- every test factory gets an isolated database name;
- the HTTP pipeline still uses real DI, controllers, model binding and authorization;
- failure scenarios can override `IDocumentProcessor` without changing production code.

## Current API test coverage

### Auth and health

| Test | What it proves |
|---|---|
| `Health_ShouldReturnOk` | `/health` is reachable |
| `Login_ShouldReturn200_WhenCredentialsValid` | Valid demo credentials return JWT token |
| `Login_ShouldReturn401_WhenCredentialsInvalid` | Invalid credentials are rejected |
| `Me_ShouldReturn200_WhenAuthenticated` | Authenticated user endpoint works with token |
| `Documents_ShouldReturn401_WhenNoToken` | Document endpoints require authentication |

### Upload and read

| Test | What it proves |
|---|---|
| `Upload_ShouldReturn201_WhenTxtFileValid` | Valid `.txt` upload works through multipart form data |
| `GetById_ShouldReturn200_AfterUpload` | Uploaded document can be fetched by id |

### Processing success

| Test | What it proves |
|---|---|
| `Process_ShouldReturn200_AndMarkDocumentProcessed` | Processing endpoint moves document to `Processed` |
| `History_ShouldReturnStatusTransitions_AfterProcessing` | History contains `Uploaded`, `Queued`, `Processing`, `Processed` transitions |

### Failure and retry

| Test | What it proves |
|---|---|
| `ProcessingFailure_ShouldMarkDocumentFailed_AndRetryShouldQueueAgain` | Processor exception becomes workflow failure, then retry returns document to `Queued` |

## What the API tests actually verify

The API integration tests verify this path:

```text
HTTP request
-> routing
-> model binding
-> JWT authentication
-> role policy
-> controller
-> application service
-> domain aggregate
-> EF Core persistence
-> HTTP response
```

This is why the tests are more valuable than isolated controller tests.

## Failure/retry test design

The failure test replaces the normal processor:

```text
IDocumentProcessor -> ThrowingDocumentProcessor
```

The throwing processor raises:

```text
InvalidOperationException("Demo parser failed.")
```

Expected behavior:

```text
Queued -> Processing -> Failed -> Queued
```

The test verifies:

- `/process` returns `200 OK`;
- the document status becomes `Failed`;
- `failureReason` is persisted;
- `/retry` returns `200 OK`;
- the document status becomes `Queued`;
- `retryCount` becomes `1`;
- `failureReason` is cleared;
- history contains the failure and retry records.

## Why processing failure returns 200 in the current demo

A parser failure is treated as a business workflow result, not as an unhandled server crash.

Current behavior:

```text
HTTP 200 + status = Failed
```

This means:

- the API request was handled successfully;
- the document processing business operation failed;
- the failure is visible in document state and history;
- the user can retry the document.

This is intentional for the current demo.

A future production version could alternatively return `202 Accepted` for async processing or expose separate job status endpoints.

## Running tests

Run the full test suite:

```bash
dotnet test
```

Run only API tests:

```bash
dotnet test tests/DocFlow.Api.Tests/DocFlow.Api.Tests.csproj
```

Run only domain tests:

```bash
dotnet test tests/DocFlow.Domain.Tests/DocFlow.Domain.Tests.csproj
```

Run the same style as CI after build:

```bash
dotnet test --configuration Release --no-build --verbosity normal
```

## CI behavior

GitHub Actions runs the test suite on push and pull request.

The CI pipeline proves:

```text
restore
build
unit/integration tests
```

A failed test fails the pipeline.

## Known test limitations

The current tests do not yet cover:

- real PostgreSQL integration with Testcontainers;
- real file-system cleanup assertions;
- real OCR/PDF parsing;
- large file upload rejection;
- unsupported file extension rejection;
- cancel endpoint scenarios;
- download endpoint binary response;
- paging and filtering edge cases;
- authorization role matrix beyond basic authenticated/unauthenticated checks.

These are good next improvements, but they are not required to prove the current portfolio-level workflow.

## Recommended next tests

Priority order:

1. `Cancel_ShouldReturn200_WhenDocumentUploaded`
2. `Download_ShouldReturnOriginalFile_WhenDocumentExists`
3. `Upload_ShouldReturn400_WhenExtensionUnsupported`
4. `GetDocuments_ShouldReturnPagedList`
5. `Retry_ShouldReturn409_WhenDocumentIsNotFailed`
6. `Process_ShouldReturn404_WhenDocumentNotFound`

This order improves coverage without adding unnecessary architecture complexity.
