# SSPMS Preparation Materials Guide

---

| Field | Details |
|-------|---------|
| **Document** | Preparation Materials Guide |
| **System** | SmartSkill Performance Monitoring System (SSPMS) |
| **Version** | 1.0.0 |
| **Date** | 2026-03-21 |
| **Purpose** | Define legitimate study materials and resources employees can access before task attempts |

---

## Table of Contents

1. [Overview](#1-overview)
2. [MCQ Preparation Materials](#2-mcq-preparation-materials)
3. [Code Question Preparation Materials](#3-code-question-preparation-materials)
4. [General Resources](#4-general-resources)
5. [Implementation in SSPMS](#5-implementation-in-sspms)
6. [What Constitutes Cheating vs Preparation](#6-what-constitutes-cheating-vs-preparation)

---

## 1. Overview

### 1.1 Purpose

Preparation materials provide employees with **legitimate study resources** to help them succeed on assignments without compromising assessment integrity. These materials help employees:

- Understand concepts before task attempts
- Practice skills in a low-stakes environment
- Review syntax and common patterns
- Build confidence

### 1.2 Core Principle

**Preparation materials teach HOW to solve problems, not the specific solutions to assigned tasks.**

---

## 2. MCQ Preparation Materials

### 2.1 Concept Study Guides

For each skill tag assigned to a class/task, trainers can provide:

#### Example: "JavaScript ES6 Fundamentals" Study Guide

```markdown
## Arrow Functions
- Syntax: `const func = (param) => expression`
- `this` binding: lexical, not dynamic
- Cannot be used as constructors

## Destructuring
const { name, age } = person;
const [first, second] = array;

## Template Literals
const message = `Hello, ${name}`;

## Spread Operator
const combined = [...array1, ...array2];
```

**Format:** Markdown files, max 10 pages per topic
**Accessibility:** Available in "Study Materials" section for enrolled employees

### 2.2 Practice Question Banks

Trainers can create **practice MCQ sets** separate from graded tasks:

- Questions cover same topics but with different scenarios
- Immediate feedback showing correct answer + explanation
- Unlimited attempts allowed
- **Does not contribute to XP or rank**
- Tagged as "Practice" in the UI

#### Example Practice Question

```
Topic: JavaScript Array Methods
Question: What does [1,2,3].map(x => x * 2) return?

A) [1, 2, 3]
B) [2, 4, 6] ✓ CORRECT
C) 6
D) Error

Explanation: .map() transforms each element by applying the function.
It returns a new array with [1*2, 2*2, 3*2] = [2, 4, 6].
```

### 2.3 Concept Flashcards

- Digital flashcard sets per skill tag
- Question on front, answer + context on back
- Employees can mark flashcards as "mastered" or "review"
- SRS-style (spaced repetition) review scheduling

---

## 3. Code Question Preparation Materials

### 3.1 Language Reference Guides

#### C# Reference (Example)

```markdown
## Common Data Types
int, double, string, bool, char

## String Methods
.Length, .ToUpper(), .Substring(start, length), .Contains(substring)

## Loops
for(int i = 0; i < n; i++) { }
foreach(var item in collection) { }
while(condition) { }

## Arrays
int[] arr = new int[5];
arr[0] = 10;

## Lists
List<int> list = new List<int>();
list.Add(5);
list.Remove(5);
```

**Format:** Markdown/PDF cheat sheets (1-2 pages per language)

### 3.2 Practice Coding Problems

Trainers create **ungraded practice code challenges**:

- Different problems than graded tasks (e.g., if graded task is "FizzBuzz", practice might be "Prime Number Checker")
- Employees write code in the same Monaco Editor interface
- Trainer can provide **sample solution** after employee submits attempt
- Marked as "Practice" — does not affect leaderboard

#### Example Practice Problem

```
Title: Sum of Even Numbers
Difficulty: Easy
Language: JavaScript

Problem:
Write a function sumEven(arr) that takes an array of integers
and returns the sum of only the even numbers.

Example:
sumEven([1, 2, 3, 4, 5, 6]) → 12 (because 2 + 4 + 6 = 12)

Test Cases (visible to employee):
sumEven([1, 2, 3, 4]) → 6
sumEven([10, 15, 20]) → 30
sumEven([1, 3, 5]) → 0

Trainer's Sample Solution (revealed after employee attempts):
function sumEven(arr) {
  return arr.filter(x => x % 2 === 0).reduce((sum, x) => sum + x, 0);
}
```

### 3.3 Algorithm Tutorials

- Step-by-step walkthroughs of common algorithms
- Pseudocode + implementation in each supported language
- Visual diagrams (e.g., array sorting animations)

#### Topics to Cover

- Sorting (bubble, selection, insertion, merge, quick)
- Searching (linear, binary)
- Recursion basics
- String manipulation patterns
- Data structures (arrays, linked lists, stacks, queues, hash maps)

### 3.4 IDE Setup Guides

- How to use Monaco Editor features (autocomplete, syntax highlighting, keyboard shortcuts)
- How to test code locally before submission
- Common debugging techniques

---

## 4. General Resources

### 4.1 External Learning Resources

Trainers can curate a **resource library** with links to:

- Official documentation (MDN for JavaScript, MSDN for C#, Python Docs)
- Free courses (Codecademy, freeCodeCamp, Coursera)
- YouTube tutorials (specific channels/playlists)
- Books/e-books (O'Reilly, Manning)

**Format:** URL list with descriptions

### 4.2 Recorded Lectures

- Trainers can upload pre-recorded video lectures
- Organized by skill tag
- Optional quizzes after each video

### 4.3 Previous Task Reviews

**After a task closes and is fully evaluated**, trainers can publish:

- Aggregate statistics (avg score, pass rate per question)
- **Sample correct answers** (anonymized) for Code/Assessment questions
- Common mistakes and how to avoid them

**Important:** This is only for **past tasks** — never for active/upcoming tasks.

---

## 5. Implementation in SSPMS

### 5.1 New Entities

#### PreparationMaterial
```
Id               : GUID (PK)
ClassId          : GUID (FK → Class, nullable for system-wide materials)
SkillTag         : NVARCHAR(100) (e.g., "JavaScript", "C#")
Type             : ENUM (StudyGuide, PracticeQuiz, CodeChallenge, VideoLecture, Reference, ExternalLink)
Title            : NVARCHAR(300)
Description      : NVARCHAR(MAX)
ContentUrl       : NVARCHAR(500) (file path or external URL)
CreatedByTrainerId : GUID (FK → User)
CreatedAt        : DATETIME2
IsPublished      : BIT
```

#### PracticeProblem (extends PreparationMaterial for code challenges)
```
Id                   : GUID (PK, also FK → PreparationMaterial)
Language             : NVARCHAR(50) (csharp | javascript | python)
StarterCode          : NVARCHAR(MAX) (optional)
SampleSolution       : NVARCHAR(MAX) (revealed after employee attempts)
TestCases            : NVARCHAR(MAX) (JSON array of {input, expectedOutput})
```

#### EmployeePracticeAttempt
```
Id                 : GUID (PK)
EmployeeId         : GUID (FK → User)
PracticeProblemId  : GUID (FK → PracticeProblem)
Code               : NVARCHAR(MAX)
SubmittedAt        : DATETIME2
```

### 5.2 API Endpoints

#### Trainer Endpoints
```
POST   /api/v1/preparation-materials              Create new material
GET    /api/v1/preparation-materials              List all materials (trainer's class)
PUT    /api/v1/preparation-materials/:id          Update material
DELETE /api/v1/preparation-materials/:id          Delete material
PATCH  /api/v1/preparation-materials/:id/publish  Publish material
```

#### Employee Endpoints
```
GET    /api/v1/preparation-materials/my-class     Get all published materials for my class
GET    /api/v1/preparation-materials/:id          View material detail
POST   /api/v1/practice-problems/:id/attempt      Submit practice code attempt
GET    /api/v1/practice-problems/:id/solution     Get sample solution (only after attempting)
```

### 5.3 Frontend Components

#### Trainer UI
- **Preparation Library Manager** (new tab in Trainer area)
  - Upload study guides (Markdown/PDF)
  - Create practice quizzes (MCQ builder, same UI as graded tasks)
  - Create practice code challenges
  - Curate external resource links
  - Organize by skill tag

#### Employee UI
- **Study Center** (new section in Employee area)
  - Sidebar filter by skill tag
  - Material type tabs: Study Guides | Practice Quizzes | Code Challenges | Resources
  - "My Progress" widget showing practice attempts
  - Bookmarks for favorite materials

---

## 6. What Constitutes Cheating vs Preparation

### ✅ Legitimate Preparation (Allowed)

1. **Studying provided materials** (guides, flashcards, lectures)
2. **Attempting practice problems** before the graded task
3. **Reviewing past task solutions** (after task has closed)
4. **Using reference guides during attempt** (if trainer allows — can be toggled per task)
5. **Discussing concepts with classmates** outside the task window
6. **Watching tutorials on the same topic**

### ❌ Cheating (Prohibited)

1. **Copying code/answers from classmates** during an active task attempt
2. **Sharing solutions to graded tasks** while task is active
3. **Using external help websites** (Stack Overflow, Chegg) during a closed-book task
4. **Having someone else complete the task** for you
5. **Using AI code generators** (ChatGPT, Copilot) during closed-book tasks
6. **Accessing task questions before the official start time**

### Trainer Control: Open-Book vs Closed-Book Tasks

Trainers can configure each task as:

- **Closed-Book** — No external resources allowed (enforced via honor system + Code Similarity detection)
- **Open-Book** — External references allowed, but solution must be original

This is stored as a boolean field `Task.IsOpenBook`.

During task attempt, a banner reminds employees:
- Closed-Book: "This is a closed-book assessment. Use only your knowledge and provided materials."
- Open-Book: "External references allowed, but your solution must be original. Plagiarism will be detected."

---

## 7. Trainer Best Practices

### 7.1 Creating Effective Practice Problems

- **Mirror task difficulty** — Practice should match graded task complexity
- **Vary the scenario** — Same concept, different context (e.g., both are loops, but one counts vowels, the other sums digits)
- **Provide immediate feedback** — Always reveal solution after attempt with explanation
- **Encourage multiple attempts** — Practice should be low-stakes

### 7.2 Timing of Material Release

- **Study guides** — Release at class start, available always
- **Practice problems** — Release 2-3 days before graded task (gives employees time to practice)
- **Past task reviews** — Release 1 day after task evaluation completes (so everyone has tried it first)

### 7.3 Encouraging Usage

- Mention materials in task announcements
- Track practice attempt rates (dashboard metric for trainers)
- Celebrate employees who practice (optional badge: "Diligent Learner — 10 practice problems attempted")

---

## 8. Example Preparation Material Workflow

### Scenario: Class learning "JavaScript Array Methods"

**Week 1 — Skill Introduction**
1. Trainer uploads **Study Guide: JavaScript Array Methods** (map, filter, reduce, forEach)
2. Trainer creates **5 practice MCQs** (ungraded)
3. Trainer creates **1 practice code challenge: "Filter and Sum"** (use .filter() and .reduce())

**Week 2 — Practice Phase**
4. Employees review study guide
5. Employees attempt practice MCQs (average: 3/5 correct on first try)
6. Employees attempt practice code challenge (50% solve it, view sample solution)

**Week 3 — Graded Assessment**
7. Trainer publishes **graded task: "Data Transformation Pipeline"** (uses same methods but different problem: transform e-commerce order data)
8. Employees attempt graded task (score distribution shows improvement due to practice)

**Week 4 — Review**
9. Trainer publishes **Past Task Review: Data Transformation** with sample solutions and common mistakes

---

*End of Preparation Materials Guide — SSPMS v1.0.0*
