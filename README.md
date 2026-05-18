# Learning Platform — backend

## Project overview

Backend for a children’s literacy learning web platform. Parents create profiles for their children; learners follow programs matched to their level (units → lessons → exercises). Completing work earns XP and raises level; streaks track consecutive active days, badges reward milestones, and parents get notifications about important events.

Stack: **ASP.NET Core 9**, **PostgreSQL**, **EF Core** (migrations), **JWT** + refresh, **Hangfire**, **SignalR**.

---

## Deployment (public access)


|                |                                |
| -------------- | ------------------------------ |
| **Base URL**   | `http://93.170.72.84`          |
| **Swagger UI** | `http://93.170.72.84/api/docs` |


The image is built by the **“Deploy backend (manual)”** workflow (`deploy-backend.yml`), pushed to GHCR, and deployed to a VPS over SSH — see `.github/workflows/deploy-backend.yml`. On the server, `.env` next to Compose sets variables such as `POSTGRES_CONNECTION_STRING` and `Jwt__Key`. **nginx** on `:80` typically proxies to the API on `:8080`.

---

## Local run (without Docker)

1. Install [.NET SDK 9](https://dotnet.microsoft.com/download) and [EF Core CLI](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) (`dotnet tool install --global dotnet-ef`).
2. Run **PostgreSQL** and create a database.
3. Copy `.env.example` to `.env` in the repo root.
4. Set `**ConnectionStrings__DefaultConnection`** and `**Jwt__Key**` (see table below).
5. Apply migrations: `dotnet ef database update`
6. Run: `dotnet run` from the folder that contains `learning-platform-back.csproj` → Swagger at `**https://localhost:<port>/api/docs**` (port is printed when the app starts).

This project does **not** use SQLite — PostgreSQL only.

---

## Running the API in Docker (server-like)

In `.env` next to `docker-compose.yml` you need at least `**BACKEND_IMAGE`** (GHCR tag or local build), `**POSTGRES_CONNECTION_STRING**`, and JWT secrets — see comments in `docker-compose.yml`. Compose maps the connection string into `**ConnectionStrings__DefaultConnection**` inside the container.

---

## Environment variables

In .NET, nested config keys use `Section__Key`. The table below covers the minimum and common options; alternate names are listed in `**.env.example**` (including adaptive-threshold aliases).


| Variable                                  | Purpose                                                                                                |
| ----------------------------------------- | ------------------------------------------------------------------------------------------------------ |
| `ConnectionStrings__DefaultConnection`    | PostgreSQL connection string for local `dotnet run` and non-Docker runs.                               |
| `POSTGRES_CONNECTION_STRING`              | Same role for **docker-compose**: injected as `ConnectionStrings__DefaultConnection` in the container. |
| `Jwt__Key`                                | Secret used to sign access tokens (long random string; do not commit).                                 |
| `Seed__AdminEmail`, `Seed__AdminPassword` | Optional initial admin for seeding.                                                                    |


Keep secrets in `.env`, CI secrets, or the VPS — not in the repo.

---

## Tests

From the directory that contains `learning-platform-back.sln`:

```bash
dotnet test
```

Line coverage (Coverlet is referenced by the test project):

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Report output: `TestResults/<guid>/coverage.cobertura.xml`; HTML reports are easy to generate with [ReportGenerator](https://github.com/danielpalme/ReportGenerator).

---

## SignalR: parent notifications

### Single pipeline

Every parent-facing event:

1. **First** a row is inserted into `Notifications` (history / offline clients).
2. **Then**, if a WebSocket is active, the hub fires a push (`notification` event — see `ParentNotificationPublisher.HubEventName`).

DB types and payload use enum `**NotificationType`** (integer in JSON):


| Scenario (spec G4)                            | `NotificationType`          |
| --------------------------------------------- | --------------------------- |
| (1) Program / difficulty change for the child | `AdaptiveProgramChange` (4) |
| (2) Badge / achievement                       | `BadgeEarned` (5)           |
| (3) Level up (XP / level)                     | `LevelUp` (6)               |
| (4) Unit completed successfully               | `UnitCompleted` (7)         |
| (5) Streak at risk (scheduled)                | `StreakReminder` (2)        |


Also on the same DB → hub path: **lesson** completed — `Milestone` (1); **weekly** summary — `WeeklySummary` (3).

Streak reminder (item 5): Hangfire cron — **18:00 in the server’s local timezone** where the process (and Hangfire server) runs; set VPS timezone accordingly.

### Handshake

1. The client performs the **first HTTP negotiate** request to the same origin as the API (Kestrel proxies SignalR).
2. Then a **WebSocket** is opened for the persistent channel.

For SPAs, pass the JWT in the **query string** as `access_token=…` when connecting to the hub (browser-friendly). On the backend, `JwtBearerEvents.OnMessageReceived` in `Program.cs` reads the token from the query for `/hubs/*`, or from `Authorization: Bearer …` (non-browser clients).

Example hub base path: `**/hubs/parent-notifications`**. Required role: **Parent**.

### `notification` payload shape

Matches `ParentNotificationPublisher.ParentNotificationPayload`: `id`, `type` (int = `NotificationType`), `title`, `body`, `childId` (nullable guid), `createdAt`, `isRead`.

### Security

**Do not log** full negotiate/WebSocket URLs that include `access_token` in the query (leaks into access logs, screenshots, Sentry). When debugging, strip the query or log only the path.

---

## RBAC: parent, child, admin (H3)

Quick matrix after curriculum map, SignalR, and catalog GETs.


| Area                                                                        | Parent                                        | Child                             | Admin                | No JWT               |
| --------------------------------------------------------------------------- | --------------------------------------------- | --------------------------------- | -------------------- | -------------------- |
| `GET …/children/...`, map, progress, child badges                           | own children only (`RequireChildAccess`)      | **only own** `childId` from token | any child            | 401                  |
| Catalog `GET /units`, `/units/{id}`, `/lessons`, `/lessons/{id}`, exercises | `childId` (own child) or explicit `programId` | child’s program only              | explicit `programId` | `programId` required |
| `all=true` (draft catalog)                                                  | denied                                        | denied                            | allowed              | denied               |
| CRUD programs, units, lessons, exercises                                    | Admin only                                    | 403                               | yes                  | 401                  |
| `POST …/exercises/…/submit`, resume, complete lesson                        | via `RequireChildAccess`                      | own child                         | yes                  | 401                  |
| `GET/POST …/notifications`                                                  | yes (`ParentId` from JWT)                     | **no** (not Parent role)          | **no** (see below)   | 401                  |
| SignalR `/hubs/parent-notifications`                                        | yes                                           | no (role policy)                  | no                   | no                   |


REST notifications are **parent-only**: `Notifications` rows always use the parent’s `ParentId`; an admin JWT is not treated as a parent.

Error semantics: see A6 in OpenAPI (`403` / `401` / `422`).

---

## REST and OpenAPI

**OpenAPI 3** is served via Swashbuckle; interactive docs and “try it” live at `**/api/docs`**. Catalog examples with `Accept-Language`: `examples/catalog-client.http`.