# Catered-By-Let-uwane

## Quick Start (Local)
From the repo root:

```bash
cd cateredByLetsuwi
dotnet ef database update
dotnet run
```

App runs with the default route: `/{controller=Home}/{action=Index}/{id?}`.

## Admin Login
Admin auth uses cookie login with `AdminOnly` policy.

Set credentials using environment variables:

```bash
export AdminAccount__Username="admin"
export AdminAccount__Password="change-me-strong-password"
```

Then open `/Auth/Login` and sign in.

## Database / Connection String
Default connection string is SQLite (`Data Source=catering.db`).
Override via env var when needed:

```bash
export ConnectionStrings__DefaultConnection="Data Source=/absolute/path/catering.db"
```

For SQL Server (if used in another environment):

```bash
export ConnectionStrings__DefaultConnection="Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True"
```

## Migrations
Apply migrations:

```bash
dotnet ef database update
```

Create a new migration:

```bash
dotnet ef migrations add <MigrationName>
```

## Deployment Notes (Short)
- Do not commit `*.db`, `bin/`, `obj/`, or secrets.
- Provide `AdminAccount__Username` and `AdminAccount__Password` in host environment.
- Provide `ConnectionStrings__DefaultConnection` per environment.
- Run `dotnet ef database update` during deployment before serving traffic.

## Request Flow (Diagram)
```text
Browser
  |
  v
ASP.NET Core MVC (Controllers/Views)
  |
  +--> Cookie Auth (/Auth/Login) --> AdminOnly policy
  |
  +--> EF Core (ApplicationDbContext)
           |
           v
         SQLite
```

## Render Deployment (Docker)
Use a **Web Service** with **Runtime = Docker**.

### Required Render Environment Variables
```bash
ASPNETCORE_ENVIRONMENT=Production
AdminAccount__Username=marothi
AdminAccount__Password=1143828wits
ConnectionStrings__DefaultConnection=Data Source=/var/data/catering.db
```

### Persistent Disk (SQLite)
Attach a persistent disk and mount it to:

```bash
/var/data
```

This keeps `catering.db` across deploys/restarts.

### Notes
- Render sets `PORT`; Docker `CMD` binds to `0.0.0.0:$PORT` automatically.
- After first deploy, open `/Auth/Login` to verify admin access.
