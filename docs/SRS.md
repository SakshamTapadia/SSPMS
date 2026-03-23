# Software Requirements Specification (SRS)

---

| Field             | Details                                                    |
|-------------------|------------------------------------------------------------|
| **Document**      | Software Requirements Specification                        |
| **System**        | SmartSkill Performance Monitoring System (SSPMS)           |
| **Version**       | 1.0.0                                                      |
| **Date**          | 2026-03-20                                                 |
| **Status**        | Draft                                                      |
| **Standard**      | IEEE Std 830-1998                                          |
| **Prepared by**   | Saksham Tapadia, Ayush Mathur, Aman Nahar, Diya Garg, Pankhuri |
| **Reviewed by**   | Benhar Charles Sir (Product Owner)                         |

---

## Revision History

| Version | Date       | Author         | Description              |
|---------|------------|----------------|--------------------------|
| 1.0.0   | 2026-03-20 | Team SSPMS     | Initial draft             |

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Overall Description](#2-overall-description)
3. [Functional Requirements](#3-functional-requirements)
4. [External Interface Requirements](#4-external-interface-requirements)
5. [Data Model](#5-data-model)
6. [API Endpoint Inventory](#6-api-endpoint-inventory)
7. [Angular Route & Screen Inventory](#7-angular-route--screen-inventory)
8. [Non-Functional Requirements](#8-non-functional-requirements)
9. [Appendix A — Gamification Multiplier Table](#9-appendix-a--gamification-multiplier-table)
10. [Appendix B — Badge Criteria Reference](#10-appendix-b--badge-criteria-reference)
11. [Appendix C — Question Type Specifications](#11-appendix-c--question-type-specifications)

---

## 1. Introduction

### 1.1 Purpose

This Software Requirements Specification defines the complete functional and non-functional requirements for the **SmartSkill Performance Monitoring System (SSPMS)**. It is the primary technical contract between the development team and the product owner. All implementation decisions must be traceable to a requirement in this document.

**Intended readers:** .NET 9 backend developers, Angular 19 frontend developers, QA engineers, and the product owner.

### 1.2 Product Scope

SSPMS is a web-based, role-aware platform for managing and monitoring employee training performance. It supports three user roles — **Admin**, **Trainer**, and **Employee** — each with distinct capabilities. Its core differentiator is a **gamification engine** that applies submission-speed multipliers to scoring, combined with an achievement badge system, XP leaderboards, and rich analytics dashboards.

The system is not a Learning Management System (LMS) content delivery platform. It focuses exclusively on **assessment assignment, submission, evaluation, and performance analytics**.

### 1.3 Definitions, Acronyms, and Abbreviations

| Term         | Definition                                                              |
|--------------|-------------------------------------------------------------------------|
| SSPMS        | SmartSkill Performance Monitoring System                                |
| SRS          | Software Requirements Specification                                     |
| CRS          | Customer Requirements Specification                                     |
| API          | Application Programming Interface                                       |
| JWT          | JSON Web Token — used for stateless authentication                      |
| RBAC         | Role-Based Access Control                                               |
| SPA          | Single Page Application                                                 |
| MCQ          | Multiple Choice Question                                                |
| SignalR      | ASP.NET real-time communication library (WebSocket-based)               |
| XP           | Experience Points — used in the gamification leaderboard                |
| OTP          | One-Time Password — used for email-based password reset                 |
| TOTP         | Time-based One-Time Password — used for 2FA                             |
| WCAG         | Web Content Accessibility Guidelines                                    |
| ER           | Entity-Relationship (data model notation)                               |
| p95          | 95th percentile (latency measurement)                                   |
| PO           | Product Owner (Benhar Charles Sir)                                      |

### 1.4 References

| Reference          | Document / URL                                      |
|--------------------|-----------------------------------------------------|
| IEEE 830-1998      | IEEE Standard for Software Requirements Specifications |
| .NET 9             | Microsoft .NET 9 official documentation             |
| Angular 19         | Angular 19 official documentation                   |
| PostgreSQL / Neon  | Neon serverless PostgreSQL documentation            |
| SignalR            | ASP.NET Core SignalR documentation                  |
| OWASP Top 10       | OWASP Top 10 Web Application Security Risks (2021)  |
| WCAG 2.1           | W3C Web Content Accessibility Guidelines 2.1        |
| CRS v1.0.0         | SSPMS Customer Requirements Specification (this repo, docs/CRS.md) |

### 1.5 Document Overview

- **Section 2** describes the system context, architecture, and user classes.
- **Section 3** lists all functional requirements, grouped by feature domain.
- **Section 4** specifies external interfaces (UI, API, real-time, email).
- **Section 5** defines the data model with entity fields and relationships.
- **Section 6** inventories all REST API endpoint groups.
- **Section 7** lists all Angular routes and screens.
- **Section 8** specifies non-functional requirements.
- **Appendices** provide reference tables for gamification, badges, and question types.

---

## 2. Overall Description

### 2.1 Product Perspective

SSPMS is a **standalone web application**. It does not integrate with any existing HR system or LMS. It consists of:

- A **RESTful backend API** built with .NET 9 (ASP.NET Core Web API), deployed on Render
- An **Angular 19 SPA** frontend deployed on Vercel
- A **PostgreSQL** relational database hosted on Neon (serverless)
- A **SignalR** hub for real-time communication
- An **SMTP** email service for transactional emails

```
┌─────────────────────────────────────────────────┐
│                  CLIENT LAYER                   │
│           Angular 19 SPA (Browser)              │
│  (Angular Material / PrimeNG Components)        │
└───────────────────┬─────────────────────────────┘
                    │ HTTPS (REST + WebSocket)
┌───────────────────▼─────────────────────────────┐
│                  API LAYER                      │
│        .NET 9 ASP.NET Core Web API              │
│  ┌──────────────┐  ┌──────────────────────────┐ │
│  │  REST API    │  │   SignalR Hub (WS)        │ │
│  │  /api/v1/... │  │  /hubs/notifications      │ │
│  │              │  │  /hubs/submissions        │ │
│  └──────────────┘  └──────────────────────────┘ │
│  ┌──────────────────────────────────────────┐   │
│  │   Application Services / Business Logic  │   │
│  │   (SOLID, Clean Architecture pattern)    │   │
│  └──────────────────────────────────────────┘   │
└───────────────────┬─────────────────────────────┘
                    │
┌───────────────────▼─────────────────────────────┐
│                DATA LAYER                       │
│         PostgreSQL / Neon (via EF Core 9)       │
└─────────────────────────────────────────────────┘
```

### 2.2 User Classes and Characteristics

| Role     | Who They Are                                          | Technical Skill | Frequency of Use |
|----------|-------------------------------------------------------|-----------------|------------------|
| Admin    | System administrator; may be the PO or IT lead        | Moderate         | Low (setup/monitoring) |
| Trainer  | Subject-matter expert running training classes        | Low–Moderate    | Daily             |
| Employee | Trainee participating in courses                      | Low             | Daily during training |

### 2.3 Operating Environment

- **Client:** Any modern web browser — Chrome 120+, Firefox 121+, Edge 120+
- **Server:** Windows Server 2022 or Ubuntu 22.04; minimum 4 vCPU / 8 GB RAM
- **Database:** PostgreSQL via Neon (serverless cloud)
- **Deployment:** Backend on Render, frontend on Vercel, database on Neon
- **Network:** HTTPS required; TLS 1.2 minimum

### 2.4 Design Constraints

| Constraint        | Detail                                                              |
|-------------------|---------------------------------------------------------------------|
| Backend           | .NET 9 (C#) — ASP.NET Core Web API; no other backend runtime       |
| Frontend          | Angular 19 (TypeScript) — no React, Vue, or other SPA frameworks   |
| ORM               | Entity Framework Core 9 with Npgsql (code-first migrations)         |
| Database          | PostgreSQL (Neon) — no other RDBMS                                  |
| Auth              | JWT (RS256 asymmetric signing); no OAuth SSO in v1.0               |
| Real-Time         | SignalR — no raw WebSocket or third-party push service             |
| Source Control    | GitHub — all code, docs, and issues                                |

### 2.5 Assumptions and Dependencies

- All users have a valid email address (used for account creation and notifications).
- A single SMTP server is available and configured in `appsettings.json`.
- The deployment timezone is fixed system-wide (configurable per installation; all times displayed in that timezone).
- GitHub repository is created before development begins; all team members are added as collaborators.
- The Neon PostgreSQL connection string is available as an environment variable in both Render and local development configuration.

---

## 3. Functional Requirements

Requirements follow the format: **FR-[GROUP]-[NN]: [Description]**
Each requirement has a priority: **[M]** Must Have · **[S]** Should Have · **[C]** Could Have

---

### 3.1 FR-AUTH — Authentication & Authorization

| ID          | Priority | Requirement                                                                                     |
|-------------|----------|-------------------------------------------------------------------------------------------------|
| FR-AUTH-01  | M        | The system must authenticate users with email + password. On success, issue a JWT (RS256) containing userId, email, and role. |
| FR-AUTH-02  | M        | The JWT must expire after 60 minutes. A refresh token (opaque, stored server-side, hashed in DB) must extend the session for up to 7 days on a sliding window. |
| FR-AUTH-03  | M        | Angular must implement role-based route guards. Admin routes, Trainer routes, and Employee routes must be inaccessible to other roles, returning HTTP 403 if called directly. |
| FR-AUTH-04  | M        | A "Forgot Password" flow must send a 6-digit OTP to the user's registered email. The OTP must expire after 15 minutes. On OTP verification, allow password reset. |
| FR-AUTH-05  | S        | The system must support optional TOTP-based 2FA (compatible with Google Authenticator). Users can enable/disable 2FA from their profile settings. |
| FR-AUTH-06  | M        | After 30 minutes of inactivity (no API calls), the Angular session must automatically expire and redirect to the login page. |
| FR-AUTH-07  | M        | Every login attempt (success or failure), logout event, and password reset must be recorded in the AuditLog table with: userId (if known), action, IP address, timestamp. |

### 3.2 FR-USER — User Management

| ID          | Priority | Requirement                                                                                     |
|-------------|----------|-------------------------------------------------------------------------------------------------|
| FR-USER-01  | M        | Admin can create Trainer accounts (name, email, temporary password, optional class assignment). Admin can edit trainer details and deactivate/reactivate trainer accounts. |
| FR-USER-02  | M        | Admin can create Employee accounts (name, email, temporary password, class assignment). Admin can edit employee details and deactivate/reactivate employee accounts. |
| FR-USER-03  | M        | Trainer can create Employee accounts. The created employee is automatically enrolled in the trainer's designated class. |
| FR-USER-04  | M        | Employees cannot create, edit, or view other user accounts. Any attempt to access user management endpoints must return HTTP 403. |
| FR-USER-05  | M        | All roles must have a Profile page: editable name, profile avatar (image upload, max 2 MB, JPEG/PNG), contact info. Password change from profile page (requires current password). |
| FR-USER-06  | S        | Admin and Trainer can import multiple employees via CSV upload. The CSV format must include: name, email. The system must validate each row, skip duplicates, and report import results (success count, error rows with reason). |
| FR-USER-07  | S        | On account creation, the system must send a welcome email containing the user's login credentials and a link to set their permanent password. |

### 3.3 FR-CLASS — Class Management

| ID           | Priority | Requirement                                                                                    |
|--------------|----------|------------------------------------------------------------------------------------------------|
| FR-CLASS-01  | M        | Admin can create a Class with: name, description, start date, end date, skill tags (comma-separated). Admin must assign a Trainer to the class at creation time. |
| FR-CLASS-02  | M        | Admin can edit class details (name, description, dates, skill tags, trainer assignment). Admin can archive a class (archived classes are read-only). |
| FR-CLASS-03  | M        | A Trainer can view and manage only their own class(es). A Trainer cannot access another trainer's class data. |
| FR-CLASS-04  | M        | An Employee can be enrolled in exactly ONE active class at any point in time. The system must enforce this constraint at the database level (unique constraint on active enrollment). |
| FR-CLASS-05  | M        | Admin or Trainer can transfer an Employee from one class to another. On transfer, the current enrollment is archived (status = Transferred), and a new enrollment is created in the target class. |
| FR-CLASS-06  | M        | Admin can see all classes system-wide. Trainer can see only their own classes. Employee can see only the class they are enrolled in. |
| FR-CLASS-07  | S        | Class detail view must show: enrolled employee count, number of tasks assigned, class average score, and a list of enrolled employees with their current rank. |

### 3.4 FR-TASK — Task & Exam Management

| ID          | Priority | Requirement                                                                                     |
|-------------|----------|-------------------------------------------------------------------------------------------------|
| FR-TASK-01  | M        | Trainer can create a Task with fields: title, description, instructions, total marks (sum of question marks), start datetime, end datetime, duration in minutes. |
| FR-TASK-02  | M        | A Task must be assigned to exactly one Class. All employees enrolled in that class at the time of assignment automatically receive the task. |
| FR-TASK-03  | M        | A Task contains one or more Questions. Questions must be orderable (drag-to-reorder in the Trainer UI). Each question has: type (MCQ / Code / Assessment), stem/problem text (rich text), marks allocated. |
| FR-TASK-04  | M        | **MCQ Question:** Must have exactly 4 answer options. Exactly one option is marked as correct. The correct answer must not be visible to employees at any time before or during the attempt. |
| FR-TASK-05  | M        | **Code Question:** Must specify programming language (C# / JavaScript / Python in v1.0). Employee writes code in an embedded code editor (Monaco Editor or CodeMirror). Trainer provides an expected output description for evaluation reference (not shown to employee). |
| FR-TASK-06  | M        | **Assessment Question:** Employee submits a URL. The system must validate that the URL is a valid GitHub repository URL format (`https://github.com/...`). |
| FR-TASK-07  | M        | Employee can see the marks allocated to each question on the task overview screen before starting, and on the results screen after evaluation. |
| FR-TASK-08  | M        | Employee can only access a task's attempt screen when: `current_time >= task.start_at AND current_time <= task.end_at`. Outside this window, the task is shown as "Upcoming", "Active", or "Closed". |
| FR-TASK-09  | M        | A countdown timer must be displayed during the attempt. The timer counts down the lesser of: (a) the task's duration in minutes from when the employee first opened the task, or (b) time remaining until `task.end_at`. |
| FR-TASK-10  | M        | Employee answers are auto-saved every 30 seconds (draft save). Employee can manually save. On browser close/refresh, answers are restored from the last draft save. |
| FR-TASK-11  | M        | When the countdown timer reaches zero, the system must auto-submit the employee's current draft answers. The employee must see a "Time's up — submitted automatically" confirmation. |
| FR-TASK-12  | M        | Once submitted, the employee cannot reopen or modify the task. The task is shown as "Submitted" in their task list. |
| FR-TASK-13  | S        | Trainer can set a task to Draft status (not yet visible to employees) or Published (visible and active per time window). Trainer can edit a task in Draft status. Published tasks cannot be edited (marks/questions locked). |
| FR-TASK-14  | S        | Trainer can duplicate an existing task (creates a Draft copy with all questions, editable before publishing). |

### 3.5 FR-GAMIFY — Gamification Engine

| ID            | Priority | Requirement                                                                                  |
|---------------|----------|----------------------------------------------------------------------------------------------|
| FR-GAMIFY-01  | M        | When an employee submits a task, the system must atomically record `submission_rank` as the ordinal position of this submission among all submissions for that task (1 = first to submit, 2 = second, etc.). |
| FR-GAMIFY-02  | M        | The system must compute a `multiplier` based on submission rank per the following tier table (see Appendix A). The multiplier is stored with the Submission record and used to compute `final_score = raw_score × multiplier`. |
| FR-GAMIFY-03  | M        | Employees who have not submitted by `task.end_at` must be auto-processed with: submission_rank = NULL, multiplier = 0.0, final_score = 0. |
| FR-GAMIFY-04  | M        | During an active task attempt, a live counter must display: "N students have already submitted" (updated in real-time via SignalR every time a new submission is recorded). |
| FR-GAMIFY-05  | M        | An **XP Ledger** must record XP earned per event. XP awards: task submitted on time = 50 XP base; Tier 1 (rank 1–5) bonus = +50 XP; Tier 2 (rank 6–10) bonus = +30 XP; Tier 3 (rank 11–15) bonus = +10 XP; badge earned = +20 XP each. |
| FR-GAMIFY-06  | M        | A **Class Leaderboard** must rank all enrolled employees by total XP within their class. A **Global Leaderboard** must rank all employees system-wide by total XP. Both must be filterable: all-time / current month / current week. |
| FR-GAMIFY-07  | M        | Badges must be awarded automatically by the system post-submission or at end-of-day. See Appendix B for badge criteria. On badge award, the employee receives an in-app notification and an email notification. |
| FR-GAMIFY-08  | S        | A **Streak Tracker** must record the employee's consecutive days on which they submitted at least one task on time. Streak resets to 0 if a day passes with no on-time submission. |
| FR-GAMIFY-09  | S        | Employee profile must display: total XP, class rank, global rank, badge wall (all earned badges with award date), and current streak count. |

### 3.6 FR-EVAL — Evaluation & Grading

| ID          | Priority | Requirement                                                                                     |
|-------------|----------|-------------------------------------------------------------------------------------------------|
| FR-EVAL-01  | M        | MCQ answers must be auto-graded at submission time. For each MCQ question: if the selected option matches the correct option, raw_score for that question = question.marks; else raw_score = 0. |
| FR-EVAL-02  | M        | Code and Assessment questions must be manually evaluated by the trainer. Trainer enters a numeric marks value (0 to question.marks) for each question. |
| FR-EVAL-03  | M        | The Evaluation Queue shows all submissions for a selected task, grouped by status: Pending Evaluation / Evaluated. Trainer can filter, sort by submission rank, and paginate. |
| FR-EVAL-04  | M        | On each submission in the queue, the trainer sees: employee name, submission time, submission rank, estimated final score tier, each question with the employee's answer, and input fields for marks + feedback per non-MCQ question. |
| FR-EVAL-05  | M        | Trainer can add per-question evaluator notes (text) visible to the employee after the submission is marked as Evaluated. |
| FR-EVAL-06  | M        | When a submission is saved as Evaluated, the system must: compute total_raw_score, apply the stored multiplier to produce total_final_score, update submission status to Evaluated, and trigger an in-app and email notification to the employee. |
| FR-EVAL-07  | S        | Trainer can flag a code submission answer as **plagiarism-suspected**. When flagged: the question's final score is set to 0, a `is_plagiarism_flag = true` marker is stored, and the submission appears flagged in all reports. The trainer can remove the flag at any time. |
| FR-EVAL-08  | S        | Trainer can **bulk-evaluate** all MCQ-only tasks with one click (since they are already auto-graded, this just changes the submission status from Submitted → Evaluated and triggers employee notifications). |

### 3.7 FR-DASH-TRAINER — Trainer Dashboards

| ID            | Priority | Requirement                                                                                  |
|---------------|----------|----------------------------------------------------------------------------------------------|
| FR-DASH-T-01  | M        | **Class Overview Dashboard:** Summary cards — total enrolled employees, total tasks published, average class score (all tasks, all time), task completion rate (% submitted on time). Top 3 performers widget. Recent activity feed. |
| FR-DASH-T-02  | M        | **Individual Employee Dashboard:** Accessible from the class employee list. Shows: task history table (task name, submitted at, rank, raw score, final score, status), score trend line chart over time, badge collection, skill gap radar chart (score % per skill tag). |
| FR-DASH-T-03  | M        | **Class Leaderboard View:** Ranked table of all class employees by total XP. Columns: rank, name, total XP, tasks completed, avg final score. Filterable by week / month / all-time. |
| FR-DASH-T-04  | M        | **Task Report:** For each task — submission count, non-submission count, avg raw score, avg final score, score distribution histogram (bar chart, 10 buckets), submission rank distribution, late/missed count. |
| FR-DASH-T-05  | S        | **Activity Heatmap:** Calendar grid (GitHub contribution style) showing number of submissions per day across the class. Clicking a day shows who submitted that day. |
| FR-DASH-T-06  | S        | **Skill Tag Performance:** Aggregated score % per skill tag across all tasks, for the whole class. Shown as a horizontal bar chart. Helps trainer identify class-wide weak areas. |

### 3.8 FR-DASH-EMPLOYEE — Employee Dashboards

| ID            | Priority | Requirement                                                                                  |
|---------------|----------|----------------------------------------------------------------------------------------------|
| FR-DASH-E-01  | M        | **My Dashboard (Home):** Shows — current class rank, global rank, total XP, XP progress bar to next rank tier, streak counter, badges earned (latest 5, with "view all" link), upcoming tasks (next 3, with countdown). |
| FR-DASH-E-02  | M        | **Daily Report:** Tasks submitted today with score. Comparison: today's score vs. class average today. "You scored X% higher/lower than class average today." |
| FR-DASH-E-03  | M        | **Performance History:** Line chart of final score % over time (x-axis = task date, y-axis = score %). Bar chart of scores grouped by question type (MCQ / Code / Assessment). |
| FR-DASH-E-04  | M        | **Class Comparison Chart:** Grouped bar chart — for each task, shows 3 bars: My Score / Class Average / Class Top Score. Allows employee to see where they stand on each task. |
| FR-DASH-E-05  | S        | **Skill Analysis:** Radar/spider chart — axes = skill tags of tasks attempted; value = average score % for that skill tag. Highlights strong and weak skill areas. |
| FR-DASH-E-06  | S        | **Streak Widget:** Visual flame icon with streak count. Shows calendar of active days this month. |
| FR-DASH-E-07  | S        | **Score Distribution Pie Chart:** Breakdown of scores earned by question type (MCQ / Code / Assessment) as a percentage of total marks earned. |

### 3.9 FR-DASH-ADMIN — Admin Dashboards

| ID            | Priority | Requirement                                                                                  |
|---------------|----------|----------------------------------------------------------------------------------------------|
| FR-DASH-A-01  | M        | **System Overview:** Total users (by role), total classes, total tasks published, total submissions today, total active sessions (real-time via SignalR). |
| FR-DASH-A-02  | M        | **User Management Table:** Searchable, filterable, paginated table of all users. Columns: name, email, role, class (if employee), status (active/inactive), last login. Actions: edit, deactivate, reset password. |
| FR-DASH-A-03  | S        | **All-Trainers Performance Rollup:** Table of trainers with their class count, total tasks published, class avg score, avg time-to-evaluate. |
| FR-DASH-A-04  | S        | **All-Classes Report:** Comparison table of all classes — class name, trainer name, employee count, tasks assigned, avg score, completion rate. |
| FR-DASH-A-05  | S        | **Audit Log Viewer:** Searchable log with filters: user, action type, date range. Columns: timestamp, user, action, entity, entity ID, IP address. Paginated, read-only. |
| FR-DASH-A-06  | S        | **System-Wide Leaderboard:** Global rank table of all employees across all classes by total XP. |

### 3.10 FR-REPORT — Reporting & Export

| ID           | Priority | Requirement                                                                                   |
|--------------|----------|-----------------------------------------------------------------------------------------------|
| FR-REPORT-01 | M        | Trainer can export a **Class Report** (all tasks, all employees, scores, completion rate) to: PDF (formatted, includes charts as static images) and Excel (.xlsx, raw data). |
| FR-REPORT-02 | M        | Employee can export their **Personal Report** (all tasks attempted, scores, comparisons) to PDF. |
| FR-REPORT-03 | M        | Admin can export any class report or the system-wide report to PDF and Excel. |
| FR-REPORT-04 | S        | **Scheduled Weekly Digest:** Every Monday at 08:00 (server time), the system automatically generates a weekly class performance summary email and sends it to the class trainer. Email includes: tasks completed last week, avg score, top 3 performers, pending evaluations. |
| FR-REPORT-05 | M        | **Day-Wise Report:** A table and bar chart showing daily submissions, avg score per day, and active employee count per day. Available to Trainer (for class) and Employee (for self). |

### 3.11 FR-NOTIFY — Notifications & Announcements

| ID            | Priority | Requirement                                                                                  |
|---------------|----------|----------------------------------------------------------------------------------------------|
| FR-NOTIFY-01  | M        | A **Notification Bell** icon in the top navigation bar must display the count of unread notifications. Clicking it opens a dropdown showing the latest 10 notifications. A "View All" link opens the full notification page. |
| FR-NOTIFY-02  | M        | **In-App Notifications (real-time via SignalR):** The following events must push a notification to the relevant user(s) immediately: new task assigned (to all class employees), task evaluated (to the employee), badge earned (to the employee), new announcement (to all class members). |
| FR-NOTIFY-03  | M        | **Email Notifications (SMTP):** The following events must trigger an email: new task assigned (subject: "New Task: [Title]"), task deadline reminder 1 hour before (subject: "Reminder: [Title] closes in 1 hour"), task evaluated (subject: "Your results are ready: [Title]"), badge earned (subject: "You earned a badge: [Badge Name]"). |
| FR-NOTIFY-04  | M        | User can mark individual notifications as read, or mark all as read. |
| FR-NOTIFY-05  | S        | **Announcements:** Trainer or Admin can create an announcement (title, body, target: class or system-wide). Announcement is shown as a pinned banner on the dashboard and stored in the Announcements feed. All targeted users receive an in-app notification. |
| FR-NOTIFY-06  | S        | User can configure notification preferences in their profile settings: toggle email notifications on/off per event type. In-app notifications cannot be disabled. |

---

## 4. External Interface Requirements

### 4.1 User Interface

- Framework: **Angular 19** with **Angular Material** (primary UI component library)
- Theming: Primary brand colour defined in `_variables.scss`; support for a dark mode toggle (user preference stored in localStorage)
- Responsiveness: Desktop-first; minimum supported width 1024px; tablets (768px) supported with layout adjustments; mobile view (< 768px) supported with simplified navigation
- Accessibility: WCAG 2.1 Level AA — all interactive elements keyboard-accessible; sufficient colour contrast ratios; ARIA labels on dynamic components
- Loading states: Skeleton loaders for all data-fetching components; no blank white screens
- Error states: Standardised error component for API failures; empty-state illustrations for empty lists

### 4.2 REST API Interface

- **Base URL:** `/api/v1/`
- **Protocol:** HTTPS only (HTTP requests redirect to HTTPS)
- **Payload format:** JSON (Content-Type: application/json)
- **Authentication:** `Authorization: Bearer <JWT>` header on all protected endpoints
- **Error response format (standardised):**
  ```json
  {
    "statusCode": 400,
    "error": "ValidationError",
    "message": "Email is already in use.",
    "details": { "field": "email" }
  }
  ```
- **Versioning:** URI versioning (`/api/v1/`, future versions at `/api/v2/`)
- **Pagination:** All list endpoints support `?page=1&pageSize=20`; response includes `totalCount`, `page`, `pageSize`

### 4.3 Real-Time Interface (SignalR)

| Hub          | Endpoint                | Events                                                        | Consumer      |
|--------------|-------------------------|---------------------------------------------------------------|---------------|
| Notification | `/hubs/notifications`   | `ReceiveNotification`, `MarkRead`                             | All roles     |
| Submission   | `/hubs/submissions`     | `SubmissionCountUpdated` (sends current count per task)       | Employee (during active task) |

- SignalR uses WebSockets with long-polling fallback.
- In production, SignalR is deployed on Render as a single instance (no Redis backplane required for v1.0).

### 4.4 Email Interface (SMTP)

- Configured in `appsettings.json` → `Email:SmtpHost`, `Email:SmtpPort`, `Email:From`, `Email:Password`
- Email templates: HTML templates using Razor (server-rendered) or a templating library
- All outbound emails must include: logo, branded header, clear call-to-action button, unsubscribe instruction (if applicable per FR-NOTIFY-06)

### 4.5 GitHub Integration

- Source control: All code, documentation, and migrations committed to GitHub repository
- Branch protection on `main` and `develop`
- GitHub Actions CI pipeline: build + unit tests must pass before merge
- GitHub Issues used for requirement tracking (issue ID referenced in commit messages and PR titles)

---

## 5. Data Model

### 5.1 Entity Reference

#### User
```
Id            : GUID (PK)
Name          : NVARCHAR(200) NOT NULL
Email         : NVARCHAR(300) NOT NULL UNIQUE
PasswordHash  : NVARCHAR(500) NOT NULL
Role          : ENUM (Admin=0, Trainer=1, Employee=2) NOT NULL
AvatarUrl     : NVARCHAR(500) NULL
IsActive      : BIT DEFAULT 1
TwoFAEnabled  : BIT DEFAULT 0
TwoFASecret   : NVARCHAR(200) NULL (encrypted at rest)
CreatedAt     : DATETIME2 NOT NULL
UpdatedAt     : DATETIME2 NOT NULL
```

#### Class
```
Id            : GUID (PK)
Name          : NVARCHAR(200) NOT NULL
Description   : NVARCHAR(1000) NULL
StartDate     : DATE NOT NULL
EndDate       : DATE NOT NULL
SkillTags     : NVARCHAR(500) NULL (comma-separated)
TrainerId     : GUID (FK → User where Role=Trainer)
IsArchived    : BIT DEFAULT 0
CreatedAt     : DATETIME2 NOT NULL
```

#### ClassEnrollment
```
Id            : GUID (PK)
EmployeeId    : GUID (FK → User where Role=Employee)
ClassId       : GUID (FK → Class)
EnrolledAt    : DATETIME2 NOT NULL
Status        : ENUM (Active=0, Transferred=1, Removed=2)
```
**Constraint:** Unique constraint on (EmployeeId) WHERE Status=Active — enforces single active enrollment.

#### Task
```
Id                   : GUID (PK)
ClassId              : GUID (FK → Class)
Title                : NVARCHAR(300) NOT NULL
Description          : NVARCHAR(MAX) NULL
Instructions         : NVARCHAR(MAX) NULL
TotalMarks           : INT NOT NULL (computed = SUM of question marks)
StartAt              : DATETIME2 NOT NULL
EndAt                : DATETIME2 NOT NULL
DurationMinutes      : INT NOT NULL
Status               : ENUM (Draft=0, Published=1, Closed=2)
CreatedByTrainerId   : GUID (FK → User)
CreatedAt            : DATETIME2 NOT NULL
```

#### Question
```
Id            : GUID (PK)
TaskId        : GUID (FK → Task)
Type          : ENUM (MCQ=0, Code=1, Assessment=2)
Stem          : NVARCHAR(MAX) NOT NULL
Marks         : INT NOT NULL
OrderIndex    : INT NOT NULL
Language      : NVARCHAR(50) NULL  (Code questions only: 'csharp'|'javascript'|'python')
ExpectedOutput: NVARCHAR(MAX) NULL  (Code questions only, trainer-visible)
```

#### MCQOption
```
Id            : GUID (PK)
QuestionId    : GUID (FK → Question WHERE Type=MCQ)
OptionText    : NVARCHAR(500) NOT NULL
IsCorrect     : BIT NOT NULL
OrderIndex    : INT NOT NULL
```

#### Submission
```
Id                : GUID (PK)
TaskId            : GUID (FK → Task)
EmployeeId        : GUID (FK → User WHERE Role=Employee)
StartedAt         : DATETIME2 NULL  (when employee first opened the task)
SubmittedAt       : DATETIME2 NULL
SubmissionRank    : INT NULL  (1-based, NULL if not submitted)
Multiplier        : DECIMAL(5,2) NULL  (0.00–1.00)
TotalRawScore     : DECIMAL(10,2) NULL
TotalFinalScore   : DECIMAL(10,2) NULL
Status            : ENUM (Draft=0, Submitted=1, Evaluated=2)
IsAutoSubmitted   : BIT DEFAULT 0
```

#### SubmissionAnswer
```
Id                : GUID (PK)
SubmissionId      : GUID (FK → Submission)
QuestionId        : GUID (FK → Question)
AnswerText        : NVARCHAR(MAX) NULL  (MCQ: optionId | Code: source code | Assessment: GitHub URL)
RawScore          : DECIMAL(10,2) NULL
FinalScore        : DECIMAL(10,2) NULL  (= RawScore × Submission.Multiplier)
EvaluatorNote     : NVARCHAR(MAX) NULL
IsPlagiarismFlag  : BIT DEFAULT 0
```

#### RefreshToken
```
Id            : GUID (PK)
UserId        : GUID (FK → User)
TokenHash     : NVARCHAR(500) NOT NULL  (BCrypt hash of token)
ExpiresAt     : DATETIME2 NOT NULL
IsRevoked     : BIT DEFAULT 0
CreatedAt     : DATETIME2 NOT NULL
```

#### Badge
```
Id            : GUID (PK)
Name          : NVARCHAR(100) NOT NULL
Description   : NVARCHAR(500) NOT NULL
IconUrl       : NVARCHAR(500) NOT NULL
Criteria      : NVARCHAR(MAX) NOT NULL  (JSON: criteria type + threshold)
```

#### EmployeeBadge
```
Id            : GUID (PK)
EmployeeId    : GUID (FK → User)
BadgeId       : GUID (FK → Badge)
AwardedAt     : DATETIME2 NOT NULL
```

#### XPLedger
```
Id            : GUID (PK)
EmployeeId    : GUID (FK → User)
Points        : INT NOT NULL
Source        : ENUM (TaskSubmission=0, Badge=1, Streak=2)
ReferenceId   : GUID NULL  (task ID or badge ID)
CreatedAt     : DATETIME2 NOT NULL
```

#### Notification
```
Id            : GUID (PK)
UserId        : GUID (FK → User)
Title         : NVARCHAR(200) NOT NULL
Body          : NVARCHAR(1000) NOT NULL
Type          : ENUM (TaskAssigned=0, TaskEvaluated=1, BadgeEarned=2, Announcement=3, DeadlineReminder=4)
IsRead        : BIT DEFAULT 0
CreatedAt     : DATETIME2 NOT NULL
```

#### Announcement
```
Id               : GUID (PK)
CreatedByUserId  : GUID (FK → User)
ClassId          : GUID NULL  (FK → Class; NULL = system-wide)
Title            : NVARCHAR(300) NOT NULL
Body             : NVARCHAR(MAX) NOT NULL
CreatedAt        : DATETIME2 NOT NULL
```

#### AuditLog
```
Id            : GUID (PK)
UserId        : GUID NULL  (FK → User; NULL for anonymous attempts)
Action        : NVARCHAR(100) NOT NULL  (e.g. 'Login.Success', 'Task.Created', 'User.Deactivated')
Entity        : NVARCHAR(100) NULL
EntityId      : GUID NULL
IPAddress     : NVARCHAR(50) NOT NULL
UserAgent     : NVARCHAR(500) NULL
Timestamp     : DATETIME2 NOT NULL
```

#### PasswordResetOTP
```
Id            : GUID (PK)
UserId        : GUID (FK → User)
OTPHash       : NVARCHAR(500) NOT NULL  (BCrypt hash)
ExpiresAt     : DATETIME2 NOT NULL
IsUsed        : BIT DEFAULT 0
CreatedAt     : DATETIME2 NOT NULL
```

### 5.2 Relationships Summary

```
User (Trainer) ──< Class (TrainerId)
Class ──< ClassEnrollment >── User (Employee)
Class ──< Task
Task ──< Question ──< MCQOption
Task ──< Submission >── User (Employee)
Submission ──< SubmissionAnswer >── Question
User ──< EmployeeBadge >── Badge
User ──< XPLedger
User ──< Notification
User ──< RefreshToken
User ──< PasswordResetOTP
Announcement >── Class (optional)
```

---

## 6. API Endpoint Inventory

All endpoints require `Authorization: Bearer <JWT>` unless marked **(Public)**.

### 6.1 Auth — `/api/v1/auth`

| Method | Path                        | Role       | Description                              |
|--------|-----------------------------|------------|------------------------------------------|
| POST   | `/login`                    | Public     | Authenticate user, return JWT + refresh token |
| POST   | `/refresh`                  | Public     | Exchange refresh token for new JWT       |
| POST   | `/logout`                   | Any        | Revoke refresh token                     |
| POST   | `/forgot-password`          | Public     | Send OTP to email                        |
| POST   | `/verify-otp`               | Public     | Verify OTP, return reset token           |
| POST   | `/reset-password`           | Public     | Set new password using reset token       |
| POST   | `/2fa/enable`               | Any        | Enable TOTP 2FA, return QR code URI      |
| POST   | `/2fa/verify`               | Any        | Verify TOTP code to activate 2FA         |
| POST   | `/2fa/disable`              | Any        | Disable 2FA (requires password confirm)  |

### 6.2 Users — `/api/v1/users`

| Method | Path                        | Role            | Description                              |
|--------|-----------------------------|-----------------|------------------------------------------|
| GET    | `/`                         | Admin           | List all users (paginated, filterable)   |
| POST   | `/`                         | Admin           | Create user (any role)                   |
| GET    | `/:id`                      | Admin/Self      | Get user details                         |
| PUT    | `/:id`                      | Admin/Self      | Update user details                      |
| PATCH  | `/:id/deactivate`           | Admin           | Deactivate user account                  |
| PATCH  | `/:id/reactivate`           | Admin           | Reactivate user account                  |
| POST   | `/import`                   | Admin, Trainer  | Bulk CSV import of employees             |
| GET    | `/me`                       | Any             | Get current user profile                 |
| PUT    | `/me/avatar`                | Any             | Upload profile avatar                    |
| PUT    | `/me/password`              | Any             | Change own password                      |
| POST   | `/trainer/employees`        | Trainer         | Trainer creates employee (auto-enrolled) |

### 6.3 Classes — `/api/v1/classes`

| Method | Path                              | Role            | Description                               |
|--------|-----------------------------------|-----------------|-------------------------------------------|
| GET    | `/`                               | Admin/Trainer   | List classes (Admin: all; Trainer: own)   |
| POST   | `/`                               | Admin           | Create class                              |
| GET    | `/:id`                            | Admin/Trainer   | Get class details                         |
| PUT    | `/:id`                            | Admin           | Update class                              |
| PATCH  | `/:id/archive`                    | Admin           | Archive class                             |
| GET    | `/:id/employees`                  | Admin/Trainer   | List enrolled employees in class          |
| POST   | `/:id/enroll`                     | Admin/Trainer   | Enroll employee in class                  |
| POST   | `/:id/transfer`                   | Admin/Trainer   | Transfer employee to another class        |
| DELETE | `/:id/employees/:employeeId`      | Admin/Trainer   | Remove employee from class                |
| GET    | `/me`                             | Employee        | Get own enrolled class                    |

### 6.4 Tasks — `/api/v1/tasks`

| Method | Path                        | Role            | Description                              |
|--------|-----------------------------|-----------------|------------------------------------------|
| GET    | `/`                         | Admin/Trainer   | List tasks for trainer's class(es)       |
| POST   | `/`                         | Trainer         | Create task (Draft)                      |
| GET    | `/:id`                      | Any             | Get task details                         |
| PUT    | `/:id`                      | Trainer         | Update task (Draft only)                 |
| DELETE | `/:id`                      | Trainer         | Delete task (Draft only)                 |
| PATCH  | `/:id/publish`              | Trainer         | Publish task                             |
| POST   | `/:id/duplicate`            | Trainer         | Duplicate task as new Draft              |
| GET    | `/me`                       | Employee        | Get tasks assigned to me                 |

### 6.5 Questions — `/api/v1/tasks/:taskId/questions`

| Method | Path        | Role    | Description                                  |
|--------|-------------|---------|----------------------------------------------|
| GET    | `/`         | Any     | List questions for task                      |
| POST   | `/`         | Trainer | Add question to task (Draft only)            |
| PUT    | `/:id`      | Trainer | Update question (Draft only)                 |
| DELETE | `/:id`      | Trainer | Remove question (Draft only)                 |
| PATCH  | `/reorder`  | Trainer | Reorder questions (body: array of ids)       |

### 6.6 Submissions — `/api/v1/submissions`

| Method | Path                          | Role     | Description                                       |
|--------|-------------------------------|----------|---------------------------------------------------|
| POST   | `/`                           | Employee | Start submission (creates Draft record)           |
| GET    | `/:id`                        | Any      | Get submission detail                             |
| PUT    | `/:id/draft`                  | Employee | Save draft answers                                |
| POST   | `/:id/submit`                 | Employee | Final submit (records rank, applies multiplier)   |
| GET    | `/task/:taskId`               | Trainer  | List all submissions for a task (eval queue)      |
| GET    | `/task/:taskId/me`            | Employee | Get own submission for a task                     |

### 6.7 Evaluations — `/api/v1/evaluations`

| Method | Path                                | Role    | Description                                |
|--------|-------------------------------------|---------|--------------------------------------------|
| PUT    | `/submissions/:id`                  | Trainer | Evaluate a submission (marks + notes)      |
| PATCH  | `/submissions/:id/complete`         | Trainer | Mark submission as Evaluated               |
| PATCH  | `/answers/:id/plagiarism`           | Trainer | Set/unset plagiarism flag on answer        |
| POST   | `/task/:taskId/bulk-complete`       | Trainer | Bulk-complete all-MCQ task evaluations     |

### 6.8 Leaderboards — `/api/v1/leaderboards`

| Method | Path                        | Role    | Description                                      |
|--------|-----------------------------|---------|--------------------------------------------------|
| GET    | `/class/:classId`           | Any     | Class leaderboard (query: period=week/month/all) |
| GET    | `/global`                   | Any     | Global leaderboard (query: period=week/month/all)|

### 6.9 Badges — `/api/v1/badges`

| Method | Path                  | Role    | Description                     |
|--------|-----------------------|---------|---------------------------------|
| GET    | `/`                   | Admin   | List all badge definitions      |
| GET    | `/me`                 | Employee| Get own earned badges           |
| GET    | `/user/:id`           | Trainer | Get an employee's badges        |

### 6.10 Reports — `/api/v1/reports`

| Method | Path                          | Role            | Description                             |
|--------|-------------------------------|-----------------|------------------------------------------|
| GET    | `/class/:classId`             | Admin/Trainer   | Class performance report (JSON)          |
| GET    | `/class/:classId/export`      | Admin/Trainer   | Export class report (query: format=pdf/xlsx) |
| GET    | `/me`                         | Employee        | Personal performance report (JSON)       |
| GET    | `/me/export`                  | Employee        | Export personal report (format=pdf)      |
| GET    | `/admin/system`               | Admin           | System-wide report                       |
| GET    | `/admin/system/export`        | Admin           | Export system report                     |

### 6.11 Notifications — `/api/v1/notifications`

| Method | Path                        | Role    | Description                             |
|--------|-----------------------------|---------|-----------------------------------------|
| GET    | `/`                         | Any     | List notifications for current user     |
| PATCH  | `/:id/read`                 | Any     | Mark notification as read               |
| PATCH  | `/read-all`                 | Any     | Mark all as read                        |
| GET    | `/announcements`            | Any     | List announcements relevant to user     |
| POST   | `/announcements`            | Admin/Trainer | Create announcement               |

### 6.12 Admin — `/api/v1/admin`

| Method | Path                        | Role  | Description                                      |
|--------|-----------------------------|-------|--------------------------------------------------|
| GET    | `/stats`                    | Admin | System-wide stats (users, classes, tasks, etc.)  |
| GET    | `/audit-log`                | Admin | Paginated audit log with filters                 |
| GET    | `/trainers/report`          | Admin | All-trainers performance rollup                  |
| GET    | `/classes/report`           | Admin | All-classes comparison report                    |

---

## 7. Angular Route & Screen Inventory

### 7.1 Public Routes (no auth required)

| Route                   | Component / Screen         |
|-------------------------|----------------------------|
| `/login`                | Login Page                 |
| `/forgot-password`      | Forgot Password Page       |
| `/verify-otp`           | OTP Verification Page      |
| `/reset-password`       | Reset Password Page        |

### 7.2 Shared Routes (all authenticated roles)

| Route            | Component / Screen         |
|------------------|----------------------------|
| `/profile`       | User Profile Page          |
| `/notifications` | Notifications Full Page    |
| `/announcements` | Announcements Feed         |

### 7.3 Admin Routes (role guard: Admin only)

| Route                       | Component / Screen                |
|-----------------------------|-----------------------------------|
| `/admin/dashboard`          | Admin Overview Dashboard          |
| `/admin/users`              | User Management Table             |
| `/admin/users/new`          | Create User Form                  |
| `/admin/users/:id`          | Edit User Form                    |
| `/admin/classes`            | All Classes List                  |
| `/admin/classes/new`        | Create Class Form                 |
| `/admin/classes/:id`        | Class Detail (Admin view)         |
| `/admin/reports`            | System Report + Export            |
| `/admin/audit-log`          | Audit Log Viewer                  |
| `/admin/leaderboard`        | System-Wide Leaderboard           |

### 7.4 Trainer Routes (role guard: Trainer only)

| Route                               | Component / Screen                     |
|-------------------------------------|----------------------------------------|
| `/trainer/dashboard`                | Trainer Class Overview Dashboard       |
| `/trainer/classes/:id`              | Class Detail + Employee List           |
| `/trainer/classes/:id/leaderboard`  | Class Leaderboard                      |
| `/trainer/employees/:id`            | Individual Employee Dashboard          |
| `/trainer/tasks`                    | Task List (Trainer's class)            |
| `/trainer/tasks/new`                | Create Task Form + Question Builder    |
| `/trainer/tasks/:id`                | Task Detail / Edit                     |
| `/trainer/tasks/:id/evaluate`       | Evaluation Queue for Task              |
| `/trainer/reports`                  | Class Reports + Export                 |
| `/trainer/announcements/new`        | Create Announcement Form               |

### 7.5 Employee Routes (role guard: Employee only)

| Route                           | Component / Screen                   |
|---------------------------------|--------------------------------------|
| `/employee/dashboard`           | My Dashboard (home)                  |
| `/employee/tasks`               | My Tasks List                        |
| `/employee/tasks/:id`           | Task Overview (before attempt)       |
| `/employee/tasks/:id/attempt`   | Task Attempt Screen (timer + editor) |
| `/employee/tasks/:id/result`    | Task Result + Evaluator Feedback     |
| `/employee/leaderboard`         | Class Leaderboard (view only)        |
| `/employee/reports`             | Personal Reports + Export            |
| `/employee/badges`              | Badge Wall (all earned badges)       |

---

## 8. Non-Functional Requirements

### 8.1 Performance

| ID     | Requirement                                                                             |
|--------|-----------------------------------------------------------------------------------------|
| NFR-01 | API endpoints (read operations) must respond within **300ms** at the 95th percentile under a load of 200 concurrent users. |
| NFR-02 | Dashboard pages must complete initial data load within **2 seconds** on a standard broadband connection. |
| NFR-03 | The submission ranking operation (FR-GAMIFY-01) must be atomic and complete within **100ms** to prevent rank ties under high concurrent submission load. Use database-level serializable transaction or optimistic locking with retry. |
| NFR-04 | PDF report generation must complete within **10 seconds** for reports spanning up to 200 employees and 50 tasks. |

### 8.2 Security

| ID     | Requirement                                                                             |
|--------|-----------------------------------------------------------------------------------------|
| NFR-05 | JWTs must be signed with RS256 (asymmetric). Private key stored in environment variable / Azure Key Vault. Public key used for verification only. |
| NFR-06 | All passwords must be hashed using **BCrypt** (work factor ≥ 12). Plaintext passwords must never be logged or stored. |
| NFR-07 | All API input must be validated and sanitised server-side (whitelist approach). SQL injection mitigated by EF Core parameterised queries. XSS mitigated by Angular's built-in template escaping. |
| NFR-08 | Authentication endpoints (`/login`, `/forgot-password`) must be rate-limited: maximum 10 requests per IP per minute. Exceeding this returns HTTP 429. |
| NFR-09 | HTTPS must be enforced. HTTP Strict Transport Security (HSTS) header must be set. Minimum TLS 1.2. |
| NFR-10 | 2FA secrets (TOTP) must be encrypted at rest in the database using AES-256. |
| NFR-11 | The application must mitigate all OWASP Top 10 (2021) vulnerabilities. Security review must be conducted before production deployment. |

### 8.3 Reliability

| ID     | Requirement                                                                             |
|--------|-----------------------------------------------------------------------------------------|
| NFR-12 | The system must achieve **99.5% uptime** (measured monthly), allowing approximately 3.6 hours of downtime per month. |
| NFR-13 | All API responses must follow the standardised error format defined in Section 4.2. Unhandled exceptions must return HTTP 500 with a generic message (no stack traces in production responses). |
| NFR-14 | The database must be backed up daily (full backup). Backup retention: 30 days. Recovery Time Objective (RTO): 4 hours. Recovery Point Objective (RPO): 24 hours. |
| NFR-15 | Draft answers (FR-TASK-10) must be persisted to the database within 5 seconds of the auto-save trigger. Data loss from browser crash must not exceed 30 seconds of work. |

### 8.4 Scalability

| ID     | Requirement                                                                             |
|--------|-----------------------------------------------------------------------------------------|
| NFR-16 | The API must be **stateless** (all session state in JWT + DB). Horizontal scaling must require no application code changes. |
| NFR-17 | The API must minimise repeated database queries for frequently read data (leaderboards, reports). EF Core query projection and in-process caching may be used. Redis is not required for v1.0. |
| NFR-18 | The system must support up to **500 concurrent users** without degradation (achieved via horizontal scaling of the API tier). |

### 8.5 Maintainability

| ID     | Requirement                                                                             |
|--------|-----------------------------------------------------------------------------------------|
| NFR-19 | Backend code must follow **Clean Architecture** (Domain / Application / Infrastructure / Presentation layers). Dependencies must point inward only. |
| NFR-20 | Frontend code must follow **Angular feature module** structure: one module per domain (auth, tasks, leaderboard, reports, etc.). |
| NFR-21 | Business logic services must have **≥ 80% unit test coverage**. All API endpoints must have integration tests using an in-memory test database. |
| NFR-22 | All commits must follow the Conventional Commits format (`feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`). |
| NFR-23 | Entity Framework migrations must be maintained as versioned migration files. No raw SQL in application code except in explicitly named raw-query repositories. |

### 8.6 Usability

| ID     | Requirement                                                                             |
|--------|-----------------------------------------------------------------------------------------|
| NFR-24 | All interactive elements (buttons, form fields, links) must be keyboard-accessible (Tab order, Enter/Space activation). |
| NFR-25 | Colour contrast ratio must meet WCAG 2.1 AA: minimum 4.5:1 for normal text, 3:1 for large text. |
| NFR-26 | All data-fetching components must display skeleton loaders during loading and a user-friendly empty-state illustration when no data exists. |
| NFR-27 | Forms must provide inline validation feedback. Required fields must be labelled. Submission errors must be displayed at the field level. |

---

## 9. Appendix A — Gamification Multiplier Table

| Submission Rank | Multiplier | Final Score Calculation        |
|-----------------|------------|--------------------------------|
| 1 – 5           | 1.00 (100%)| final_score = raw_score × 1.00 |
| 6 – 10          | 0.80 (80%) | final_score = raw_score × 0.80 |
| 11 – 15         | 0.60 (60%) | final_score = raw_score × 0.60 |
| 16 – 20         | 0.40 (40%) | final_score = raw_score × 0.40 |
| 21 – 25         | 0.20 (20%) | final_score = raw_score × 0.20 |
| 26+             | 0.00 (0%)  | final_score = 0                |
| Not Submitted   | 0.00 (0%)  | final_score = 0 (auto-set)     |

> **Note:** The multiplier is applied to the **raw score** (marks earned from correct answers / trainer grading) — not to the maximum possible marks. An employee who scores 40/100 raw in Tier 1 (rank 1–5) earns a final score of 40. An employee who scores 100/100 raw in Tier 2 (rank 6–10) earns a final score of 80.

---

## 10. Appendix B — Badge Criteria Reference

| Badge Name       | Icon        | Award Criteria                                                                | XP Bonus |
|------------------|-------------|-------------------------------------------------------------------------------|----------|
| Speed Demon      | ⚡          | Submission rank is 1–5 for any task                                           | +20 XP   |
| Perfect Score    | ⭐          | Raw score = 100% of total marks on any task                                   | +20 XP   |
| Consistent       | 🔥          | Submitted on time for 5 consecutive tasks                                     | +20 XP   |
| Streak Master    | 📅          | Active submission streak reaches 10 consecutive days                          | +20 XP   |
| Top of Class     | 🏆          | Ranked #1 on the class leaderboard at the end of any calendar week            | +20 XP   |
| Early Bird       | 🌅          | Submits a task within the first 10 minutes of the task window opening         | +20 XP   |
| Comeback King    | 👑          | Improves score by ≥ 30% compared to the previous task (minimum 2 tasks done) | +20 XP   |

Badges are **cumulative** — a user can earn the same badge multiple times (each award is a separate EmployeeBadge record and XP event). The badge wall shows count if earned multiple times.

---

## 11. Appendix C — Question Type Specifications

### C.1 MCQ (Type A)

```
Question fields:
  - Stem: Rich text (HTML) — question body
  - Marks: Integer ≥ 1

4 Option fields (MCQOption):
  - OptionText: Plain text (max 500 chars)
  - IsCorrect: Boolean (exactly ONE must be true)

Employee UI:
  - Radio button group (single selection)
  - Options presented in shuffled order (shuffled per employee session to reduce copying)

Grading:
  - Automatic on submission
  - RawScore = question.Marks if correct, else 0
  - No partial credit
```

### C.2 Code Question (Type B)

```
Question fields:
  - Stem: Rich text — problem statement
  - Language: enum (csharp | javascript | python)
  - ExpectedOutput: Text (trainer reference, NOT shown to employee)
  - Marks: Integer ≥ 1

Employee UI:
  - Monaco Editor (or CodeMirror) embedded code editor
  - Language-appropriate syntax highlighting
  - Code persisted to draft on auto-save

Grading:
  - Manual by trainer
  - Trainer sees employee's code side-by-side with problem statement and ExpectedOutput
  - Trainer enters RawScore (0 to question.Marks)
  - Trainer can write EvaluatorNote
  - Trainer can set IsPlagiarismFlag
```

### C.3 Assessment / GitHub (Type C)

```
Question fields:
  - Stem: Rich text — instructions for the GitHub repository submission
  - Marks: Integer ≥ 1

Employee UI:
  - Text input field (validated against GitHub URL pattern)
  - Pattern: ^https://github\.com/[a-zA-Z0-9_.-]+/[a-zA-Z0-9_.-]+(/.*)?$
  - Clickable link preview shown after entry so employee can verify their URL

Grading:
  - Manual by trainer
  - Trainer sees the URL as a clickable link, opens in new tab
  - Trainer enters RawScore (0 to question.Marks)
  - Trainer can write EvaluatorNote
  - No plagiarism flag for Assessment type (GitHub repos are inherently identifiable)
```

---

*SmartSkill Performance Monitoring System — SRS v1.0.0 — Confidential*
*IEEE Std 830-1998 compliant*
