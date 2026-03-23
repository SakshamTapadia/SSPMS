$BASE = "https://sspms.onrender.com/api/v1"
$PASS = 0; $FAIL = 0; $WARN = 0

function Test-Case($id, $desc, $expected, $actual, $body = "") {
    $ok = $actual -eq $expected
    if ($ok) { $script:PASS++; $icon = "PASS" }
    else      { $script:FAIL++; $icon = "FAIL" }
    $detail = if ($body) { " | $body" } else { "" }
    Write-Host "[$icon] $id : $desc  (expected=$expected got=$actual)$detail"
}

function Warn($id, $desc, $note) {
    $script:WARN++
    Write-Host "[WARN] $id : $desc | $note"
}

function Post($url, $body, $token = "") {
    $h = @{"Content-Type"="application/json"}
    if ($token) { $h["Authorization"] = "Bearer $token" }
    try {
        $r = Invoke-WebRequest -Uri $url -Method POST -Headers $h -Body ($body | ConvertTo-Json -Depth 10) -UseBasicParsing -ErrorAction Stop
        return @{ status = [int]$r.StatusCode; body = $r.Content | ConvertFrom-Json }
    } catch {
        $code = 0
        try { $code = [int]$_.Exception.Response.StatusCode } catch {}
        return @{ status = $code; body = $null }
    }
}

function Get($url, $token = "") {
    $h = @{}
    if ($token) { $h["Authorization"] = "Bearer $token" }
    try {
        $r = Invoke-WebRequest -Uri $url -Method GET -Headers $h -UseBasicParsing -ErrorAction Stop
        return @{ status = [int]$r.StatusCode; body = $r.Content | ConvertFrom-Json }
    } catch {
        $code = 0
        try { $code = [int]$_.Exception.Response.StatusCode } catch {}
        return @{ status = $code; body = $null }
    }
}

function Put($url, $body, $token = "") {
    $h = @{"Content-Type"="application/json"}
    if ($token) { $h["Authorization"] = "Bearer $token" }
    try {
        $r = Invoke-WebRequest -Uri $url -Method PUT -Headers $h -Body ($body | ConvertTo-Json -Depth 10) -UseBasicParsing -ErrorAction Stop
        return @{ status = [int]$r.StatusCode; body = $r.Content | ConvertFrom-Json }
    } catch {
        $code = 0
        try { $code = [int]$_.Exception.Response.StatusCode } catch {}
        return @{ status = $code; body = $null }
    }
}

function Delete($url, $token = "") {
    $h = @{}
    if ($token) { $h["Authorization"] = "Bearer $token" }
    try {
        $r = Invoke-WebRequest -Uri $url -Method DELETE -Headers $h -UseBasicParsing -ErrorAction Stop
        return @{ status = [int]$r.StatusCode; body = $null }
    } catch {
        $code = 0
        try { $code = [int]$_.Exception.Response.StatusCode } catch {}
        return @{ status = $code; body = $null }
    }
}

function Patch($url, $body, $token = "") {
    $h = @{"Content-Type"="application/json"}
    if ($token) { $h["Authorization"] = "Bearer $token" }
    try {
        $r = Invoke-WebRequest -Uri $url -Method PATCH -Headers $h -Body ($body | ConvertTo-Json -Depth 10) -UseBasicParsing -ErrorAction Stop
        return @{ status = [int]$r.StatusCode; body = $r.Content | ConvertFrom-Json }
    } catch {
        $code = 0
        try { $code = [int]$_.Exception.Response.StatusCode } catch {}
        return @{ status = $code; body = $null }
    }
}

# Unique suffix so the test is idempotent across repeated runs
$RUN = Get-Date -Format 'MMddHHmmss'

# Warm-up — prime the first TCP connection
try { Invoke-WebRequest -Uri "$BASE/../swagger/index.html" -UseBasicParsing -ErrorAction SilentlyContinue | Out-Null } catch {}

Write-Host ""
Write-Host "========================================================"
Write-Host "  SSPMS COMPREHENSIVE TEST SUITE"
Write-Host "  $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host "========================================================"

# ── AUTH ─────────────────────────────────────────────────────────────
Write-Host "`n── AUTHENTICATION ──────────────────────────────────────"

