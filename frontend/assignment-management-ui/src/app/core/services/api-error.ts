import { HttpErrorResponse } from '@angular/common/http';

/** The RFC 7807 ProblemDetails body returned by the API for every failure. */
export interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

/** Maps an HTTP error to a concise message suitable for display to the user. */
export function extractError(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const problem = error.error as ProblemDetails | undefined;
    if (problem?.detail) {
      return problem.detail;
    }
    if (problem?.title) {
      return problem.title;
    }
    if (error.status === 0) {
      return 'The server could not be reached.';
    }
    return `The request failed (${error.status}).`;
  }
  return 'An unexpected error occurred.';
}
