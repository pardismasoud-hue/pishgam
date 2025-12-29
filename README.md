# MSP Platform (Local, No Docker)

## Prerequisites

- Windows 10/11
- .NET 8 SDK
- Node.js 18+
- SQL Server (local instance reachable at `DBTEST`)

## Backend (ASP.NET Core)

1. Create `backend/src/WebApi/appsettings.Development.json` (already included in Phase 0).
2. From `backend/src/WebApi`, run:

```powershell
dotnet restore
dotnet run
```

The API is expected to run at `http://localhost:5000` in future phases.

## Frontend (Vue 3 + Vite)

1. Create `frontend/.env` (already included in Phase 0).
2. From `frontend`, run:

```powershell
npm install
npm run dev
```

