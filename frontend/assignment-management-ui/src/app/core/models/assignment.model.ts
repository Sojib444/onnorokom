/** Lifecycle status of an assignment. Only drafts can be edited, published can be seen. */
export type AssignmentStatus = 'Draft' | 'Published';

/** An assignment with its class and subject names resolved for display. */
export interface Assignment {
  id: string;
  teacherId: string;
  classId: string;
  className: string | null;
  subjectId: string;
  subjectName: string | null;
  title: string;
  description: string;
  deadline: string;
  maximumMarks: number;
  status: AssignmentStatus;
  createdAt: string;
  updatedAt: string;
}

export interface CreateAssignmentRequest {
  classId: string;
  subjectId: string;
  title: string;
  description: string;
  deadline: string;
  maximumMarks: number;
}
