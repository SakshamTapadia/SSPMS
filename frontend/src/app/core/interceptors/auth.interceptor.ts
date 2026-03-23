import { Injectable } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { AuthResponse } from '../models';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private refreshing = false;
  private refreshSubject = new BehaviorSubject<string | null>(null);

  constructor(private auth: AuthService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const token = this.auth.getAccessToken();
    const authReq = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

    return next.handle(authReq).pipe(
      catchError((err: HttpErrorResponse) => {
        if (err.status === 401 && !req.url.includes('/auth/')) {
          return this.handle401(req, next);
        }
        return throwError(() => err);
      })
    );
  }

  private handle401(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    if (this.refreshing) {
      return this.refreshSubject.pipe(
        filter(t => t !== null), take(1),
        switchMap(t => next.handle(req.clone({ setHeaders: { Authorization: `Bearer ${t}` } })))
      );
    }

    this.refreshing = true;
    this.refreshSubject.next(null);

    return this.auth.refreshToken().pipe(
      switchMap((res: AuthResponse) => {
        this.refreshing = false;
        this.refreshSubject.next(res.accessToken);
        return next.handle(req.clone({ setHeaders: { Authorization: `Bearer ${res.accessToken}` } }));
      }),
      catchError(err => {
        this.refreshing = false;
        this.auth.logout();
        return throwError(() => err);
      })
    );
  }
}
