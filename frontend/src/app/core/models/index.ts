// ── Auth ────────────────────────────────────────────────
export interface LoginRequest { email: string; password: string; totpCode?: string; }
export interface AuthResponse { accessToken: string; refreshToken: string; expiresIn: number; user: UserProfile; }
export interface UserProfile { id: string; name: string; email: string; role: 'Admin' | 'Trainer' | 'Employee'; avatarUrl?: string; isTwoFactorEnabled: boolean; }
export interface AuthCheckResponse { requiresVerification?: boolean; email?: string; accessToken?: string; refreshToken?: string; expiresIn?: number; user?: UserProfile; }

// ── Users ────────────────────────────────────────────────
export interface UserDto { id: string; name: string; email: string; role: string; avatarUrl?: string; isActive: boolean; createdAt: string; classId?: string; className?: string; }
export interface CreateUserRequest { name: string; email: string; role: string; password?: string; classId?: string; }
export interface UpdateUserRequest { name: string; email: string; avatarUrl?: string; }

// ── Classes ──────────────────────────────────────────────
export interface ClassDto { id: string; name: string; description?: string; startDate: string; endDate: string; trainerId: string; trainerName: string; skillTags?: string; employeeCount: number; taskCount: number; }
export interface CreateClassRequest { name: string; description?: string; startDate: string; endDate: string; trainerId: string; skillTags?: string; }

// ── Tasks ────────────────────────────────────────────────
export type AssignmentStatus = 'Draft' | 'Published' | 'Closed';
export type QuestionType = 'MCQ' | 'Code' | 'Assessment' | 'Link';

export interface TaskDto { id: string; classId: string; className: string; title: string; description?: string; instructions?: string; totalMarks: number; startAt: string; endAt: string; durationMinutes: number; status: AssignmentStatus; createdByTrainerId: string; trainerName: string; createdAt: string; questionCount: number; submissionCount: number; }
export interface CreateTaskRequest { classId: string; title: string; description?: string; instructions?: string; startAt: string; endAt: string; durationMinutes: number; }
export interface UpdateTaskRequest { title: string; description?: string; instructions?: string; startAt: string; endAt: string; durationMinutes: number; }

export interface MCQOptionDto { id: string; optionText: string; orderIndex: number; isCorrect?: boolean; }
export interface QuestionDto { id: string; taskId: string; type: QuestionType; stem: string; marks: number; orderIndex: number; language?: string; options?: MCQOptionDto[]; }
export interface CreateQuestionRequest { type: QuestionType; stem: string; marks: number; orderIndex: number; language?: string; expectedOutput?: string; options?: { optionText: string; isCorrect: boolean; orderIndex: number; }[]; }

// ── Submissions ──────────────────────────────────────────
export type SubmissionStatus = 'Draft' | 'Submitted' | 'Evaluated';
export interface SubmissionAnswerDto { id: string; questionId: string; answerText?: string; rawScore?: number; finalScore?: number; evaluatorNote?: string; isPlagiarismFlag?: boolean; }
export interface SubmissionDto { id: string; taskId: string; taskTitle: string; taskEndAt: string; taskDurationMinutes: number; employeeId: string; employeeName: string; startedAt?: string; submittedAt?: string; submissionRank?: number; multiplier?: number; totalRawScore?: number; totalFinalScore?: number; status: SubmissionStatus; isAutoSubmitted: boolean; isMalpractice: boolean; tabSwitchCount: number; answers: SubmissionAnswerDto[]; }
export interface SubmissionSummary { id: string; employeeId: string; employeeName: string; submittedAt?: string; submissionRank?: number; multiplier?: number; totalRawScore?: number; totalFinalScore?: number; status: SubmissionStatus; }
export interface SaveDraftRequest { answers: { questionId: string; answerText?: string; }[]; }
export interface EvaluateSubmissionRequest { answers: { answerId: string; rawScore: number; evaluatorNote?: string; isPlagiarismFlag: boolean; }[]; }

