/** A class (or course) that students belong to and assignments target. */
export interface Class {
  id: string;
  name: string;
  description: string | null;
}

export interface CreateClassRequest {
  name: string;
  description: string | null;
}