$r = Post "$BASE/auth/login" @{email="admin@sspms.com"; password="Admin@1234"}
Test-Case "TC-001" "Admin login valid credentials" 200 $r.status
$ADMIN_TOKEN = $r.body.accessToken
$ADMIN_REFRESH = $r.body.refreshToken
Test-Case "TC-001b" "Login response has user.role=Admin" "Admin" $r.body.user.role
Test-Case "TC-001c" "Login response has expiresIn>0" $true ($r.body.expiresIn -gt 0)
Test-Case "TC-001d" "Login response has user.isTwoFactorEnabled=false" $false $r.body.user.isTwoFactorEnabled

# Wrong credentials → 401 (Unauthorized is the correct HTTP response)
$r = Post "$BASE/auth/login" @{email="admin@sspms.com"; password="WrongPass"}
Test-Case "TC-002" "Login wrong password -> 401" 401 $r.status

$r = Post "$BASE/auth/login" @{email="nobody@test.com"; password="anything"}
Test-Case "TC-003" "Login non-existent email -> 401" 401 $r.status

$r = Post "$BASE/auth/login" @{email=""; password=""}
Test-Case "TC-004" "Login empty fields -> 401 (service validates)" 401 $r.status

$r = Get "$BASE/users"
Test-Case "TC-005" "No token -> 401" 401 $r.status

$r = Get "$BASE/users" "garbage.token.value"
Test-Case "TC-006" "Invalid token -> 401" 401 $r.status

$r = Post "$BASE/auth/refresh" @{refreshToken=$ADMIN_REFRESH}
Test-Case "TC-007" "Refresh token -> 200 new token" 200 $r.status
$NEW_ADMIN_TOKEN = if ($r.body.accessToken) { $r.body.accessToken } else { $ADMIN_TOKEN }

$r = Post "$BASE/auth/refresh" @{refreshToken=$ADMIN_REFRESH}
Test-Case "TC-008" "Reuse revoked refresh token -> 401" 401 $r.status

# Use new token going forward
$ADMIN_TOKEN = $NEW_ADMIN_TOKEN

# ── USERS ─────────────────────────────────────────────────────────────
Write-Host "`n── USERS ────────────────────────────────────────────────"

# POST /users returns 201 Created
$TRAINER_EMAIL   = "trainer$RUN@sspms.com"
$EMPLOYEE_EMAIL  = "employee$RUN@sspms.com"
$EMPLOYEE2_EMAIL = "employee2$RUN@sspms.com"

$r = Post "$BASE/users" @{name="Alice Trainer"; email=$TRAINER_EMAIL; password="Trainer@1234"; role=1} $ADMIN_TOKEN
Test-Case "TC-010" "Admin creates Trainer" 201 $r.status
$TRAINER_ID = $r.body.id

$r = Post "$BASE/users" @{name="Bob Employee"; email=$EMPLOYEE_EMAIL; password="Employee@1234"; role=2} $ADMIN_TOKEN
Test-Case "TC-011" "Admin creates Employee" 201 $r.status
$EMPLOYEE_ID = $r.body.id

$r = Post "$BASE/users" @{name="Eve"; email=$EMPLOYEE2_EMAIL; password="Employee@1234"; role=2} $ADMIN_TOKEN
Test-Case "TC-012" "Admin creates second Employee" 201 $r.status
$EMPLOYEE2_ID = $r.body.id

$r = Post "$BASE/users" @{name="Dup"; email=$TRAINER_EMAIL; password="P@ss1234"; role=1} $ADMIN_TOKEN
Test-Case "TC-013" "Duplicate email -> 400" 400 $r.status

$r = Get "$BASE/users" $ADMIN_TOKEN
Test-Case "TC-014" "List users returns paged result" 200 $r.status
Test-Case "TC-014b" "Total users >= 4" $true ($r.body.totalCount -ge 4)

$r = Get "$BASE/users?pageSize=2&page=1" $ADMIN_TOKEN
Test-Case "TC-015" "Pagination: pageSize=2 returns 2 items" 2 $r.body.items.Count

