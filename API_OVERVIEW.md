# API Overview — DocFlow Processing System

Base URL for Docker target:

```text
http://localhost:5000
```

## Authentication

### Login

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

Use token:

```http
Authorization: Bearer <accessToken>
```

### Current user

```http
GET /api/auth/me
```

Requires authentication.

## Documents

All document endpoints require JWT authentication.

### Upload document

```http
POST /api/documents
Content-Type: multipart/form-data
```

Form field:

```text
file
```

Supported files:

```text
.pdf
.docx
.txt
```

Max file size:

```text
10 MB
```

### Get paged documents

```http
GET /api/documents?page=1&pageSize=20
```

Optional status filter:

```http
GET /api/documents?status=Processed&page=1&pageSize=20
```

### Get document by id

```http
GET /api/documents/{documentId}
```

### Process document

```http
POST /api/documents/{documentId}/process
```

Allowed only when document is `Queued`.

### Retry document

```http
POST /api/documents/{documentId}/retry
```

Request:

```json
{
  "reason": "Temporary processing issue was fixed."
}
```

Allowed only when document is `Failed`.

### Cancel document

```http
POST /api/documents/{documentId}/cancel
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

### Get document history

```http
GET /api/documents/{documentId}/history
```

### Download document

```http
GET /api/documents/{documentId}/download
```

Returns original uploaded file.

## Health checks

```http
GET /health
GET /health/live
GET /health/ready
```

Readiness checks database connection.

## Error response

Expected business errors use:

```json
{
  "code": "Document.NotFound",
  "message": "Document was not found.",
  "type": "NotFound",
  "traceId": "trace-id",
  "correlationId": "correlation-id"
}
```

Validation errors use:

```json
{
  "code": "Validation.InvalidRequest",
  "message": "Request validation failed.",
  "type": "Validation",
  "errors": {
    "Reason": [
      "Retry reason is required."
    ]
  }
}
```

## Common status codes

| Status | Meaning |
|---|---|
| 200 | Request succeeded |
| 201 | Document uploaded |
| 400 | Validation error |
| 401 | Authentication required |
| 403 | Forbidden |
| 404 | Resource not found |
| 409 | Business conflict |
| 413 | Request body too large |
| 500 | Unexpected server error |
