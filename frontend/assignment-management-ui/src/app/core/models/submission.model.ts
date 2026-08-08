/** Lifecycle status of a submission. A returned submission may be edited past the deadline. */
export type SubmissionStatus = 'Submitted' | 'Returned' | 'Graded';

/** A file attached to a submission; metadata only, the bytes live in API storage. */
export interface SubmissionAttachment {
  id: string;
  fileName: string;
  contentType: string;
  size: number;
}

/** A submission with the assignment title and student name resolved for display. */
export interface Submission {
  id: string;
  assignmentId: string;
  assignmentTitle: string | null;
  studentId: string;
  studentName: string | null;
  answer: string;
  status: SubmissionStatus;
  marks: number | null;
  feedback: string | null;
  submittedAt: string | null;
  gradedAt: string | null;
  createdAt: string;
  updatedAt: string;
  attachments: SubmissionAttachment[];
}

export interface GradeSubmissionRequest {
  marks: number;
  feedback?: string | null;
}