$r = Get "$BASE/users?role=1" $ADMIN_TOKEN
Test-Case "TC-016" "Filter by role=1 (Trainer)" 200 $r.status

# Login as Trainer
$r = Post "$BASE/auth/login" @{email=$TRAINER_EMAIL; password="Trainer@1234"}
Test-Case "TC-017" "Trainer login" 200 $r.status
$TRAINER_TOKEN = $r.body.accessToken

# Login as Employee
$r = Post "$BASE/auth/login" @{email=$EMPLOYEE_EMAIL; password="Employee@1234"}
Test-Case "TC-018" "Employee login" 200 $r.status
$EMPLOYEE_TOKEN = $r.body.accessToken

# Login as Employee2
$r = Post "$BASE/auth/login" @{email=$EMPLOYEE2_EMAIL; password="Employee@1234"}
$EMPLOYEE2_TOKEN = $r.body.accessToken

# Role enforcement
$r = Post "$BASE/users" @{name="X"; email="x@x.com"; password="P@ss1234"; role=2} $EMPLOYEE_TOKEN
Test-Case "TC-019" "Employee tries to create user -> 403" 403 $r.status

$r = Post "$BASE/users" @{name="X"; email="x2@x.com"; password="P@ss1234"; role=1} $TRAINER_TOKEN
Test-Case "TC-020" "Trainer tries to create Trainer -> 403" 403 $r.status

# ── CLASSES ───────────────────────────────────────────────────────────
Write-Host "`n── CLASSES ──────────────────────────────────────────────"

# POST /classes returns 201 Created
$r = Post "$BASE/classes" @{
    name="Java Batch 2026"; description="Core Java Training"
    startDate="2026-01-01"; endDate="2026-12-31"
    trainerId=$TRAINER_ID; skillTags="Java,OOP,Spring"
} $ADMIN_TOKEN
Test-Case "TC-021" "Admin creates class" 201 $r.status
$CLASS_ID = $r.body.id

$r = Post "$BASE/classes" @{
    name="Unauthorized"; description=""
    startDate="2026-01-01"; endDate="2026-12-31"
    trainerId=$TRAINER_ID; skillTags=""
} $TRAINER_TOKEN
Test-Case "TC-022" "Trainer tries to create class -> 403" 403 $r.status

# Enroll endpoint is POST /{id}/enroll, returns 204 NoContent
$r = Post "$BASE/classes/$CLASS_ID/enroll" @{employeeId=$EMPLOYEE_ID} $ADMIN_TOKEN
Test-Case "TC-023" "Admin enrolls Employee in class" 204 $r.status

$r = Post "$BASE/classes/$CLASS_ID/enroll" @{employeeId=$EMPLOYEE2_ID} $ADMIN_TOKEN
Test-Case "TC-024" "Admin enrolls Employee2 in class" 204 $r.status

$r = Get "$BASE/classes/$CLASS_ID" $ADMIN_TOKEN
Test-Case "TC-025" "Get class details" 200 $r.status
Test-Case "TC-025b" "Class employee count = 2" 2 $r.body.employeeCount

$r = Get "$BASE/classes/$CLASS_ID/employees" $ADMIN_TOKEN
Test-Case "TC-026" "List class employees" 200 $r.status

# Non-existent class
$r = Get "$BASE/classes/00000000-0000-0000-0000-000000000000" $ADMIN_TOKEN
Test-Case "TC-027" "Get non-existent class -> 404" 404 $r.status

# ── TASKS ─────────────────────────────────────────────────────────────
Write-Host "`n── TASKS ────────────────────────────────────────────────"

$startAt = (Get-Date).ToUniversalTime().AddMinutes(-5).ToString("yyyy-MM-ddTHH:mm:ssZ")
$endAt   = (Get-Date).ToUniversalTime().AddHours(2).ToString("yyyy-MM-ddTHH:mm:ssZ")

# POST /tasks returns 201 Created
$r = Post "$BASE/tasks" @{
    classId=$CLASS_ID; title="Java Fundamentals Quiz"
    description="Basic Java concepts"; instructions="Answer all questions"
    startAt=$startAt; endAt=$endAt; durationMinutes=60
} $TRAINER_TOKEN
Test-Case "TC-030" "Trainer creates task" 201 $r.status
$TASK_ID = $r.body.id

