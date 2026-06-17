# API Overview — DocFlow Processing System

## Purpose

This API is designed to answer the main workflow question:

> What is happening with this document now, why is it in this status, and what should be done next?

The API is not a simple CRUD wrapper. Most document endpoints represent lifecycle actions: upload, process, fail, retry, cancel, inspect history and download.

## Base URL

Docker/local demo target:

```text
http://localhost:5000
```

Swagger UI in Development:

```text
http://localhost:5000/swagger
```

## Authentication model

The API uses JWT Bearer authentication.

Document endpoints require the `DocumentUser` policy.

Allowed roles:

```text
Operator
Admin
```

Use the token in protected requests:

```http
Authorization: Bearer <accessToken>
```

## Demo users

The current project uses demo authentication.

| User name | Password | Role |
|---|---|---|
| `operator` | `Operator123!` | `Operator` |
| `admin` | `Admin123!` | `Admin` |

This is suitable for a portfolio demo, not for production identity management.

## Auth endpoints

### Login

```http
POST /api/auth/login
Content-Type: application/json
```

Request:

```json
{
  "userName": "operator",
  "password": "Operator123!"
}
```

Successful response: `200 OK`

```json
{
  "accessToken": "jwt-token",
  "tokenType": "Bearer",
  "expiresAtUtc": "2026-06-17T12:00:00Z",
  "userName": "operator",
  "role": "Operator"
}
```

Invalid credentials response: `401 Unauthorized`

### Current user

```http
GET /api/auth/me
Authorization: Bearer <accessToken>
```

Successful response: `200 OK`

```json
{
  "userName": "operator",
  "role": "Operator"
}
```

## Document status lifecycle

Current statuses:

```text
Uploaded
Queued
Processing
Processed
Failed
Cancelled
```

Main success flow:

```text
Uploaded -> Queued -> Processing -> Processed
```

Failure and retry flow:

```text
Uploaded -> Queued -> Processing -> Failed -> Queued
```

Cancel flow:

```text
Uploaded -> Cancelled
Queued -> Cancelled
Failed -> Cancelled
```

Important rule: status changes are controlled by the domain model, not by the controller.

## Document response shape

Most document endpoints return a `DocumentResponse`:

```json
{
  "id": "c8f2cf9c-a772-4df6-8f74-77f62472805c",
  "originalFileName": "example.txt",
  "storedFileName": "stored-file-name.txt",
  "contentType": "text/plain",
  "sizeBytes": 128,
  "checksum": "sha256-checksum",
  "status": "Processed",
  "uploadedAtUtc": "2026-06-17T08:59:26Z",
  "processedAtUtc": "2026-06-17T08:59:26Z",
  "failedAtUtc": null,
  "failureReason": null,
  "retryCount": 0,
  "maxRetryCount": 3,
  "extractedTitle": "example.txt",
  "extractedTextPreview": "Extracted text preview...",
  "pageCount": 1,
  "metadataJson": "{}"
}
```

## Document endpoints

All endpoints below require:

```http
Authorization: Bearer <accessToken>
```

### Upload document

```http
POST /api/documents
Content-Type: multipart/form-data
```

Form file field:

```text
file
```

Supported extensions:

```text
.pdf
.docx
.txt
```

Supported content types:

```text
application/pdf
text/plain
```

Special note: `.docx` is allowed by extension even when the content type is not included in the strict content-type allowlist.

Max file size:

```text
10 MB
```

Successful response: `201 Created`

```json
{
  "id": "document-id",
  "originalFileName": "example.txt",
  "contentType": "text/plain",
  "status": "Uploaded",
  "retryCount": 0,
  "maxRetryCount": 3
}
```

The actual response contains the full `DocumentResponse` shape.

### Get paged documents

```http
GET /api/documents?page=1&pageSize=20
```

Optional status filter:

```http
GET /api/documents?status=Processed&page=1&pageSize=20
```

