# API Overview — DocFlow Processing System

## Purpose

The API exposes a document workflow, not a CRUD wrapper.

Main question:

> What is happening with this document now, why is it in this status, and what should be done next?

## Base URL

```text
http://localhost:5000
```

Swagger in Development:

```text
http://localhost:5000/swagger
```

## Authentication

Document endpoints require JWT Bearer authentication and the `Operator` or `Admin` role.

Demo users:

| User | Password | Role |
|---|---|---|
| `operator` | `Operator123!` | `Operator` |
| `admin` | `Admin123!` | `Admin` |

Login:

```http
POST /api/auth/login
```

Request:

```json
{
  "userName": "operator",
  "password": "Operator123!"
}
```

Use the returned token:

```http
Authorization: Bearer <accessToken>
```

## Document statuses

```text
Uploaded
Queued
Processing
Processed
Failed
Cancelled
```

Main flows:

```text
Uploaded -> Queued -> Processing -> Processed
Uploaded -> Queued -> Processing -> Failed -> Queued
Uploaded -> Cancelled
```

## Endpoints

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

## Upload document

```http
POST /api/documents
Content-Type: multipart/form-data
Authorization: Bearer <accessToken>
```

Form field:

```text
file
```

Allowed extensions:

```text
.pdf
.docx
.txt
```

Allowed content types:

```text
application/pdf
text/plain
```

Max file size:

```text
10 MB
```

Success:

```text
201 Created
```

Unsupported extension:

```text
400 BadRequest
File.UnsupportedExtension
```

CI-proven example:

```text
virus.exe -> 400 BadRequest -> File.UnsupportedExtension
```

## Download document

```http
GET /api/documents/{documentId}/download
Authorization: Bearer <accessToken>
```

Success:

```text
200 OK
Content-Type: original content type
Content-Disposition: attachment; filename=<originalFileName>
```

The CI test verifies that downloaded `.txt` content equals the original uploaded content.

## Process document

```http
POST /api/documents/{documentId}/process
Authorization: Bearer <accessToken>
```

Success flow:

```text
Uploaded -> Queued -> Processing -> Processed
```

If the processor throws, the current demo returns `200 OK` with document status `Failed`. The failure is a workflow result, not an unhandled server crash.

## Retry document

```http
POST /api/documents/{documentId}/retry
Content-Type: application/json
Authorization: Bearer <accessToken>
```

Request:

```json
{
  "reason": "Temporary processing issue was fixed."
}
```

Allowed only from `Failed`.

Flow:

```text
Failed -> Queued
```

## Cancel document

```http
POST /api/documents/{documentId}/cancel
Content-Type: application/json
Authorization: Bearer <accessToken>
```

Request:

```json
{
  "reason": "User cancelled processing."
}
```

Allowed from:

```text
Uploaded
Queued
Failed
```

## History

```http
GET /api/documents/{documentId}/history
Authorization: Bearer <accessToken>
```

History explains the document status through transition records.

## Error response

```json
{
  "code": "Document.NotFound",
  "message": "Document was not found.",
  "type": "NotFound"
}
```

## CI-proven scenarios

```text
Total tests: 13
Passed: 13
```

Covered scenarios:

- health endpoint;
- login success and failure;
- `/api/auth/me`;
- unauthorized document access returns 401;
- valid upload;
- unsupported extension rejection;
- get by id;
- download original file;
- process successfully;
- history after processing;
- processing failure and retry;
- cancel uploaded document.

## Limitations

The API intentionally does not include production identity management, refresh tokens, real OCR, async queue execution, object storage, antivirus scanning or frontend UI.
