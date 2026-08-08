/** The identity of the authenticated user, embedded in the login response. */
export interface AuthenticatedUser {
  id: string;
  fullName: string;
  email: string;
  role: 'Admin' | 'Teacher' | 'Student';
  classId: string | null;
}

/** Successful login result: a signed access token plus the user's identity. */
export interface LoginResponse {
  token: string;
  tokenType: string;
  expiresAt: string;
  user: AuthenticatedUser;
}

/** Credentials submitted to the login endpoint. */
export interface LoginRequest {
  email: string;
  password: string;
}
