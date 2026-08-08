/** A user account as exposed by the API. Never contains a password or hash. */
export interface User {
  id: string;
  fullName: string;
  email: string;
  role: string;
  classId: string | null;
  className: string | null;
  createdAt: string;
}

/** Body for creating a user. The role is fixed at creation time. */
export interface CreateUserRequest {
  fullName: string;
  email: string;
  password: string;
  role: string;
  classId: string | null;
}

/** Body for updating a user's profile. Only name and (for students) class can change. */
export interface UpdateUserRequest {
  fullName: string;
  classId: string | null;
}
