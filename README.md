# SSPMS — SmartSkill Performance Monitoring System

A full-stack, production-ready gamified training performance platform.

**Backend:** .NET 9 (Clean Architecture) · **Frontend:** Angular 19 · **DB:** PostgreSQL (Neon) · **Real-time:** SignalR

---

## Quick Start

### Prerequisites
| Tool | Version |
|------|---------|
| .NET SDK | 9.0+ |
| Node.js | 20+ |
| PostgreSQL | Neon cloud (no local install needed — use connection string from Neon dashboard) |
| Angular CLI | `npm i -g @angular/cli` |

### 1. Backend Setup

```bash
cd backend

# Copy and configure settings
cp src/SSPMS.API/appsettings.json src/SSPMS.API/appsettings.Development.json
# Edit: ConnectionStrings, Jwt:Secret, Email:*

# Apply migrations and seed data
dotnet ef database update -s src/SSPMS.API -p src/SSPMS.Infrastructure

# Run the API (http://localhost:5000)
dotnet run --project src/SSPMS.API
```

### 2. Frontend Setup

```bash
cd frontend
npm install

# Run dev server (http://localhost:4200)
ng serve
```

Open [http://localhost:4200](http://localhost:4200).

---

## Project Structure

```
SSPMS/
├── backend/
│   └── src/
│       ├── SSPMS.Domain/          # Entities, enums (no dependencies)
│       ├── SSPMS.Application/     # DTOs, interfaces, service contracts
│       ├── SSPMS.Infrastructure/  # EF Core, services, email, SignalR
│       └── SSPMS.API/             # Controllers, middleware, Program.cs
├── frontend/
│   └── src/app/
│       ├── core/                  # Auth service, HTTP interceptors, guards
│       ├── shared/                # Angular Material module
│       └── features/
│           ├── auth/              # Login, forgot/reset password
│           ├── admin/             # User management, audit log
│           ├── trainer/           # Task management, evaluation, reports
│           └── employee/          # Task attempt, results, leaderboard
├── docs/
│   ├── SRS.md                     # Software Requirements Specification
│   ├── CRS.md                     # Customer Requirements Specification
│   └── TEAM.md                    # Team workflow & GitHub standards
└── .github/workflows/ci.yml       # CI pipeline (build + test)
```

---

## Configuration (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=<neon-host>;Database=<db-name>;Username=<user>;Password=<password>;SSL Mode=Require;"
  },
  "Jwt": {
    "Secret": "your-32-char-minimum-secret-key-here",
    "Issuer": "SSPMS",
    "Audience": "SSPMS",
    "ExpiryMinutes": 60
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "From": "noreply@yourorg.com",
    "Password": "app-password"
  }
}
```

---

## Deployment

The production environment uses the following cloud services:

| Layer | Provider | Notes |
|-------|----------|-------|
| Frontend (Angular SPA) | **Vercel** | Auto-deploys from `main` branch |
| Backend (.NET 9 API) | **Render** | Web Service; ~50 s cold start on free tier |
| Database | **Neon** (PostgreSQL) | Serverless PostgreSQL; connection via Npgsql |

> **Cold start note:** The Render free tier spins down after inactivity. The first request after idle may take ~50 seconds — this is normal and is not a bug.

---

## Key Features

| Feature | Status |
|---------|--------|
| JWT auth with refresh tokens | ✅ |
| 2FA (TOTP) | ✅ |
| Password reset via email OTP | ✅ |
| Role-based access (Admin/Trainer/Employee) | ✅ |
| Class & employee management | ✅ |
| Task creation (Draft → Published → Closed) | ✅ |
| MCQ (auto-graded) + Code + Assessment questions | ✅ |
| Timed task attempt with auto-submit | ✅ |
| Submission rank multiplier gamification | ✅ |
| Live submission counter (SignalR) | ✅ |
| XP system & achievement badges | ✅ |
| Class + global leaderboards | ✅ |
| PDF + Excel report export | ✅ |
| Real-time notifications (SignalR) | ✅ |
| Email notifications (MailKit) | ✅ |
| Background job (expired task processing) | ✅ |
| Rate limiting on auth endpoints | ✅ |
| Audit log | ✅ |

---

## API Documentation

With the backend running, open [http://localhost:5000/swagger](http://localhost:5000/swagger).

---

## Team

| Name | Role |
|------|------|
| Benhar Charles Sir | Product Owner |
| Saksham Tapadia | Dev Lead |
| Ayush Mathur | Developer |
| Aman Nahar | Developer |
| Diya Garg | Developer |
| Pankhuri | Developer |

See [docs/TEAM.md](docs/TEAM.md) for GitHub workflow and collaboration standards.

---

## Gamification: Multiplier Tiers

| Submission Rank | Score Multiplier |
|----------------|-----------------|
| 1–5 | 100% |
| 6–10 | 80% |
| 11–15 | 60% |
| 16–20 | 40% |
| 21–25 | 20% |
| 26+ | 0% |
| Not submitted | 0% |
