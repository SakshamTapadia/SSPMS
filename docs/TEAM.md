# SSPMS Team Handbook

---

| Field          | Details                                          |
|----------------|--------------------------------------------------|
| **Project**    | SmartSkill Performance Monitoring System (SSPMS) |
| **Version**    | 1.0.0                                            |
| **Date**       | 2026-03-20                                       |
| **Status**     | Active                                           |

---

## Table of Contents

1. [Team Roster](#1-team-roster)
2. [Communication Channels](#2-communication-channels)
3. [Escalation Path](#3-escalation-path)
4. [GitHub Repository Setup](#4-github-repository-setup)
5. [Branching Strategy](#5-branching-strategy)
6. [Commit Message Convention](#6-commit-message-convention)
7. [Pull Request Workflow](#7-pull-request-workflow)
8. [Code Review Checklist](#8-code-review-checklist)
9. [Development Environment Setup](#9-development-environment-setup)
10. [Sprint & Meeting Cadence](#10-sprint--meeting-cadence)
11. [Definition of Done](#11-definition-of-done)
12. [Issue & Task Tracking](#12-issue--task-tracking)

---

## 1. Team Roster

| Name               | Role                         | Responsibilities                                           |
|--------------------|------------------------------|------------------------------------------------------------|
| **Benhar Charles Sir** | Product Owner (PO)       | Requirements sign-off, scope decisions, final approvals    |
| **Saksham Tapadia**    | Dev Lead / Full-Stack    | Architecture decisions, PR reviews, GitHub admin, .NET + Angular |
| **Ayush Mathur**       | Developer                | Assigned per sprint; backend or frontend tasks             |
| **Aman Nahar**         | Developer                | Assigned per sprint; backend or frontend tasks             |
| **Diya Garg**          | Developer                | Assigned per sprint; backend or frontend tasks             |
| **Pankhuri**           | Developer                | Assigned per sprint; backend or frontend tasks             |

> Role assignments for specific features will be captured in GitHub Issues at the start of each sprint. The Dev Lead assigns issues in the sprint planning meeting.

---

## 2. Communication Channels

| Channel                 | Purpose                                              |
|-------------------------|------------------------------------------------------|
| **GitHub Issues**       | Feature tracking, bug reports, task assignment       |
| **GitHub Pull Requests**| Code review and merge workflow                       |
| **GitHub Discussions**  | Architecture decisions, design debates               |
| **Team Chat (WhatsApp/Discord — team to confirm)** | Daily quick updates, blockers     |
| **Email**               | Escalations to Product Owner (Benhar Charles Sir)    |
| **Sprint Meetings (MS Teams / Google Meet)** | Sprint planning, reviews, retrospectives |

---

## 3. Escalation Path

```
Level 1:  Team Member
           → If blocked for > 2 hours, raise in team chat

Level 2:  Dev Lead (Saksham Tapadia)
           → If blocked for > 1 day, escalate to Dev Lead
           → Dev Lead resolves technical blockers or re-scopes the issue

Level 3:  Product Owner (Benhar Charles Sir)
           → If blocker is scope, requirement ambiguity, or business decision
           → Dev Lead communicates to PO via email with context
           → PO responds within 1 business day

Rule: No developer should be blocked for more than 2 business days without it being escalated.
```

---

## 4. GitHub Repository Setup

### 4.1 Repository Structure

```
SSPMS/
├── docs/
│   ├── SRS.md           ← Software Requirements Specification
│   ├── CRS.md           ← Customer Requirements Specification
│   └── TEAM.md          ← This document
├── backend/
│   └── src/             ← .NET 9 ASP.NET Core Web API solution
├── frontend/
│   └── src/             ← Angular 19 SPA project
├── .github/
│   ├── PULL_REQUEST_TEMPLATE.md
│   └── workflows/
│       └── ci.yml       ← GitHub Actions CI pipeline
└── README.md
```

### 4.2 Adding Collaborators

All team members must be added to the GitHub repository as **Collaborators** with **Write** access.

Steps (repository owner / Saksham):
1. Go to the repository → **Settings** → **Collaborators and teams**
2. Click **Add people**
3. Add each team member by their GitHub username
4. Set permission level to **Write**

Collaborators to invite:
- Saksham Tapadia (owner / admin)
- Ayush Mathur
- Aman Nahar
- Diya Garg
- Pankhuri
- Benhar Charles Sir (PO — **Read** access for review)

### 4.3 Branch Protection Rules

Configure branch protection on **`main`** and **`develop`**:

**Settings → Branches → Add branch protection rule:**

| Rule                                          | `main` | `develop` |
|-----------------------------------------------|--------|-----------|
| Require pull request before merging           | ✅ Yes | ✅ Yes    |
| Required approvals                            | 2      | 1         |
| Dismiss stale reviews on new push             | ✅ Yes | ✅ Yes    |
| Require status checks to pass (CI)            | ✅ Yes | ✅ Yes    |
| Require branches to be up to date before merge | ✅ Yes | ✅ Yes   |
| Restrict direct pushes to branch              | ✅ Yes | ✅ Yes    |
| Do not allow force pushes                     | ✅ Yes | ✅ Yes    |

---

## 5. Branching Strategy

SSPMS uses a **Git Flow-inspired** model with three persistent branch levels:

```
main                 ← Production-ready code. Tagged with version numbers.
  └── develop        ← Integration branch. Always deployable to staging.
        └── feature/SSPMS-XXX-short-description
        └── bugfix/SSPMS-XXX-short-description
        └── hotfix/SSPMS-XXX-short-description   ← branches off main, merges to main + develop
```

### 5.1 Branch Naming Convention

```
feature/SSPMS-001-auth-jwt-login
feature/SSPMS-012-gamification-engine
bugfix/SSPMS-034-submission-rank-race-condition
hotfix/SSPMS-051-otp-expiry-not-applied
docs/SSPMS-002-srs-initial-draft
```

Format: `{type}/SSPMS-{issue-number}-{kebab-case-description}`

| Type      | When to Use                                          |
|-----------|------------------------------------------------------|
| `feature` | New functionality from a GitHub Issue                |
| `bugfix`  | Fix for a bug found in `develop`                     |
| `hotfix`  | Urgent fix for a bug in `main` (production)          |
| `docs`    | Documentation-only changes                           |
| `refactor`| Code refactoring with no functional change           |
| `test`    | Adding or fixing tests only                          |

### 5.2 Branch Lifecycle

```
1. Create GitHub Issue for the task (or use existing sprint issue)
2. Create branch from develop:
   git checkout develop
   git pull origin develop
   git checkout -b feature/SSPMS-XXX-description

3. Work locally → commit frequently with Conventional Commits format

4. Push and open PR against develop:
   git push origin feature/SSPMS-XXX-description

5. PR reviewed → approved → squash-and-merge into develop

6. At end of sprint: develop merged into main (after full QA pass)
   main tagged: v1.0.0, v1.1.0, etc.

7. Delete feature branch after merge
```

---

## 6. Commit Message Convention

All commits must follow the **Conventional Commits** specification.

### Format
```
<type>(<scope>): <short description>

[optional body]

[optional footer(s)]
```

### Types

| Type       | Use For                                               |
|------------|-------------------------------------------------------|
| `feat`     | A new feature                                         |
| `fix`      | A bug fix                                             |
| `docs`     | Documentation changes only                            |
| `refactor` | Code restructuring (no feature or bug change)         |
| `test`     | Adding or updating tests                              |
| `chore`    | Build scripts, config, dependency updates             |
| `perf`     | Performance improvements                              |
| `ci`       | CI/CD pipeline changes                                |

### Scope (optional)
The scope should name the module: `auth`, `tasks`, `gamification`, `reports`, `notifications`, `dashboard`, `users`, `classes`, etc.

### Examples

```
feat(auth): add JWT login endpoint with RS256 signing

fix(gamification): resolve race condition in submission rank assignment

docs(srs): add question type specifications to appendix

test(submissions): add integration tests for auto-submit on timer expiry

chore(deps): update EF Core to 8.0.3

feat(dashboard): add class leaderboard with week/month/all-time filter

refactor(users): extract user validation into dedicated service class
```

### Linking Issues
Reference the GitHub issue in the commit or PR description:
```
feat(tasks): implement MCQ auto-grading on submission

Closes #23
```

---

## 7. Pull Request Workflow

### 7.1 Creating a PR

1. Push your feature branch to GitHub
2. Open a Pull Request against **`develop`** (or `main` for hotfixes)
3. Use the PR template (`.github/PULL_REQUEST_TEMPLATE.md`):
   - Summary of changes
   - Linked issue(s)
   - Type of change (feature / bugfix / refactor / docs)
   - Testing done (unit tests / manual testing steps)
   - Screenshots (for UI changes)
   - Checklist (see below)

### 7.2 PR Template

```markdown
## Summary
Brief description of what this PR does.

## Linked Issue(s)
Closes #XXX

## Type of Change
- [ ] New feature
- [ ] Bug fix
- [ ] Refactoring
- [ ] Documentation
- [ ] Tests

## Testing Done
- [ ] Unit tests written and passing
- [ ] Manual testing steps performed (describe below)

### Manual Test Steps
1. ...
2. ...

## Screenshots (if UI change)
<!-- Add before/after screenshots here -->

## Checklist
- [ ] My code follows the project's clean architecture conventions
- [ ] I have added/updated tests for my changes
- [ ] All existing tests pass (CI green)
- [ ] No hardcoded credentials, secrets, or API keys
- [ ] Error handling is in place for failure scenarios
- [ ] My branch is up to date with develop
```

### 7.3 Review Requirements

| Target Branch | Required Approvals | CI Checks Required |
|---------------|--------------------|--------------------|
| `develop`     | 1 approval         | Build + Tests pass |
| `main`        | 2 approvals        | Build + Tests pass |

- **Dev Lead (Saksham)** must approve all PRs touching architecture-level code (auth, gamification engine, data model migrations).
- **Self-review** is not counted — you cannot approve your own PR.
- If CI is red, the PR cannot be merged regardless of approvals.

### 7.4 PR Review Etiquette

- Review PRs within **24 hours** of assignment
- Use GitHub's **Review suggestions** feature for line-level changes
- Distinguish feedback type in comments:
  - `[blocking]` — must be addressed before merge
  - `[nit]` — minor style point, author's discretion
  - `[question]` — seeking clarification, not requiring a change
- Approve only when all `[blocking]` comments are resolved

---

## 8. Code Review Checklist

Use this checklist when reviewing any PR:

### General
- [ ] Code does exactly what the linked issue/PR description says
- [ ] No unnecessary changes outside the PR scope
- [ ] No commented-out code left behind
- [ ] No TODO/FIXME left untracked (must have an associated GitHub Issue)

### Backend (.NET 9)
- [ ] Follows Clean Architecture — no business logic in controllers
- [ ] All inputs validated (FluentValidation or data annotations)
- [ ] EF Core queries avoid N+1 problems (use `.Include()` or projection)
- [ ] No raw SQL unless in designated raw-query repository
- [ ] Async/await used consistently (no `.Result` or `.Wait()`)
- [ ] New endpoints have corresponding unit + integration tests
- [ ] Sensitive data (passwords, tokens) never logged
- [ ] Authorization attributes present on all protected endpoints (`[Authorize(Roles = "...")]`)

### Frontend (Angular 19)
- [ ] Feature placed in the correct Angular module
- [ ] No logic in HTML templates — computed values in component class
- [ ] HTTP calls go through Angular services, not directly from components
- [ ] Angular route guards applied to protected routes
- [ ] No hardcoded API URLs — use environment files (`environment.ts`)
- [ ] Loading and error states handled in every data-fetching component
- [ ] No `any` TypeScript types (strict mode enforced)
- [ ] Reactive Forms used for all user input forms (no Template-driven forms)

### Database / Migrations
- [ ] EF Core migration file is present for any schema change
- [ ] Migration is reviewed for correctness before PR approval
- [ ] No breaking schema changes without backward-compatibility plan

### Security
- [ ] No secrets, API keys, or passwords in code or config files committed
- [ ] User-provided data never directly interpolated into queries or HTML
- [ ] File uploads validated for type and size

---

## 9. Development Environment Setup

### 9.1 Prerequisites

| Tool                     | Version       | Download                        |
|--------------------------|---------------|---------------------------------|
| .NET SDK                 | 9.0+          | https://dotnet.microsoft.com    |
| Node.js                  | 20 LTS+       | https://nodejs.org              |
| Angular CLI              | 19+           | `npm install -g @angular/cli`   |
| Git                      | Latest        | https://git-scm.com             |
| Visual Studio Code       | Latest        | https://code.visualstudio.com   |
| VS Code Extension: C#    | Latest        | ms-dotnettools.csharp           |
| VS Code Extension: Angular| Latest       | Angular Language Service        |

> **Database:** No local PostgreSQL install required. The project connects to **Neon** (cloud PostgreSQL). Obtain the connection string from the project's Neon dashboard and set it in `appsettings.Development.json`.

### 9.2 IDE — Visual Studio Code (Team Standard)

All team members must use **Visual Studio Code** as the IDE. This ensures consistent editor behaviour, extension support, and shared workspace settings.

**Recommended Extensions (add to `.vscode/extensions.json`):**
```json
{
  "recommendations": [
    "ms-dotnettools.csharp",
    "ms-dotnettools.csdevkit",
    "angular.ng-template",
    "dbaeumer.vscode-eslint",
    "esbenp.prettier-vscode",
    "EditorConfig.EditorConfig",
    "eamodio.gitlens",
    "GitHub.vscode-pull-request-github"
  ]
}
```

### 9.3 Local Setup Steps

```bash
# 1. Clone the repository
git clone https://github.com/<org>/SSPMS.git
cd SSPMS

# 2. Backend setup
cd backend
cp src/SSPMS.API/appsettings.json src/SSPMS.API/appsettings.Development.json
# Edit appsettings.Development.json — set Neon PostgreSQL connection string, JWT secret, email config

dotnet restore
dotnet ef database update -s src/SSPMS.API -p src/SSPMS.Infrastructure  # Apply EF Core migrations
dotnet run --project src/SSPMS.API  # API runs on http://localhost:5000

# 3. Frontend setup
cd ../frontend
npm install
ng serve                    # App runs on http://localhost:4200
```

### 9.4 Environment Variables (never commit)

The following must be set in `appsettings.Development.json` (gitignored):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=<neon-host>;Database=<db>;Username=<user>;Password=<pass>;SSL Mode=Require;"
  },
  "Jwt": {
    "Secret": "your-32-char-minimum-secret-key-here",
    "Issuer": "SSPMS",
    "Audience": "SSPMS",
    "ExpiryMinutes": 60
  },
  "Email": {
    "SmtpHost": "smtp.your-provider.com",
    "SmtpPort": 587,
    "From": "noreply@sspms.com",
    "Password": "your-smtp-password"
  }
}
```

> **Never commit secrets.** The `.gitignore` must exclude `appsettings.Development.json`, `appsettings.Production.json`, and the `keys/` directory.

---

## 10. Sprint & Meeting Cadence

| Meeting              | Frequency         | Duration    | Attendees         | Purpose                                          |
|----------------------|-------------------|-------------|-------------------|--------------------------------------------------|
| **Sprint Planning**  | Every 2 weeks (Monday) | 1–1.5 hr | Full team + PO | Select issues for sprint, assign, estimate        |
| **Daily Standup**    | Daily (weekdays)  | 15 min      | Dev team          | What did I do? What will I do? Any blockers?     |
| **Sprint Review**    | Every 2 weeks (Friday) | 1 hr    | Full team + PO | Demo completed features, PO feedback              |
| **Retrospective**    | Every 2 weeks (Friday, after review) | 30 min | Dev team | What went well? What to improve?        |
| **PO Check-in**      | As needed         | 30 min      | Dev Lead + PO    | Scope questions, requirement clarifications       |

### Standup Format (async acceptable in team chat)

```
Yesterday: [what I completed]
Today: [what I'm working on — include issue #]
Blocker: [anything blocking me, or "None"]
```

---

## 11. Definition of Done

A feature or bug fix is considered **Done** when ALL of the following are true:

- [ ] Code is written and self-reviewed
- [ ] Unit tests are written and passing (≥ 80% coverage for new code)
- [ ] Integration tests pass
- [ ] CI pipeline is green (build + test)
- [ ] PR is reviewed and approved by the required number of reviewers
- [ ] PR is merged into `develop`
- [ ] The linked GitHub Issue is closed with the merge
- [ ] Feature works correctly in the team's shared development/staging environment
- [ ] Any new environment variables are documented in the project README / this handbook
- [ ] Relevant documentation (SRS/CRS) is updated if the implementation deviates from spec (raise to Dev Lead)

---

## 12. Issue & Task Tracking

All work is tracked as **GitHub Issues** in the SSPMS repository.

### Issue Labels

| Label              | Use For                                      |
|--------------------|----------------------------------------------|
| `feature`          | New functionality from SRS                   |
| `bug`              | Something isn't working as expected          |
| `documentation`    | SRS, CRS, or README updates                  |
| `enhancement`      | Improvement to existing functionality        |
| `priority: high`   | Must be done this sprint                     |
| `priority: medium` | Should be done this sprint                   |
| `priority: low`    | Could be deferred                            |
| `backend`          | .NET API work                                |
| `frontend`         | Angular work                                 |
| `in-progress`      | Actively being worked on                     |
| `blocked`          | Waiting on another issue or decision         |
| `needs-review`     | PR open, awaiting review                     |

### Issue Template

```markdown
## Description
Clear description of what needs to be built or fixed.

## SRS Reference
FR-TASK-01, FR-GAMIFY-02 (link to relevant SRS requirement)

## Acceptance Criteria
- [ ] Criterion 1
- [ ] Criterion 2

## Technical Notes
Any implementation hints, API contracts, or design considerations.

## Estimate
Story Points: [1 / 2 / 3 / 5 / 8]
```

### GitHub Project Board

Use a **GitHub Projects** Kanban board with columns:
- **Backlog** — all open issues not yet in a sprint
- **Sprint** — issues selected for the current sprint
- **In Progress** — actively being worked on
- **In Review** — PR open, awaiting approval
- **Done** — merged and closed

---

*SmartSkill Performance Monitoring System — TEAM v1.0.0*
*All team members are expected to have read and understood this document before beginning development.*