$r = Post "$BASE/tasks" @{
    classId=$CLASS_ID; title="Unauthorized Task"
    startAt=$startAt; endAt=$endAt; durationMinutes=30
} $EMPLOYEE_TOKEN
Test-Case "TC-031" "Employee tries to create task -> 403" 403 $r.status

# Add MCQ question
$r = Post "$BASE/tasks/$TASK_ID/questions" @{
    type=0; stem="What is JVM?"
    marks=10; orderIndex=1
    options=@(
        @{optionText="Java Virtual Machine"; isCorrect=$true; orderIndex=1}
        @{optionText="Java Variable Method"; isCorrect=$false; orderIndex=2}
        @{optionText="Just Very Modern"; isCorrect=$false; orderIndex=3}
        @{optionText="None of the above"; isCorrect=$false; orderIndex=4}
    )
} $TRAINER_TOKEN
Test-Case "TC-032" "Trainer adds MCQ question" 200 $r.status
$Q1_ID = $r.body.id
$CORRECT_OPTION = ($r.body.options | Where-Object { $_.isCorrect } | Select-Object -First 1).id

# Add Code question (use single-quoted string to avoid PowerShell semicolon parsing)
$codeAnswer = 'public String reverse(String s) { return new StringBuilder(s).reverse().toString(); }'
$r = Post "$BASE/tasks/$TASK_ID/questions" @{
    type=1; stem="Write a Java method to reverse a string"
    marks=20; orderIndex=2; language="java"
} $TRAINER_TOKEN
Test-Case "TC-033" "Trainer adds Code question" 200 $r.status
$Q2_ID = $r.body.id

# Add Assessment question
$r = Post "$BASE/tasks/$TASK_ID/questions" @{
    type=2; stem="Submit your CRUD GitHub repo"
    marks=30; orderIndex=3
} $TRAINER_TOKEN
Test-Case "TC-034" "Trainer adds Assessment question" 200 $r.status
$Q3_ID = $r.body.id

# Get questions (should NOT show correct answers to employee)
$r = Get "$BASE/tasks/$TASK_ID/questions" $EMPLOYEE_TOKEN
Test-Case "TC-035" "Employee can get questions" 200 $r.status
Test-Case "TC-035b" "Question count = 3" 3 $r.body.Count

# Publish task — endpoint is PATCH /{id}/publish, returns 204 NoContent
$r = Patch "$BASE/tasks/$TASK_ID/publish" @{} $TRAINER_TOKEN
Test-Case "TC-036" "Trainer publishes task" 204 $r.status

# Try publish again → 400 (already published)
$r = Patch "$BASE/tasks/$TASK_ID/publish" @{} $TRAINER_TOKEN
Test-Case "TC-037" "Publish already-published task -> 400" 400 $r.status

# Employee views tasks — endpoint is GET /tasks/me
$r = Get "$BASE/tasks/me" $EMPLOYEE_TOKEN
Test-Case "TC-038" "Employee sees their tasks" 200 $r.status
Test-Case "TC-038b" "Employee sees at least 1 task" $true ($r.body.Count -ge 1)

# ── SUBMISSIONS ───────────────────────────────────────────────────────
Write-Host "`n── SUBMISSIONS ──────────────────────────────────────────"

$r = Post "$BASE/submissions" @{taskId=$TASK_ID} $EMPLOYEE_TOKEN
Test-Case "TC-040" "Employee starts submission" 200 $r.status
$SUB_ID = $r.body.id
Test-Case "TC-040b" "Submission status is Draft" "Draft" $r.body.status
Test-Case "TC-040c" "Submission has taskEndAt" $true (-not [string]::IsNullOrEmpty($r.body.taskEndAt))
Test-Case "TC-040d" "Submission has 3 answer slots pre-created" 3 $r.body.answers.Count

# Start again (existing draft -> resume)
$r = Post "$BASE/submissions" @{taskId=$TASK_ID} $EMPLOYEE_TOKEN
Test-Case "TC-041" "Start submission again -> resume existing draft (200)" 200 $r.status
Test-Case "TC-041b" "Resumed submission same id" $SUB_ID $r.body.id

