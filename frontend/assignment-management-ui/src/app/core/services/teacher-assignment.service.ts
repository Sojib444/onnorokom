import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import type {
  CreateTeacherAssignmentRequest,
  TeacherAssignment,
} from '../models/teacher-assignment.model';

/**
 * Teacher allocations. The admin endpoints manage all allocations; the "mine" endpoint
 * returns the authenticated teacher's own pairs so the assignment form only offers
 * class/subject combinations the teacher is allowed to use.
 */
@Injectable({ providedIn: 'root' })
export class TeacherAssignmentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/teacher-assignments`;

  getAll(): Observable<TeacherAssignment[]> {
    return this.http.get<TeacherAssignment[]>(this.baseUrl);
  }

  getMine(): Observable<TeacherAssignment[]> {
    return this.http.get<TeacherAssignment[]>(`${this.baseUrl}/mine`);
  }

  create(request: CreateTeacherAssignmentRequest): Observable<TeacherAssignment> {
    return this.http.post<TeacherAssignment>(this.baseUrl, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
