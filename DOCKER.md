# Docker Setup — DocFlow Processing System

## Overview

Target Docker Compose setup:

- PostgreSQL;
- DocFlow API.

Frontend is not included in the current version.

## Start

```bash
docker compose up --build
```

## API

```text
http://localhost:5000
```

## Health

```bash
curl http://localhost:5000/health
curl http://localhost:5000/health/live
curl http://localhost:5000/health/ready
```

## Login

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"operator","password":"Operator123!"}'
```

## Demo users

| User | Password | Role |
|---|---|---|
| operator | Operator123! | Operator |
| admin | Admin123! | Admin |

## Stop

```bash
docker compose down
```

## Reset database and storage

```bash
docker compose down -v
```

## Notes

This Docker setup is intended for local demo and portfolio review.

It is not a production deployment.