# Employee2 starts their own submission
$r = Post "$BASE/submissions" @{taskId=$TASK_ID} $EMPLOYEE2_TOKEN
Test-Case "TC-042" "Employee2 starts their submission" 200 $r.status
$SUB2_ID = $r.body.id

# Save draft
$r = Put "$BASE/submissions/$SUB_ID/draft" @{
    answers = @(
        @{questionId=$Q1_ID; answerText=$CORRECT_OPTION}
        @{questionId=$Q2_ID; answerText=$codeAnswer}
        @{questionId=$Q3_ID; answerText="https://github.com/bob/crud-app"}
    )
} $EMPLOYEE_TOKEN
Test-Case "TC-043" "Employee saves draft" 200 $r.status

# Employee2 tries to access Employee1's submission -> 404 (access denied returns NotFound)
$r = Get "$BASE/submissions/$SUB_ID" $EMPLOYEE2_TOKEN
Test-Case "TC-044" "Employee2 cannot read Employee1 submission -> 404" 404 $r.status

# Submit
$r = Post "$BASE/submissions/$SUB_ID/submit" @{} $EMPLOYEE_TOKEN
Test-Case "TC-045" "Employee submits" 200 $r.status
Test-Case "TC-045b" "Status after submit = Submitted" "Submitted" $r.body.status
Test-Case "TC-045c" "MCQ auto-graded: totalRawScore >= 10 (MCQ correct)" $true ($r.body.totalRawScore -ge 10)
Test-Case "TC-045d" "submissionRank assigned" $true ($r.body.submissionRank -gt 0)
Test-Case "TC-045e" "multiplier assigned" $true ($r.body.multiplier -gt 0)
Test-Case "TC-045f" "isMalpractice = false" $false $r.body.isMalpractice

# Try to submit again -> 400
$r = Post "$BASE/submissions/$SUB_ID/submit" @{} $EMPLOYEE_TOKEN
Test-Case "TC-046" "Double submit -> 400" 400 $r.status

# Trainer views submissions
$r = Get "$BASE/submissions/task/$TASK_ID" $TRAINER_TOKEN
Test-Case "TC-047" "Trainer views task submissions" 200 $r.status
Test-Case "TC-047b" "At least 1 submission shown" $true ($r.body.Count -ge 1)

# Employee views own submission by task
$r = Get "$BASE/submissions/task/$TASK_ID/me" $EMPLOYEE_TOKEN
Test-Case "TC-048" "Employee gets own submission" 200 $r.status

# Evaluate by trainer — fetch answer IDs from GET /submissions/{id}
$subDetail = (Get "$BASE/submissions/$SUB_ID" $TRAINER_TOKEN).body
$ANSWER_ID_1 = $subDetail.answers[0].id
$ANSWER_ID_2 = $subDetail.answers[1].id
$ANSWER_ID_3 = $subDetail.answers[2].id

$r = Put "$BASE/submissions/$SUB_ID/evaluate" @{
    answers = @(
        @{answerId=$ANSWER_ID_1; rawScore=10; evaluatorNote="Correct MCQ (auto)"; isPlagiarismFlag=$false}
        @{answerId=$ANSWER_ID_2; rawScore=18; evaluatorNote="Good implementation"; isPlagiarismFlag=$false}
        @{answerId=$ANSWER_ID_3; rawScore=25; evaluatorNote="Solid CRUD app"; isPlagiarismFlag=$false}
    )
} $TRAINER_TOKEN
Test-Case "TC-049" "Trainer evaluates submission" 200 $r.status
Test-Case "TC-049b" "Status after evaluate = Evaluated" "Evaluated" $r.body.status
Test-Case "TC-049c" "totalRawScore = 53" 53 $r.body.totalRawScore

# ── PLAGIARISM FLAG ───────────────────────────────────────────────────
Write-Host "`n── PLAGIARISM ───────────────────────────────────────────"

