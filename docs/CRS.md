# Customer Requirements Specification (CRS)

---

| Field            | Details                                              |
|------------------|------------------------------------------------------|
| **Document**     | Customer Requirements Specification                  |
| **System**       | SmartSkill Performance Monitoring System (SSPMS)     |
| **Version**      | 1.0.0                                                |
| **Date**         | 2026-03-20                                           |
| **Status**       | Draft — Pending Product Owner Sign-off               |
| **Prepared by**  | Saksham Tapadia, Ayush Mathur, Aman Nahar, Diya Garg, Pankhuri |
| **Product Owner**| Benhar Charles Sir                                   |

---

## Revision History

| Version | Date       | Author          | Description             |
|---------|------------|-----------------|-------------------------|
| 1.0.0   | 2026-03-20 | Team SSPMS      | Initial draft            |

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Project Background & Motivation](#2-project-background--motivation)
3. [Business Objectives](#3-business-objectives)
4. [Stakeholder Register](#4-stakeholder-register)
5. [High-Level Business Requirements](#5-high-level-business-requirements)
6. [User Journeys](#6-user-journeys)
7. [Acceptance Criteria](#7-acceptance-criteria)
8. [Success Metrics (KPIs)](#8-success-metrics-kpis)
9. [Constraints & Risks](#9-constraints--risks)
10. [Project Phases & Roadmap](#10-project-phases--roadmap)
11. [Sign-off](#11-sign-off)

---

## 1. Executive Summary

The **SmartSkill Performance Monitoring System (SSPMS)** is a web-based platform designed to transform the way employee training programs are managed, monitored, and experienced. It provides a centralized, role-aware environment where **administrators** govern the system, **trainers** design and evaluate assessments, and **employees** engage with their learning journey in a gamified, competitive setting.

SSPMS moves beyond passive training delivery. By embedding leaderboards, submission-speed bonuses, achievement badges, and real-time performance analytics, it creates a high-motivation learning culture where employees are intrinsically driven to perform. Trainers gain unprecedented visibility into class and individual performance, enabling targeted intervention. Administrators maintain full oversight of the platform through comprehensive reporting and audit capabilities.

The platform is built on **.NET 9** (backend API) and **Angular 19** (frontend SPA), deployed on **Render** (backend), **Vercel** (frontend), and **Neon** (PostgreSQL database), with all source code managed on **GitHub** and all collaboration conducted through professional pull-request workflows.

---

## 2. Project Background & Motivation

### 2.1 The Problem

Many organisations run structured employee training programs but lack a unified platform to:
- Assign, time, and track training assessments digitally
- Automatically evaluate multiple-choice submissions
- Provide employees with transparent, real-time feedback on their performance
- Motivate employees to submit early and consistently through reward mechanisms
- Give trainers a data-driven view of class progress without manual collation

Training performance data is either absent, scattered, or delayed, leading to missed intervention opportunities and disengaged trainees.

### 2.2 The Solution

SSPMS provides a **single platform** that covers the full lifecycle of a training assessment:

```
Task Creation → Assignment → Attempt (Timed) → Submission
                                    ↓
                           Gamification Engine
                       (rank recorded, multiplier applied)
                                    ↓
                   Trainer Evaluation → Results Published
                                    ↓
                 Employee Dashboard → Reports → Leaderboard
```

The **gamification layer** is the differentiating feature: submission order determines a multiplier on final marks (first 5 get 100%, next 5 get 80%, and so on), creating urgency and healthy competition without compromising fairness — everyone starts with the same questions at the same time.

---

## 3. Business Objectives

| ID    | Objective                                                                                     | Target                           |
|-------|-----------------------------------------------------------------------------------------------|----------------------------------|
| BO-01 | Increase training task completion rates through gamified incentives                           | ≥ 40% improvement vs. baseline   |
| BO-02 | Enable trainers to identify underperforming employees quickly via dashboards                  | Within 24 hours of task close    |
| BO-03 | Eliminate manual grading overhead for objective (MCQ) question types                         | 100% auto-grading for MCQs       |
| BO-04 | Provide employees with transparent, personalised performance feedback                         | Every employee, every task       |
| BO-05 | Standardise assessment delivery across all trainers and classes on a single platform          | All assessments on SSPMS         |
| BO-06 | Foster a healthy competitive culture through public leaderboards and achievement badges       | Measured by badge unlock rate    |

---

## 4. Stakeholder Register

| Stakeholder         | Role                 | Primary Interest                                        | Communication Channel     |
|---------------------|----------------------|---------------------------------------------------------|---------------------------|
| Benhar Charles Sir  | Product Owner        | Scope alignment, quality, delivery milestones           | GitHub Issues + Meetings  |
| Trainers            | Primary Users        | Easy task creation, reliable evaluation, rich reporting | In-app + Email            |
| Employees           | End Users            | Fair evaluation, motivating UX, clear performance data  | In-app + Email            |
| Saksham Tapadia     | Dev Lead / Team      | Technical clarity, feasibility, GitHub workflow         | GitHub PRs                |
| Ayush Mathur        | Team Member          | Delivery, code quality                                  | GitHub PRs                |
| Aman Nahar          | Team Member          | Delivery, code quality                                  | GitHub PRs                |
| Diya Garg           | Team Member          | Delivery, code quality                                  | GitHub PRs                |
| Pankhuri            | Team Member          | Delivery, code quality                                  | GitHub PRs                |

---

## 5. High-Level Business Requirements

### 5.1 Access & Security (BR-01 – BR-04)

| ID    | Business Requirement                                                                                    |
|-------|--------------------------------------------------------------------------------------------------------|
| BR-01 | Users must log in with a verified email and password. The system must enforce role-based access — Admin, Trainer, and Employee roles must see only what is permitted for their role. |
| BR-02 | Employees must not be able to access trainer or admin functions under any circumstance.                |
| BR-03 | All user activity that affects data (login, task creation, evaluation) must be logged for audit purposes. |
| BR-04 | Users who forget their password must be able to reset it securely via email. Optional two-factor authentication must be available for added account security. |

### 5.2 User & Class Management (BR-05 – BR-08)

| ID    | Business Requirement                                                                                    |
|-------|--------------------------------------------------------------------------------------------------------|
| BR-05 | Administrators must be able to create, edit, and deactivate both Trainer and Employee accounts. Trainers must be able to create Employee accounts. Employees cannot create accounts. |
| BR-06 | Each employee must be enrolled in exactly one class at a time. Transferring an employee between classes must be possible and must retain historical enrollment records. |
| BR-07 | Each trainer may manage one or more classes. A class belongs to exactly one trainer at a time.         |
| BR-08 | Administrators and trainers must be able to import multiple employee records at once via a file upload (CSV format). |

### 5.3 Assessment & Task Management (BR-09 – BR-12)

| ID    | Business Requirement                                                                                    |
|-------|--------------------------------------------------------------------------------------------------------|
| BR-09 | Trainers must be able to create assessments (tasks) with a defined start time, end time, and duration. Tasks must be assigned to a class so that all enrolled employees receive them simultaneously. |
| BR-10 | Tasks must support three question formats: multiple-choice questions (MCQ), code-based questions where the employee writes code in-browser, and assessment questions where the employee submits a link to a GitHub repository. |
| BR-11 | Employees must be able to see the marks allocated to each question before and after their attempt. They must be able to save their progress and resume before final submission. |
| BR-12 | Once an employee submits a task, they must not be allowed to reopen or modify it. If the timer expires, the system must auto-submit whatever the employee has answered. |

### 5.4 Gamification & Scoring (BR-13 – BR-14)

| ID    | Business Requirement                                                                                    |
|-------|--------------------------------------------------------------------------------------------------------|
| BR-13 | Submission speed must influence final marks. The first 5 employees to submit receive 100% of available marks; the next 5 receive 80%; the next 5 receive 60%; and so on, reducing by 20% per tier. Employees who do not submit before the deadline receive 0% of available marks. |
| BR-14 | The system must award achievement badges automatically for milestones such as submitting in the top 5, achieving a perfect raw score, maintaining a submission streak, and topping the class leaderboard. An experience points (XP) system must power a class leaderboard and a system-wide leaderboard. |

### 5.5 Evaluation & Feedback (BR-15 – BR-16)

| ID    | Business Requirement                                                                                    |
|-------|--------------------------------------------------------------------------------------------------------|
| BR-15 | MCQ questions must be automatically graded when an employee submits. Code and GitHub-assessment questions must be manually evaluated by the trainer, who must be able to assign marks and provide written feedback. Trainers must also be able to flag suspected plagiarism on code submissions. |
| BR-16 | Employees must be notified when their task has been evaluated and must be able to view their marks and any feedback left by the trainer. |

### 5.6 Dashboards & Reporting (BR-17 – BR-19)

| ID    | Business Requirement                                                                                    |
|-------|--------------------------------------------------------------------------------------------------------|
| BR-17 | Trainers must have access to: a class-level dashboard showing overall performance and completion, an individual employee dashboard showing detailed history and skill gaps, a class leaderboard, per-task reports, and a day-wise activity heatmap. |
| BR-18 | Employees must have access to: a personal dashboard showing their rank, XP, and badges, a daily performance report compared to the class average, a score history over time, and a skill analysis chart identifying strength and gap areas. |
| BR-19 | Administrators must have access to a system-wide overview covering all users, classes, tasks, and activity. Any report must be exportable to PDF and Excel. Trainers must receive a weekly performance digest via email. |

### 5.7 Communication (BR-20)

| ID    | Business Requirement                                                                                    |
|-------|--------------------------------------------------------------------------------------------------------|
| BR-20 | Trainers and administrators must be able to broadcast announcements to a class. All users must receive real-time in-app notifications and email alerts for key events: task assigned, task evaluated, badge earned, and upcoming deadline reminders. |

---

## 6. User Journeys

### 6.1 Admin Journey

```
1. Log in → Land on Admin Dashboard (system-wide stats)
2. Create Trainer accounts → Assign them to classes
3. Create Employee accounts OR let Trainers create them
4. Monitor all classes and tasks from the admin panel
5. Review audit logs for any suspicious activity
6. Generate and export system-wide or class-specific reports
7. Broadcast system-wide announcements
```

### 6.2 Trainer Journey

```
1. Log in → Land on Trainer Dashboard (class snapshot)
2. View enrolled employees in their class
3. Create a new Task:
   a. Set title, description, start/end time, duration
   b. Add questions (MCQ / Code / GitHub assessment)
   c. Assign marks per question
4. Publish task to class → all employees are notified
5. Monitor live submission count during the task window
6. Once task closes → enter Evaluation Queue
   a. MCQs already graded — review flagged submissions
   b. Grade code questions → add feedback → optionally flag plagiarism
   c. Grade GitHub assessment submissions
7. View Class Dashboard: completion rates, avg scores, leaderboard
8. Drill into Individual Employee Dashboard for targeted coaching
9. Export class report to PDF or Excel
```

### 6.3 Employee Journey

```
1. Log in → Land on My Dashboard (rank, XP bar, badges, upcoming tasks)
2. See notification: "New task assigned — [Task Name] starts at [Time]"
3. When task window opens → click "Start Attempt"
4. See countdown timer + live counter: "X students have already submitted"
5. Answer questions:
   a. MCQ — select one option
   b. Code — write code in the embedded editor
   c. Assessment — paste GitHub repository URL
6. Save progress (draft) at any point
7. Click "Submit" → see submission rank and estimated multiplier tier
8. Receive notification when trainer evaluates
9. View Results: raw score, final score (after multiplier), per-question feedback
10. Badge unlocked (if applicable) → notification received
11. Visit My Dashboard → updated rank, XP, charts, streak counter
12. View Daily Report → compare score to class average
```

---

## 7. Acceptance Criteria

### BR-13 — Gamification Scoring

| Given | When | Then |
|-------|------|------|
| A task has been published to a class | The 3rd employee submits | Their submission rank is 3, multiplier is 100%, final score = raw score × 1.0 |
| A task has been published to a class | The 7th employee submits | Their submission rank is 7, multiplier is 80%, final score = raw score × 0.8 |
| A task has been published to a class | The task deadline passes with an employee who has not submitted | Their final score is recorded as 0 automatically |
| Two employees submit at the same clock second | The system assigns submission rank | Rank is assigned by server-side insertion order (first to reach the server wins) |

### BR-09 — Task Window Enforcement

| Given | When | Then |
|-------|------|------|
| A task has a start time of 14:00 | An employee tries to access the task at 13:59 | The task shows as "Not Yet Open" and the attempt button is disabled |
| A task has an end time of 15:00 | An employee is mid-attempt at 15:00 | The system auto-submits all saved answers; employee sees "Time's up — submitted automatically" |

### BR-06 — Single Class Enrollment

| Given | When | Then |
|-------|------|------|
| An employee is already enrolled in Class A | An admin attempts to enroll them in Class B | The system archives the Class A enrollment and creates a new enrollment in Class B |
| An employee is enrolled in Class A | A trainer from Class B tries to add the same employee | The system prevents the action and displays a warning that the employee is already enrolled elsewhere |

### BR-15 — Plagiarism Flag

| Given | When | Then |
|-------|------|------|
| A trainer evaluates a code submission | The trainer marks it as plagiarism-suspected | The submission's final marks are set to 0 and a plagiarism flag appears on the employee's task report |

---

## 8. Success Metrics (KPIs)

| KPI                             | Definition                                                          | Target          |
|---------------------------------|---------------------------------------------------------------------|-----------------|
| Task Completion Rate            | % of enrolled employees who submit before the deadline              | ≥ 85%           |
| Average Score Trend             | Week-over-week change in class average score                        | Upward trend    |
| Time-to-Evaluate                | Hours between task close and all submissions evaluated              | ≤ 48 hours      |
| Leaderboard Engagement Rate     | % of employees who check the leaderboard at least once per week     | ≥ 60%           |
| Badge Unlock Rate               | Average number of badges earned per employee per month              | ≥ 2 badges/mo   |
| Early Submission Rate           | % of submissions landing in the top 10 (Tier 1 or Tier 2)          | ≥ 30%           |
| Report Export Usage             | Number of PDF/Excel exports generated per trainer per month         | ≥ 4/month       |

---

## 9. Constraints & Risks

### 9.1 Constraints

| Constraint          | Detail                                                                              |
|---------------------|-------------------------------------------------------------------------------------|
| Technology Stack    | Backend: .NET 9 (ASP.NET Core); Frontend: Angular 19. No deviations.               |
| Database            | PostgreSQL via Neon (serverless cloud). No other RDBMS.                             |
| Deployment          | Backend on Render, frontend on Vercel, database on Neon.                            |
| Source Control      | All code on GitHub; PR-based workflow enforced.                                     |
| Browser Support     | Chrome, Firefox, and Edge (latest 2 versions each). No IE support.                 |
| Language            | Application UI language: English.                                                   |

### 9.2 Risk Register

| Risk ID | Risk Description                                      | Probability | Impact | Mitigation Strategy                                              |
|---------|-------------------------------------------------------|-------------|--------|------------------------------------------------------------------|
| R-01    | SignalR real-time features under high concurrent load | Medium      | Medium | Use Redis backplane for SignalR; load test before release        |
| R-02    | Plagiarism false positives damaging employee records  | Low         | High   | Trainer manual override always available; flag ≠ automatic zero |
| R-03    | Scope creep from new feature requests post-sign-off   | High        | High   | All changes require CRS version update + PO sign-off            |
| R-04    | Gamification mechanics perceived as unfair            | Low         | Medium | Transparent tier display before and during task                 |
| R-05    | Data loss on auto-submit if connectivity drops        | Low         | High   | Draft auto-save every 30 seconds; optimistic sync               |
| R-06    | Timezone mismatches for task scheduling               | Medium      | Medium | Single timezone configured per deployment; displayed to users   |

---

## 10. Project Phases & Roadmap

| Phase   | Weeks   | Deliverables                                                                   |
|---------|---------|--------------------------------------------------------------------------------|
| Phase 1 | 1 – 2   | Authentication, JWT, RBAC, User Management (CRUD), Class Management            |
| Phase 2 | 3 – 4   | Task creation, Question types (MCQ/Code/Assessment), Submission engine, Auto-grading |
| Phase 3 | 5 – 6   | Gamification engine (submission ranking, multipliers), Badge system, XP Ledger, Leaderboards |
| Phase 4 | 7 – 8   | All Dashboards (Admin / Trainer / Employee), Charts (bar, pie, line, radar), Activity Heatmap |
| Phase 5 | 9       | Notifications (SignalR + email), Announcements, PDF/Excel Export, Scheduled Reports |
| Phase 6 | 10      | Full system testing, Bug resolution, Security hardening, PO demo & sign-off, Production deployment |

---

## 11. Sign-off

By signing below, the signatories confirm that this Customer Requirements Specification accurately represents the agreed requirements for the SmartSkill Performance Monitoring System and authorise the development team to proceed on this basis. Any changes to requirements after this sign-off must follow the formal change control process (new CRS version + PO approval).

| Role              | Name                  | Signature | Date       |
|-------------------|-----------------------|-----------|------------|
| Product Owner     | Benhar Charles Sir    |           |            |
| Developer         | Saksham Tapadia       |           |            |
| Developer         | Ayush Mathur          |           |            |
| Developer         | Aman Nahar            |           |            |
| Developer         | Diya Garg             |           |            |
| Developer         | Pankhuri              |           |            |

---

*SmartSkill Performance Monitoring System — CRS v1.0.0 — Confidential*
