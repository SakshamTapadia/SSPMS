import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  UserDto, CreateUserRequest, UpdateUserRequest, PagedResult,
  ClassDto, CreateClassRequest,
  TaskDto, CreateTaskRequest, UpdateTaskRequest, QuestionDto, CreateQuestionRequest,
  SubmissionDto, SubmissionSummary, SaveDraftRequest, EvaluateSubmissionRequest,
  LeaderboardEntry, BadgeDto, EmployeeBadgeDto, XPSummaryDto, DashboardStatsDto,
  ClassReportDto, EmployeeReportDto,
  NotificationDto, AnnouncementDto,
  TaskBlindSpotReport, CodeSimilarityReport, EmployeeVelocityDto, ClassVelocityReport,
  TaskResultsGrid
} from '../models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly base = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // ── Users ───────────────────────────────────────────
  getUsers(page = 1, pageSize = 20, role?: string, search?: string, isActive?: boolean): Observable<PagedResult<UserDto>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (role) params = params.set('role', role);
    if (search) params = params.set('search', search);
    if (isActive !== undefined) params = params.set('isActive', isActive);
    return this.http.get<PagedResult<UserDto>>(`${this.base}/users`, { params });
  }
  getUser(id: string): Observable<UserDto> { return this.http.get<UserDto>(`${this.base}/users/${id}`); }
  createUser(req: CreateUserRequest): Observable<UserDto> { return this.http.post<UserDto>(`${this.base}/users`, req); }
  updateUser(id: string, req: UpdateUserRequest): Observable<UserDto> { return this.http.put<UserDto>(`${this.base}/users/${id}`, req); }
  deactivateUser(id: string): Observable<void> { return this.http.patch<void>(`${this.base}/users/${id}/deactivate`, {}); }
  reactivateUser(id: string): Observable<void> { return this.http.patch<void>(`${this.base}/users/${id}/reactivate`, {}); }
  changeUserRole(id: string, role: string): Observable<void> { return this.http.patch<void>(`${this.base}/users/${id}/role`, { role }); }
  bulkImportUsers(file: File, classId: string): Observable<{ success: number; errors: string[] }> {
    const fd = new FormData(); fd.append('file', file);
    return this.http.post<{ success: number; errors: string[] }>(`${this.base}/users/import?classId=${classId}`, fd);
  }
  getMyProfile(): Observable<UserDto> { return this.http.get<UserDto>(`${this.base}/users/me`); }
  updateMyProfile(req: { name: string; avatarUrl?: string }): Observable<UserDto> { return this.http.put<UserDto>(`${this.base}/users/me/profile`, req); }
  getTrainers(): Observable<UserDto[]> {
    const params = new HttpParams().set('role', 'Trainer').set('pageSize', '100');
    return this.http.get<PagedResult<UserDto>>(`${this.base}/users`, { params }).pipe(map(r => r.items));
  }
  createEmployeeByTrainer(req: CreateUserRequest): Observable<UserDto> { return this.http.post<UserDto>(`${this.base}/users/trainer/employees`, req); }

  // ── Classes ─────────────────────────────────────────
  getClasses(): Observable<ClassDto[]> { return this.http.get<ClassDto[]>(`${this.base}/classes`); }
  getMyClass(): Observable<ClassDto | null> { return this.http.get<ClassDto>(`${this.base}/classes/me`).pipe(catchError(() => of(null))); }
  getClass(id: string): Observable<ClassDto> { return this.http.get<ClassDto>(`${this.base}/classes/${id}`); }
  createClass(req: CreateClassRequest): Observable<ClassDto> { return this.http.post<ClassDto>(`${this.base}/classes`, req); }
  updateClass(id: string, req: CreateClassRequest): Observable<ClassDto> { return this.http.put<ClassDto>(`${this.base}/classes/${id}`, req); }
  deleteClass(id: string): Observable<void> { return this.http.delete<void>(`${this.base}/classes/${id}`); }
  getClassEmployees(id: string): Observable<UserDto[]> { return this.http.get<UserDto[]>(`${this.base}/classes/${id}/employees`); }
  enrollEmployee(classId: string, employeeId: string): Observable<void> { return this.http.post<void>(`${this.base}/classes/${classId}/enroll`, { employeeId }); }
  removeEmployee(classId: string, employeeId: string): Observable<void> { return this.http.delete<void>(`${this.base}/classes/${classId}/employees/${employeeId}`); }

  // ── Tasks ───────────────────────────────────────────
  getTasks(classId?: string): Observable<TaskDto[]> {
    let params = new HttpParams();
    if (classId) params = params.set('classId', classId);
    return this.http.get<TaskDto[]>(`${this.base}/tasks`, { params });
  }
  getMyTasks(): Observable<TaskDto[]> { return this.http.get<TaskDto[]>(`${this.base}/tasks/me`); }
  getTask(id: string): Observable<TaskDto> { return this.http.get<TaskDto>(`${this.base}/tasks/${id}`); }
  createTask(req: CreateTaskRequest): Observable<TaskDto> { return this.http.post<TaskDto>(`${this.base}/tasks`, req); }
  updateTask(id: string, req: UpdateTaskRequest): Observable<TaskDto> { return this.http.put<TaskDto>(`${this.base}/tasks/${id}`, req); }
  deleteTask(id: string): Observable<void> { return this.http.delete<void>(`${this.base}/tasks/${id}`); }
  publishTask(id: string): Observable<void> { return this.http.patch<void>(`${this.base}/tasks/${id}/publish`, {}); }
  duplicateTask(id: string): Observable<TaskDto> { return this.http.post<TaskDto>(`${this.base}/tasks/${id}/duplicate`, {}); }
  getQuestions(taskId: string, includeAnswers = false): Observable<QuestionDto[]> {
    return this.http.get<QuestionDto[]>(`${this.base}/tasks/${taskId}/questions`, { params: { includeAnswers } });
  }
  addQuestion(taskId: string, req: CreateQuestionRequest): Observable<QuestionDto> { return this.http.post<QuestionDto>(`${this.base}/tasks/${taskId}/questions`, req); }
  updateQuestion(taskId: string, qId: string, req: CreateQuestionRequest): Observable<QuestionDto> { return this.http.put<QuestionDto>(`${this.base}/tasks/${taskId}/questions/${qId}`, req); }
  deleteQuestion(taskId: string, qId: string): Observable<void> { return this.http.delete<void>(`${this.base}/tasks/${taskId}/questions/${qId}`); }
  reorderQuestions(taskId: string, questionIds: string[]): Observable<void> { return this.http.patch<void>(`${this.base}/tasks/${taskId}/questions/reorder`, { questionIds }); }
  importQuestionsFromDocument(taskId: string, file: File): Observable<QuestionDto[]> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<QuestionDto[]>(`${this.base}/tasks/${taskId}/questions/import`, form);
  }
  uploadImage(file: File): Observable<{ imageUrl: string }> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<{ imageUrl: string }>(`${this.base}/upload/image`, form);
  }

  // ── Submissions ─────────────────────────────────────
  startSubmission(taskId: string): Observable<SubmissionDto> { return this.http.post<SubmissionDto>(`${this.base}/submissions`, { taskId }); }
  getSubmission(id: string): Observable<SubmissionDto> { return this.http.get<SubmissionDto>(`${this.base}/submissions/${id}`); }
  saveDraft(id: string, req: SaveDraftRequest): Observable<SubmissionDto> { return this.http.put<SubmissionDto>(`${this.base}/submissions/${id}/draft`, req); }
  submitSubmission(id: string): Observable<SubmissionDto> { return this.http.post<SubmissionDto>(`${this.base}/submissions/${id}/submit`, {}); }
  getTaskSubmissions(taskId: string): Observable<SubmissionSummary[]> { return this.http.get<SubmissionSummary[]>(`${this.base}/submissions/task/${taskId}`); }
  getMySubmission(taskId: string): Observable<SubmissionDto> { return this.http.get<SubmissionDto>(`${this.base}/submissions/task/${taskId}/me`); }
  evaluateSubmission(id: string, req: EvaluateSubmissionRequest): Observable<SubmissionDto> { return this.http.put<SubmissionDto>(`${this.base}/submissions/${id}/evaluate`, req); }
  setPlagiarismFlag(answerId: string, flag: boolean): Observable<void> { return this.http.put<void>(`${this.base}/submissions/answers/${answerId}/plagiarism`, { flag }); }
  malpracticeSubmit(id: string, tabSwitchCount: number): Observable<SubmissionDto> { return this.http.post<SubmissionDto>(`${this.base}/submissions/${id}/malpractice-submit`, { tabSwitchCount }); }
  bulkCompleteEvaluation(taskId: string): Observable<void> { return this.http.post<void>(`${this.base}/submissions/task/${taskId}/bulk-complete`, {}); }

  // ── Leaderboard ─────────────────────────────────────
  getClassLeaderboard(classId: string, period = 'all'): Observable<LeaderboardEntry[]> { return this.http.get<LeaderboardEntry[]>(`${this.base}/leaderboards/class/${classId}`, { params: { period } }); }
  getGlobalLeaderboard(period = 'all'): Observable<LeaderboardEntry[]> { return this.http.get<LeaderboardEntry[]>(`${this.base}/leaderboards/global`, { params: { period } }); }
  getXPSummary(): Observable<XPSummaryDto> { return this.http.get<XPSummaryDto>(`${this.base}/badges/xp/me`); }
  getMyDashboard(): Observable<DashboardStatsDto> { return this.http.get<DashboardStatsDto>(`${this.base}/badges/dashboard/me`); }

  // ── Badges ──────────────────────────────────────────
  getAllBadges(): Observable<BadgeDto[]> { return this.http.get<BadgeDto[]>(`${this.base}/badges`); }
  getEmployeeBadges(employeeId: string): Observable<EmployeeBadgeDto[]> { return this.http.get<EmployeeBadgeDto[]>(`${this.base}/badges/${employeeId}`); }

  // ── Reports ─────────────────────────────────────────
  getClassReport(classId: string): Observable<ClassReportDto> { return this.http.get<ClassReportDto>(`${this.base}/reports/class/${classId}`); }
  getEmployeeReport(): Observable<EmployeeReportDto> { return this.http.get<EmployeeReportDto>(`${this.base}/reports/me`); }
  exportClassReportPdf(classId: string): Observable<Blob> { return this.http.get(`${this.base}/reports/class/${classId}/export`, { params: { format: 'pdf' }, responseType: 'blob' }); }
  exportClassReportExcel(classId: string): Observable<Blob> { return this.http.get(`${this.base}/reports/class/${classId}/export`, { params: { format: 'xlsx' }, responseType: 'blob' }); }
  exportEmployeeReportPdf(): Observable<Blob> { return this.http.get(`${this.base}/reports/me/export`, { responseType: 'blob' }); }
  exportTaskResultsGridExcel(taskId: string): Observable<Blob> { return this.http.get(`${this.base}/reports/task/${taskId}/export`, { responseType: 'blob' }); }

  // ── Notifications ────────────────────────────────────
  getNotifications(): Observable<NotificationDto[]> { return this.http.get<NotificationDto[]>(`${this.base}/notifications`); }
  markNotificationRead(id: string): Observable<void> { return this.http.patch<void>(`${this.base}/notifications/${id}/read`, {}); }
  markAllRead(): Observable<void> { return this.http.patch<void>(`${this.base}/notifications/read-all`, {}); }
  getAnnouncements(): Observable<AnnouncementDto[]> { return this.http.get<AnnouncementDto[]>(`${this.base}/notifications/announcements`); }
  createAnnouncement(req: { classId?: string; title: string; body: string }): Observable<AnnouncementDto> { return this.http.post<AnnouncementDto>(`${this.base}/notifications/announcements`, req); }

  // ── Admin ────────────────────────────────────────────
  getSystemStats(): Observable<any> { return this.http.get(`${this.base}/admin/stats`); }
  getAuditLog(page = 1, search?: string): Observable<PagedResult<any>> {
    let params = new HttpParams().set('page', page);
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<any>>(`${this.base}/admin/audit-log`, { params });
  }
  getSystemReport(): Observable<any> { return this.http.get(`${this.base}/reports/admin/system`); }

  // ── Analytics ────────────────────────────────────────
  getTaskBlindSpots(taskId: string): Observable<TaskBlindSpotReport> { return this.http.get<TaskBlindSpotReport>(`${this.base}/analytics/task/${taskId}/blind-spots`); }
  getCodeSimilarity(taskId: string): Observable<CodeSimilarityReport> { return this.http.get<CodeSimilarityReport>(`${this.base}/analytics/task/${taskId}/code-similarity`); }
  getTaskResultsGrid(taskId: string): Observable<TaskResultsGrid> { return this.http.get<TaskResultsGrid>(`${this.base}/analytics/task/${taskId}/results-grid`); }
  getEmployeeVelocity(employeeId: string): Observable<EmployeeVelocityDto> { return this.http.get<EmployeeVelocityDto>(`${this.base}/analytics/employee/${employeeId}/velocity`); }
  getClassVelocity(classId: string): Observable<ClassVelocityReport> { return this.http.get<ClassVelocityReport>(`${this.base}/analytics/class/${classId}/velocity`); }

  // ── File download helper ─────────────────────────────
  downloadBlob(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = filename; a.click();
    URL.revokeObjectURL(url);
  }
}