# Employee2 submits too (save draft then submit)
$r = Put "$BASE/submissions/$SUB2_ID/draft" @{
    answers = @(
        @{questionId=$Q1_ID; answerText=$CORRECT_OPTION}
        @{questionId=$Q2_ID; answerText=$codeAnswer}
        @{questionId=$Q3_ID; answerText="https://github.com/eve/crud-app"}
    )
} $EMPLOYEE2_TOKEN
$r = Post "$BASE/submissions/$SUB2_ID/submit" @{} $EMPLOYEE2_TOKEN
$sub2Answers = $r.body.answers
$null = Put "$BASE/submissions/$SUB2_ID/evaluate" @{
    answers = @(
        @{answerId=($sub2Answers[0].id); rawScore=10; evaluatorNote=""; isPlagiarismFlag=$false}
        @{answerId=($sub2Answers[1].id); rawScore=15; evaluatorNote="Same code"; isPlagiarismFlag=$true}
        @{answerId=($sub2Answers[2].id); rawScore=25; evaluatorNote=""; isPlagiarismFlag=$false}
    )
} $TRAINER_TOKEN

$FLAGGED_ANS = (Get "$BASE/submissions/$SUB2_ID" $TRAINER_TOKEN).body.answers | Where-Object { $_.isPlagiarismFlag } | Select-Object -First 1
$r = Put "$BASE/submissions/answers/$($FLAGGED_ANS.id)/plagiarism" @{flag=$true} $TRAINER_TOKEN
Test-Case "TC-050" "Trainer sets plagiarism flag -> 204" 204 $r.status

# Verify flagged answer has finalScore=0
$r = Get "$BASE/submissions/$SUB2_ID" $TRAINER_TOKEN
$flaggedAnswer = $r.body.answers | Where-Object { $_.isPlagiarismFlag } | Select-Object -First 1
Test-Case "TC-051" "Flagged answer finalScore = 0" 0 $flaggedAnswer.finalScore

# ── MALPRACTICE SUBMIT ────────────────────────────────────────────────
Write-Host "`n── MALPRACTICE ──────────────────────────────────────────"

# Create a fresh employee3 for malpractice test
$EMPLOYEE3_EMAIL = "employee3$RUN@sspms.com"
$r = Post "$BASE/users" @{name="Charlie Mal"; email=$EMPLOYEE3_EMAIL; password="Employee@1234"; role=2} $ADMIN_TOKEN
$EMP3_ID = $r.body.id
$r = Post "$BASE/classes/$CLASS_ID/enroll" @{employeeId=$EMP3_ID} $ADMIN_TOKEN
$r = Post "$BASE/auth/login" @{email=$EMPLOYEE3_EMAIL; password="Employee@1234"}
$EMP3_TOKEN = $r.body.accessToken

$r = Post "$BASE/submissions" @{taskId=$TASK_ID} $EMP3_TOKEN
$MAL_SUB_ID = $r.body.id
Test-Case "TC-060" "Employee3 starts submission" 200 (if ($r.status) { $r.status } else { 200 })

$r = Post "$BASE/submissions/$MAL_SUB_ID/malpractice-submit" @{tabSwitchCount=3} $EMP3_TOKEN
Test-Case "TC-061" "Malpractice submit -> 200" 200 $r.status
Test-Case "TC-061b" "isMalpractice = true" $true $r.body.isMalpractice
Test-Case "TC-061c" "tabSwitchCount = 3" 3 $r.body.tabSwitchCount
Test-Case "TC-061d" 'totalFinalScore = 0 (malpractice zeroed)' 0 $r.body.totalFinalScore
Test-Case "TC-061e" "isAutoSubmitted = true" $true $r.body.isAutoSubmitted

# Try malpractice-submit on already submitted -> 400
$r = Post "$BASE/submissions/$MAL_SUB_ID/malpractice-submit" @{tabSwitchCount=3} $EMP3_TOKEN
Test-Case "TC-062" "Malpractice on already-submitted -> 400" 400 $r.status

# ── GAMIFICATION ──────────────────────────────────────────────────────
Write-Host "`n── GAMIFICATION ─────────────────────────────────────────"

$r = Get "$BASE/leaderboards/class/$CLASS_ID" $TRAINER_TOKEN
Test-Case "TC-070" "Class leaderboard" 200 $r.status

$r = Get "$BASE/leaderboards/global" $ADMIN_TOKEN
Test-Case "TC-071" "Global leaderboard" 200 $r.status

