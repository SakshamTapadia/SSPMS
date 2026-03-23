import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, map } from 'rxjs';
import { Router } from '@angular/router';
import { AuthResponse, LoginRequest, UserProfile, AuthCheckResponse } from '../models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly API = `${environment.apiUrl}/auth`;
  private _user$ = new BehaviorSubject<UserProfile | null>(this.loadUser());

  readonly user$ = this._user$.asObservable();
  readonly isAuthenticated$ = this.user$.pipe(map(u => !!u));

  constructor(private http: HttpClient, private router: Router) {}

  get currentUser(): UserProfile | null { return this._user$.value; }
  get role(): string { return this._user$.value?.role ?? ''; }
  get userId(): string { return this._user$.value?.id ?? ''; }

  register(req: { name: string; email: string; password: string }): Observable<AuthCheckResponse> {
    return this.http.post<AuthCheckResponse>(`${this.API}/register`, req);
  }

  login(req: LoginRequest): Observable<AuthCheckResponse> {
    return this.http.post<AuthCheckResponse>(`${this.API}/login`, req).pipe(
      tap(res => { if (!res.requiresVerification && res.accessToken) this.storeAuth(res as AuthResponse); })
    );
  }

  verifyEmail(email: string, otp: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API}/verify-email`, { email, otp }).pipe(
      tap(res => this.storeAuth(res))
    );
  }

  resendVerification(email: string): Observable<void> {
    return this.http.post<void>(`${this.API}/resend-verification`, { email });
  }

  googleLogin(idToken: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API}/google`, { idToken }).pipe(
      tap(res => this.storeAuth(res))
    );
  }

  logout(): void {
    const token = localStorage.getItem('refreshToken');
    if (token) this.http.post(`${this.API}/logout`, { refreshToken: token }).subscribe();
    this.clearAuth();
    this.router.navigate(['/login']);
  }

  refreshToken(): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API}/refresh`, {
      refreshToken: localStorage.getItem('refreshToken')
    }).pipe(tap(res => this.storeAuth(res)));
  }

  forgotPassword(email: string): Observable<void> {
    return this.http.post<void>(`${this.API}/forgot-password`, { email });
  }

  verifyOtp(email: string, otp: string): Observable<void> {
    return this.http.post<void>(`${this.API}/verify-otp`, { email, otp });
  }

  resetPassword(email: string, otp: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${this.API}/reset-password`, { email, otp, newPassword });
  }

  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.put<void>(`${this.API}/password`, { currentPassword, newPassword });
  }

  getAccessToken(): string | null { return localStorage.getItem('accessToken'); }

  storeAuth(res: AuthResponse): void {
    localStorage.setItem('accessToken', res.accessToken);
    localStorage.setItem('refreshToken', res.refreshToken);
    localStorage.setItem('user', JSON.stringify(res.user));
    this._user$.next(res.user);
  }

  private clearAuth(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    this._user$.next(null);
  }

  private loadUser(): UserProfile | null {
    try { return JSON.parse(localStorage.getItem('user') ?? 'null'); } catch { return null; }
  }
}
