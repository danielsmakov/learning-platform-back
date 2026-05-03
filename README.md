# Learning Platform — backend

ASP.NET Core 9, PostgreSQL, JWT, Hangfire, SignalR.

## Локальный запуск

1. Скопируйте `.env.example` в `.env`, задайте `ConnectionStrings__DefaultConnection`, `Jwt__Key`, при необходимости `Seed__*`.
2. `dotnet ef database update` (из каталога проекта, при установленном EF CLI).
3. `dotnet run` — Swagger UI: `/api/docs`.

## SignalR: уведомления родителю (G4 / G5)

### Единая модель (G4)

Любое событие для родителя:

1. **Сначала** сохраняется строка в таблице `Notifications` (история и офлайн-клиенты).
2. **Затем** при активном WebSocket вызывается push в хаб (событие `notification`, см. `ParentNotificationPublisher.HubEventName`).

Типы в БД и в payload совпадают с enum **`NotificationType`** (число в JSON):

| Сценарий (ТЗ G4) | `NotificationType` |
|------------------|----------------------|
| (1) Смена программы / сложности у ребёнка | `AdaptiveProgramChange` (4) |
| (2) Бейдж / ачивка | `BadgeEarned` (5) |
| (3) Повышение уровня (XP / level) | `LevelUp` (6) |
| (4) Успешное завершение юнита | `UnitCompleted` (7) |
| (5) Streak at-risk (расписание) | `StreakReminder` (2) |

Дополнительно (тот же pipeline БД → хаб): завершение **урока** — `Milestone` (1); еженедельное резюме — `WeeklySummary` (3).

Напоминание по стрику (п.5): Hangfire cron **`0 18 * * *`** — **18:00 по локальному времени машины**, где запущен процесс (и Hangfire Server); при деплое учитывайте TZ сервера.

### Handshake (G5)

1. Клиент делает **первый HTTP-запрос negotiate** к тому же origin, что и API (Kestrel проксирует SignalR).
2. Затем поднимается **WebSocket** для постоянного канала.

Для SPA рекомендуется передавать JWT в **query** как `access_token=…` при подключении к хабу (удобно для браузера). На бэкенде `JwtBearerEvents.OnMessageReceived` в `Program.cs` подставляет токен из query для путей `/hubs/*`, либо из заголовка `Authorization: Bearer …` (удобно для не-браузерных клиентов).

Пример базового URL хаба: **`/hubs/parent-notifications`**. Роль подключения: **Parent**.

### Payload события `notification`

Совпадает с `ParentNotificationPublisher.ParentNotificationPayload`: `id`, `type` (int = `NotificationType`), `title`, `body`, `childId` (nullable guid), `createdAt`, `isRead`.

### Безопасность

**Не логируйте** полный URL negotiate/WebSocket с `access_token` в query (утечка в access-logs, скриншоты, Sentry). Для отладки маскируйте query или логируйте только path.

## RBAC: родитель, ребёнок, админ (H3)

Краткая матрица после появления карты куррикулума, SignalR и новых GET каталога.

| Область | Parent | Child | Admin | Без JWT |
|--------|--------|-------|-------|---------|
| `GET …/children/...`, карта, прогресс, бейджи ребёнка | только свои дети (`RequireChildAccess`) | только **свой** `childId` = id из токена | любой ребёнок | 401 |
| Каталог `GET /units`, `/units/{id}`, `/lessons`, `/lessons/{id}`, упражнения | `childId` (свой ребёнок) или явный `programId` | только программа из профиля ребёнка | явный `programId` | обязателен `programId` |
| `all=true` (черновики каталога) | запрещено | запрещено | разрешено | запрещено |
| CRUD программ, юнитов, уроков, упражнений | только Admin | 403 | да | 401 |
| `POST …/exercises/…/submit`, resume, complete lesson | через `RequireChildAccess` | свой ребёнок | да | 401 |
| `GET/POST …/notifications` | да (`ParentId` = из JWT) | **нет** (роль не Parent) | **нет** (см. ниже) | 401 |
| SignalR `/hubs/parent-notifications` | да | нет (политика роли) | нет | нет |

Уведомления в REST **только для родителя**: записи в `Notifications` всегда с `ParentId` родителя; админский JWT не подставляется как родитель.

Коды отказов: см. A6 в OpenAPI (`403` / `401` / `422`).

## REST

Контракты и описание SignalR вынесены в **OpenAPI** (`/api/docs` → Swagger v1). Примеры каталога с `Accept-Language`: `examples/catalog-client.http`.