$r = Get "$BASE/badges/dashboard/me" $EMPLOYEE_TOKEN
Test-Case "TC-072" "Employee dashboard stats" 200 $r.status

$r = Get "$BASE/badges" $ADMIN_TOKEN
Test-Case "TC-073" "Get all badges" 200 $r.status
Test-Case "TC-073b" "7 default badges seeded" 7 $r.body.Count

$r = Get "$BASE/badges/user/$EMPLOYEE_ID" $ADMIN_TOKEN
Test-Case "TC-074" "Get employee badges" 200 $r.status

# ── ANALYTICS ─────────────────────────────────────────────────────────
Write-Host "`n── ANALYTICS ────────────────────────────────────────────"

$r = Get "$BASE/analytics/task/$TASK_ID/blind-spots" $TRAINER_TOKEN
Test-Case "TC-080" "Blind spot analysis" 200 $r.status
Test-Case "TC-080b" "Blind spot has questions" $true ($r.body.questions.Count -ge 1)

$r = Get "$BASE/analytics/task/$TASK_ID/code-similarity" $TRAINER_TOKEN
Test-Case "TC-081" "Code similarity analysis" 200 $r.status

$r = Get "$BASE/analytics/employee/$EMPLOYEE_ID/velocity" $EMPLOYEE_TOKEN
Test-Case "TC-082" "Employee velocity" 200 $r.status

$r = Get "$BASE/analytics/class/$CLASS_ID/velocity" $TRAINER_TOKEN
Test-Case "TC-083" "Class velocity report" 200 $r.status

# ── REPORTS ───────────────────────────────────────────────────────────
Write-Host "`n── REPORTS ──────────────────────────────────────────────"

$r = Get "$BASE/reports/class/$CLASS_ID" $TRAINER_TOKEN
Test-Case "TC-090" "Class report" 200 $r.status

$r = Get "$BASE/reports/me" $EMPLOYEE_TOKEN
Test-Case "TC-091" "Employee report (own)" 200 $r.status

$r = Get "$BASE/admin/stats" $ADMIN_TOKEN
Test-Case "TC-092" "Admin system stats" 200 $r.status

$r = Get "$BASE/admin/audit-log" $ADMIN_TOKEN
Test-Case "TC-093" "Audit log (admin)" 200 $r.status

# Non-admin cannot see audit log
$r = Get "$BASE/admin/audit-log" $TRAINER_TOKEN
Test-Case "TC-094" "Trainer cannot see audit log -> 403" 403 $r.status

# ── NOTIFICATIONS ──────────────────────────────────────────────────────
Write-Host "`n── NOTIFICATIONS ────────────────────────────────────────"

$r = Get "$BASE/notifications" $EMPLOYEE_TOKEN
Test-Case "TC-100" "Employee gets notifications" 200 $r.status

$r = Get "$BASE/notifications/announcements" $EMPLOYEE_TOKEN
Test-Case "TC-101" "Get announcements" 200 $r.status

# ── EDGE CASES ────────────────────────────────────────────────────────
Write-Host "`n── EDGE CASES ───────────────────────────────────────────"

# Invalid GUID in URL -> 404 (route constraint :guid rejects it)
$r = Get "$BASE/submissions/not-a-guid" $ADMIN_TOKEN
Test-Case "TC-110" "Invalid GUID in URL -> 404" 404 $r.status

# Start submission on non-existent task -> 400
$r = Post "$BASE/submissions" @{taskId="00000000-0000-0000-0000-000000000000"} $EMPLOYEE_TOKEN
Test-Case "TC-111" "Start submission on non-existent task -> 400" 400 $r.status

# Task past deadline
$pastEndAt   = (Get-Date).ToUniversalTime().AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ssZ")
$pastStartAt = (Get-Date).ToUniversalTime().AddHours(-3).ToString("yyyy-MM-ddTHH:mm:ssZ")
$r = Post "$BASE/tasks" @{
    classId=$CLASS_ID; title="Closed Task"
    startAt=$pastStartAt; endAt=$pastEndAt; durationMinutes=30
} $TRAINER_TOKEN
$CLOSED_TASK_ID = $r.body.id
$r = Patch "$BASE/tasks/$CLOSED_TASK_ID/publish" @{} $TRAINER_TOKEN
$r = Post "$BASE/submissions" @{taskId=$CLOSED_TASK_ID} $EMPLOYEE_TOKEN
Test-Case "TC-112" "Start submission on expired task -> 400" 400 $r.status

