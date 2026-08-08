import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import type { Class, CreateClassRequest } from '../models/class.model';

/** Classes (courses). Listing is available to any authenticated user; mutations require Admin. */
@Injectable({ providedIn: 'root' })
export class ClassService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/classes`;

  getAll(): Observable<Class[]> {
    return this.http.get<Class[]>(this.baseUrl);
  }

  create(request: CreateClassRequest): Observable<Class> {
    return this.http.post<Class>(this.baseUrl, request);
  }

  update(id: string, request: CreateClassRequest): Observable<Class> {
    return this.http.put<Class>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
