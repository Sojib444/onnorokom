import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import type { Submission } from '../models/submission.model';

/**
 * Submissions. Students submit, revise and download their own; teachers grade, return
 * and view submissions for their own assignments.
 */
@Injectable({ providedIn: 'root' })
export class SubmissionService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/submissions`;

  getMine(): Observable<Submission[]> {
    return this.http.get<Submission[]>(`${this.baseUrl}/mine`);
  }

  getById(id: string): Observable<Submission> {
    return this.http.get<Submission>(`${this.baseUrl}/${id}`);
  }

  /** Submits an answer, optionally with a file attachment. Multipart form body. */
  submit(assignmentId: string, answer: string, file?: File | null): Observable<Submission> {
    const form = new FormData();
    form.append('answer', answer);
    if (file) {
      form.append('file', file);
    }
    return this.http.post<Submission>(
      `${environment.apiUrl}/api/assignments/${assignmentId}/submissions`,
      form,
    );
  }

  /** Updates the caller's answer before the deadline. A new file replaces attachments. */
  update(id: string, answer: string, file?: File | null): Observable<Submission> {
    const form = new FormData();
    form.append('answer', answer);
    if (file) {
      form.append('file', file);
    }
    return this.http.put<Submission>(`${this.baseUrl}/${id}`, form);
  }

  /** Grades a submission. The assignment's teacher only. */
  grade(id: string, marks: number, feedback?: string | null): Observable<Submission> {
    return this.http.post<Submission>(`${this.baseUrl}/${id}/grade`, { marks, feedback });
  }

  /** Returns a submission to the student for revision. The assignment's teacher only. */
  returnForRevision(id: string): Observable<Submission> {
    return this.http.post<Submission>(`${this.baseUrl}/${id}/return`, null);
  }

  /** Downloads an attachment as a blob; the caller then saves it client-side. */
  downloadAttachment(submissionId: string, attachmentId: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/${submissionId}/attachments/${attachmentId}/download`, {
      responseType: 'blob',
    });
  }
}