Successful response: `200 OK`

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 0,
  "totalPages": 0
}
```

### Get document by id

```http
GET /api/documents/{documentId}
```

Successful response: `200 OK`

Returns `DocumentResponse`.

Not found response: `404 Not Found`

### Process document

```http
POST /api/documents/{documentId}/process
```

Successful response: `200 OK`

Returns `DocumentResponse`.

Expected successful lifecycle inside the use case:

```text
Queued -> Processing -> Processed
```

Current API-level behavior:

- the endpoint accepts an uploaded document id;
- the application service starts processing through the domain model;
- the fake processor extracts demo metadata;
- the document becomes `Processed`;
- history receives processing transition records.

### Processing failure behavior

The same endpoint can also produce a failed document state if the document processor throws an exception.

Expected lifecycle:

```text
Queued -> Processing -> Failed
```

The API response still returns `200 OK` with the document in `Failed` status because the workflow failure is a business result, not an unhandled server crash.

Example failed response fragment:

```json
{
  "status": "Failed",
  "failedAtUtc": "2026-06-17T08:59:26Z",
  "failureReason": "Processing failed in test processor.",
  "retryCount": 0
}
```

### Retry document

```http
POST /api/documents/{documentId}/retry
Content-Type: application/json
```

Request:

```json
{
  "reason": "Temporary processing issue was fixed."
}
```

Allowed only when the document is `Failed`.

Successful response: `200 OK`

Expected lifecycle:

```text
Failed -> Queued
```

Response fragment:

```json
{
  "status": "Queued",
  "failedAtUtc": null,
  "failureReason": null,
  "retryCount": 1
}
```

### Cancel document

```http
POST /api/documents/{documentId}/cancel
Content-Type: application/json
```

Request:

```json
{
  "reason": "User cancelled processing."
}
```

Allowed statuses:

```text
Uploaded
Queued
Failed
```

Successful response: `200 OK`

Returns `DocumentResponse` with `status = "Cancelled"`.

### Get document history

```http
GET /api/documents/{documentId}/history
```

Successful response: `200 OK`

```json
[
  {
    "id": "history-id",
    "documentId": "document-id",
    "fromStatus": null,
    "toStatus": "Uploaded",
    "action": "Document uploaded",
    "reason": null,
    "createdAtUtc": "2026-06-17T08:59:26Z"
  },
  {
    "id": "history-id",
    "documentId": "document-id",
    "fromStatus": "Processing",
    "toStatus": "Processed",
    "action": "Document processed successfully",
    "reason": null,
    "createdAtUtc": "2026-06-17T08:59:26Z"
  }
]
```

The history endpoint is the main endpoint for explaining why the document is in its current state.

### Download document

```http
GET /api/documents/{documentId}/download
```

Successful response: `200 OK`

Returns the original uploaded file as binary content.

The response uses:

```text
Content-Type: original document content type
Content-Disposition: attachment; filename=<originalFileName>
```

## Health check

```http
GET /health
```

Successful response: `200 OK`

```text
Healthy
```

The current implementation exposes only `/health`.

## Error response

Business errors use `ErrorResponse`:

```json
{
  "code": "Document.NotFound",
  "message": "Document was not found.",
  "type": "NotFound"
}
```

Current error response shape:

```text
code
message
type
```

The current implementation does not add `traceId` or `correlationId` to the `ErrorResponse` contract.

## Common status codes

| Status | Meaning |
|---|---|
| 200 | Request succeeded |
| 201 | Document uploaded |
| 400 | Validation or invalid request |
| 401 | Authentication required or invalid credentials |
| 403 | Authenticated user does not satisfy policy/role |
| 404 | Resource not found |
| 409 | Business conflict / invalid state transition |
| 413 | Request body too large |
| 500 | Unexpected server error |

## CI-proven scenarios

The current API test suite proves these scenarios in GitHub Actions:

```text
Total tests: 10
Passed: 10
```

Covered API scenarios:

- health endpoint returns OK;
- login succeeds with valid demo credentials;
- login fails with invalid credentials;
- `/api/auth/me` works with token;
- document endpoint without token returns 401;
- `.txt` upload returns 201;
- uploaded document can be fetched by id;
- document can be processed successfully;
- processing history contains transitions;
- processing failure marks document as Failed;
- retry after failure queues the document again.

## Minimal manual demo sequence

### 1. Login

```http
POST /api/auth/login
```

```json
{
  "userName": "operator",
  "password": "Operator123!"
}
```

Copy `accessToken` from the response.

### 2. Upload document

```http
POST /api/documents
Authorization: Bearer <accessToken>
Content-Type: multipart/form-data
```

Attach a file using form field:

```text
file
```

Copy `id` from the response.

### 3. Process document

```http
POST /api/documents/{documentId}/process
Authorization: Bearer <accessToken>
```

Expected result:

```text
status = Processed
```

### 4. Check history

```http
GET /api/documents/{documentId}/history
Authorization: Bearer <accessToken>
```

Expected result:

```text
Uploaded -> Queued -> Processing -> Processed
```

## Current limitations

The current API intentionally does not provide:

- production user management;
- refresh tokens;
- real OCR;
- async queue execution;
- S3/MinIO/Azure Blob storage;
- antivirus scanning;
- frontend UI;
- public anonymous document access.
