# Scoundrel R1 Basic Setup

Dieses Repository enthält ein bewusst schlankes Start-Setup für R1:

- **Frontend:** React + TypeScript + Vite (mobile-first)
- **API:** ASP.NET Core 10 Minimal API
- **Datenbank:** SQLite ohne EF Core (direkt via `Microsoft.Data.Sqlite`)
- **Orchestrierung:** Docker Compose

## Architektur (R1)

### Frontend
- React
- TypeScript
- Vite
- TanStack Query für API Calls
- Mobile-first Styling mit einfachem CSS

### Backend
- ASP.NET Core Web API (Minimal APIs)
- Swagger / OpenAPI aktiviert
- CORS für lokale Frontend-Hosts konfiguriert

### Datenmodell
- `Profile`
- `Ruleset`
- `GameSession`
- `Achievement`
- `LeaderboardEntry` (aus Sessions berechnet)

## Schnellstart

```bash
docker compose up --build
```

Danach erreichbar unter:

- Frontend: http://localhost:5173
- API: http://localhost:8080
- Swagger UI: http://localhost:8080/swagger

## Hinweise

- SQLite-Datei liegt im Docker-Volume `scoundrel-data`.
- Das Setup ist auf spätere Migration zu PostgreSQL vorbereitet (durch klare API-/DB-Schicht im Backend).
