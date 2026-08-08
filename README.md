# Assignment & Submission Management System

A role-based assignment and submission system for a school or college, built for the
OnnoRokom Projukti Assistant Software Engineer recruitment project.

Teachers set work for a class and subject, students submit answers before a deadline,
and teachers mark and give feedback. Administrators manage the users, classes,
subjects and teaching allocations behind it.

> **Build status: phase 8 of 8 — complete.** The full backend — domain, persistence,
> JWT auth, error handling and the complete CQRS API — is done, verified by 198 tests
> (152 unit + 46 integration). The Angular 20 frontend is complete, and the whole
> stack runs from a single `docker compose up --build` (frontend, API, PostgreSQL,
> health checks, migrations and seed data verified). Final documentation and the
> security review are done — see [Build progress](#build-progress).
> The full design and phase-by-phase plan are in
> [`docs/IMPLEMENTATION_PLAN.md`](docs/IMPLEMENTATION_PLAN.md).

---

## Technology stack

| Layer | Technology |
|---|---|
| Backend | C# 14, .NET 10 (LTS), ASP.NET Core Web API |
| Architecture | Clean Architecture, CQRS, MediatR, Repository + Unit of Work |
| Persistence | Entity Framework Core 10, Npgsql (PostgreSQL) |
| Validation | FluentValidation via a MediatR pipeline behavior |
| Security | JWT bearer authentication, role-based + ownership authorization |
| Docs | Swagger / OpenAPI with JWT support |
| Logging | Serilog, structured |
| Testing | xUnit, FluentAssertions, Moq |
| Frontend | Angular 20, TypeScript, standalone components, signals, RxJS, typed HTTP services |
| Runtime | Docker Compose — frontend (nginx), API, PostgreSQL |

### One deliberate substitution

The assignment brief lists Next.js/React and PostgreSQL/MongoDB, and allows
"equivalent technologies suitable for the project".

- **Angular 20 instead of Next.js/React.** A single-page app with the same
  responsibilities: routing, reactive forms with validation, and API integration.

### One deliberate deviation from the brief

The brief names **Microsoft SQL Server** and explicitly says not to replace it.
This project uses **PostgreSQL 16** (via Npgsql) instead, for two practical reasons:

- The target reviewer environment is Windows, where SQL Server normally requires
  either a local install or the amd64-only Linux container; PostgreSQL's official
  `postgres` image runs everywhere without emulation.
- The recruitment brief's "equivalent technologies suitable for the project" clause
  covers this: PostgreSQL is a relational database implementing the same schema,
  relationships and indexes, and the EF Core provider abstraction kept every
  repository, migration and query provider-agnostic in practice.

The migration path back to SQL Server is a provider swap: replace the
`Npgsql.EntityFrameworkCore.PostgreSQL` package with
`Microsoft.EntityFrameworkCore.SqlServer`, update the connection string in
`appsettings.json`/`.env`, and regenerate the EF Core migration. No domain,
application, controller or frontend code changes.

### Two version pins made for licensing reasons

- **MediatR 12.x**, not 13+. MediatR moved to a dual RPL-1.5/commercial licence under
  Lucky Penny Software. This project would qualify for the free Community tier, but
  12.x is plain Apache-2.0 and raises no question at all.
- **FluentAssertions 7.x**, not 8+. Version 8 moved to the Xceed Community License and
  is paid for commercial use. Version 7 remains Apache-2.0.

---

## Roles

| Admin | Teacher | Student |
|---|---|---|
| Manage users | Create, update, delete assignments | View assignments for their class |
| Manage classes and subjects | Set title, description, deadline, max marks | View details and deadline |
| Assign teachers to class + subject | Save as draft or publish | Submit an answer, with attachments |
| View all assignments and submissions | View submissions | Update before the deadline |
| | Award marks and feedback | View status, marks and feedback |
| | Return a submission for revision | |

---

## Features

**Assignments**
- Teachers author assignments for a class + subject pair they are allocated to,
  with title, description, max marks, and a future deadline; save as draft or publish.
- Publish is the only way to open an assignment for submissions; only the owning
  teacher can edit or delete an assignment (and only while it has no submissions).
- Students see assignments published for their own class; everyone sees the deadline.

**Submissions & grading**
- One submission per student per assignment — submitting again edits the existing
  answer, with an optional file attachment (≤ 10 MB), until the deadline.
- Teachers grade (marks clamped to the assignment maximum, with feedback) and can
  return a submission for revision, which reopens it for the student past the deadline.
- Students view their own status, marks and feedback; attachment downloads are
  ownership-checked (student/own files, teacher/own assignments, admin/all).

**Administration**
- Users, classes, subjects, and teacher-to-class-subject allocations; password resets.
- Full visibility of assignments and submissions for oversight.

**Platform**
- JWT role-based authentication, PBKDF2 password hashing, Swagger/OpenAPI.
- CQRS + MediatR, FluentValidation, structured Serilog logging, ProblemDetails errors.
- 198 automated tests; a complete Docker Compose stack with health checks and seeding.

---

## Architecture

```text
Api  ──►  Application  ──►  Domain
 │             ▲
 └──►  Infrastructure ──┘
```

- **Domain** — entities, value objects, invariants, domain events. Has *zero* package
  references, which is what makes the dependency rule impossible to break by accident.
- **Application** — CQRS commands, queries, handlers, validators, and the interfaces
  (`IUnitOfWork`, `I*Repository`, `ICurrentUser`, `IFileStorage`) that Infrastructure implements.
- **Infrastructure** — EF Core, PostgreSQL (Npgsql), repositories, JWT, password hashing, file storage.
- **Api** — controllers that do nothing but `sender.Send(request, ct)`, plus middleware and DI wiring.

`Api` references `Infrastructure` only to register implementations in `Program.cs`.
That is the standard composition-root exception; no controller or middleware touches
an Infrastructure type.

### Key design decisions

- **Business rules live in the domain entities**, not in controllers or handlers.
  Publishing, deadline enforcement, editability and the grading ceiling are all
  enforced by `Assignment` and `Submission`.
- **The grading ceiling crosses two aggregates.** `Submission.Grade` takes the
  assignment's maximum marks as a parameter rather than holding a reference to the
  assignment or duplicating the value. The rule stays in the domain and stays testable
  without a database.
- **Deadlines are absolute, with one explicit exception.** A teacher can return a
  submission for revision, and while it is reopened that student may edit past the
  deadline. The exception is recorded in the submission's status, so it is auditable.
- **Ownership is checked in the application layer.** `[Authorize(Roles = "Teacher")]`
  is not enough — one teacher must not be able to edit or grade another's work.
- **Identity always comes from the validated JWT**, never from a request body.

### Database design

Seven tables, owned by EF Core (one initial migration), with unique indexes on the
natural keys and composite indexes on the hot query paths:

```text
Users ───┐                      Classes
  ClassId ┘ FK ──►  Classes     Subjects
  Email unique

Assignments  FK ClassId ──► Classes
             FK SubjectId ─► Subjects
             FK TeacherId ─► Users
             (ClassId, Status) and (TeacherId, Status) composite indexes

TeacherAssignments  FK TeacherId ─► Users     unique (TeacherId, ClassId, SubjectId)
                    FK ClassId ───► Classes   — "a teacher may only author for allocated pairs"
                    FK SubjectId ─► Subjects

Submissions  FK AssignmentId ─► Assignments   unique (AssignmentId, StudentId)
             FK StudentId ────► Users         — "one submission per student per assignment"

SubmissionAttachments  FK SubmissionId ─► Submissions
```

Every foreign key is enforced; cascade rules follow EF defaults (e.g. deleting an
assignment removes its submissions and their attachments; deleting a class cascades
to its users and teacher assignments). The full column list is in
[`docs/IMPLEMENTATION_PLAN.md`](docs/IMPLEMENTATION_PLAN.md) and in the migration
`src/AssignmentManagement.Infrastructure/Persistence/Migrations/`.

### CQRS surface

Every controller action is a thin `sender.Send(...)` over MediatR; commands mutate,
queries read, and validators gate every command.

| Feature | Commands | Queries |
|---|---|---|
| Auth | `Login` | — |
| Users | `Create`, `Update`, `UpdatePassword`, `Delete` | `GetUsers`, `GetUserById` |
| Classes | `Create`, `Update`, `Delete` | `GetClasses` |
| Subjects | `Create`, `Update`, `Delete` | `GetSubjects` |
| TeacherAssignments | `Create`, `Delete` | `GetTeacherAssignments`, `GetMyTeacherAssignments` |
| Assignments | `Create`, `Update`, `Delete`, `Publish` | `GetAssignments`, `GetAssignmentById` |
| Submissions | `Create`, `Update`, `Grade`, `Return` | `GetMySubmissions`, `GetAssignmentSubmissions`, `GetSubmissionById`, `DownloadAttachment` |

Ownership is enforced inside the handlers (not just by `[Authorize]`): teachers act
only on their own assignments, students only on their own submissions, admins on
everything.

---

## Build progress

| Phase | Scope | Status |
|---|---|---|
| 1 | Solution skeleton, domain model, domain unit tests | **Complete** |
| 2 | EF Core, repositories, Unit of Work, migrations, seed data | **Complete** |
| 3 | JWT auth, password hashing, error middleware, Serilog, Swagger | **Complete** |
| 4 | CQRS features, validators, controllers | **Complete** |
| 5 | Application unit tests and integration tests | **Complete** |
| 6 | Angular 20 frontend | **Complete** |
| 7 | Dockerfiles, Compose, health checks | **Complete** |
| 8 | Documentation, demo credentials, final security review | **Complete** |

### What exists today

```text
src/AssignmentManagement.Domain/         Domain model, value objects, exceptions, events
src/AssignmentManagement.Application/    CQRS (MediatR), validators, DTOs, abstractions
src/AssignmentManagement.Infrastructure/ EF Core + Npgsql, repositories, JWT, PBKDF2 hashing,
                                        file storage, seeder
src/AssignmentManagement.Api/            Controllers, exception middleware, Serilog, Swagger,
                                        JWT bearer auth, health checks
tests/AssignmentManagement.UnitTests/    152 tests: domain, Application handlers/validators, hashing
tests/AssignmentManagement.IntegrationTests/ 46 tests: auth, assignments, submissions, access control
frontend/assignment-management-ui/       Angular 20 SPA: standalone components, signals, RxJS,
                                        typed HTTP services, functional interceptors/guards
docker/backend.Dockerfile               Multi-stage .NET 10 API image (restore -> publish -> runtime)
docker/frontend.Dockerfile              Multi-stage Angular image (node build -> nginx)
docker/frontend/nginx.conf              Serves the SPA and proxies /api to the API
docker-compose.yml                      postgres + api + frontend with health checks
docker-compose.prod.yml                 production stack for AWS EC2 (GHCR images)
.env.example                            Environment template (DB, JWT, ports) - never commit .env
.github/workflows/ci-cd.yml             CI (build/test) + CD (GHCR images -> SSH deploy)
deploy/ec2-provision.sh               idempotent host bootstrap (Docker + .env generation)
deploy/ec2-update.sh                   remote deploy step (pull images + health wait)
docs/IMPLEMENTATION_PLAN.md             Design, database schema and phase-by-phase plan
```

### Frontend structure

The SPA is an Angular 20 standalone-app project under
`frontend/assignment-management-ui/`. It mirrors the backend's CQRS resources
with typed services and models, and keeps the browser out of the security
boundary: guards and role-aware menus are navigation UX only, while every API
request is still authorized server-side.

```text
src/app/core/        models, auth (signals + localStorage), HTTP services,
                     functional auth interceptor, guards
src/app/features/    login, dashboard, and admin/teacher/student areas
src/app/layout/      authenticated shell with role-aware navigation
src/app/shared/      loading/empty/error components, status & file-size pipes
```

Key points:

- **Signals** drive authentication state and reactive lists; components use
  `ChangeDetectionStrategy.OnPush`.
- **Reactive forms** mirror the backend domain rules (title/description lengths,
  marks ceiling, future-deadline validation, optional password reset).
- **Multipart submissions** are sent as `FormData`, matching the API's
  `[FromForm]` binding; attachments download as blobs.
- **`GET /api/teacher-assignments/mine`** (added for the frontend) lets a teacher
  pick only their own allocated class/subject pairs when authoring an assignment.
- **Dev/prod environments** target `http://localhost:5105` and `''` respectively,
  so the same build works behind the nginx proxy planned for Docker.

---

## Running with Docker (recommended)

The whole stack runs from the repository root with Docker Compose. **Docker with
Compose is the only requirement** — no .NET SDK, Node.js or database install needed.

```bash
# 1. Configure environment (one time). Copy the example; the placeholders work as-is
#    for local development but you must change the passwords before real use.
cp .env.example .env

# 2. Build and start everything
docker compose up --build

# 3. Stop everything (the database and uploads volumes are kept)
docker compose down

# Reset the database and uploads completely
docker compose down -v
```

The compose services are `postgres`, `api` and `frontend`:

| Service | Reachable at | Notes |
|---|---|---|
| Frontend | http://localhost:8080 | nginx; `/api` is proxied to the API |
| API / Swagger | http://localhost:5105/swagger | OpenAPI UI with JWT "Authorize" |
| PostgreSQL | localhost:5433 | mapped from 5432; set `POSTGRES_PORT=5432` to use the standard port |

Useful commands:

```bash
docker compose logs -f              # tail logs from all services
docker compose logs api             # API logs only
docker compose restart api          # restart one service
docker compose up --build frontend  # rebuild + restart one service
docker compose up -d                # detached mode
docker compose ps                   # status incl. health
```

### How Docker works here

- **Backend** (`docker/backend.Dockerfile`): multi-stage build — `dotnet restore`
  (cached on the csproj layer), `dotnet publish -c Release`, then a minimal
  `aspnet:10.0` runtime image with `curl` for the health check. The API listens on
  8080 inside the container.
- **Frontend** (`docker/frontend.Dockerfile`): `npm ci` → `ng build` in a Node 24
  image, then the bundle is served by nginx. nginx also proxies `/api/*` to the API
  container, so the browser talks to one origin and CORS is not involved. SPA routes
  fall back to `index.html`.
- **Health checks**: PostgreSQL runs `pg_isready`; the API exposes `GET /health`.
  Compose gates startup with `depends_on: condition: service_healthy`, so the API
  only migrates once the database is up, and the frontend starts after the API.
- **Migrations & seed data**: the API applies EF Core migrations on startup
  (`Database.MigrateAsync()`), then runs the idempotent seeder. This is the
  documented strategy for this single-instance app — a dedicated migration job is
  unnecessary here, but the choice is explicit in `Program.cs`. The first API start
  therefore creates the schema and demo data automatically.
- **Attachments** are stored on the `uploads` volume mounted at `/app/uploads`.
- **Environment**: all configuration (database, JWT, ports) comes from `.env`
  through `docker-compose.yml`. `.env` is git-ignored; only `.env.example` is
  committed. Never put real secrets in `.env.example`.

### Demo credentials

The seeder creates these development-only accounts:

| Role | Email | Password |
|---|---|---|
| Admin | admin@school.edu | Admin@123 |
| Teacher | teacher@school.edu | Teacher@123 |
| Student | student@school.edu | Student@123 |

The student belongs to a seeded class with a published assignment and a sample
submission, so each role has something to work with on first login.

---

## Deploying to AWS (CI/CD)

The repo ships with a GitHub Actions pipeline that builds and tests every push, then
deploys automatically to a single EC2 host running the same Docker Compose stack as
local development.

### How it works

```text
push to main ─► GitHub Actions
                 ├─ ci     : dotnet build + 198 tests (against a real PostgreSQL
                 │          service container) + Angular production build
                 └─ deploy : build api + frontend images, push to GHCR
                             (ghcr.io/sojib444/onnorokom/{api,frontend}),
                             then SSH to EC2, auto-provision the host, and run
                             docker compose up
```

- **`docker-compose.prod.yml`** is the production stack. It pulls prebuilt images
  from GHCR (no build context on the server), reads configuration from
  `~/onnorokom/.env`, maps nginx to port 80, and keeps the API on a loopback-only
  port so only the frontend is internet-facing.
- **Provisioning is automatic.** `deploy/ec2-provision.sh` runs on the host before
  every deploy and is idempotent: it installs Docker only if missing and generates
  `~/onnorokom/.env` (random Postgres password + JWT secret) only once. A brand-new
  instance needs nothing but the SSH key and port 80 open.
- **Migrations and seeding** run on API startup (`Program.cs`), so a deploy is just
  "pull the new image and restart". The database and uploads live on persistent
  volumes that survive redeploys.
- **Rollback** = redeploy the previous `main` commit (or run
  `docker compose -f docker-compose.prod.yml up -d` with the older image tag).

### One-time setup

**1. EC2 instance** (Ubuntu 24.04, any size; open **TCP 80** and **TCP 22** in the
security group):

- When launching the instance, create a **key pair** and download the `.pem` file —
  this is the SSH identity the pipeline uses to deploy (e.g.
  `home_barud_aws_dev.pem`). Store it somewhere safe, outside the repo.
- **Windows note:** Windows OpenSSH rejects a private key it considers
  "UNPROTECTED" until the file grants access only to your user. Fix it once with:

  ```powershell
  icacls.exe home_barud_aws_dev.pem /inheritance:r
  icacls.exe home_barud_aws_dev.pem /grant:r "$($env:USERDOMAIN)\$($env:USERNAME):R"
  ```

  (run an elevated shell if the file's ACL is locked by another machine's SID).
  The connection can then be tested with
  `ssh -i home_barud_aws_dev.pem ubuntu@<EC2-PUBLIC-IP>`.

> No software needs to be installed on the instance beforehand — the deploy job
> installs Docker and generates the configuration automatically on first deploy.
> If the GitHub repo is **private**, the host also needs `GHCR_USER` and
> `GHCR_TOKEN` (a fine-grained PAT with `read:packages`) in `~/onnorokom/.env` so
> it can pull the images.

**2. GitHub repository secrets** (Settings → Secrets and variables → Actions):

| Secret | Value |
|---|---|
| `AWS_EC2_HOST` | public IP or DNS of the EC2 instance |
| `AWS_EC2_USER` | SSH user, usually `ubuntu` |
| `AWS_EC2_SSH_KEY` | the **entire contents** of the key pair's `.pem` file (e.g. `home_barud_aws_dev.pem`), pasted as the value |
| `AWS_EC2_PORT` | SSH port (optional, defaults to 22) |

**3. Push to `main`.** The pipeline runs CI, builds both images, pushes them to
GitHub Container Registry, copies `docker-compose.prod.yml` and the `deploy/` scripts
to the host, provisions it, and starts the stack. The app is then live at
`http://<EC2-PUBLIC-IP>/`.

### Day-to-day operations

```bash
# On the EC2 host, from ~/onnorokom:
docker compose -f docker-compose.prod.yml ps             # status incl. health
docker compose -f docker-compose.prod.yml logs -f api    # API logs
docker compose -f docker-compose.prod.yml logs -f frontend
docker compose -f docker-compose.prod.yml down           # stop (volumes kept)
docker compose -f docker-compose.prod.yml down -v        # stop + wipe db/uploads
```

A health check probes `http://127.0.0.1:5105/health` on the host; the deploy fails
loudly (with API logs) if it does not pass. See `deploy/ec2-update.sh`.

### Not yet covered (deliberately)

- **HTTPS** — the frontend currently serves plain HTTP on :80. Add nginx TLS (e.g.
  Let's Encrypt via certbot) in front of the frontend service before exposing real
  student data.
- **Multi-instance scaling** — uploads are a node-local volume; a second host needs
  object storage (S3) behind `IFileStorage`.
- **Backups** — the Postgres volume is not snapshotted automatically. Add a nightly
  `pg_dump` to S3 (or move to RDS) for real data.

---

## Running locally without Docker

Requires the **.NET 10 SDK** and **Node.js 20+** (Node 24 used here), plus a
PostgreSQL 16 instance on `localhost:5432` (the default development connection
string in `appsettings.json`).

```bash
# Backend
dotnet restore
dotnet build
dotnet test
dotnet run --project src/AssignmentManagement.Api   # API on http://localhost:5105
# PostgreSQL must already exist; the API migrates and seeds on startup.

# Frontend (from frontend/assignment-management-ui)
npm install
npm run build        # production bundle into dist/
npm start            # dev server on http://localhost:4200; calls the API directly
                     # at http://localhost:5105 (CORS already allows localhost:4200)
```

> The integration tests (46) connect to a local PostgreSQL at `localhost:5432` and
> create/drop the `AssignmentManagement_Test` database, so run them with PostgreSQL
> available — or simply use the Docker Postgres (`docker compose up -d postgres`)
> and point the tests at it via `localhost:5433` if 5432 is already in use.

---

## Assumptions

1. A student belongs to exactly one class; multi-class enrolment is out of scope.
2. One submission per student per assignment — revising edits the existing record.
   Previous versions are not retained.
3. "Application-level settings" is interpreted as user, class, subject and
   teacher-allocation administration. There is no separate settings table.
4. An assignment targets one class *and* one subject, and a teacher may only author
   for pairs they have been allocated.
5. All timestamps are stored and compared in UTC; the UI renders local time.
6. Attachments are stored on a local volume behind an `IFileStorage` interface.
   Object storage is the production path but is not implemented.
7. There are no email or in-app notifications.
8. Grading is manual and per submission; there is no bulk grading.

## Security

Final review findings (phase 8). The controls below are implemented and covered by
tests; the notes that follow list the deliberate, documented limitations.

**Implemented and verified**
- **Authentication** — HS256 JWTs with a 60-minute lifetime; issuer, audience,
  lifetime and signing key are all validated. The signing secret is required to be
  at least 32 characters and must come from configuration or `JWT__SECRET`.
- **Password storage** — PBKDF2-HMAC-SHA256, 100,000 iterations, 16-byte random salt,
  constant-time comparison. Passwords are never logged or returned.
- **No user enumeration** — login returns the same generic "email or password is
  incorrect" message whether or not the account exists.
- **Authorization** — role checks on endpoints plus ownership checks in handlers
  (teachers only their own assignments, students only their own submissions). Identity
  is read exclusively from the validated JWT, never from a request body.
- **Input validation** — FluentValidation on every command (lengths, marks ceiling,
  future deadlines, password policy) returning RFC 7807 ProblemDetails.
- **Injection & data access** — all persistence is EF Core LINQ over parameterized
  queries; no raw SQL or string-built queries anywhere.
- **File uploads** — stored under generated, non-guessable keys; a path-traversal
  guard blocks keys escaping the storage root; nginx caps the body at 12 MB.
- **CORS** — an explicit allow-list (`http://localhost:4200`), never `AllowAnyOrigin`.
- **Error handling** — a central middleware maps exceptions to ProblemDetails; stack
  traces are never returned and 5xx responses are generic.
- **Logging** — structured Serilog; request bodies, passwords and tokens are never
  logged.
- **Secrets** — `.env` and local config overrides are git-ignored; only the
  placeholder `.env.example` is committed, and appsettings defaults are explicitly
  named `DevOnly_*`.

**Documented limitations**
- **No refresh tokens** — sessions last exactly one access token (60 minutes).
- **No login rate limiting or account lockout.**
- **No attachment type allow-list** — the size cap is enforced (nginx/Kestrel) but any
  file type is accepted; files are downloaded, never executed, and served with the
  content type provided at upload.
- **Swagger is enabled by default** (including the Docker deployment) to make the API
  easy to evaluate; disable it with `Api__EnableSwagger=false` in production.
- **`AllowedHosts` is `*`** — appropriate for the local/Docker deployment; pin the
  expected host names in production.

## Known limitations

- Access tokens do not refresh; sessions last 60 minutes.
- No notifications of any kind.
- Attachments are node-local and would not survive a multi-instance deployment.
- No full-text search across assignments or submissions.
- Administrators deliberately cannot author or grade assignments.
- The database is PostgreSQL, a deliberate deviation from the brief's SQL Server
  requirement (see [Technology stack](#technology-stack) for rationale and the
  migration path back).
- Angular 20 leaves LTS in November 2026; it is pinned here for consistency with the
  project brief.

## License

Written as a recruitment exercise for OnnoRokom Projukti Limited.
