# Assignment & Submission Management System

**A role-based platform where teachers create assignments, students submit work, and
teachers grade it — with deadlines, attachments, and audit-grade ownership rules.**

Built as a production-ready full-stack application: a Clean Architecture CQRS API in
.NET, an Angular SPA, and an automated CI/CD pipeline that ships it to a live server.

![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Angular](https://img.shields.io/badge/Angular-20-DD0031?style=flat-square&logo=angular&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?style=flat-square&logo=postgresql&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-5-3178C6?style=flat-square&logo=typescript&logoColor=white)
![Tests](https://img.shields.io/badge/tests-198%20green-2ea44f?style=flat-square)
![CI/CD](https://img.shields.io/badge/CI%2FCD-GitHub%20Actions-2088FF?style=flat-square&logo=githubactions&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat-square&logo=docker&logoColor=white)
![Cloud](https://img.shields.io/badge/Deployed-AWS%20EC2-FF9900?style=flat-square&logo=amazonaws&logoColor=white)

---

## 🚀 Try the live demo

> **http://13.229.60.186** — deployed automatically from `main` by the CI/CD pipeline.

| Role | Email | Password |
|---|---|---|
| Admin | `admin@school.edu` | `Admin@123` |
| Teacher | `teacher@school.edu` | `Teacher@123` |
| Student | `student@school.edu` | `Student@123` |

The database is pre-seeded with a class, a published assignment and a sample
submission, so every role has something real to click on at first login.

---

## Why this project stands out

- **Full-stack, end-to-end.** Domain model, REST API, SPA, database, tests,
  containerization and cloud deployment — one coherent system.
- **Serious engineering structure.** Clean Architecture with a dependency rule the
  compiler enforces, CQRS via MediatR, and business rules that live in domain entities
  instead of leaking into controllers.
- **Security is a first-class feature, not an afterthought.** JWT auth, PBKDF2
  password hashing, ownership checks on every resource, RFC 7807 ProblemDetails, and
  an explicit threat-model review — not just `[Authorize]` attributes.
- **198 automated tests** (152 unit + 46 integration against a real PostgreSQL),
  covering domain invariants, handlers, validators and cross-user access control.
- **Automated deployment.** Every push to `main` is built, tested, and deployed to a
  live AWS EC2 host — the server even provisions itself (installs Docker, generates
  secrets) on first deploy. Zero manual steps.

---

## Features

**For students** — view assignments for your class, submit answers with file
attachments (≤10 MB), edit until the deadline, then see your marks and feedback.

**For teachers** — author assignments for the class/subject pairs you're allocated,
save drafts or publish, grade submissions with feedback, and return work for revision
(reopening it past the deadline).

**For admins** — manage users, classes, subjects and teaching allocations, reset
passwords, and see the full picture across all assignments and submissions.

**Platform-wide** — role-based JWT authentication, deadline enforcement in the domain,
grading clamped to each assignment's maximum marks, auditable submission statuses, and
ownership checks that stop a teacher editing another teacher's work or a student
reading another student's submission.

---

## Technology stack

| Layer | Technology |
|---|---|
| Backend | C#, .NET 10 (LTS), ASP.NET Core Web API |
| Architecture | Clean Architecture, CQRS, MediatR, Repository + Unit of Work |
| Data | Entity Framework Core 10, PostgreSQL 16 (Npgsql) |
| Validation | FluentValidation via a MediatR pipeline behavior |
| Security | JWT (HS256), PBKDF2-HMAC-SHA256 password hashing, role + ownership authorization |
| Logging / Errors | Serilog (structured) · RFC 7807 ProblemDetails |
| Frontend | Angular 20, TypeScript, standalone components, signals, RxJS, reactive forms |
| Testing | xUnit, FluentAssertions, Moq |
| DevOps | Docker Compose, multi-stage Dockerfiles, GitHub Actions, GitHub Container Registry, AWS EC2 |

---

## Architecture

```text
        ┌─────────────┐   HTTP    ┌──────────────┐
        │  Angular 20 │ ───────►  │  ASP.NET API │
        │  (nginx)    │   /api    │  (CQRS)      │
        └─────────────┘           └──────┬───────┘
                                         ▼
              ┌──────────────┬──────────────┬──────────────┐
              │  Api         │  Application │  Domain      │
              │  controllers │  handlers,   │  entities,   │
              │  middleware  │  validators  │  invariants  │
              └──────┬───────┴──────┬───────┴──────┬───────┘
                     └───── Infrastructure (EF Core, PostgreSQL) ─────┘
```

- **Domain** — entities, value objects and business rules with *zero* package
  references, so the dependency rule literally cannot be broken by accident.
- **Application** — commands, queries, handlers, validators, and the interfaces
  (`IUnitOfWork`, repositories, `IFileStorage`) that Infrastructure implements.
- **Infrastructure** — EF Core, PostgreSQL, repositories, JWT, password hashing, file
  storage, seeding.
- **Api** — controllers that do nothing but forward to handlers, plus middleware and
  DI wiring. That is the only place Infrastructure is referenced.

**Design decisions worth knowing about**

- **Business rules live in the domain, not in handlers.** Publishing, deadline
  enforcement, editability and the grading ceiling are invariants enforced by the
  `Assignment` and `Submission` aggregates themselves — which is why they're tested as
  pure unit tests with no database.
- **The grading ceiling crosses two aggregates cleanly.** `Submission.Grade` receives
  the assignment's maximum marks as a parameter instead of duplicating the value, so
  the rule stays in the domain and stays testable in isolation.
- **Deadlines are absolute, with one auditable exception.** Returning a submission for
  revision reopens it for that student past the deadline — recorded in the
  submission's status so it's always traceable.
- **Ownership is enforced in handlers, not just by attributes.** `[Authorize]` proves
  the role; the handler proves the resource belongs to the caller. Identity always
  comes from the validated JWT, never from the request body.

**Database** — 7 tables, one EF Core migration, unique indexes on natural keys and
composite indexes on the hot query paths, with enforced foreign keys.

---

## Getting started

**Docker is the only requirement** — no .NET SDK, Node.js or database install needed.

```bash
cp .env.example .env        # one-time config (placeholders work locally)
docker compose up --build   # build + start postgres, api, frontend
```

| Service | URL |
|---|---|
| Frontend | http://localhost:8080 |
| API / Swagger | http://localhost:5105/swagger |

Then log in with the demo credentials above. The API applies EF Core migrations and
runs the idempotent seeder on first startup, so the schema and demo data appear
automatically.

Without Docker: `dotnet run --project src/AssignmentManagement.Api` + `npm start`
from `frontend/assignment-management-ui` (see the footer for details).

---

## CI/CD — every push ships to production

The repository ships with a GitHub Actions pipeline: **push to `main` → build → run
all 198 tests against a real PostgreSQL service container → build Docker images → push
to GitHub Container Registry → deploy to AWS EC2.**

- **Self-provisioning server.** The deploy job copies the production compose stack to
  the host and runs an idempotent bootstrap: if Docker is missing it installs it, and
  it generates a secure `.env` (random Postgres password + JWT secret) exactly once.
  A fresh instance goes from zero to deployed with nothing but an SSH key.
- **Immutable images.** `docker-compose.prod.yml` pulls prebuilt images from GHCR —
  no build on the server. Migrations and seeding run on API startup, so a deploy is
  just "pull the new image and restart".
- **Defense in depth on the network.** The API binds to loopback only; all external
  traffic enters through nginx on :80, which proxies `/api` same-origin.
- **Healthy deploys only.** A health probe must pass before the deploy reports success;
  on failure it dumps the API logs so a rollback is one previous commit away.

Production stack: `docker-compose.prod.yml` · pipeline: `.github/workflows/ci-cd.yml`
· bootstrap: `deploy/ec2-provision.sh` · deploy step: `deploy/ec2-update.sh`.

---

## Security

Implemented, tested, and reviewed:

- **Authentication** — HS256 JWTs (60-min lifetime, all claims validated); signing
  secret required to be ≥32 characters and sourced from configuration only.
- **Password storage** — PBKDF2-HMAC-SHA256, 100,000 iterations, random per-user salt,
  constant-time comparison; passwords are never logged or returned.
- **Anti-enumeration** — login returns the same generic error whether or not the
  account exists.
- **Authorization** — role checks + per-resource ownership checks in handlers.
- **Input validation** — FluentValidation gates every command, returning RFC 7807
  ProblemDetails.
- **Uploads** — stored under non-guessable keys with a path-traversal guard and a
  12 MB nginx body cap.
- **Data access** — 100% EF Core LINQ over parameterized queries; no raw SQL.
- **Errors & logging** — central middleware maps every failure to ProblemDetails, stack
  traces never leave the server, and Serilog never logs bodies, passwords or tokens.

Documented limitations (deliberate for this scope): no refresh tokens, no rate
limiting, no attachment type allow-list, uploads are node-local (S3 would sit behind
`IFileStorage`), and HTTPS is the recommended next step for a public deployment.

---

## Project structure

```text
src/
  AssignmentManagement.Domain/         Entities, value objects, invariants, domain events
  AssignmentManagement.Application/    CQRS commands/queries, handlers, validators, DTOs
  AssignmentManagement.Infrastructure/ EF Core, repositories, JWT, PBKDF2, file storage, seeder
  AssignmentManagement.Api/            Controllers, middleware, Serilog, Swagger, health checks
tests/
  AssignmentManagement.UnitTests/      152 tests — domain, handlers, validators, hashing
  AssignmentManagement.IntegrationTests/  46 tests — auth, submissions, access control (real PG)
frontend/assignment-management-ui/     Angular 20 SPA (standalone components, signals)
docker/                                Multi-stage Dockerfiles + nginx config
deploy/                                Server bootstrap + deploy scripts for the CI/CD pipeline
.github/workflows/ci-cd.yml            Build, test, publish and deploy
docs/                                  Design doc, schema and implementation plan
```

The SPA mirrors the API's resources with typed services and models, and keeps the
browser out of the security boundary: role-aware menus are navigation UX only, while
every API request is authorized server-side.

---

## License

Copyright © 2026. Built as a portfolio project.
