/** A subject taught at the institution, identified by its unique code. */
export interface Subject {
  id: string;
  name: string;
  code: string;
}

export interface CreateSubjectRequest {
  name: string;
  code: string;
}
