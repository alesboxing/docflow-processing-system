# Testing — DocFlow Processing System

## Purpose

The test suite proves the main workflow question:

> What is happening with this document now, why is it in this status, and what should be done next?

The tests check workflow behavior, not only status codes.

## Current CI status

```text
Build succeeded
Total tests: 13
Passed: 13
0 warnings
0 errors
```

## Test projects

```text
tests/DocFlow.Domain.Tests
tests/DocFlow.Api.Tests
```

## Domain tests

| Test | Purpose |
|---|---|
| `Create_ShouldCreateUploadedDocument_WhenValid` | Creates valid aggregate |
| `StartProcessing_ShouldFail_WhenDocumentIsUploaded` | Rejects invalid transition |
| `MarkProcessed_ShouldMoveProcessingToProcessed` | Allows valid processing transition |

Domain tests do not use HTTP, EF Core, database or file system.

## API integration tests

API tests use `WebApplicationFactory<Program>`, real controllers, real application services, real authorization pipeline and EF Core InMemory for isolated CI execution.

## CI-proven API scenarios

| Test | Purpose |
|---|---|
| `Health_ShouldReturnOk` | Health endpoint works |
| `Login_ShouldReturn200_WhenCredentialsValid` | Valid login returns JWT |
| `Login_ShouldReturn401_WhenCredentialsInvalid` | Invalid login is rejected |
| `Me_ShouldReturn200_WhenAuthenticated` | JWT-authenticated user endpoint works |
| `Documents_ShouldReturn401_WhenNoToken` | Document endpoints require auth |
| `Upload_ShouldReturn201_WhenTxtFileValid` | Valid upload works |
| `Upload_ShouldReturn400_WhenExtensionUnsupported` | Unsupported extension is rejected |
| `GetById_ShouldReturn200_AfterUpload` | Uploaded document can be read |
| `Download_ShouldReturnOriginalFile_WhenDocumentExists` | Original uploaded file can be downloaded |
| `Process_ShouldReturn200_AndMarkDocumentProcessed` | Processing moves document to Processed |
| `History_ShouldReturnStatusTransitions_AfterProcessing` | History shows status transitions |
| `ProcessingFailure_ShouldMarkDocumentFailed_AndRetryShouldQueueAgain` | Processor exception becomes Failed, then retry returns Queued |
| `Cancel_ShouldReturn200_WhenDocumentUploaded` | Uploaded document can be cancelled |

## Important workflow tests

Unsupported upload:

```text
login -> upload virus.exe -> 400 BadRequest -> File.UnsupportedExtension
```

Download:

```text
login -> upload .txt -> download -> content equals original
```

Failure and retry:

```text
Queued -> Processing -> Failed -> Queued
```

Cancel:

```text
Uploaded -> Cancelled
```

## Run tests

```bash
dotnet test
```

Run API tests only:

```bash
dotnet test tests/DocFlow.Api.Tests/DocFlow.Api.Tests.csproj
```

Run domain tests only:

```bash
dotnet test tests/DocFlow.Domain.Tests/DocFlow.Domain.Tests.csproj
```

## Remaining useful tests

- paged document list;
- retry conflict when document is not failed;
- process not found;
- download not found;
- missing file upload;
- file too large;
- PostgreSQL Testcontainers persistence test.
