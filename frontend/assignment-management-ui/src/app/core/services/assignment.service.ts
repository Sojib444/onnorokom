import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import type { Assignment, CreateAssignmentRequest } from '../models/assignment.model';
import type { Submission } from '../models/submission.model';

/**
 * Assignments. The list is role-filtered by the server: all for admins, the caller's
 * own for teachers, and published assignments for the student's class.
 */
@Injectable({ providedIn: 'root' })
export class AssignmentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/assignments`;

  getAll(): Observable<Assignment[]> {
    return this.http.get<Assignment[]>(this.baseUrl);
  }

  getById(id: string): Observable<Assignment> {
    return this.http.get<Assignment>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateAssignmentRequest): Observable<Assignment> {
    return this.http.post<Assignment>(this.baseUrl, request);
  }

  update(id: string, request: CreateAssignmentRequest): Observable<Assignment> {
    return this.http.put<Assignment>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /** Publishes a draft, opening it for submissions. The owning teacher only. */
  publish(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/publish`, null);
  }

  /** Returns all submissions for an assignment. The assignment's teacher or an admin. */
  getSubmissions(id: string): Observable<Submission[]> {
    return this.http.get<Submission[]>(`${this.baseUrl}/${id}/submissions`);
  }
}