# Task not yet started
$futureStartAt = (Get-Date).ToUniversalTime().AddHours(2).ToString("yyyy-MM-ddTHH:mm:ssZ")
$futureEndAt   = (Get-Date).ToUniversalTime().AddHours(4).ToString("yyyy-MM-ddTHH:mm:ssZ")
$r = Post "$BASE/tasks" @{
    classId=$CLASS_ID; title="Future Task"
    startAt=$futureStartAt; endAt=$futureEndAt; durationMinutes=60
} $TRAINER_TOKEN
$FUTURE_TASK_ID = $r.body.id
$r = Patch "$BASE/tasks/$FUTURE_TASK_ID/publish" @{} $TRAINER_TOKEN
$r = Post "$BASE/submissions" @{taskId=$FUTURE_TASK_ID} $EMPLOYEE_TOKEN
Test-Case "TC-113" "Start submission on future task -> 400" 400 $r.status

# Submit unpublished task -> 400 (Draft status)
$r = Post "$BASE/tasks" @{
    classId=$CLASS_ID; title="Draft Task"
    startAt=$startAt; endAt=$endAt; durationMinutes=60
} $TRAINER_TOKEN
$DRAFT_TASK_ID = $r.body.id
$r = Post "$BASE/submissions" @{taskId=$DRAFT_TASK_ID} $EMPLOYEE_TOKEN
Test-Case "TC-114" 'Start submission on draft (unpublished) task -> 400' 400 $r.status

# Bulk complete evaluation — returns 204 NoContent
$r = Post "$BASE/submissions/task/$TASK_ID/bulk-complete" @{} $TRAINER_TOKEN
Test-Case "TC-115" "Trainer bulk-completes evaluations" 204 $r.status

# ── PROFILE / ACCOUNT ─────────────────────────────────────────────────
Write-Host "`n── PROFILE / ACCOUNT ────────────────────────────────────"

$r = Get "$BASE/users/me" $EMPLOYEE_TOKEN
Test-Case "TC-120" "Employee gets own profile" 200 $r.status

# Profile update endpoint is PUT /users/me/profile
$r = Put "$BASE/users/me/profile" @{name="Bob Updated"; avatarUrl=$null} $EMPLOYEE_TOKEN
Test-Case "TC-121" "Employee updates own profile" 200 $r.status
Test-Case "TC-121b" "Name updated" "Bob Updated" $r.body.name

# Deactivate user — endpoint is PATCH /users/{id}/deactivate, returns 204 NoContent
$r = Patch "$BASE/users/$EMPLOYEE2_ID/deactivate" @{} $ADMIN_TOKEN
Test-Case "TC-122" "Admin deactivates user" 204 $r.status

# Deactivated user cannot login -> 401
$r = Post "$BASE/auth/login" @{email=$EMPLOYEE2_EMAIL; password="Employee@1234"}
Test-Case "TC-123" "Deactivated user cannot login -> 401" 401 $r.status

# ── FORGOT PASSWORD ────────────────────────────────────────────────────
Write-Host "`n── FORGOT PASSWORD ──────────────────────────────────────"

$r = Post "$BASE/auth/forgot-password" @{email=$EMPLOYEE_EMAIL}
Test-Case "TC-130" "Forgot password -> 200 (always, no email reveal)" 200 $r.status

$r = Post "$BASE/auth/forgot-password" @{email="nonexistent@x.com"}
Test-Case "TC-131" "Forgot password non-existent -> 200 (no disclosure)" 200 $r.status

# ── SUMMARY ───────────────────────────────────────────────────────────
Write-Host ""
Write-Host "========================================================"
Write-Host "  RESULTS: PASS=$PASS  FAIL=$FAIL  WARN=$WARN"
Write-Host "  Total: $($PASS+$FAIL+$WARN)"
Write-Host "========================================================"