// ── Gamification ─────────────────────────────────────────
export interface LeaderboardEntry { rank: number; employeeId: string; employeeName: string; avatarUrl?: string; totalXP: number; tasksCompleted: number; avgScore: number; className?: string; }
export interface BadgeDto { id: string; name: string; description: string; iconUrl?: string; }
export interface EmployeeBadgeDto { badgeId: string; name: string; description: string; iconUrl?: string; awardedAt: string; count: number; }
export interface XPSummaryDto { employeeId: string; totalXP: number; classRank: number; globalRank: number; currentStreak: number; recentBadges: EmployeeBadgeDto[]; }
export interface DashboardStatsDto { classRank: number; globalRank: number; totalXP: number; currentStreak: number; totalTasks: number; completedTasks: number; avgScore: number; recentBadges?: EmployeeBadgeDto[]; upcomingTasks?: UpcomingTaskDto[]; }
export interface UpcomingTaskDto { id: string; title: string; startAt: string; endAt: string; totalMarks: number; }

// ── Reports ──────────────────────────────────────────────
export interface ScoreBucket { label: string; count: number; }
export interface TaskReportItem { taskId: string; taskTitle: string; startAt: string; endAt: string; totalMarks: number; submittedCount: number; notSubmittedCount: number; avgRawScore: number; avgFinalScore: number; scoreBuckets: ScoreBucket[]; }
export interface DailyActivityItem { date: string; submissionCount: number; avgScore: number; }
export interface ClassReportDto { classId: string; className: string; trainerName: string; totalEmployees: number; totalTasks: number; avgScore: number; completionRate: number; taskReports: TaskReportItem[]; dailyActivity: DailyActivityItem[]; }
export interface EmployeeTaskResult { taskId?: string; taskTitle: string; submittedAt?: string; rank?: number; rawScore?: number; finalScore?: number; multiplier?: number; classAvg: number; classTop: number; status: string; }
export interface EmployeeReportDto { employeeId: string; employeeName: string; className: string; totalXP: number; classRank: number; globalRank: number; avgFinalScore: number; taskResults: EmployeeTaskResult[]; badges: EmployeeBadgeDto[]; dailyActivity: DailyActivityItem[]; }

// ── Notifications ────────────────────────────────────────
export interface NotificationDto { id: string; title: string; body: string; isRead: boolean; createdAt: string; type: string; }
export interface AnnouncementDto { id: string; classId: string; className: string; createdByName: string; title: string; body: string; createdAt: string; }

// ── Pagination ───────────────────────────────────────────
export interface PagedResult<T> { items: T[]; totalCount: number; page: number; pageSize: number; totalPages: number; }
export interface ApiError { message: string; errors?: Record<string, string[]>; }

// ── Results Grid ─────────────────────────────────────────
export interface GridQuestionHeader { questionId: string; orderIndex: number; stem: string; type: string; marks: number; accuracyPercent: number; }
export interface GridAnswerCell { questionId: string; isCorrect: boolean | null; rawScore: number | null; maxScore: number | null; }
export interface GridParticipantRow { employeeId: string; employeeName: string; totalPoints: number; totalMarks: number; accuracyPercent: number; score: number; answers: GridAnswerCell[]; }
export interface TaskResultsGrid { taskId: string; taskTitle: string; totalEnrolled: number; totalParticipants: number; overallAccuracy: number; participationRate: number; questionCount: number; questions: GridQuestionHeader[]; participants: GridParticipantRow[]; }

// ── Analytics ────────────────────────────────────────────
export interface ScoreDataPoint { taskId: string; taskTitle: string; submittedAt: string; finalScore: number; totalMarks: number; }
export interface EmployeeVelocityDto { employeeId: string; employeeName: string; recentScores: ScoreDataPoint[]; velocityPercent: number; trend: 'Rising' | 'Falling' | 'Stable'; predictedNextScore: number; }
export interface ClassVelocityReport { classId: string; className: string; employees: EmployeeVelocityDto[]; }

export interface QuestionBlindSpot { questionId: string; stem: string; type: string; marks: number; orderIndex: number; totalAnswered: number; correctAnswers: number; passRate: number; avgScorePercent: number; isBlindSpot: boolean; }
export interface TaskBlindSpotReport { taskId: string; taskTitle: string; totalSubmissions: number; questions: QuestionBlindSpot[]; }

export interface SimilarityEntry { submissionAId: string; employeeAName: string; submissionBId: string; employeeBName: string; similarity: number; isSuspected: boolean; }
export interface SimilarityCluster { clusterId: number; employeeNames: string[]; avgSimilarity: number; riskLevel: 'Low' | 'Medium' | 'High'; }
export interface CodeSimilarityReport { taskId: string; taskTitle: string; pairs: SimilarityEntry[]; clusters: SimilarityCluster[]; }
